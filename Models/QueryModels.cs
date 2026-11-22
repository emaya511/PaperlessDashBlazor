using PaperlessDashBlazor.Services;

namespace PaperlessDashBlazor.Models;

public enum QueryType
{
    Tag,
    FullText
}

/// <summary>
/// Encapsulates query configuration, results, selected document,
/// and execution logic against PaperlessService.
/// UI-agnostic — no Blazor references.
/// </summary>
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

    public PaperlessQuery(PaperlessService paperless)
    {
        _paperless = paperless;
    }

    // ── Query Execution ───────────────────────────────────────────────────

    public async Task RunQueryAsync()
    {
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

    /// <summary>Selects a document and loads its thumbnail.</summary>
    public async Task SelectDocumentAsync(PaperlessDocument doc)
    {
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
