namespace API.Services;

using API.Models;

public interface IBursaryApplicationService
{
    Task<BursaryApplicationResponse> CreateAsync(BursaryApplicationCreateRequest request);
    Task<PagedResponse<BursaryApplicationResponse>> ListAsync(BursaryApplicationListRequest request);
}
