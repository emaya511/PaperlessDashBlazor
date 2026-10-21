using System.Text.Json.Serialization;

namespace PaperlessDashBlazor.Services;

public enum QueryType
{
    Tag,
    FullText
}

/// <summary>
/// Encapsulates all query configuration, matched results, selected document,
/// and execution logic against PaperlessService.
/// Keeps Razor components state-free, scalable, and independent.
/// </summary>
public class PaperlessQuery
{
    private readonly PaperlessService _paperless;

    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Query Parameters / Search inputs ──────────────────────────────────
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

    /// <summary>Event raised whenever state changes (e.g. results loaded, selection changed)</summary>
    public event Action? OnChange;

    public void NotifyChanged() => OnChange?.Invoke();

    public PaperlessQuery(PaperlessService paperless)
    {
        _paperless = paperless;
    }

    // ── Query Execution ───────────────────────────────────────────────────

    /// <summary>
    /// Runs or refreshes the query against PaperlessService.
    /// Updates Documents, TotalCount, and default selection.
    /// </summary>
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

            // Auto-select first doc if none selected or previous selection no longer present
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

    /// <summary>
    /// Selects a document and loads its thumbnail preview dynamically.
    /// </summary>
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

    /// <summary>
    /// Helper to set Tag and run query in one call.
    /// </summary>
    public async Task SetTagAndRunAsync(PaperlessTag tag)
    {
        Type = QueryType.Tag;
        TagSelected = tag;
        await RunQueryAsync();
    }
}
