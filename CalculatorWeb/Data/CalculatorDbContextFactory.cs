using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CalculatorWeb.Data;

// Allows migrations add/script to work without starting the web app or connecting to MariaDB.
public sealed class CalculatorDbContextFactory : IDesignTimeDbContextFactory<CalculatorDbContext>
{
    public CalculatorDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var options = new DbContextOptionsBuilder<CalculatorDbContext>()
            .UseMySql(DatabaseConfiguration.ConnectionString(configuration, requirePassword: false),
                DatabaseConfiguration.ServerVersion)
            .Options;
        return new CalculatorDbContext(options);
    }
}
