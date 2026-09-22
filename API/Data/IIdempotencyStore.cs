namespace API.Data;

// One stored outcome per Idempotency-Key. RequestHash exists to catch a
// key being reused for a genuinely DIFFERENT request — that's a bug to
// reject, not a retry to honor.
public record IdempotencyRecord(string RequestHash, object? ResponseBody);

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(string key);
    Task<bool> TryReserveAsync(string key); // false = another request already holds this key
    Task SaveAsync(string key, IdempotencyRecord record);
}
