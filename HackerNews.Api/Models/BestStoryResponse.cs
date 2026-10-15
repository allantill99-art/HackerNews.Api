namespace HackerNews.Api.Models;

/// <summary>
/// Define the response DTO as immutable
/// </summary>
/// <param name="Title"></param>
/// <param name="Uri"></param>
/// <param name="PostedBy"></param>
/// <param name="Time"></param>
/// <param name="Score"></param>
/// <param name="CommentCount"></param>
public sealed record BestStoryResponse(
    string Title,
    string Uri,
    string PostedBy,
    DateTimeOffset Time,
    int Score,
    int CommentCount
);