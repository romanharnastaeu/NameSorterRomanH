namespace NameSorter.Tests;

public sealed class PersonNameTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Constructor_rejects_unsupported_given_name_counts(int count)
    {
        Assert.Throws<ArgumentException>(() => new PersonName(Enumerable.Repeat("Alex", count), "Smith"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Alex John")]
    [InlineData("Alex\tJohn")]
    [InlineData(null)]
    public void Constructor_rejects_empty_or_multiple_components(string? component)
    {
        Assert.Throws<ArgumentException>(() => new PersonName([component!], "Smith"));
        Assert.Throws<ArgumentException>(() => new PersonName(["Alex"], component!));
    }

    [Fact]
    public void Constructor_copies_given_names_and_exposes_a_read_only_collection()
    {
        string[] givenNames = ["Alex", "John"];
        var name = new PersonName(givenNames, "Smith");

        givenNames[0] = "Changed";

        Assert.Equal(["Alex", "John"], name.GivenNames);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)name.GivenNames)[0] = "Changed");
        Assert.Equal("Alex John Smith", name.ToString());
    }
}
