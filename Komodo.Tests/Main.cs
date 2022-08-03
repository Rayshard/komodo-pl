namespace Komodo.Tests.Compilation;

using System.Diagnostics;
using System.Text.Json;

public class Main
{
    static string KMD_BIN => Environment.GetEnvironmentVariable("KMD_BIN") ?? throw new Exception("No environment variable set for KMD_BIN");
    static string TESTS_DIR => Environment.GetEnvironmentVariable("TESTS_DIR") ?? throw new Exception("No environment variable set for TESTS_DIR");

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void RunTestCase(string directory)
    {
        var name = Path.GetDirectoryName(directory);
        var testCasePath = Path.Combine(directory, "test-case.json");

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(testCasePath));
        var testCase = document.RootElement;

        var args = testCase.GetProperty("args").GetString()!;
        var exitcode = testCase.GetProperty("exitcode").GetInt32()!;
        var stdin = testCase.GetProperty("stdin").EnumerateArray().Select(x => x.GetString())!;
        var stdout = testCase.GetProperty("stdout").EnumerateArray().Select(x => x.GetString())!;
        var stderr = testCase.GetProperty("stderr").EnumerateArray().Select(x => x.GetString())!;

        var psi = new ProcessStartInfo();
        psi.FileName = KMD_BIN;
        psi.Arguments = args;
        psi.WorkingDirectory = directory;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        using var process = Process.Start(psi)!;

        var output = process.StandardOutput.ReadToEnd();
        Console.WriteLine(output);
        process.WaitForExit();
    }

    public static IEnumerable<object[]> GetTestCases()
    {
        foreach (var directory in Directory.GetDirectories(TESTS_DIR))
            yield return new object[] { Path.GetFullPath(directory) };
    }
}