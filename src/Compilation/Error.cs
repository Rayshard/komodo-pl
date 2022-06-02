namespace Komodo.Utilities
{
    class Error
    {
        public Location Location { get; }
        public string Message { get; }

        private Error(Location location, string message)
        {
            Location = location;
            Message = message;
        }
    }
}