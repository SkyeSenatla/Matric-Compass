namespace API.Data;

using System.Collections.Concurrent;

// Week 4's stand-in, same spirit as InMemoryStudentRepository. A real
// implementation backs this with a database row and a UNIQUE constraint
// on Key — that constraint is what actually closes the race condition
// TryReserveAsync only approximates here with a ConcurrentDictionary.
public class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord?> _records = new();

    public Task<IdempotencyRecord?> FindAsync(string key) =>
        Task.FromResult(_records.TryGetValue(key, out var record) ? record : null);

    public Task<bool> TryReserveAsync(string key) =>
        Task.FromResult(_records.TryAdd(key, null)); // null = "reserved, still in flight"

    public Task SaveAsync(string key, IdempotencyRecord record)
    {
        _records[key] = record;
        return Task.CompletedTask;
    }
}
