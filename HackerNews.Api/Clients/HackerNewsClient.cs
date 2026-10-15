using System.Net;
using System.Net.Http.Json;
using HackerNews.Api.Models;

namespace HackerNews.Api.Clients;

/// <summary>
/// Implementation of IHackerNewsClient to retrieve raw data from HackerNews
/// Sorting, caching and concurrency is handles in the service layer
/// Http errors will propagate as exceptions handled at service level
/// </summary>
public sealed class HackerNewsClient : IHackerNewsClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor with injection of HttpClient to support managed
    /// HTTP connections through IHttpClientFactory
    /// </summary>
    /// <param name="httpClient"></param>
    public HackerNewsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Implementation of GetBestStoryIdsAsync using await/async
    /// so non-blocking while awaiting HackerNews response, with cancellation
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        var ids = await _httpClient.GetFromJsonAsync<long[]>("v0/beststories.json", cancellationToken);
        return ids ?? Array.Empty<long>();
    }

    /// <summary>
    /// Implementation of GetStoryAsync, missing story returns null
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<HackerNewsStory?> GetStoryAsync(long id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"v0/item/{id}.json", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HackerNewsStory>(cancellationToken);
    }
}