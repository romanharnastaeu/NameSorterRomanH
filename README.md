# Name Sorter

**This README is the documentation for this submission.** It explains what the application does, how to run and work on it, and the assumptions behind its behaviour.

## Table of contents

- [Executive summary](#executive-summary)
- [Requirements](#requirements)
- [Build](#build)
- [Run](#run)
- [Test](#test)
- [Behaviour](#behaviour)
- [Design](#design)
- [Development](#development)
- [Assumptions](#assumptions)
- [Error handling](#error-handling)

## Executive summary

Name Sorter validates names from a text file, sorts them by surname and given names, and writes the results to stdout and a file. C# provides strong types that make the name rules explicit. .NET 10 LTS supplies file handling, Unicode strings, and comparison tools through its standard library, so the application needs no external packages. xUnit tests and GitHub Actions support verification as the code changes.

## Requirements

- .NET 10 SDK (LTS). `global.json` selects an installed stable .NET 10 SDK.

Make `dotnet` available in your terminal and run the commands below from the repository root, which contains `NameSorter.sln`. Input must be a text file with one person per line; CSV files must first be converted to that format.

## Build

```sh
dotnet restore --locked-mode
dotnet build --no-restore --configuration Release
```

## Run

From the repository root, using the included assessment sample:

```sh
dotnet run --project src/NameSorter -- ./unsorted-names-list.txt
```

Replace `./unsorted-names-list.txt` with your input path to sort another file. Quote paths containing spaces. The output is always `sorted-names-list.txt` in the **current working directory**, even when the input is elsewhere. The included sample produces 11 sorted names.

To publish a framework-dependent executable for your current platform:

```sh
dotnet publish src/NameSorter --configuration Release --output .artifacts/publish
```

Run `.artifacts/publish/name-sorter ./unsorted-names-list.txt` on Linux/macOS, or `.\.artifacts\publish\name-sorter.exe .\unsorted-names-list.txt` on Windows. The target machine needs the .NET 10 runtime; adding the publish directory to `PATH` allows `name-sorter <input-file>`.

## Test

```sh
dotnet test
```

The xUnit suite covers domain invariants, parsing, comparison (including different cultures), temporary-file integration, the exact assessment sample, and actual CLI processes with up to 10,000 generated names. Edge cases include line endings, missing final newlines, and using the same file for input and output. Temporary directories are cleaned up; tests never change the process-wide working directory.

## Behaviour

Each person has 1–3 given names followed by exactly one surname. Names are sorted by surname, then by each given name in order. Every component is compared with `OrdinalIgnoreCase`, immediately followed by an `Ordinal` case tie-breaker. When all shared components match, the shorter given-name sequence comes first.

Blank lines are ignored, repeated whitespace is normalized to single ASCII spaces, and duplicates are retained. Spelling, Unicode, apostrophes, and hyphens are preserved. Malformed non-empty records fail the entire run with a one-based source line number.

Successful runs print only the sorted names to stdout and overwrite `sorted-names-list.txt` with the same text. Both use UTF-8 without a BOM and LF line endings, including a final newline for non-empty output. Empty input produces an empty file and no stdout.

## Design

- [PersonName](src/NameSorter/PersonName.cs) holds an immutable, structurally valid name and formats its components.
- [NameParser](src/NameSorter/NameParser.cs) normalizes whitespace and reports structural errors with source context.
- [PersonNameComparer](src/NameSorter/PersonNameComparer.cs) makes the component-by-component ordering policy explicit.
- [NameSorterApplication](src/NameSorter/NameSorterApplication.cs) reads, validates, sorts, and writes the results using standard file APIs and a `TextWriter` for stdout.
- [Program](src/NameSorter/Program.cs) handles arguments, exit codes, console encoding, and error presentation. [NameSorterException](src/NameSorter/NameSorterException.cs) carries expected failures to that boundary.

The implementation intentionally avoids unnecessary abstractions. Responsibilities are separated where doing so improves readability, testability, or isolates I/O boundaries. There are no application NuGet dependencies, custom I/O interfaces, or dependency injection containers.

## Development

Application code lives in [src/NameSorter](src/NameSorter), with tests in [tests/NameSorter.Tests](tests/NameSorter.Tests). Keep changes in the type responsible for the behaviour and add a regression test in the corresponding test class. Use temporary files for I/O tests and keep each test independent of the process-wide working directory.

```sh
dotnet restore --locked-mode
dotnet format --verify-no-changes --no-restore
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release
```

The [GitHub Actions workflow](.github/workflows/ci.yml) is configured to run these checks, including the entire test suite, on Windows and Linux for every push and pull request. Include the workflow and package lock files in the submission. Build output, temporary artifacts, and generated sorted output are excluded by `.gitignore`.

## Assumptions

- Each non-empty line represents one person with 1–3 given names; the final component is the surname. Compound surnames separated by whitespace are not supported.
- Repeated whitespace is normalized, blank lines are ignored, and duplicate records are retained.
- Malformed non-empty records fail the operation before stdout or an existing output file is changed.
- The output file is overwritten after successful validation, including when the result is empty.
- Comparison is deterministic and culture-independent; linguistic collation and Unicode normalization are not applied.
- Input is expected to be UTF-8. All names are held in memory, which is appropriate for approximately 1,000 records.

## Error handling

Success returns exit code `0`; failures return `1`. Missing/extra arguments print `Usage: name-sorter <input-file>`. Unreadable input, malformed names, and unwritable output produce concise messages on stderr without stack traces.

The file is written before stdout so file-write failures do not print a successful-looking result. File and console writes are not a transaction: an I/O failure during writing may leave partial file contents, and a subsequent stdout failure leaves the saved file in place.
