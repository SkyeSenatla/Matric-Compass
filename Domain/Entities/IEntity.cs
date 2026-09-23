namespace Domain.Entities;

// The one thing every entity in this domain has in common: identity. This
// is what lets a single generic repository work across Student,
// TertiaryApplication, BursaryApplication and whatever comes next, without
// each one having to re-declare "here's how you find me by id."
public interface IEntity
{
    Guid Id { get; }
}
