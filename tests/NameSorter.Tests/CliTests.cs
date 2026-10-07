using System.Diagnostics;
using System.Text;

namespace NameSorter.Tests;

public sealed class CliTests : IDisposable
{
    private readonly TestDirectory _directory = new();

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Invalid_argument_count_returns_failure_and_usage(int argumentCount)
    {
        var result = await Invoke(Enumerable.Repeat("input.txt", argumentCount).ToArray());

        AssertFailure(result, "Usage: name-sorter <input-file>");
        Assert.False(File.Exists(_directory.OutputPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_argument_returns_failure_and_usage(string argument)
    {
        AssertFailure(await Invoke(argument), "Usage: name-sorter <input-file>");
    }

    [Fact]
    public async Task Missing_input_returns_failure()
    {
        AssertFailure(await Invoke("does-not-exist.txt"), "Cannot read input file");
        Assert.False(File.Exists(_directory.OutputPath));
    }

    [Fact]
    public async Task Unreadable_input_returns_failure()
    {
        AssertFailure(await Invoke(_directory.Path), "Cannot read input file");
        Assert.False(File.Exists(_directory.OutputPath));
    }

    [Fact]
    public async Task Malformed_input_returns_failure_and_preserves_previous_output()
    {
        var inputPath = _directory.WriteInput("Alex Smith\n\nRoman\n");
        File.WriteAllText(_directory.OutputPath, "previous output");

        AssertFailure(await Invoke(inputPath), "Invalid name at line 3: expected 1-3 given names followed by a surname.");
        Assert.Equal("previous output", File.ReadAllText(_directory.OutputPath));
    }

    [Fact]
    public async Task Unwritable_output_returns_failure()
    {
        var inputPath = _directory.WriteInput("Alex Smith\n");
        Directory.CreateDirectory(_directory.OutputPath);

        AssertFailure(await Invoke(inputPath), "Cannot write output file");
    }

    [Fact]
    public async Task Assessment_sample_returns_zero_and_exact_output_in_the_working_directory()
    {
        var sourceDirectory = Directory.CreateDirectory(System.IO.Path.Combine(_directory.Path, "source files"));
        var inputPath = System.IO.Path.Combine(sourceDirectory.FullName, "names with spaces.txt");
        File.WriteAllText(inputPath, AssessmentSample.Input);

        var result = await Invoke(System.IO.Path.GetRelativePath(_directory.Path, inputPath));

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StandardError);
        Assert.Equal(AssessmentSample.Expected, result.StandardOutput);
        Assert.Equal(Encoding.UTF8.GetBytes(result.StandardOutput), File.ReadAllBytes(_directory.OutputPath));
        Assert.False(File.Exists(System.IO.Path.Combine(sourceDirectory.FullName, NameSorterApplication.OutputFileName)));
    }

    [Fact]
    public async Task Successful_run_overwrites_existing_output()
    {
        var inputPath = _directory.WriteInput("José Alvarez\nZoë Müller\n");
        File.WriteAllText(_directory.OutputPath, new string('x', 500));

        var result = await Invoke(inputPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StandardError);
        Assert.Equal("José Alvarez\nZoë Müller\n", result.StandardOutput);
        Assert.Equal(Encoding.UTF8.GetBytes(result.StandardOutput), File.ReadAllBytes(_directory.OutputPath));
    }

    [Fact]
    public async Task Empty_input_returns_zero_and_creates_an_empty_output_file()
    {
        var result = await Invoke(_directory.WriteInput(string.Empty));

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.Empty(result.StandardError);
        Assert.Empty(File.ReadAllBytes(_directory.OutputPath));
    }

    [Fact]
    public async Task Ten_thousand_names_are_sorted_by_surname_and_every_given_name()
    {
        // Generate the expected order directly so the oracle does not use the comparer under test.
        var expectedNames = (
            from surname in Enumerable.Range(0, 10)
            from first in Enumerable.Range(0, 10)
            from second in Enumerable.Range(0, 10)
            from third in Enumerable.Range(0, 10)
            select $"Given{first} Middle{second} Third{third} Surname{surname}").ToArray();
        var inputPath = _directory.WriteInput(string.Join('\n', expectedNames.Reverse()));
        var expected = string.Join('\n', expectedNames) + "\n";

        var result = await Invoke(inputPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StandardError);
        Assert.Equal(expected, result.StandardOutput);
        Assert.Equal(Encoding.UTF8.GetBytes(expected), File.ReadAllBytes(_directory.OutputPath));
    }

    private async Task<CliResult> Invoke(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            WorkingDirectory = _directory.Path,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(typeof(PersonName).Assembly.Location);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the CLI.");
        try
        {
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await process.WaitForExitAsync(timeout.Token);
            return new CliResult(process.ExitCode, await stdout, await stderr);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    private static void AssertFailure(CliResult result, string message)
    {
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.Contains(message, result.StandardError);
        Assert.Single(result.StandardError.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
        Assert.DoesNotContain("Exception", result.StandardError);
        Assert.DoesNotContain("   at ", result.StandardError);
    }

    public void Dispose() => _directory.Dispose();

    private sealed record CliResult(int ExitCode, string StandardOutput, string StandardError);
}
