using DTO;
using HackerNewsAPI.Model;
using HackerNewsAPI.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HackerNewsAPI.Services
{
    /// <summary>
    /// Retrieves and processes stories from Hacker News, applying caching and filtering.
    /// </summary>

    public class HackerNewsService : IHackerNewsService
    {

        private const string BestIdsCacheKey = "hn:best_story_ids";
        private static readonly TimeSpan BestIdsCacheDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan StoryCacheDuration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan ResultCacheDuration = TimeSpan.FromMinutes(2);
        private readonly ILogger<HackerNewsService> _logger;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly SemaphoreSlim _cacheFillLock = new(1, 1);

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="HackerNewsService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Factory used to create configured HTTP clients.</param>
        /// <param name="cache">In-memory cache used to reduce calls to Hacker News.</param>
        /// <param name="logger"> Logger used to record application flow, cache behavior, and external API interactions.
        /// </param>
        public HackerNewsService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<HackerNewsService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the top N best stories from Hacker News, ordered by score and recency.
        /// Applies multi-level caching to minimize external API calls and improve performance.
        /// </summary>
        /// <param name="n">Number of stories to retrieve.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>
        /// A read-only list of the best stories, filtered and sorted by score and time.
        /// </returns>
        public async Task<IReadOnlyList<StoryDTO>> GetBestStoriesAsync(int n, CancellationToken cancellationToken = default)
        {
            if (n <= 0)
                return Array.Empty<StoryDTO>();

            _logger.LogInformation("Request received for best {Count} stories.", n);

            var resultCacheKey = $"hn:best_stories:{n}";

            if (_cache.TryGetValue(resultCacheKey, out IReadOnlyList<StoryDTO>? cachedResult) &&
                cachedResult is not null)
            {
                _logger.LogInformation("Cache hit for key {CacheKey}. Returning cached result.", resultCacheKey);
                return cachedResult;
            }

            _logger.LogInformation("Cache miss for key {CacheKey}. Building result from Hacker News.", resultCacheKey);


            // Prevents cache stampede: ensures only one request rebuilds the cache at a time
            await _cacheFillLock.WaitAsync(cancellationToken);

            try
            {
                // Double-check pattern: cache might have been filled while waiting for the lock
                if (_cache.TryGetValue(resultCacheKey, out cachedResult) && cachedResult is not null)
                {
                    _logger.LogInformation("Cache filled by another request for key {CacheKey}.", resultCacheKey);
                    return cachedResult;
                }

                // Create a configured HTTP client for Hacker News API
                var client = _httpClientFactory.CreateClient("HackerNews");

                var bestIds = await GetBestStoryIdsAsync(client, cancellationToken);

                var stories = new List<StoryDTO>(n);

                // Fetch stories in batches to control concurrency and avoid overwhelming the external API
                var batchSize = Math.Clamp(n * 2, 10, 40);

                for (var i = 0; i < bestIds.Count && stories.Count < n; i += batchSize)
                {
                    var batch = bestIds.Skip(i).Take(batchSize).ToArray();

                    var tasks = batch.Select(id => GetStoryByIdAsync(client, id, cancellationToken));

                    // Execute requests in parallel within the batch for better performance
                    var batchStories = await Task.WhenAll(tasks);

                    foreach (var story in batchStories)
                    {
                        if (story is not null)
                            stories.Add(story);
                    }
                }

                // Order by score (descending) and use time as a tie-breaker
                var result = stories
                    .OrderByDescending(s => s.Score)
                    .ThenByDescending(s => s.Time)
                    .Take(n)
                    .ToList()
                    .AsReadOnly();

                // Cache final result per 'n' value to avoid recomputation on repeated requests
                _cache.Set(resultCacheKey, result, ResultCacheDuration);

                _logger.LogInformation("Caching result for key {CacheKey} for {Minutes} minutes.", resultCacheKey, 2);

                _logger.LogInformation("Returning {Count} stories to client.", result.Count);

                return result;
            }
            finally
            {
                // Ensure the lock is always released, even if an exception occurs
                _cacheFillLock.Release();
            }
        }

        /// <summary>
        /// Retrieves the list of best story IDs from Hacker News.
        /// </summary>
        /// <param name="client">Configured HTTP client used to call the external API.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The list of best story IDs.</returns>
        private async Task<List<int>> GetBestStoryIdsAsync(HttpClient client, CancellationToken cancellationToken)
        {
            // Try to get cached IDs to avoid calling the external API on every request
            if (_cache.TryGetValue(BestIdsCacheKey, out List<int>? cachedIds) && cachedIds is not null)
            {
                _logger.LogInformation("Cache hit for best story IDs.");
                return cachedIds;
            }

            _logger.LogInformation("Cache miss for best story IDs. Calling Hacker News API.");

            var response = await client.GetAsync("beststories.json", cancellationToken);
            response.EnsureSuccessStatusCode();

            // Read response as stream for better performance (avoids loading entire content into memory)
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var ids = await JsonSerializer.DeserializeAsync<List<int>>(stream, _jsonOptions, cancellationToken)
                      ?? new List<int>();

            // Cache IDs for a short period to reduce external calls while keeping data relatively fresh
            _cache.Set(BestIdsCacheKey, ids, BestIdsCacheDuration);

            _logger.LogInformation("Caching best story IDs for {Minutes} minutes.", 5);

            return ids;
        }

        /// <summary>
        /// Retrieves a story by its Hacker News ID and maps it to the API DTO.
        /// </summary>
        /// <param name="client">Configured HTTP client used to call the external API.</param>
        /// <param name="id">Hacker News story ID.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The mapped story if valid; otherwise <c>null</c>.</returns>
        private async Task<StoryDTO?> GetStoryByIdAsync(HttpClient client, int id, CancellationToken cancellationToken)
        {
            var cacheKey = $"hn:item:{id}";

            if (_cache.TryGetValue(cacheKey, out StoryDTO? cachedStory) && cachedStory is not null)
            {
                _logger.LogInformation("Cache hit for story {StoryId}.", id);
                return cachedStory;
            }

            _logger.LogDebug("Cache miss for story {StoryId}. Calling Hacker News API.", id);

            var response = await client.GetAsync($"item/{id}.json", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch story {StoryId}. StatusCode: {StatusCode}", id, response.StatusCode);
                return null;
            }

            // Deserialize response stream into internal model
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var item = await JsonSerializer.DeserializeAsync<HnItem>(stream, _jsonOptions, cancellationToken);

            // Filter out invalid items (deleted, dead, not a story, or missing required data)
            if (item is null ||
                item.Deleted ||
                item.Dead ||
                !string.Equals(item.Type, "story", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(item.Title))
            {
                _logger.LogDebug("Story {StoryId} ignored (invalid/deleted/dead/not a story).", id);
                return null;
            }

            // Map internal model to DTO exposed by the API
            var dto = new StoryDTO
            {
                Title = item.Title,
                Uri = item.Url,
                PostedBy = item.By ?? string.Empty,
                Time = DateTimeOffset.FromUnixTimeSeconds(item.Time),
                Score = item.Score ?? 0,
                CommentCount = item.Descendants ?? 0
            };

            // Cache individual story for longer duration since it rarely changes
            _cache.Set(cacheKey, dto, StoryCacheDuration);
            
            _logger.LogInformation("Caching story {StoryId} for {Minutes} minutes.", id, 30);

            return dto;
        }
    }
}
