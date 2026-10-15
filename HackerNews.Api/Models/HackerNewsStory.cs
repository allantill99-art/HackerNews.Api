using System.Text.Json.Serialization;

namespace HackerNews.Api.Models;

/// <summary>
/// Represents the JSON returned by the HackerNews Api
/// </summary>
public sealed class HackerNewsStory
{
    public long Id { get; init; }
    public string? Title { get; init; }
    public string? Url { get; init; }

    //Hacker news has by field as lower case so we attribute here
    [JsonPropertyName("by")]
    public string? By { get; init; }

    public long Time { get; init; }
    public int Score { get; init; }
    public int Descendants { get; init; }

    public string? Type { get; init; }
    public bool Deleted { get; init; }
    public bool Dead { get; init; }
}