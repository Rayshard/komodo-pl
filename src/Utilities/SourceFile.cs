namespace Komodo.Utilities
{
    class SourceFile
    {
        public string Path { get; }
        public string Text { get; }

        private SourceFile(string path, string text)
        {
            Path = path;
            Text = text;
        }

        public static Result<SourceFile, string> Load(string path)
        {
            try { return new Result<SourceFile, string>.Success(new SourceFile(path, System.IO.File.ReadAllText(path))); }
            catch (Exception e) { return new Result<SourceFile, string>.Failure(e.Message); }
        }
    }
}