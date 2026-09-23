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
    Task<T?> GetByIdAsync(Guid id);
    Task<T> AddAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);
}
