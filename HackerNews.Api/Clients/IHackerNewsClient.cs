using HackerNews.Api.Models;

namespace HackerNews.Api.Clients;

/// <summary>
/// Contract for HttpClient to retrieve raw data from HackerNews
/// </summary>
public interface IHackerNewsClient
{
    /// <summary>
    /// Retrieve the best stories id's asynchronously and
    /// define result as a non-modifiable IReadOnlyList with ability to cancel
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve stories asynchronously with a nullable result where stories don't exist
    /// with cancellation feature
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<HackerNewsStory?> GetStoryAsync(long id, CancellationToken cancellationToken);
}