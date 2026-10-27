using CalculatorWeb.Data;
using CalculatorWeb.Services;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

// A probe for Docker HEALTHCHECK; this mode does not start another web server.
if (args.Contains("--health-check"))
{
    try
    {
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "5316";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        using var response = await client.GetAsync($"http://127.0.0.1:{port}/health/ready");
        Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
    {
        Environment.ExitCode = 1;
    }
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<CalculatorService>();
builder.Services.AddDbContext<CalculatorDbContext>(options =>
    options.UseMySql(DatabaseConfiguration.ConnectionString(builder.Configuration, requirePassword: !EF.IsDesignTime),
        DatabaseConfiguration.ServerVersion));
builder.Services.AddScoped<ICalculationRepository, MariaDbCalculationRepository>();

var app = builder.Build();
// This lab runs one web replica. Apply the committed EF migrations before accepting requests.
if (!EF.IsDesignTime)
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<CalculatorDbContext>();
    await database.Database.MigrateAsync();
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Calculator/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapGet("/health", () => Results.Text("OK"));
app.MapGet("/health/ready", async (CalculatorDbContext database, CancellationToken cancellationToken) =>
{
    try
    {
        await database.DataInputVariants.AsNoTracking().AnyAsync(cancellationToken);
        return Results.Text("OK");
    }
    catch (Exception exception) when (exception is MySqlException or TimeoutException)
    {
        return Results.Text("Database unavailable", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapControllerRoute("default", "{controller=Calculator}/{action=Index}/{id?}");
app.Run();
