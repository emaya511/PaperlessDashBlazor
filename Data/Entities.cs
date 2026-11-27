using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperlessDashBlazor.Data;

// ── EF Core Entities (DB schema) ──────────────────────────────────────────

public class PageEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    public List<CardEntity> Cards { get; set; } = [];
}

public class CardEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int? DocId { get; set; }
    public string? DocName { get; set; }

    public Guid PageId { get; set; }

    [ForeignKey(nameof(PageId))]
    public PageEntity Page { get; set; } = null!;

    public List<QueryEntity> Queries{ get; set; }  = [];
}

public class QueryEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? TagSelected { get; set; }

    public Guid CardId { get; set; }

    [ForeignKey(nameof(CardId))]
    public CardEntity Card { get; set; } = null!;

}
