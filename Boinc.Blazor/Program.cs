using Boinc.Blazor.Data;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddTransient<HostService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<BoincHostConnector>();
builder.Services.AddHostedService(x => 
    new BaseHostedService<BoincHostConnector>(x, x.GetRequiredService<ILogger<BaseHostedService<BoincHostConnector>>>(), TimeSpan.FromSeconds(20)));
builder.Services.AddDbContextFactory<AppDb>(opt => opt.UseSqlite($"Data Source={nameof(AppDb)}.db"));
builder.Services.AddMudServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
