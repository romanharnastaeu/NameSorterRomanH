using System.Text;

namespace NameSorter;

public static class NameSorterApplication
{
    public const string OutputFileName = "sorted-names-list.txt";

    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

    public static void Run(string inputPath, string outputDirectory, TextWriter standardOutput)
    {
        var names = ReadNames(inputPath);
        names.Sort(new PersonNameComparer());
        var text = names.Count == 0 ? string.Empty : string.Join('\n', names) + "\n";

        // Validate the whole input before touching the destination, and save before
        // printing so an unwritable destination cannot look like a successful run.
        WriteOutput(outputDirectory, text);
        standardOutput.Write(text);
    }

    private static List<PersonName> ReadNames(string inputPath)
    {
        try
        {
            var names = new List<PersonName>();
            var lineNumber = 0;
            foreach (var line in File.ReadLines(inputPath, Utf8))
            {
                var name = NameParser.Parse(line, ++lineNumber);
                if (name is not null)
                {
                    names.Add(name);
                }
            }

            return names;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new NameSorterException($"Cannot read input file '{inputPath}': {exception.Message}", exception);
        }
    }

    private static void WriteOutput(string outputDirectory, string text)
    {
        var outputPath = Path.Combine(outputDirectory, OutputFileName);
        try
        {
            File.WriteAllText(outputPath, text, Utf8);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new NameSorterException($"Cannot write output file '{outputPath}': {exception.Message}", exception);
        }
    }
}
