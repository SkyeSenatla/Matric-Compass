namespace Domain.Exceptions;

// The base type nothing outside Domain needs to know the specifics of —
// a handler can catch THIS if it only cares "was this an expected domain
// failure, or something truly unexpected." Abstract: never thrown
// directly, only ever as one of its concrete subtypes below.
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
