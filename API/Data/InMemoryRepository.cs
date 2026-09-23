namespace API.Data;

using Domain.Entities;

// One in-memory list per entity type. Every method here mirrors what
// InMemoryStudentRepository already did by hand on Day 1 — writing the
// generic version doesn't change the behavior, it removes the need to
// write this same List</T>/FirstOrDefault/Add pattern again for every new
// entity. IStudentRepository and IBursaryApplicationRepository both build
// on this instead of duplicating it.
public class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly List<T> _items;

    public InMemoryRepository(IEnumerable<T>? seed = null)
    {
        _items = seed?.ToList() ?? new List<T>();
    }

    public Task<IEnumerable<T>> GetAllAsync() =>
        Task.FromResult(_items.AsEnumerable());

    public Task<T?> GetByIdAsync(Guid id) =>
        Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task<T> AddAsync(T entity)
    {
        _items.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<bool> UpdateAsync(T entity)
    {
        var index = _items.FindIndex(x => x.Id == entity.Id);
        if (index == -1)
            return Task.FromResult(false);

        _items[index] = entity;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var item = _items.FirstOrDefault(x => x.Id == id);
        if (item is null)
            return Task.FromResult(false);

        _items.Remove(item);
        return Task.FromResult(true);
    }
}
