using HackerNews.Api.Models;

namespace HackerNews.Api.Caching;

/// <summary>
/// Contract for a cache to return complete snapshot
/// </summary>
public interface IBestStoriesSnapshotProvider
{
    Task<IReadOnlyList<HackerNewsStory>> GetSnapshotAsync(CancellationToken cancellationToken);
}