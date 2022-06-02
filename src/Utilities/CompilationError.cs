namespace Komodo.Utilities
{
    class CompilationError
    {
        public Location Location { get; }
        public string Message { get; }

        private CompilationError(Location location, string message)
        {
            Location = location;
            Message = message;
        }
    }
}