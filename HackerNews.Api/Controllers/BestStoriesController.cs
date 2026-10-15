using HackerNews.Api.Models;
using HackerNews.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNews.Api.Controllers;

/// <summary>
/// Class that 
/// </summary>
[ApiController]
[Route("api/stories")]
public sealed class BestStoriesController : ControllerBase
{
    private readonly IBestStoriesService _service;

    /// <summary>
    /// Constructor takes an injected IBestStoriesService
    /// </summary>
    /// <param name="service"></param>
    public BestStoriesController(IBestStoriesService service)
    {
        _service = service;
    }

    /// <summary>
    /// Retrieves the n best stories where n is bounded on limits
    /// </summary>
    /// <param name="n"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("best")]
    public async Task<ActionResult<IReadOnlyList<BestStoryResponse>>> GetBestStories([FromQuery] int n, CancellationToken cancellationToken)
    {
        // Implement bounded limits for requests
        if (n < 1 || n > 500)
        {
            return BadRequest("Parameter 'n' must be between 1 and 500.");
        }

        var stories = await _service.GetBestStoriesAsync(n, cancellationToken);
        return Ok(stories);
    }
}