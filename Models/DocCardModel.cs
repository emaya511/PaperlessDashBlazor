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
    public PaperlessQuery Query { get; }

    /// <summary>The currently selected document.</summary>
    public PaperlessDocument? SelectedDoc => Query.DocSelected;

    /// <summary>Thumbnail for the selected document.</summary>
    public string? SelectedDocThumbnail => Query.SelectedDocThumbnail;

    public event Action? OnChange;

    public DocCardModel(Guid id, PaperlessQuery query, string? title = null)
    {
        Id = id;
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
