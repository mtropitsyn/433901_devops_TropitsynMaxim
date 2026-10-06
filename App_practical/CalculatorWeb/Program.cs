using CalculatorWeb.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<CalculatorService>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Calculator/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapGet("/health", () => Results.Text("OK"));
app.MapControllerRoute("default", "{controller=Calculator}/{action=Index}/{id?}");
app.Run();
