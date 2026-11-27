using PaperlessDashBlazor.Data;
using PaperlessDashBlazor.Services;

namespace PaperlessDashBlazor.Models;
public enum QueryType
{
    Tag,
    FullText
}

public class PaperlessQuery
{
    private readonly PaperlessService _paperless;

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
    public event Action? OnChange;
    public void NotifyChanged() => OnChange?.Invoke();
    public QueryEntity Query {get; set;}
    public PaperlessQuery(PaperlessService paperless)
    {
        _paperless = paperless;
        Query = new QueryEntity();
    }
    public PaperlessQuery(QueryEntity qryEnty, PaperlessService paperless)
    {
        _paperless = paperless;
        Query = qryEnty;
    }

}
