namespace NameSorter;

public sealed class NameSorterException(string message, Exception? innerException = null)
    : Exception(message, innerException);
