namespace API.Services;

using API.Models;

public interface IBursaryApplicationService
{
    Task<CreateBursaryApplicationResult> CreateAsync(BursaryApplicationCreateRequest request);
}
