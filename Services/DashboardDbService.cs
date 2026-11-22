using Microsoft.EntityFrameworkCore;
using PaperlessDashBlazor.Data;

namespace PaperlessDashBlazor.Services;

/// <summary>
/// Thin DB-only service. No business logic, no events.
/// One per Blazor circuit (Scoped). Each call gets a short-lived DbContext.
/// </summary>
public class DashboardDbService(IDbContextFactory<AppDbContext> dbFactory)
{
    // ── Pages ────────────────────────────────────────────────────────────────

    public async Task<List<PageEntity>> GetPagesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Pages.AsNoTracking().OrderBy(p => p.Title).ToListAsync();
    }

    public async Task<PageEntity?> GetPageWithCardsAsync(string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Pages
                       .Include(p => p.Cards)
                       .FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<PageEntity> AddPageAsync(string title)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var slug = GenerateSlug(title);
        var existing = await db.Pages.CountAsync(p => p.Slug.StartsWith(slug));
        if (existing > 0) slug = $"{slug}-{existing}";

        var page = new PageEntity { Title = title, Slug = slug };
        db.Pages.Add(page);
        await db.SaveChangesAsync();
        return page;
    }

    public async Task DeletePageAsync(Guid pageId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var page = await db.Pages.FindAsync(pageId);
        if (page is null) return;
        db.Pages.Remove(page);   // cascade deletes cards
        await db.SaveChangesAsync();
    }

    // ── Cards ────────────────────────────────────────────────────────────────

    public async Task<CardEntity> AddDocCardAsync(Guid pageId, int docId, string docName)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var card = new CardEntity
        {
            PageId  = pageId,
            DocId   = docId,
            DocName = docName
        };
        db.Cards.Add(card);
        await db.SaveChangesAsync();
        return card;
    }

    public async Task DeleteCardAsync(Guid cardId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var card = await db.Cards.FindAsync(cardId);
        if (card is null) return;
        db.Cards.Remove(card);
        await db.SaveChangesAsync();
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
