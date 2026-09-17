using System.Text;
using DsacReporting.Api.Data;
using DsacReporting.Api.Auth;
using DsacReporting.Api.Middleware;
using DsacReporting.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// DbContext - support DATABASE_URL (Railway) or ConnectionStrings:Default
var rawConn = Environment.GetEnvironmentVariable("DATABASE_URL") ?? builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrEmpty(rawConn))
{
    throw new InvalidOperationException("No database connection configured. Set DATABASE_URL or ConnectionStrings:Default.");
}

string ConvertDatabaseUrlToConnectionString(string databaseUrl)
{
    if (string.IsNullOrEmpty(databaseUrl)) return databaseUrl;
    // If already a key=value connection string, return as-is
    if (!databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) && !databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        return databaseUrl;

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty);
    var password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty);
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    var builder = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = port,
        Username = username,
        Password = password,
        Database = database,
        SslMode = Npgsql.SslMode.Require,
    };

    return builder.ToString();
}

var conn = ConvertDatabaseUrlToConnectionString(rawConn);
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(conn));

// Register application services
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
// Add other services as needed

// Authentication (JWT)
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (!string.IsNullOrEmpty(jwt.Secret))
{
    var key = Encoding.UTF8.GetBytes(jwt.Secret);
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

    // Authorization policies mapped to roles
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(RolePolicies.EntityOfficerPolicy, policy => policy.RequireRole("entity_officer"));
        options.AddPolicy(RolePolicies.DsacMePolicy, policy => policy.RequireRole("dsac_me"));
        options.AddPolicy(RolePolicies.DsacExecPolicy, policy => policy.RequireRole("dsac_exec"));
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Middleware ordering: authentication -> entity-scope enforcement -> authorization -> audit logging
app.UseAuthentication();
app.UseMiddleware<EntityScopeMiddleware>();
app.UseAuthorization();
app.UseMiddleware<AuditLoggingMiddleware>();

app.MapControllers();

// Optionally apply init SQL to the database. Set environment variable APPLY_INIT_SQL=true to run.
if (Environment.GetEnvironmentVariable("APPLY_INIT_SQL") == "true")
{
    var sqlPath = Path.Combine(AppContext.BaseDirectory, "Data", "Database", "init.sql");
    if (!File.Exists(sqlPath))
    {
        sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database", "init.sql");
    }

    if (File.Exists(sqlPath))
    {
        Console.WriteLine($"Applying init SQL from {sqlPath}");
        await DsacReporting.Api.Data.Database.DatabaseInitializer.ApplyInitSqlAsync(app.Services, sqlPath);
        Console.WriteLine("Init SQL applied.");
    }
    else
    {
        Console.WriteLine("init.sql not found; skipping apply.");
    }
}

app.Run();
