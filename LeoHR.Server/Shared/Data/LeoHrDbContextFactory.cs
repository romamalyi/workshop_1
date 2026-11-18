using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using Npgsql;

namespace LeoHR.Server.Shared.Data;

public class LeoHrDbContextFactory : IDesignTimeDbContextFactory<LeoHrDbContext>
{
    public LeoHrDbContext CreateDbContext(string[] args)
    {
        string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Test";

        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddJsonFile($"secrets.{env}.json", optional: true)
            .Build();

        string connectionString = config.GetConnectionString("LeoHr")
            ?? throw new InvalidOperationException($"ConnectionStrings:LeoHr not found in secrets.{env}.json");

        NpgsqlDataSource dataSource = new NpgsqlDataSourceBuilder(connectionString)
            .EnableDynamicJson()
            .Build();

        DbContextOptions<LeoHrDbContext> options = new DbContextOptionsBuilder<LeoHrDbContext>()
            .UseNpgsql(dataSource)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new LeoHrDbContext(options, null!);
    }
}
