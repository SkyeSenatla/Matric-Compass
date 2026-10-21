namespace API.Common;

// Week 7 Day 3: one conversion from the domain's decimal rands to the
// integer cents every response sends. A JavaScript number is a binary
// double: whole cents are exact, R45 000.10 as a double is not.
public static class Money
{
    public static long ToCents(decimal rands) => (long)Math.Round(rands * 100m);
}
