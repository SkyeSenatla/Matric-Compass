namespace Domain.Repositories;

using Domain.Entities;

public interface IRefreshTokenRepository
{
    // Tracked: the caller is about to rotate (modify) what it gets back.
    Task<RefreshToken?> GetByHashAsync(string tokenHash);

    Task AddAsync(RefreshToken token);

    // Saves the successor AND the now-revoked predecessor in one
    // SaveChangesAsync — one implicit transaction. Never one without the other.
    Task SaveRotationAsync(RefreshToken successor);

    // Revokes every still-active token in the family, straight in the
    // database (one UPDATE), and commits immediately.
    Task RevokeFamilyAsync(Guid familyId, DateTime now);
}
