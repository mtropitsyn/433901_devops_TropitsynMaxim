using CalculatorWeb.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

if (args is ["--health-check"])
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    try
    {
        using var response = await client.GetAsync("http://127.0.0.1:5016/health");
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
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("tropitsyn16-calculator"))
    .WithMetrics(metrics => metrics
        .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel")
        .AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 10],
        })
        .AddPrometheusExporter());

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Calculator/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapGet("/health", () => Results.Text("OK"));
app.MapPrometheusScrapingEndpoint();
if (app.Configuration.GetValue<bool>("Monitoring:EnableDemoEndpoints"))
{
    app.MapGet("/monitoring/slow", async (CancellationToken cancellationToken) =>
    {
        await Task.Delay(800, cancellationToken);
        return Results.Text("Slow request completed");
    });
    app.MapGet("/monitoring/error", () => Results.StatusCode(500));
}
app.MapControllerRoute("default", "{controller=Calculator}/{action=Index}/{id?}");
app.Run();
