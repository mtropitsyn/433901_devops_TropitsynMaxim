using CalculatorWeb.Data;
using CalculatorWeb.Services;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<CalculatorService>();
var databasePassword = builder.Configuration["Database:Password"];
if (string.IsNullOrWhiteSpace(databasePassword))
    throw new InvalidOperationException("Set Database__Password before starting the application. See README.md.");
var databaseConnection = new MySqlConnectionStringBuilder
{
    Server = builder.Configuration["Database:Server"] ?? "localhost",
    Port = builder.Configuration.GetValue<uint>("Database:Port", 3307),
    Database = builder.Configuration["Database:Name"] ?? "calculator",
    UserID = builder.Configuration["Database:User"] ?? "calculator",
    Password = databasePassword,
    ConnectionTimeout = 5,
    DefaultCommandTimeout = 10,
};
builder.Services.AddSingleton(_ => new MySqlDataSourceBuilder(databaseConnection.ConnectionString).Build());
builder.Services.AddSingleton<ICalculationRepository, MariaDbCalculationRepository>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Calculator/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapGet("/health", () => Results.Text("OK"));
app.MapGet("/health/ready", async (MySqlDataSource database, CancellationToken cancellationToken) =>
{
    try
    {
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM calculations LIMIT 1;";
        await command.ExecuteScalarAsync(cancellationToken);
        return Results.Text("OK");
    }
    catch (Exception exception) when (exception is MySqlException or TimeoutException)
    {
        return Results.Text("Database unavailable", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapControllerRoute("default", "{controller=Calculator}/{action=Index}/{id?}");
app.Run();
