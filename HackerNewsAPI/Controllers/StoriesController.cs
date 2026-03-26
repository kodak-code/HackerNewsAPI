using HackerNewsAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsAPI.Controllers
{
    /// <summary>
    /// Exposes endpoints related to Hacker News stories.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class StoriesController : ControllerBase
    {
        private readonly IHackerNewsService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoriesController"/> class.
        /// </summary>
        /// <param name="service">Service that retrieves stories from Hacker News.</param>
        public StoriesController(IHackerNewsService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets the best stories ordered by score in descending order.
        /// </summary>
        /// <param name="n">Number of stories to retrieve.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>An HTTP 200 response containing the list of stories, or 400 if the value of n is invalid.</returns>
        [HttpGet("best")]
        public async Task<IActionResult> GetBestStories([FromQuery] int n = 10, CancellationToken cancellationToken = default)
        {
            if (n <= 0)
                return BadRequest(new { message = "n must be greater than 0" });

            var stories = await _service.GetBestStoriesAsync(n, cancellationToken);
            return Ok(stories);
        }
    }
}
