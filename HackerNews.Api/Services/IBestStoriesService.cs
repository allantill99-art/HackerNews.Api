using HackerNews.Api.Models;

namespace HackerNews.Api.Services;

/// <summary>
/// Contract for retrieving the best stories ranked by score,time
/// </summary>
public interface IBestStoriesService
{
    Task<IReadOnlyList<BestStoryResponse>> GetBestStoriesAsync(int count, CancellationToken cancellationToken);
}