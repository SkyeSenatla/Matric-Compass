namespace Infrastructure.Data;

using Domain.Entities;
using Domain.Repositories;

// One in-memory list per entity type. Every method here mirrors what
// InMemoryStudentRepository already did by hand on Day 1 — writing the
// generic version doesn't change the behavior, it removes the need to
// write this same List</T>/FirstOrDefault/Add pattern again for every new
// entity. IStudentRepository and IBursaryApplicationRepository both build
// on this instead of duplicating it.
// Day 4: backing storage is a Dictionary<Guid, T>, not a List<T> — purely
// an internal change. The public contract (GetAllAsync, GetByIdAsync,
// AddAsync, UpdateAsync, DeleteAsync) promises exactly what it promised
// before: find by id, delete returns whether it existed. Every test written
// against this class (unit and integration, Demos 3-5) still passes without
// modification, because none of them were coupled to how the repository
// stores data — only to what it guarantees. See Demo 6's talking points.
public class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly Dictionary<Guid, T> _items;

    public InMemoryRepository(IEnumerable<T>? seed = null)
    {
        _items = seed?.ToDictionary(x => x.Id) ?? new Dictionary<Guid, T>();
    }

    public Task<IEnumerable<T>> GetAllAsync() =>
        Task.FromResult(_items.Values.AsEnumerable());

    public Task<T?> GetByIdAsync(Guid id, bool trackChanges = false) =>
        // "trackChanges" is an EF Core concept with nothing to mean here —
        // accepted only so this class keeps satisfying the same interface
        // EfStudentRepository and EfBursaryApplicationRepository do. A leaky
        // abstraction, on purpose, named as one.
        Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

    public Task<T> AddAsync(T entity)
    {
        _items[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<bool> UpdateAsync(T entity)
    {
        if (!_items.ContainsKey(entity.Id))
            return Task.FromResult(false);

        _items[entity.Id] = entity;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id) => Task.FromResult(_items.Remove(id));
}
