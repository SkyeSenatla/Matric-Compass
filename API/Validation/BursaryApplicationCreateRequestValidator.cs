namespace API.Validation;

using API.Models;
using FluentValidation;

// A separate class, not an attribute on the DTO — see the slide deck for
// why. This only checks the SHAPE of the request: it never touches a
// repository, never knows another bursary application exists.
public class BursaryApplicationCreateRequestValidator : AbstractValidator<BursaryApplicationCreateRequest>
{
    public BursaryApplicationCreateRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Funder).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Deadline).NotEmpty();
        RuleFor(x => x.RequiredDocuments).NotNull();
    }
}
