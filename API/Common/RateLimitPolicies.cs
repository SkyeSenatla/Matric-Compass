namespace API.Common;

// Week 6 Day 3: rate-limiting policy names. Program.cs owns the rules;
// endpoints only name the policy they need, same idea as Auth/Policies.cs.
public static class RateLimitPolicies
{
    public const string Login = "Login";
}
