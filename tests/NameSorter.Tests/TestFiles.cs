using System.Text;

namespace NameSorter.Tests;

internal sealed class TestDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("name-sorter-tests-").FullName;

    public string OutputPath => System.IO.Path.Combine(Path, NameSorterApplication.OutputFileName);

    public string WriteInput(string contents, string filename = "input.txt")
    {
        var inputPath = System.IO.Path.Combine(Path, filename);
        File.WriteAllText(inputPath, contents, new UTF8Encoding(false));
        return inputPath;
    }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

internal static class AssessmentSample
{
    public static string[] InputLines => File.ReadAllLines(FixturePath("unsorted-names-list.txt"));

    public static string[] SortedLines => File.ReadAllLines(FixturePath("expected-sorted-names.txt"));

    public static string Input => string.Join('\n', InputLines) + "\n";

    public static string Expected => string.Join('\n', SortedLines) + "\n";

    private static string FixturePath(string filename) => System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", filename);
}
