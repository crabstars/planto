namespace Planto.Exceptions;

internal class PlantoDbException : Exception
{
    public PlantoDbException()
    {
    }

    public PlantoDbException(string message)
        : base(message)
    {
    }

    public PlantoDbException(string message, Exception inner)
        : base(message, inner)
    {
    }
}