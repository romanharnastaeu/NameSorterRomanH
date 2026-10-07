namespace NameSorter;

public static class NameParser
{
    public static PersonName? Parse(string line, int lineNumber)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentOutOfRangeException.ThrowIfLessThan(lineNumber, 1);

        // A null separator uses .NET's Unicode whitespace rules, including tabs.
        var components = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (components.Length == 0)
        {
            return null;
        }

        if (components.Length is < 2 or > 4)
        {
            throw new NameSorterException(
                $"Invalid name at line {lineNumber}: expected 1-3 given names followed by a surname.");
        }

        return new PersonName(components[..^1], components[^1]);
    }
}
