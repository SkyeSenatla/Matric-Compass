namespace API.Validation;

using API.Models;
using FluentValidation;

public class AptitudeTestCreateRequestValidator : AbstractValidator<AptitudeTestCreateRequest>
{
    public AptitudeTestCreateRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.TestType).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(0, 100);
    }
}
