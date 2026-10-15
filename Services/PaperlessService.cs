using System.Net.Http.Headers;
using System.Text.Json;

namespace PaperlessDashBlazor.Services;

public class PaperlessService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public PaperlessService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("paperless");
    }

    // ── Tags ──────────────────────────────────────────────────────────────

    /// <summary>Returns all tags, ordered by name.</summary>
    public async Task<List<PaperlessTag>> GetTagsAsync()
    {
        try
        {
            var response = await _http.GetStringAsync("api/tags/?page_size=200&ordering=name");
            var page = JsonSerializer.Deserialize<PaperlessPage<PaperlessTag>>(response, _json);
            return page?.Results ?? [];
        }
        catch
        {
            return [];
        }
    }

    // ── Documents ─────────────────────────────────────────────────────────

    /// <summary>Returns up to <paramref name="limit"/> documents that have the given tag.</summary>
    public async Task<(List<PaperlessDocument> Documents, int TotalCount)> GetDocumentsByTagAsync(
        int tagId, int limit = 10)
    {
        try
        {
            var url = $"api/documents/?tags__id__all={tagId}&page_size={limit}&ordering=-created";
            var response = await _http.GetStringAsync(url);
            var page = JsonSerializer.Deserialize<PaperlessPage<PaperlessDocument>>(response, _json);
            return (page?.Results ?? [], page?.Count ?? 0);
        }
        catch
        {
            return ([], 0);
        }
    }

    /// <summary>Returns the URL to open a document directly in Paperless-NGX.</summary>
    public string GetDocumentUrl(int documentId) =>
        $"{_http.BaseAddress}documents/{documentId}/details/";
}
