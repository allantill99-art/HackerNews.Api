using HackerNews.Api.Caching;
using HackerNews.Api.Clients;
using HackerNews.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace HackerNews.Tests.Caching;

[TestFixture]
public class BestStoriesSnapshotProviderTests
{
    private Mock<IHackerNewsClient> _client = null!;
    private ServiceProvider _services = null!;
    private BestStoriesSnapshotProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _client = new Mock<IHackerNewsClient>();

        var services = new ServiceCollection();

        services.AddSingleton(_client.Object);

        _services = services.BuildServiceProvider();

        _provider = new BestStoriesSnapshotProvider(_services.GetRequiredService<IServiceScopeFactory>());
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
        _services.Dispose();
    }

    private static HackerNewsStory CreateStory(
        long id,
        int score,
        bool dead = false,
        bool deleted = false)
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
            Type = "story",
            Dead = dead,
            Deleted = deleted
        };
    }

    [Test]
    public async Task GetSnapshotAsync_ReturnsStoriesOrderedByScore()
    {
        _client
            .Setup(c => c.GetBestStoryIdsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new long[] { 1, 2, 3 });

        var stories = new Dictionary<long, HackerNewsStory>
        {
            [1] = CreateStory(1, 100),
            [2] = CreateStory(2, 500),
            [3] = CreateStory(3, 300)
        };

        _client
            .Setup(c => c.GetStoryAsync(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) =>
                stories[id]);

        var result = await _provider.GetSnapshotAsync(
            CancellationToken.None);

        Assert.That(
            result.Select(s => s.Score),
            Is.EqualTo(new[] { 500, 300, 100 }));
    }


    [Test]
    public async Task GetSnapshotAsync_ExcludesDeadAndDeletedStories()
    {
        _client
            .Setup(c => c.GetBestStoryIdsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new long[] { 1, 2, 3 });

        var stories = new Dictionary<long, HackerNewsStory>
        {
            [1] = CreateStory(1, 100),
            [2] = CreateStory(2, 500, dead: true),
            [3] = CreateStory(3, 300, deleted: true)
        };

        _client
            .Setup(c => c.GetStoryAsync(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) =>
                stories[id]);

        var result = await _provider.GetSnapshotAsync(
            CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.Single().Id, Is.EqualTo(1));
    }


    [Test]
    public async Task GetSnapshotAsync_WithinCacheLifetime_FetchesOnlyOnce()
    {
        _client
            .Setup(c => c.GetBestStoryIdsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new long[] { 1 });

        _client
            .Setup(c => c.GetStoryAsync(
                1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStory(1, 100));

        var first = await _provider.GetSnapshotAsync(
            CancellationToken.None);

        var second = await _provider.GetSnapshotAsync(
            CancellationToken.None);

        Assert.That(second, Is.SameAs(first));

        _client.Verify(
            c => c.GetBestStoryIdsAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _client.Verify(
            c => c.GetStoryAsync(
                1, It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Test]
    public async Task GetSnapshotAsync_ConcurrentCallers_OnlyOneRefreshOccurs()
    {
        int fetchCount = 0;

        _client
            .Setup(c => c.GetBestStoryIdsAsync(
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref fetchCount);

                await Task.Delay(100);

                return (IReadOnlyList<long>)new long[] { 1, 2 };
            });

        _client
            .Setup(c => c.GetStoryAsync(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) =>
                CreateStory(id, 100));
        var requests = Enumerable.Range(0, 100)
            .Select(_ => _provider.GetSnapshotAsync(
                CancellationToken.None));

        var results = await Task.WhenAll(requests);

        Assert.That(fetchCount, Is.EqualTo(1));

        Assert.That(
            results.All(r => ReferenceEquals(r, results[0])),
            Is.True);
    }




}