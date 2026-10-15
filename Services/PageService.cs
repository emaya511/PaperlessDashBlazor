using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperlessDashBlazor.Services;

public enum CardSize
{
    Doc,    // 1×1 grid cell
    Table   // 2×2 grid cells
}

public class CardEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public CardSize Size { get; set; }

    // Paperless-NGX tag filter (only used when Size == Doc)
    public int? TagId { get; set; }
    public string? TagName { get; set; }
    public string? TagColour { get; set; }   // CSS hex colour, e.g. "#1f78b4"

    // Foreign key + navigation back to the page
    public Guid PageId { get; set; }

    [ForeignKey(nameof(PageId))]
    public DynamicPageEntry Page { get; set; } = null!;
}

public class DynamicPageEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    public List<CardEntry> Cards { get; set; } = [];
}
