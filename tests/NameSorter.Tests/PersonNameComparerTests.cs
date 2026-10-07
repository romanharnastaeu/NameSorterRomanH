using System.Globalization;

namespace NameSorter.Tests;

public sealed class PersonNameComparerTests
{
    private readonly PersonNameComparer _comparer = new();

    [Theory]
    [InlineData("Zoe Archer", "Alex Smith")]
    [InlineData("Alex Smith", "Zoe Smith")]
    [InlineData("Alex Aaron Smith", "Alex John Smith")]
    [InlineData("Alex John Aaron Smith", "Alex John Peter Smith")]
    [InlineData("Alex Smith", "Alex John Smith")]
    [InlineData("Alex John Smith", "Alex John Peter Smith")]
    [InlineData("Alex Zoe Smith", "Bob Smith")]
    [InlineData("Alex Zed Smith", "Alex Alpha smith")]
    [InlineData("Alex Zed Smith", "alex Alpha Smith")]
    [InlineData("Alex John Zoe Smith", "Alex john Aaron Smith")]
    [InlineData("Alex John Peter Smith", "Alex John peter Smith")]
    [InlineData("Zoe Archer", "Alex bentley")]
    [InlineData("alex Smith", "Bob Smith")]
    [InlineData("Alex aaron Smith", "Alex Bob Smith")]
    [InlineData("Alex John aaron Smith", "Alex John Bob Smith")]
    [InlineData("José Alvarez", "Zoë Müller")]
    [InlineData("Jose\u0301 Alvarez", "José Alvarez")]
    [InlineData("Anne O'Connor", "Mary-Jane Watson")]
    public void Compare_orders_each_component_and_applies_its_case_tie_breaker(string earlier, string later)
    {
        var first = Parse(earlier);
        var second = Parse(later);

        Assert.True(_comparer.Compare(first, second) < 0);
        Assert.True(_comparer.Compare(second, first) > 0);
    }

    [Fact]
    public void Compare_returns_zero_for_identical_components()
    {
        Assert.Equal(0, _comparer.Compare(Parse("Alex John Smith"), Parse("Alex John Smith")));
    }

    [Fact]
    public void Compare_supports_the_standard_null_ordering_contract()
    {
        var name = Parse("Alex Smith");

        Assert.Equal(0, _comparer.Compare(null, null));
        Assert.Equal(0, _comparer.Compare(name, name));
        Assert.True(_comparer.Compare(null, name) < 0);
        Assert.True(_comparer.Compare(name, null) > 0);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    [InlineData("sv-SE")]
    public void Sort_is_independent_of_current_culture(string cultureName)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            string[] input = ["İpek Smith", "Zoë Smith", "Åke Smith", "ipek Smith", "Ipek Smith", "Zoe Smith"];

            Assert.Equal(
                ["Ipek Smith", "ipek Smith", "Zoe Smith", "Zoë Smith", "Åke Smith", "İpek Smith"],
                Sort(input));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Sort_matches_the_assessment_sample()
    {
        Assert.Equal(AssessmentSample.SortedLines, Sort(AssessmentSample.InputLines));
    }

    [Fact]
    public void Sort_retains_duplicate_records()
    {
        Assert.Equal(
            ["Marin Alvarez", "Alex Smith", "Alex Smith"],
            Sort(["Alex Smith", "Marin Alvarez", "Alex Smith"]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Sort_handles_sorted_and_reverse_sorted_input(bool reverse)
    {
        var expected = AssessmentSample.SortedLines;
        var input = reverse ? expected.Reverse() : expected;

        Assert.Equal(expected, Sort(input));
    }

    [Fact]
    public void Sort_handles_a_single_name()
    {
        Assert.Equal(["Marin Alvarez"], Sort(["Marin Alvarez"]));
    }

    [Fact]
    public void Sort_handles_an_empty_collection()
    {
        Assert.Empty(Sort([]));
    }

    private string[] Sort(IEnumerable<string> lines)
    {
        var names = lines.Select(Parse).ToList();
        names.Sort(_comparer);
        return names.Select(name => name.ToString()).ToArray();
    }

    private static PersonName Parse(string line) => Assert.IsType<PersonName>(NameParser.Parse(line, 1));
}
