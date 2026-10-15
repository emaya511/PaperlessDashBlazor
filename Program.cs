using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using PaperlessDashBlazor.Components;
using PaperlessDashBlazor.Data;
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

// Add services to the container.
// ── Razor / Blazor ────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SQLite database — file stored next to the app
// ── SQLite ────────────────────────────────────────────────────────────────
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "paperless-dash.db");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// PageService is scoped — one per Blazor circuit, uses IDbContextFactory
builder.Services.AddScoped<PageService>();

// ── Paperless-NGX HTTP client ─────────────────────────────────────────────
var paperlessUrl = builder.Configuration["PAPERLESS_URL"]
                   ?? Environment.GetEnvironmentVariable("PAPERLESS_URL")
                   ?? "http://localhost:8000";
var paperlessKey = builder.Configuration["PAPERLESS_API_KEY"]
                   ?? Environment.GetEnvironmentVariable("PAPERLESS_API_KEY")
                   ?? string.Empty;

builder.Services.AddHttpClient("paperless", client =>
{
    client.BaseAddress = new Uri(paperlessUrl.TrimEnd('/') + "/");
    if (!string.IsNullOrWhiteSpace(paperlessKey))
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Token", paperlessKey);
});

builder.Services.AddScoped<PaperlessService>();

// ── App pipeline ──────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-create DB tables on startup (no migration CLI needed)
// Auto-create DB tables on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
