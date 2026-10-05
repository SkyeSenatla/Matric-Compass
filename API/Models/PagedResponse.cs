namespace API.Models;

// AIP-158's response shape. NextPageToken is "" — not null, not absent —
// exactly when there are no more results; that's the ONLY end-of-list
// signal a client should trust. No TotalSize: counting every matching row
// can cost more than fetching the page itself, and nothing needs it yet.
public record PagedResponse<T>(IReadOnlyList<T> Items, string NextPageToken);
