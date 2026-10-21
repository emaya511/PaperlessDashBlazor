namespace PaperlessDashBlazor.Services;

/// <summary>
/// Business logic and model representation of a DocCard.
/// Holds its query, selected document, and manages card-level operations.
/// Completely independent of Blazor UI.
/// </summary>
public class DocCardModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;

    /// <summary>The query that supplies documents to this card.</summary>
    public PaperlessQuery Query { get; set; }

    /// <summary>The currently selected document for this card.</summary>
    public PaperlessDocument? SelectedDoc => Query.DocSelected;

    /// <summary>Thumbnail for the selected document.</summary>
    public string? SelectedDocThumbnail => Query.SelectedDocThumbnail;

    public event Action? OnChange;

    public DocCardModel(PaperlessQuery query, string? title = null)
    {
        Query = query;
        Title = title ?? query.TagSelected?.Name ?? "Untagged";
        Query.OnChange += () => OnChange?.Invoke();
    }

    public async Task InitializeAsync()
    {
        await Query.RunQueryAsync();
    }

    public async Task SelectDocumentAsync(PaperlessDocument doc)
    {
        await Query.SelectDocumentAsync(doc);
    }
}
