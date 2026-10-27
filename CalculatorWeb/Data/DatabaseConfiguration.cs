using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace CalculatorWeb.Data;

public static class DatabaseConfiguration
{
    public static readonly MariaDbServerVersion ServerVersion = new(new Version(11, 4, 0));

    public static string ConnectionString(IConfiguration configuration, bool requirePassword = true)
    {
        var password = configuration["Database:Password"];
        if (requirePassword && string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Set Database__Password before starting the application. See README.md.");

        return new MySqlConnectionStringBuilder
        {
            Server = configuration["Database:Server"] ?? "localhost",
            Port = configuration.GetValue<uint>("Database:Port", 53316),
            Database = configuration["Database:Name"] ?? "16-WebCalculatorDb",
            UserID = configuration["Database:User"] ?? "calculator",
            Password = password ?? "",
            ConnectionTimeout = 5,
            DefaultCommandTimeout = 10,
        }.ConnectionString;
    }
}
