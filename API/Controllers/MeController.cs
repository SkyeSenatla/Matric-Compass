using API.Auth;
using API.Common;
using API.Models;
using API.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// Week 6 Day 3: endpoints about "the signed-in user", shaped for screens.
// There's no id in the URL — the id comes from the token — so there's
// nothing to tamper with and no resource-based check to forget.
[ApiController]
[Route(ApiRoutes.V1 + "/me")]
[Authorize(Policy = Policies.LearnerOnly)]
public class MeController : ControllerBase
{
    private readonly LearnerDashboardService _dashboardService;

    public MeController(LearnerDashboardService dashboardService) => _dashboardService = dashboardService;

    /// <summary>Everything the learner dashboard shows, in one response.</summary>
    /// <response code="200">The signed-in learner's dashboard.</response>
    /// <response code="401">No valid access token.</response>
    /// <response code="403">Signed in, but not as a learner.</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(LearnerDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LearnerDashboardResponse>> GetDashboardAsync()
    {
        var studentId = User.GetStudentId()
            ?? throw new NotFoundException("This account has no learner record.");

        return Ok(await _dashboardService.GetAsync(studentId));
    }
}
