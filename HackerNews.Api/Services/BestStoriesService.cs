using HackerNews.Api.Caching;
using HackerNews.Api.Models;

namespace HackerNews.Api.Services;

/// <summary>
/// Retrieves the top n stories from the latest snapshot cache
/// </summary>
public sealed class BestStoriesService : IBestStoriesService
{
    // the story retriever
    private readonly IBestStoriesSnapshotProvider _provider;

    /// <summary>
    /// Constructor taking a IBestStoriesSnapshotProvider
    /// </summary>
    /// <param name="provider"></param>
    public BestStoriesService(IBestStoriesSnapshotProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Selects the requested number of stories and returns as a collection of BestStoryResponse
    /// </summary>
    /// <param name="count"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<BestStoryResponse>> GetBestStoriesAsync(int count, CancellationToken cancellationToken)
    {
        var snapshot = await _provider.GetSnapshotAsync(cancellationToken);

        return snapshot
            .Take(count)
            .Select(MapToResponse)
            .ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="story"></param>
    /// <returns></returns>
    private static BestStoryResponse MapToResponse(HackerNewsStory story)
    {
        return new BestStoryResponse(
            Title: story.Title ?? string.Empty,
            Uri: story.Url ??
                 $"https://news.ycombinator.com/item?id={story.Id}",
            PostedBy: story.By ?? string.Empty,
            Time: DateTimeOffset.FromUnixTimeSeconds(story.Time),
            Score: story.Score,
            CommentCount: story.Descendants
        );
    }
}