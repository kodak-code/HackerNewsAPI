namespace HackerNewsAPI.Model
{
    /// <summary>
    /// Represents a raw Hacker News item retrieved from the API.
    /// </summary>
    public class HnItem
    {
        private string? _type;
        public string? Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private string? _title;
        public string? Title
        {
            get { return _title; }
            set { _title = value; }
        }

        private string? _url;
        public string? Url
        {
            get { return _url; }
            set { _url = value; }
        }

        private string? _by;
        public string? By
        {
            get { return _by; }
            set { _by = value; }
        }

        private long _time;
        public long Time
        {
            get { return _time; }
            set { _time = value; }
        }

        private int? _score;
        public int? Score
        {
            get { return _score; }
            set { _score = value; }
        }

        private int? _descendants;
        public int? Descendants
        {
            get { return _descendants; }
            set { _descendants = value; }
        }

        private bool _deleted;
        public bool Deleted
        {
            get { return _deleted; }
            set { _deleted = value; }
        }

        private bool _dead;
        public bool Dead
        {
            get { return _dead; }
            set { _dead = value; }
        }
    }
}
