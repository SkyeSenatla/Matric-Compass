namespace Domain.Exceptions;

// Named to avoid colliding with FluentValidation's own ValidationException —
// this one means "well-formed, understood, rejected by a business rule,"
// never "the request was malformed."
public class UnprocessableEntityException : DomainException
{
    public UnprocessableEntityException(string message) : base(message) { }
}
