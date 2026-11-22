using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using PaperlessDashBlazor.Components;
using PaperlessDashBlazor.Data;
using PaperlessDashBlazor.Models;
using PaperlessDashBlazor.Services;

// ── Load .env before builder so vars appear in IConfiguration ────────────
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile)
                             .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#')))
    {
        var idx = line.IndexOf('=');
        if (idx <= 0) continue;
        var key = line[..idx].Trim();
        var val = line[(idx + 1)..].Trim().Trim('"').Trim('\'');
        Environment.SetEnvironmentVariable(key, val);
    }
}

var builder = WebApplication.CreateBuilder(args);

// ── Razor / Blazor ────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── SQLite ────────────────────────────────────────────────────────────────
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "paperless-dash.db");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<DashboardDbService>();

var paperlessUrl = builder.Configuration["PAPERLESS_URL"]
                   ?? Environment.GetEnvironmentVariable("PAPERLESS_URL")
                   ?? "http://localhost:8000";
var paperlessToken = builder.Configuration["PAPERLESS_API_TOKEN"]
                     ?? Environment.GetEnvironmentVariable("PAPERLESS_API_TOKEN")
                     ?? builder.Configuration["PAPERLESS_API_KEY"]
                     ?? Environment.GetEnvironmentVariable("PAPERLESS_API_KEY")
                     ?? string.Empty;

builder.Services.AddHttpClient("paperless", client =>
{
    client.BaseAddress = new Uri(paperlessUrl.TrimEnd('/') + "/");
    if (!string.IsNullOrWhiteSpace(paperlessToken))
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Token", paperlessToken);
});

builder.Services.AddScoped<PaperlessService>();

// ── Models (one per circuit) ──────────────────────────────────────────────
builder.Services.AddScoped<PaperlessDash>();

// ── App pipeline ──────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-create DB tables on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
