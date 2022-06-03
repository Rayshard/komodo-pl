using Komodo.Utilities;

namespace Komodo
{
    class CLI
    {
        static void Main(string[] args) 
        {
            if(args.Length != 1)
            {
                Console.WriteLine("Usage: komodo [input file path]");
                Environment.Exit(-1);
            }

            var inputFilePath = args[0];
            var sourceFileResult = SourceFile.Load(inputFilePath);
            if (sourceFileResult.IsFailure)
            {
                Console.WriteLine($"ERROR: {sourceFileResult.UnwrapFailure()}");
                Environment.Exit(-1);
            }
            
            var sourceFile = sourceFileResult.UnwrapSuccess();
            Console.WriteLine(sourceFile.Text);
        }
    }
}
