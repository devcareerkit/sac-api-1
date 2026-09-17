using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace DsacReporting.Api.Data.Database;

public static class DatabaseInitializer
{
    public static async Task ApplyInitSqlAsync(IServiceProvider services, string sqlPath)
    {
        if (!File.Exists(sqlPath))
            throw new FileNotFoundException("SQL init script not found", sqlPath);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        var sql = await File.ReadAllTextAsync(sqlPath);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
        await conn.CloseAsync();
    }
}
