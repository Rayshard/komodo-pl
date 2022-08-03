namespace Komodo.Utilities;

public record Pass<T>(T? Value = null) where T : class
{
    public Pass<Output> Bind<Output>(string name, Func<T, Diagnostics?, Output?> Function, Diagnostics? diagnostics) where Output : class
    {
        if(Value is null)
            return new Pass<Output>();

        var stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start();
        var output = Function(Value, diagnostics);
        stopwatch.Stop();

        if (output == null || (diagnostics is not null && diagnostics.HasError))
            return new Pass<Output>();

        Utility.PrintInfo($"{name} pass finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");
        return new Pass<Output>(output);
    }
}