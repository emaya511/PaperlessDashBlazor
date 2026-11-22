using PaperlessDashBlazor.Data;
using PaperlessDashBlazor.Services;

namespace PaperlessDashBlazor.Models;

/// <summary>
/// Top-level application model — one per Blazor circuit (Scoped).
/// Holds the list of pages and orchestrates page CRUD.
/// Injected into Razor components; they read state and call methods here.
/// </summary>
public class PaperlessDash
{
    private readonly DashboardDbService _db;
    private readonly PaperlessService _paperless;
    private bool _loaded;

    public List<DashPage> Pages { get; private set; } = [];

    /// <summary>Fires when the page list changes (add/delete).</summary>
    public event Action? OnChange;

    public PaperlessDash(DashboardDbService db, PaperlessService paperless)
    {
        _db = db;
        _paperless = paperless;
    }

    // ── Load pages from DB (once) ─────────────────────────────────────────

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        var entities = await _db.GetPagesAsync();
        Pages = entities.Select(e => new DashPage(e, _db, _paperless)).ToList();
        _loaded = true;
        OnChange?.Invoke();
    }

    // ── Lookup ────────────────────────────────────────────────────────────

    public DashPage? GetPageBySlug(string slug)
        => Pages.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    // ── Page CRUD ─────────────────────────────────────────────────────────

    public async Task<DashPage> AddPageAsync(string title)
    {
        var entity = await _db.AddPageAsync(title);
        var page = new DashPage(entity, _db, _paperless);
        Pages.Add(page);
        OnChange?.Invoke();
        return page;
    }

    public async Task DeletePageAsync(Guid pageId)
    {
        await _db.DeletePageAsync(pageId);
        Pages.RemoveAll(p => p.Id == pageId);
        OnChange?.Invoke();
    }
}
