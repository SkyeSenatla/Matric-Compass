namespace API.Data;

using Domain.Entities;

// The shape every entity repository shares, introduced today for Student
// and reused as-is for TertiaryApplication and BursaryApplication. Week 4
// keeps this interface and every in-memory implementation inside the Api
// project; Week 5 Day 4 moves the interface into Domain and the
// implementation into a separate Infrastructure project — a bigger
// refactor for a later day, not this one.
public interface IRepository<T> where T : class, IEntity
{
    Task<IEnumerable<T>> GetAllAsync();
    // Week 5 Day 2: trackChanges defaults to false — every read-only caller
    // gets the cheaper AsNoTracking() path for free, and the call sites that
    // are about to mutate and save say so explicitly.
    Task<T?> GetByIdAsync(Guid id, bool trackChanges = false);
    Task<T> AddAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);
}
