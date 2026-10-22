namespace API.Common;

// Week 7 Day 3: one conversion from the domain's decimal rands to the
// integer cents every response sends. A JavaScript number is a binary
// double: whole cents are exact, R45 000.10 as a double is not.
//
// Week 7 Day 4: and back again, because requests now send cents too.
public static class Money
{
    public static long ToCents(decimal rands) => (long)Math.Round(rands * 100m);

    // Exact: decimal division by 100 never rounds.
    public static decimal ToRands(long cents) => cents / 100m;
}
