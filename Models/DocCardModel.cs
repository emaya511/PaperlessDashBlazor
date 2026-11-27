using PaperlessDashBlazor.Data;
using PaperlessDashBlazor.Services;

namespace PaperlessDashBlazor.Models;

/// <summary>
/// Business-logic model for a single Doc card.
/// Wraps a PaperlessQuery and bubbles change notifications.
/// UI-agnostic — no Blazor references.
/// </summary>
public class DocCardModel
{
    public Guid Id { get; }
    public string Title { get; set; } = string.Empty;

    /// <summary>The query that supplies documents to this card.</summary>
    public PaperlessQuery? Query { get; }
    //List<QueryEntity> Queries {get; set;}
    public List<PaperlessQuery> Queries { get; private set; } = [];

    /// <summary>The currently selected document.</summary>
    //public PaperlessDocument? SelectedDoc => Query.DocSelected;

    /// <summary>Thumbnail for the selected document.</summary>
    //public string? SelectedDocThumbnail => Query.SelectedDocThumbnail;

    public event Action? OnChange;

    public CardEntity Card;
    private readonly PaperlessService _paperless;

    /*
    //*************
    */
    //private readonly PaperlessService _paperless;

    public QueryType Type { get; set; } = QueryType.Tag;

    /// <summary>Selected tag when Type == QueryType.Tag</summary>
    public PaperlessTag? TagSelected { get; set; }

    /// <summary>Search text when Type == QueryType.FullText</summary>
    public string? SearchText { get; set; }

    public int PageSize { get; set; } = 10;

    // ── Execution State ───────────────────────────────────────────────────
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? LastRefreshed { get; set; }

    // ── Results ───────────────────────────────────────────────────────────
    public List<PaperlessDocument> Documents { get; set; } = [];
    public int TotalCount { get; set; }

    // ── Selected Document & Preview Details ──────────────────────────────
    public PaperlessDocument? DocSelected { get; set; }
    public string? SelectedDocThumbnail { get; set; }

    /// <summary>Event raised whenever state changes (results loaded, selection changed)</summary>
    //public event Action? OnChange;

    public void NotifyChanged() => OnChange?.Invoke();


     /*
    //*************
    */





/*
    public DocCardModel(Guid id, PaperlessQuery query, string? title = null)
    {
        Id = id;
        Query = query;
        Title = title ?? query.TagSelected?.Name ?? "Untagged";
        Query.OnChange += () => OnChange?.Invoke();
    }*/

    public DocCardModel(CardEntity card, PaperlessService paperless)
    {
        Card = card;
        List<QueryEntity> queryEntities  = card.Queries;
        _paperless = paperless;

        foreach(var query in queryEntities)
        {
           PaperlessQuery PLQry = new PaperlessQuery(query,_paperless); 
        }
        //Id = card.ID;
        //Query = query;
        //Title = title ?? query.TagSelected?.Name ?? "Untagged";
        //Query.OnChange += () => OnChange?.Invoke();
    }

    

    public async Task InitializeAsync()
    {
        //await Query.RunQueryAsync();

        IsLoading = true;
        ErrorMessage = null;
        NotifyChanged();

        try
        {
            if (Type == QueryType.Tag && TagSelected is not null)
            {
                var (docs, total) = await _paperless.GetDocumentsByTagAsync(TagSelected.Id, PageSize);
                Documents = docs;
                TotalCount = total;
            }
            else
            {
                Documents = [];
                TotalCount = 0;
            }

            // Auto-select first doc if none selected or previous selection gone
            if (Documents.Count > 0 && (DocSelected is null || !Documents.Any(d => d.Id == DocSelected.Id)))
            {
                await SelectDocumentAsync(Documents[0]);
            }
            else if (Documents.Count == 0)
            {
                DocSelected = null;
                SelectedDocThumbnail = null;
            }

            LastRefreshed = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyChanged();
        }

    }

    public async Task SelectDocumentAsync(PaperlessDocument doc)
    {
        //await Query.SelectDocumentAsync(doc);


        DocSelected = doc;
        SelectedDocThumbnail = null;
        NotifyChanged();

        if (doc.Id > 0)
        {
            SelectedDocThumbnail = await _paperless.GetDocumentThumbnailAsync(doc.Id);
            NotifyChanged();
        }
    }
}
