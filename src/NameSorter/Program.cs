using System.Text;

namespace NameSorter;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            Console.Error.WriteLine("Usage: name-sorter <input-file>");
            return 1;
        }

        try
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            NameSorterApplication.Run(args[0], Directory.GetCurrentDirectory(), Console.Out);
            return 0;
        }
        catch (NameSorterException exception)
        {
            Console.Error.WriteLine(exception.Message);
        }
        catch (IOException)
        {
            Console.Error.WriteLine("Cannot access the working directory or write to standard output.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine("Access denied to the working directory or standard output.");
        }
        catch (Exception)
        {
            Console.Error.WriteLine("An unexpected error prevented the names from being sorted.");
        }

        return 1;
    }
}
