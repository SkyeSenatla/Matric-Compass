namespace Infrastructure.Data;

using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class EfUserRepository : IUserRepository
{
    private readonly MatricCompassDbContext _dbContext;

    public EfUserRepository(MatricCompassDbContext dbContext) => _dbContext = dbContext;

    public async Task<AppUser?> GetByEmailAsync(string email)
    {
        var normalized = AppUser.NormalizeEmail(email);
        return await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalized);
    }

    public async Task<AppUser?> GetByIdAsync(Guid id) =>
        await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public async Task<AppUser> AddAsync(AppUser user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }
}
