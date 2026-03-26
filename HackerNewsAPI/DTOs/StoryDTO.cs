namespace DTO
{
    /// <summary>
    /// Represents a Hacker News story returned to the client.
    /// </summary>
    public class StoryDTO
    {
        private string _title = string.Empty;
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        private string? _uri;
        public string? Uri
        {
            get { return _uri; }
            set { _uri = value; }
        }

        private string _postedBy = string.Empty;
        public string PostedBy
        {
            get { return _postedBy; }
            set { _postedBy = value; }
        }

        private DateTimeOffset _time;
        public DateTimeOffset Time
        {
            get { return _time; }
            set { _time = value; }
        }

        private int _score;
        public int Score
        {
            get { return _score; }
            set { _score = value; }
        }

        private int _commentCount;
        public int CommentCount
        {
            get { return _commentCount; }
            set { _commentCount = value; }
        }
    }
}