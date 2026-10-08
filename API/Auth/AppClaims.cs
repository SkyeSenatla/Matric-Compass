namespace API.Auth;

using System.Security.Claims;

// The claim names this API puts in its tokens. Short, standard JWT names
// ("sub", "role") — MapInboundClaims = false in Program.cs keeps them that
// way instead of renaming them to long Microsoft URIs on the way in.
public static class AppClaims
{
    public const string Role = "role";
    public const string StudentId = "student_id";

    // The student record this caller owns, if they're a learner.
    public static Guid? GetStudentId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(StudentId), out var id) ? id : null;
}
