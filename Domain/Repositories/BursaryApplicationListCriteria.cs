namespace Domain.Repositories;

using Domain.Entities;

// The allow-list. A client can sort by exactly these fields and nothing
// else — every value here is an index we've promised to keep fast.
public enum BursaryApplicationSort
{
    Deadline,
    Amount
}

// "The last row of the previous page" — keyset paging resumes AFTER this
// row instead of skipping N rows. Id is always part of it: it's the unique
// tiebreaker that makes the order deterministic when two applications share
// the same deadline or amount.
public record BursaryApplicationCursor(DateTime Deadline, decimal Amount, Guid Id);

// Already-validated input for one page. The controller/service layer turns
// raw query-string values into this; the repository never sees a string it
// has to parse.
public record BursaryApplicationListCriteria(
    Guid? StudentId,
    BursaryApplicationStatus? Status,
    BursaryApplicationSort OrderBy,
    BursaryApplicationCursor? After,
    int Take);
