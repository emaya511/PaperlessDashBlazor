using PaperlessDashBlazor.Data;
using PaperlessDashBlazor.Services;

namespace PaperlessDashBlazor.Models;

/// <summary>
/// Business-logic model for a single page.
/// Holds its DB entry (read-only) and a list of DocCardModel children.
/// Manages card CRUD: add/delete cards, load cards from DB.
/// </summary>
public class DashPage
{
    private readonly DashboardDbService _db;
    private readonly PaperlessService _paperless;
    private bool _cardsLoaded;

    // ── Read-only identity from DB ────────────────────────────────────────
    public Guid Id { get; }
    public string Title { get; }
    public string Slug { get; }

    // ── Child aggregate ───────────────────────────────────────────────────
    public List<DocCardModel> Cards { get; private set; } = [];

    public event Action? OnChange;

    public DashPage(PageEntity entity, DashboardDbService db, PaperlessService paperless)
    {
        _db = db;
        _paperless = paperless;
        Id = entity.Id;
        Title = entity.Title;
        Slug = entity.Slug;
    }

    // ── Load cards from DB and hydrate queries ────────────────────────────

    public async Task EnsureCardsLoadedAsync()
    {
        if (_cardsLoaded) return;

        var entity = await _db.GetPageWithCardsAsync(Slug);
        if (entity is null) return;

        Cards.Clear();
        foreach (var card in entity.Cards.Where(c => c.DocId.HasValue))
        {
            var query = new PaperlessQuery(_paperless)
            {
                Type = QueryType.Tag,
                TagSelected = new PaperlessTag
                {
                    Id = card.DocId!.Value,
                    Name = card.DocName ?? string.Empty
                }
            };
            Cards.Add(new DocCardModel(card.Id, query, card.DocName));
        }

        // Initialize all cards in parallel (fetch docs + thumbnails)
        await Task.WhenAll(Cards.Select(c => c.InitializeAsync()));
        _cardsLoaded = true;
        OnChange?.Invoke();
    }

    // ── Card CRUD ─────────────────────────────────────────────────────────

    public async Task<DocCardModel> AddCardAsync(int docId, string docName)
    {
        var cardEntity = await _db.AddDocCardAsync(Id, docId, docName);

        var query = new PaperlessQuery(_paperless)
        {
            Type = QueryType.Tag,
            TagSelected = new PaperlessTag { Id = docId, Name = docName }
        };
        var card = new DocCardModel(cardEntity.Id, query, docName);
        await card.InitializeAsync();
        Cards.Add(card);
        OnChange?.Invoke();
        return card;
    }

    public async Task DeleteCardAsync(Guid cardId)
    {
        await _db.DeleteCardAsync(cardId);
        Cards.RemoveAll(c => c.Id == cardId);
        OnChange?.Invoke();
    }
}
