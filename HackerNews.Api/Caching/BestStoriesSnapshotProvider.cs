
using HackerNews.Api.Clients;
using HackerNews.Api.Models;
using System.Collections.Concurrent;

namespace HackerNews.Api.Caching;

/// <summary>
/// Singleton class responsible for returning a complete snapshot of ranked Hacker News stories.
/// </summary>
public sealed class BestStoriesSnapshotProvider : IBestStoriesSnapshotProvider, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
    private const int MaxConcurrentRequests = 10;
    private CacheEntry? _cache;
    private sealed record CacheEntry(IReadOnlyList<HackerNewsStory> Stories,DateTimeOffset ExpiresAt);

    /// <summary>
    /// Constructor takes IServiceScopeFactory which works well with this single service
    /// allows for safe creation and disposal 
    /// </summary>
    /// <param name="scopeFactory"></param>
    public BestStoriesSnapshotProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Populates the cache with snapshots while using a semaphore slim to ensure
    /// only single refresh at any one time, has no kno
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<HackerNewsStory>> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        // we test for a valid cache and return the snapshot if valid
        if (IsCacheValid())
        {
            return _cache!.Stories;
        }


        // we signal to callers that cache is being refreshed
        await _refreshLock.WaitAsync(cancellationToken);

        try
        {
            // Another caller may have refreshed the cache while we were waiting outside so we check and return
            if (IsCacheValid())
            {
                return _cache!.Stories!;
            }
            
            // refresh the cache with the latest snapshot
            var latestSnapshot = await FetchStoriesAsync(cancellationToken);

            // update with the latest snapshot and set the expiry
            _cache = new CacheEntry(latestSnapshot, DateTimeOffset.UtcNow.Add(CacheDuration));

            //return latest snapshot
            return latestSnapshot;
        }
        finally
        {
            // signal lock removed
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Determine if cache is valid
    /// </summary>
    /// <returns></returns>
    private bool IsCacheValid()
    {
        return _cache is not null && DateTimeOffset.UtcNow < _cache.ExpiresAt;
    }

    /// <summary>
    /// Gets the Id's and then stories in parallel from HackerNews 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<IReadOnlyList<HackerNewsStory>> FetchStoriesAsync(CancellationToken cancellationToken)
    {
        // need a thread safe collection due to concurrency of async story retrieval
        var stories = new ConcurrentBag<HackerNewsStory>();

        // create a scope container from the injected scope factory 
        // and ask the container for the IHackerNewsClient
        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IHackerNewsClient>();

        // first get the best story ids
        var ids = await client.GetBestStoryIdsAsync(cancellationToken);
        
        // retrieve stories in parallel bounded by MaxConcurrentRequests
        await Parallel.ForEachAsync(
            ids,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxConcurrentRequests,
                CancellationToken = cancellationToken
            },
            async (id, token) =>
            {
                var story = await client.GetStoryAsync(id, token);

                if (story is not null &&
                    !story.Deleted &&
                    !story.Dead &&
                    story.Type == "story")
                {
                    stories.Add(story);
                } });

        // return the retrieved stories ordered by score and latest time
        return stories
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.Time)
            .ToArray();
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        _refreshLock.Dispose();
    }
}
