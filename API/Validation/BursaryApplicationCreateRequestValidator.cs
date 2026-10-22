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
        // Week 7 Day 4: these messages now appear next to form fields, so
        // they're written for the person filling in the form.
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Choose a student.");
        RuleFor(x => x.Funder).NotEmpty().WithMessage("Enter the funder's name.");
        // Week 7 Day 4: the column had no limit, so a 10 000-character
        // funder was accepted. The form doesn't check this yet: it's the
        // rule the demo uses to show a server 400 landing on its field.
        RuleFor(x => x.Funder).MaximumLength(100).WithMessage("Funder names are 100 characters or fewer.");
        RuleFor(x => x.AmountInCents).GreaterThan(0).WithMessage("Enter an amount greater than R0.");
        RuleFor(x => x.Deadline).NotEmpty();
        RuleFor(x => x.RequiredDocuments).NotNull();
    }
}
