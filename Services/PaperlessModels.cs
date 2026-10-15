using System.Text.Json.Serialization;

namespace PaperlessDashBlazor.Services;

// ── Generic paged response from Paperless-NGX ──────────────────────────────

public class PaperlessPage<T>
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("results")]
    public List<T> Results { get; set; } = [];
}

// ── Tag ────────────────────────────────────────────────────────────────────

public class PaperlessTag
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("colour")]
    public int Colour { get; set; }

    [JsonPropertyName("document_count")]
    public int DocumentCount { get; set; }

    /// <summary>Maps Paperless colour index (1-16) to a CSS hex colour.</summary>
    public string CssColour => Colour switch
    {
        1  => "#a6cee3",
        2  => "#1f78b4",
        3  => "#b2df8a",
        4  => "#33a02c",
        5  => "#fb9a99",
        6  => "#e31a1c",
        7  => "#fdbf6f",
        8  => "#ff7f00",
        9  => "#cab2d6",
        10 => "#6a3d9a",
        11 => "#ffff99",
        12 => "#b15928",
        13 => "#8dd3c7",
        14 => "#bc80bd",
        15 => "#ccebc5",
        16 => "#ffed6f",
        _  => "#aaaaaa"
    };
}

// ── Document ───────────────────────────────────────────────────────────────

public class PaperlessDocument
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    [JsonPropertyName("correspondent")]
    public int? Correspondent { get; set; }

    [JsonPropertyName("tags")]
    public List<int> Tags { get; set; } = [];
}
