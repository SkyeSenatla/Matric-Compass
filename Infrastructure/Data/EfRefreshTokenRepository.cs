namespace Infrastructure.Data;

using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly MatricCompassDbContext _dbContext;

    public EfRefreshTokenRepository(MatricCompassDbContext dbContext) => _dbContext = dbContext;

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash) =>
        await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task AddAsync(RefreshToken token)
    {
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveRotationAsync(RefreshToken successor)
    {
        // The predecessor was loaded TRACKED by GetByHashAsync and Rotate()
        // set its RevokedAt — so this one SaveChangesAsync sends an UPDATE
        // (guarded by xmin) and an INSERT, in a single transaction.
        _dbContext.RefreshTokens.Add(successor);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RevokeFamilyAsync(Guid familyId, DateTime now) =>
        // ExecuteUpdateAsync runs immediately as one SQL UPDATE — it doesn't
        // wait for SaveChangesAsync, and nothing that throws afterwards can
        // roll it back. That's exactly what reuse detection needs.
        await _dbContext.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, (DateTime?)now));
}
