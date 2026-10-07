using System.Text;

namespace NameSorter.Tests;

public sealed class NameSorterApplicationTests : IDisposable
{
    private readonly TestDirectory _directory = new();

    [Fact]
    public void Run_writes_the_exact_assessment_sample_to_stdout_and_file()
    {
        var output = Run(AssessmentSample.Input);

        Assert.Equal(AssessmentSample.Expected, output);
        Assert.Equal(output, File.ReadAllText(_directory.OutputPath));
    }

    [Fact]
    public void Run_ignores_blank_lines_and_normalizes_whitespace()
    {
        var output = Run("\n\t\r\n  Alex\t Smith  \r\n\n Marin   Alvarez\n");

        Assert.Equal("Marin Alvarez\nAlex Smith\n", output);
        Assert.Equal(output, File.ReadAllText(_directory.OutputPath));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void Run_accepts_common_line_endings_and_an_unterminated_final_line(string newline)
    {
        var output = Run($"Zoe Smith{newline}{newline}Alex Smith");

        Assert.Equal("Alex Smith\nZoe Smith\n", output);
        Assert.Equal(output, File.ReadAllText(_directory.OutputPath));
    }

    [Theory]
    [InlineData("Roman\n", 1)]
    [InlineData("Alex Smith\n\n\t\nRoman\n", 4)]
    [InlineData("\r\nAlex Smith\r\nOne Two Three Four Five\r\nMarin Alvarez\r\n", 3)]
    [InlineData("Alex Smith\r\rRoman", 3)]
    [InlineData("Alex Smith\r\n\nMarin Alvarez\rRoman", 4)]
    public void Run_reports_the_physical_line_number_without_producing_partial_output(string input, int invalidLine)
    {
        File.WriteAllText(_directory.OutputPath, "previous output");
        var inputPath = _directory.WriteInput(input);
        using var stdout = new StringWriter();

        var exception = Assert.Throws<NameSorterException>(() => NameSorterApplication.Run(inputPath, _directory.Path, stdout));

        Assert.Equal($"Invalid name at line {invalidLine}: expected 1-3 given names followed by a surname.", exception.Message);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Equal("previous output", File.ReadAllText(_directory.OutputPath));
    }

    [Fact]
    public void Run_does_not_create_an_output_file_for_malformed_input()
    {
        Assert.Throws<NameSorterException>(() => Run("Alex Smith\nRoman\n"));

        Assert.False(File.Exists(_directory.OutputPath));
    }

    [Fact]
    public void Run_overwrites_an_existing_output_file()
    {
        File.WriteAllText(_directory.OutputPath, new string('x', 500));

        Run("Marin Alvarez\n");

        Assert.Equal("Marin Alvarez\n", File.ReadAllText(_directory.OutputPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n\t\n  \n")]
    public void Run_creates_empty_output_for_an_empty_valid_collection(string input)
    {
        File.WriteAllText(_directory.OutputPath, "stale content");

        Assert.Equal(string.Empty, Run(input));
        Assert.Empty(File.ReadAllBytes(_directory.OutputPath));
    }

    [Fact]
    public void Run_sorts_1500_names_and_retains_duplicates()
    {
        var expected = Enumerable.Range(0, 1500).Select(index => $"Given{index:D4} Surname").ToList();
        expected.Insert(750, expected[750]);
        var input = string.Join('\n', expected.AsEnumerable().Reverse());

        Assert.Equal(string.Join('\n', expected) + "\n", Run(input));
        Assert.Equal(expected, File.ReadAllLines(_directory.OutputPath));
    }

    [Fact]
    public void Run_preserves_unicode_and_writes_utf8_without_a_bom()
    {
        const string expected = "José Alvarez\nAnne O'Connor\nMary-Jane Watson\n";
        var inputPath = _directory.WriteInput(string.Empty);
        File.WriteAllText(inputPath, "Mary-Jane Watson\r\nJosé Alvarez\r\nAnne O'Connor\r\n", new UTF8Encoding(true));
        using var stdout = new StringWriter();

        NameSorterApplication.Run(inputPath, _directory.Path, stdout);

        Assert.Equal(expected, stdout.ToString());
        Assert.Equal(Encoding.UTF8.GetBytes(expected), File.ReadAllBytes(_directory.OutputPath));
    }

    [Fact]
    public void Run_rejects_invalid_utf8_without_silently_replacing_characters()
    {
        var inputPath = _directory.WriteInput(string.Empty);
        File.WriteAllBytes(inputPath, [0xc3, 0x28, 0x20, 0x41]);
        using var stdout = new StringWriter();

        var exception = Assert.Throws<NameSorterException>(() => NameSorterApplication.Run(inputPath, _directory.Path, stdout));

        Assert.Contains("Cannot read input file", exception.Message);
        Assert.Empty(stdout.ToString());
        Assert.False(File.Exists(_directory.OutputPath));
    }

    [Fact]
    public void Run_reads_all_input_before_overwriting_the_same_file()
    {
        File.WriteAllText(_directory.OutputPath, AssessmentSample.Input);
        using var stdout = new StringWriter();

        NameSorterApplication.Run(_directory.OutputPath, _directory.Path, stdout);

        Assert.Equal(AssessmentSample.Expected, stdout.ToString());
        Assert.Equal(stdout.ToString(), File.ReadAllText(_directory.OutputPath));
    }

    [Fact]
    public void Run_preserves_malformed_input_when_it_is_also_the_output_file()
    {
        const string input = "Alex Smith\nRoman\n";
        File.WriteAllText(_directory.OutputPath, input);
        var originalBytes = File.ReadAllBytes(_directory.OutputPath);
        using var stdout = new StringWriter();

        var exception = Assert.Throws<NameSorterException>(
            () => NameSorterApplication.Run(_directory.OutputPath, _directory.Path, stdout));

        Assert.Equal("Invalid name at line 2: expected 1-3 given names followed by a surname.", exception.Message);
        Assert.Empty(stdout.ToString());
        Assert.Equal(originalBytes, File.ReadAllBytes(_directory.OutputPath));
    }

    [Fact]
    public void Run_reports_an_unreadable_input_file()
    {
        using var stdout = new StringWriter();

        var exception = Assert.Throws<NameSorterException>(() => NameSorterApplication.Run(_directory.Path, _directory.Path, stdout));

        Assert.Contains("Cannot read input file", exception.Message);
        Assert.Empty(stdout.ToString());
        Assert.False(File.Exists(_directory.OutputPath));
    }

    [Fact]
    public void Run_reports_an_unwritable_output_without_printing_successful_output()
    {
        Directory.CreateDirectory(_directory.OutputPath);
        var inputPath = _directory.WriteInput("Alex Smith");
        using var stdout = new StringWriter();

        var exception = Assert.Throws<NameSorterException>(() => NameSorterApplication.Run(inputPath, _directory.Path, stdout));

        Assert.Contains("Cannot write output file", exception.Message);
        Assert.Empty(stdout.ToString());
    }

    private string Run(string input)
    {
        var inputPath = _directory.WriteInput(input);
        using var stdout = new StringWriter();
        NameSorterApplication.Run(inputPath, _directory.Path, stdout);
        return stdout.ToString();
    }

    public void Dispose() => _directory.Dispose();
}
