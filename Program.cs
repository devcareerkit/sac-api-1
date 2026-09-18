using System.Text;
using DsacReporting.Api.Data;
using DsacReporting.Api.Auth;
using DsacReporting.Api.Middleware;
using DsacReporting.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Bind to Railway's dynamically assigned port
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned from POST /api/auth/login (no \"Bearer \" prefix needed).",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SharePointSettings>(builder.Configuration.GetSection("SharePoint"));

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
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IDocumentStorageService, SharePointDocumentStorageService>();
builder.Services.AddScoped<IEntityPortfolioService, EntityPortfolioService>();
builder.Services.AddScoped<IAlertsService, AlertsService>();
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
}

// Swagger is enabled in dev by default, or in any environment via ENABLE_SWAGGER=true
// (useful for inspecting a deployed instance while auth is still a stub).
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("ENABLE_SWAGGER") == "true")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Railway terminates TLS at its edge proxy and forwards plain HTTP to the container,
// so redirecting to HTTPS here would break requests behind that proxy.
if (string.IsNullOrEmpty(port))
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

// Middleware ordering: authentication -> entity-scope enforcement -> authorization -> audit logging
app.UseAuthentication();
app.UseMiddleware<EntityScopeMiddleware>();
app.UseAuthorization();
app.UseMiddleware<AuditLoggingMiddleware>();

app.MapControllers();

// Optionally apply init SQL to the database. Set environment variable APPLY_INIT_SQL=true to run.
if (Environment.GetEnvironmentVariable("APPLY_INIT_SQL") == "true")
{
    var sqlFiles = new[] { "init.sql", "002_add_password_hash.sql", "003_seed_demo_users.sql", "004_seed_demo_cycle_and_kpis.sql", "005_seed_budget_kpi.sql", "006_app_kpi_workflow.sql" };
    foreach (var fileName in sqlFiles)
    {
        var sqlPath = Path.Combine(AppContext.BaseDirectory, "Data", "Database", fileName);
        if (!File.Exists(sqlPath))
        {
            sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database", fileName);
        }

        if (File.Exists(sqlPath))
        {
            Console.WriteLine($"Applying SQL from {sqlPath}");
            await DsacReporting.Api.Data.Database.DatabaseInitializer.ApplyInitSqlAsync(app.Services, sqlPath);
            Console.WriteLine($"{fileName} applied.");
        }
        else
        {
            Console.WriteLine($"{fileName} not found; skipping.");
        }
    }
}

app.Run();
