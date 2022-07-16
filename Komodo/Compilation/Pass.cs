namespace Komodo.Utilities;

public record Pass<T>(T Value)
{
    public Pass<Output>? Run<Output>(string name, Func<T, Diagnostics, Output?> Function, Dictionary<string, TextSource> sourceFiles, Diagnostics diagnostics)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start();
        var output = Function(Value, diagnostics);
        stopwatch.Stop();

        if (output == null || diagnostics.HasError)
            return null;

        Utility.PrintInfo($"{name} pass finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");
        return new Pass<Output>(output);
    }
}