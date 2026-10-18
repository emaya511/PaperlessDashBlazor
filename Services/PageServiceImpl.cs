using Microsoft.EntityFrameworkCore;
using PaperlessDashBlazor.Data;

namespace PaperlessDashBlazor.Services;

/// <summary>
/// Scoped service — one per Blazor circuit.
/// Uses IDbContextFactory so each async operation gets its own short-lived DbContext,
/// preventing concurrency issues on the SignalR circuit.
/// </summary>
public class PageService(IDbContextFactory<AppDbContext> dbFactory)
{
    // UI components subscribe to this to re-render without polling
    public event Action? OnChange;

    // ── Pages ────────────────────────────────────────────────────────────────

    public async Task<List<DynamicPageEntry>> GetPagesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Pages.AsNoTracking().OrderBy(p => p.Title).ToListAsync();
    }

    public async Task<DynamicPageEntry?> GetBySlugAsync(string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Pages
                       .Include(p => p.Cards)
                       .FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<DynamicPageEntry> AddPageAsync(string title)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var slug = GenerateSlug(title);
        var existing = await db.Pages.CountAsync(p => p.Slug.StartsWith(slug));
        if (existing > 0) slug = $"{slug}-{existing}";

        var page = new DynamicPageEntry { Title = title, Slug = slug };
        db.Pages.Add(page);
        await db.SaveChangesAsync();

        OnChange?.Invoke();
        return page;
    }

    /// <summary>Delete a page and all its cards.</summary>
    public async Task DeletePageAsync(Guid pageId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var page = await db.Pages.FindAsync(pageId);
        if (page is null) return;
        db.Pages.Remove(page);   // cascade deletes cards via EF config
        await db.SaveChangesAsync();
        OnChange?.Invoke();
    }

    // ── Cards ────────────────────────────────────────────────────────────────

    /// <summary>Add a Doc card linked to a Paperless-NGX tag.</summary>
    public async Task AddDocCardAsync(Guid pageId, int docId, string docName)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Cards.Add(new CardEntry
        {
            PageId    = pageId,
            Size      = CardSize.Doc,
            DocId     = docId,
            DocName   = docName
        });
        await db.SaveChangesAsync();
        OnChange?.Invoke();
    }

    /// <summary>Add a Table card (no tag filter).</summary>
    public async Task AddTableCardAsync(Guid pageId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Cards.Add(new CardEntry { PageId = pageId, Size = CardSize.Table });
        await db.SaveChangesAsync();
        OnChange?.Invoke();
    }

    /// <summary>Delete a card by ID.</summary>
    public async Task DeleteCardAsync(Guid cardId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var card = await db.Cards.FindAsync(cardId);
        if (card is null) return;
        db.Cards.Remove(card);
        await db.SaveChangesAsync();
        OnChange?.Invoke();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GenerateSlug(string title)
    {
        var slug = new string(
            title.ToLowerInvariant()
                 .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                 .ToArray()
        ).Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? "page" : slug;
    }
}
