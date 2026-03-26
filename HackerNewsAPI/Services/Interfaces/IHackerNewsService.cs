using DTO;

namespace HackerNewsAPI.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for retrieving best stories from Hacker News.
    /// </summary>

    public interface IHackerNewsService
    {
        /// <summary>
        /// Gets the best N stories ordered by score in descending order.
        /// </summary>
        /// <param name="n">Number of stories to retrieve.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A read-only list of stories.</returns>
        Task<IReadOnlyList<StoryDTO>> GetBestStoriesAsync(int n, CancellationToken cancellationToken = default);
    }
}