namespace Domain.Entities;

// Week 6 Day 1: the three kinds of people who use Matric Compass. Constants,
// not an enum, because [Authorize(Roles = ...)] needs a compile-time string —
// and because these exact strings travel inside every access token.
public static class Roles
{
    public const string Learner = "Learner";
    public const string Counsellor = "Counsellor";
    public const string Admin = "Admin";

    public static readonly IReadOnlyCollection<string> All = [Learner, Counsellor, Admin];
}
