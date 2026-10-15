using HackerNews.Api.Caching;
using HackerNews.Api.Models;
using HackerNews.Api.Services;
using Moq;
using NUnit.Framework;

namespace HackerNews.Tests.Services;

/// <summary>
/// Test class to isolate the BestStoriesService for testing
/// </summary>
[TestFixture]
public class BestStoriesServiceTests
{
    private Mock<IBestStoriesSnapshotProvider> _provider = null!;
    private BestStoriesService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new Mock<IBestStoriesSnapshotProvider>();
        _service = new BestStoriesService(_provider.Object);
    }

    [Test]
    public async Task GetBestStoriesAsync_ReturnsRequestedNumberOfStories()
    {
        // Arrange
        var stories = new[]
        {
            CreateStory(1, 300),
            CreateStory(2, 200),
            CreateStory(3, 100)
        };

        _provider
            .Setup(p => p.GetSnapshotAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stories);

        // Act
        var result = await _service.GetBestStoriesAsync(2, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Score, Is.EqualTo(300));
        Assert.That(result[1].Score, Is.EqualTo(200));
    }


    [Test]
    public async Task GetBestStoriesAsync_MapsStoryFieldsCorrectly()
    {

        var story = new HackerNewsStory
        {
            Id = 123,
            Title = "Test Story",
            Url = "https://example.com/article",
            By = "allan",
            Time = 1700000000,
            Score = 500,
            Descendants = 42,
            Type = "story"
        };

        _provider
            .Setup(p => p.GetSnapshotAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { story });


        var result = await _service.GetBestStoriesAsync(
            1, CancellationToken.None);


        var item = result.Single();

        Assert.Multiple(() =>
        {
            Assert.That(item.Title, Is.EqualTo("Test Story"));
            Assert.That(item.Uri,
                Is.EqualTo("https://example.com/article"));
            Assert.That(item.PostedBy, Is.EqualTo("allan"));
            Assert.That(item.Score, Is.EqualTo(500));
            Assert.That(item.CommentCount, Is.EqualTo(42));
            Assert.That(item.Time,
                Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(
                    1700000000)));
        });
    }


    [Test]
    public async Task GetBestStoriesAsync_WhenUrlMissing_UsesDiscussionUrl()
    {
        var story = new HackerNewsStory
        {
            Id = 12345,
            Title = "Ask HN",
            Url = null,
            By = "testuser",
            Time = 1700000000,
            Score = 100,
            Descendants = 20,
            Type = "story"
        };

        _provider
            .Setup(p => p.GetSnapshotAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { story });

        var result = await _service.GetBestStoriesAsync(
            1, CancellationToken.None);

        Assert.That(
            result.Single().Uri,
            Is.EqualTo(
                "https://news.ycombinator.com/item?id=12345"));
    }



    private static HackerNewsStory CreateStory(
        long id, int score)
    {
        return new HackerNewsStory
        {
            Id = id,
            Title = $"Story {id}",
            Url = $"https://example.com/{id}",
            By = "testuser",
            Time = 1700000000,
            Score = score,
            Descendants = 10,
            Type = "story"
        };
    }
}