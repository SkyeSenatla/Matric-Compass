namespace Domain.Repositories;

using Domain.Entities;

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email);
    Task<AppUser?> GetByIdAsync(Guid id);
    Task<AppUser> AddAsync(AppUser user);
}
