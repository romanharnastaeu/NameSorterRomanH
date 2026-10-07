namespace NameSorter;

public sealed class PersonName
{
    public PersonName(IEnumerable<string> givenNames, string surname)
    {
        ArgumentNullException.ThrowIfNull(givenNames);
        var components = givenNames.ToArray();

        if (components.Length is < 1 or > 3)
        {
            throw new ArgumentException("Expected 1-3 given names.", nameof(givenNames));
        }

        foreach (var component in components)
        {
            ValidateComponent(component, nameof(givenNames));
        }

        ValidateComponent(surname, nameof(surname));
        GivenNames = Array.AsReadOnly(components);
        Surname = surname;
    }

    public IReadOnlyList<string> GivenNames { get; }

    public string Surname { get; }

    public override string ToString() => $"{string.Join(" ", GivenNames)} {Surname}";

    private static void ValidateComponent(string component, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(component) || component.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Each name component must be non-empty and contain no whitespace.", parameterName);
        }
    }
}
