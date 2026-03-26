# Hacker News Best Stories API

This project is a REST API built with ASP.NET Core.

It gets the best `n` stories from the Hacker News API and returns them ordered by score, from highest to lowest.

## How to run the application

### Requirements
- .NET SDK
- Internet connection

### Run the project
```bash
dotnet restore
dotnet build
dotnet run


🚀 Use the API

The main endpoint is:

GET /api/stories/best?n=10
If n is not provided, the default value is 10.
If n is invalid (≤ 0 or too large), the API returns a 400 Bad Request.
Results are returned ordered by score (descending) and then by time (descending).

You can also explore and test the API using Swagger UI in development mode.

🧠 Assumptions

The following assumptions were made based on the Hacker News API behavior:

Some items returned by the API may be invalid, deleted, dead, or not of type "story", so they are filtered out.
The descendants field represents the number of comments.
The time field is a Unix timestamp, converted to a standard date format.
The beststories endpoint may include items that are not usable, so additional fetching and filtering is required.
The API may be called frequently, so caching is necessary to avoid overloading the external service.
⚙️ Design Decisions
Implemented a multi-level caching strategy:
Best story IDs (short-lived cache)
Individual stories (longer-lived cache)
Final results per n (very short-lived cache)
Used SemaphoreSlim to prevent cache stampede under concurrent requests.
Used IHttpClientFactory to properly manage HTTP connections.
Used batching + Task.WhenAll to balance performance and external API load.
Filtered invalid stories to ensure consistent and clean results.
🔮 Improvements (With More Time)
Add unit and integration tests for service and controller layers.
Introduce a distributed cache (e.g., Redis) for scalability across multiple instances.
Implement retry policies (e.g., Polly) for handling transient HTTP failures.
Make cache durations configurable via appsettings.json.
Enforce and document a maximum allowed value for n to prevent abuse.
Improve observability with structured logging and metrics.
Add rate limiting to protect the API under heavy load.
Consider pagination or streaming for large datasets.
