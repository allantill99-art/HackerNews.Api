# Hacker News Best Stories API

An ASP.NET Core REST API that retrieves the highest-scoring stories from the Hacker News API.
The API accepts a requested number of stories e.g. n=5 and returns the top results ordered by score in descending order.

## Getting Started

1. Clone the repository.
2. Open HackerNewsBestStories.sln in Visual Studio 2022.
3. Ensure HackerNews.Api as the startup project.
4. Press F5 to run the application.
5. Use the HTTPS localhost address shown by Visual Studio to call the endpoint.

## API Endpoint

### GET /api/stories/best?n={n}

Returns the requested number of highest-scoring available Hacker News stories.

**Example request**

https://localhost:7176/api/stories/best?n=5
(replacing 7176 as appropriate)

### Validation

The n query parameter must be between 1 and 500.
Invalid values return HTTP Bad Request.

## Data

The API uses the public Hacker News Firebase API:

Best-story IDs: https://hacker-news.firebaseio.com/v0/beststories.json
Story details: https://hacker-news.firebaseio.com/v0/item/{id}.json

The best story IDs are retrieved first, followed by the details for each story.
Stories are then ordered by score in descending order, with latest time used when scores are equal

## Architecture/Design

The application separates HTTP handling, business logic, caching, and external API access.

BestStoriesController
Handles incoming HTTP requests, validates and returns response.

BestStoriesService
Selects the requested number of stories from the ranked snapshot and maps HackerNewsStory results to BestStoryResponse

BestStoriesSnapshotProvider
Maintains a shared in-memory snapshot of ranked stories and co-ordinates cache refreshes.

HackerNewsClient
A typed HttpClient registered through IHttpClientFactory

In a production project, the single HackerNews.Api project would likely benefit fromsplit into
relevant libraries rather than a single monolithic project.

### In-memory caching

The solution caches the complete ranked story snapshot for 60 seconds.
Requests for different values of n reuse the same snapshot while it remains valid.

This reduces repeated network calls and improves response times for subsequent requests.

However, in effect, the cache is a snapshot of a single point time. For a production system, a CacheUpdateService could be used
that monitors story updates in real time and injects them into the (potentially distributed) cache. 
The Firebase Api supports change notifications which could be used for this.

### Single cache refresh

A SemaphoreSlim guards the retrieval so that only one refresh from HackerNews occurs at any time.
Only one request performs the refresh within the expiry time while other simultaneous requests use the refreshed result.

Any initial request may take additional time so a 'warm-up' process for the cache could be implemented in a production ready system 

### Bounded concurrency

Options to retrieve stories include:
A. Sequentially - Fetch one story at a time - slower option
B. All concurrently - Fetch every request together, potential overload upstream
C. Bounded concurrency - Fetch multiple stories concurrently, with a configurable maximum bound

Bounded concurrency is a good trade off between multiple requests v. sequential with ability to tune in config to avoid overloading upstream.
Story details are fetched asynchronously using Parallel.ForEachAsync with a maximum degree of parallelism of 10.

### HTTP client management

The HttpClient handles HTTP communication and JSON deserialization separately from the business logic with a 30s timeout

## Assumptions and Design Decisions

Story items are excluded if they are null or deleted etc.
Stories without an external URL use the Hacker News discussion URL.
The maximum accepted value of n is 500 to match HackerNews
A 60-second cache staleness before refresh is used as a trade-off between uopdates to the cache

## Testing

The solution includes an NUnit test project using Moq.
Implemented unit tests cover:

Returning the requested number of stories.
Mapping Hacker News story fields into the API response.
Falling back to the Hacker News discussion URL when an external URL is missing.
Returned stories are ordered by score
Checked dead and deleted stories are excluded
Check that multiple requests only trigger one cache refresh within expiry limit
Multiple concurrent requests triggers only one refresh after cache expiry

## Limitations/Improvements

1.	Logic around cancellation of in flight requests needs tests and potential improvements
2.	Failures around cache refresh needs additional focus with potential retry mechanism
3.  Cache duration, concurrency limit, and HTTP timeout etc, need to be configurable or loaded at startup from storage
4.	For a production system that may have many processes an external distributed cache could be used
5.	As well as more overall test coverage, integration tests could be added for a production ready system
6.  For production better exception handling, logging and metrics would be required

## Scope

The focus of this solution was on showing how usage of caching and concurrency constraints could be used to satisfy the functional requirements of the
specification as well as demonstrating how usage of injection can be used to support testability
