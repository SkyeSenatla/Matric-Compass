namespace API.Auth;

using Domain.Entities;
using Microsoft.AspNetCore.Authorization;

// Named policies. A controller says WHAT it needs ("staff only"); this file
// is the one place that says HOW that's decided. Change the rule here and
// every endpoint using the policy follows.
public static class Policies
{
    public const string StaffOnly = "StaffOnly";
    public const string StudentDataAccess = "StudentDataAccess";
}

// "May this caller touch data belonging to this student?" — a question no
// role can answer on its own, because the answer depends on WHICH student.
public class StudentDataAccessRequirement : IAuthorizationRequirement;

// Resource-based: the resource is the id of the student who owns the data
// (a Student's own Id, or a BursaryApplication's StudentId).
public class StudentDataAccessHandler : AuthorizationHandler<StudentDataAccessRequirement, Guid>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, StudentDataAccessRequirement requirement, Guid owningStudentId)
    {
        // Staff can see every learner's data.
        if (context.User.IsInRole(Roles.Counsellor) || context.User.IsInRole(Roles.Admin))
            context.Succeed(requirement);

        // A learner can see their own, and nobody else's.
        else if (context.User.GetStudentId() == owningStudentId)
            context.Succeed(requirement);

        // No Fail() call: not succeeding IS failing, and leaving it unset lets
        // another handler for the same requirement still succeed.
        return Task.CompletedTask;
    }
}
