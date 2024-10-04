namespace Planto.Exceptions;

internal class ColumnCheckException : Exception
{
    public ColumnCheckException()
    {
    }

    public ColumnCheckException(string message)
        : base(message)
    {
    }

    public ColumnCheckException(string message, Exception inner)
        : base(message, inner)
    {
    }
}