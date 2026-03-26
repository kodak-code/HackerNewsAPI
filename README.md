# Hacker News Best Stories API

A REST API built with ASP.NET Core that retrieves the top `n` stories from the Hacker News API and returns them ordered by score in descending order.

---

## Features

- Retrieves the best stories from Hacker News
- Supports a configurable `n` query parameter
- Orders results by:
  1. `score` descending
  2. `time` descending
- Filters out invalid, deleted, dead, or non-story items
- Uses caching to improve performance and reduce external API calls
- Includes Swagger UI for interactive exploration in development

---

## Requirements

- .NET SDK
- Internet connection

---

## Getting Started

### Clone the repository

```bash
git clone <repository-url>
cd <repository-folder>
```
Restore dependencies
dotnet restore
Build the project
dotnet build
Run the application
dotnet run
API Usage
Endpoint
GET /api/stories/best?n=10
Query Parameters
Parameter	Type	Description	Default
n	int	Number of top stories to return	10
Response Rules
If n is not provided, the API returns the top 10 stories.
If n is less than or equal to 0, the API returns 400 Bad Request.
If n exceeds the allowed maximum, the API returns 400 Bad Request.
Results are sorted by:
score descending
time descending
Example Response
[
  {
    "id": 123456,
    "title": "Example Story",
    "url": "https://example.com",
    "score": 850,
    "author": "john",
    "time": "2026-03-26T12:34:56Z",
    "comments": 120
  }
]
Swagger

In development mode, you can explore and test the API using Swagger UI.

Assumptions

The implementation is based on the behavior of the Hacker News API:

Some items returned by the API may be invalid, deleted, dead, or not of type story
The descendants field represents the number of comments
The time field is a Unix timestamp and is converted to a standard date format
The beststories endpoint may include unusable items, so additional fetching and filtering is required
The API may be called frequently, so caching is necessary to avoid overloading the external service
Design Decisions
Caching Strategy

A multi-level caching approach was implemented:

Best story IDs: short-lived cache
Individual stories: longer-lived cache
Final results per n: very short-lived cache
Performance and Reliability
SemaphoreSlim is used to prevent cache stampede under concurrent requests
IHttpClientFactory is used to manage HTTP connections properly
Batching and Task.WhenAll are used to balance performance with external API load
Invalid stories are filtered out to ensure clean and consistent results

Project Structure

src/
├── Controllers/
├── Services/
├── Models/
├── Caching/
└── Program.cs

Future Improvements
Add unit and integration tests for service and controller layers
Introduce a distributed cache such as Redis
Add retry policies, for example with Polly
Make cache durations configurable through appsettings.json
Improve observability with structured logging and metrics
Add rate limiting for better protection under heavy load
Consider pagination or streaming for larger datasets
