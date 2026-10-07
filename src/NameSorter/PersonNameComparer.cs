namespace NameSorter;

public sealed class PersonNameComparer : IComparer<PersonName>
{
    public int Compare(PersonName? x, PersonName? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var result = CompareComponent(x.Surname, y.Surname);
        if (result != 0)
        {
            return result;
        }

        var sharedGivenNames = Math.Min(x.GivenNames.Count, y.GivenNames.Count);
        for (var index = 0; index < sharedGivenNames; index++)
        {
            result = CompareComponent(x.GivenNames[index], y.GivenNames[index]);
            if (result != 0)
            {
                return result;
            }
        }

        return x.GivenNames.Count.CompareTo(y.GivenNames.Count);
    }

    private static int CompareComponent(string x, string y)
    {
        var result = StringComparer.OrdinalIgnoreCase.Compare(x, y);
        return result != 0 ? result : StringComparer.Ordinal.Compare(x, y);
    }
}
