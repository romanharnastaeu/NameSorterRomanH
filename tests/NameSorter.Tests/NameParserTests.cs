namespace NameSorter.Tests;

public sealed class NameParserTests
{
    [Theory]
    [InlineData("Marin Alvarez", "Marin", "Alvarez")]
    [InlineData("Adonis Julius Archer", "Adonis Julius", "Archer")]
    [InlineData("Hunter Uriah Mathew Clarke", "Hunter Uriah Mathew", "Clarke")]
    public void Parse_separates_given_names_from_the_final_surname(string line, string givenNames, string surname)
    {
        var name = Assert.IsType<PersonName>(NameParser.Parse(line, 1));

        Assert.Equal(givenNames.Split(' '), name.GivenNames);
        Assert.Equal(surname, name.Surname);
        Assert.Equal(line, name.ToString());
    }

    [Theory]
    [InlineData("  Marin Alvarez")]
    [InlineData("Marin Alvarez  ")]
    [InlineData("Marin    Alvarez")]
    [InlineData("\tMarin\t\tAlvarez\t")]
    [InlineData("Marin\v\fAlvarez")]
    [InlineData("\u2003Marin\u00a0\u2002Alvarez\u2003")]
    public void Parse_normalizes_whitespace(string line)
    {
        Assert.Equal("Marin Alvarez", NameParser.Parse(line, 1)?.ToString());
    }

    [Theory]
    [InlineData("Mary-Jane Watson")]
    [InlineData("Anne O'Connor")]
    [InlineData("José Alvarez")]
    [InlineData("Zoë Müller")]
    [InlineData("Łukasz Kowalski")]
    [InlineData("李 王")]
    [InlineData("Jose\u0301 Alvarez")]
    public void Parse_preserves_component_spelling(string line)
    {
        Assert.Equal(line, NameParser.Parse(line, 1)?.ToString());
    }

    [Theory]
    [InlineData("Roman")]
    [InlineData("One Two Three Four Five")]
    public void Parse_rejects_malformed_names_with_the_source_line_number(string line)
    {
        var exception = Assert.Throws<NameSorterException>(() => NameParser.Parse(line, 17));

        Assert.Equal("Invalid name at line 17: expected 1-3 given names followed by a surname.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\v\f\u00a0\u2003")]
    public void Parse_returns_no_person_for_blank_lines(string line)
    {
        Assert.Null(NameParser.Parse(line, 1));
    }
}
