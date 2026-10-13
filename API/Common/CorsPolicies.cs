namespace API.Common;

// Week 6 Day 3: the CORS policy name, in one place. Program.cs registers it
// and applies it; nothing else needs to know the string.
public static class CorsPolicies
{
    public const string Frontend = "Frontend";
}
