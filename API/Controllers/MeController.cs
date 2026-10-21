using System.Security.Claims;
using API.Auth;
using API.Common;
using API.Models;
using API.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace API.Controllers;

// Week 6 Day 3: endpoints about "the signed-in user", shaped for screens.
// There's no id in the URL — the id comes from the token — so there's
// nothing to tamper with and no resource-based check to forget.
//
// Week 7 Day 3: any signed-in user may ask "who am I?" (the frontend needs
// it to choose a learner or a staff screen), so LearnerOnly moved from the
// class onto the dashboard action, the one endpoint that needs it.
[ApiController]
[Route(ApiRoutes.V1 + "/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly LearnerDashboardService _dashboardService;

    public MeController(LearnerDashboardService dashboardService) => _dashboardService = dashboardService;

    /// <summary>Who the signed-in user is, and their role.</summary>
    /// <remarks>
    /// The frontend uses the role to choose which screen to show. That's a
    /// convenience, not security: every endpoint still checks the role itself.
    /// </remarks>
    /// <response code="200">The signed-in user.</response>
    /// <response code="401">No valid access token.</response>
    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<MeResponse> GetMe() =>
        // Straight from the token: no database lookup, and nothing the
        // caller can change.
        Ok(new MeResponse(
            User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "",
            User.FindFirstValue(AppClaims.Role) ?? ""));

    /// <summary>Everything the learner dashboard shows, in one response.</summary>
    /// <response code="200">The signed-in learner's dashboard.</response>
    /// <response code="401">No valid access token.</response>
    /// <response code="403">Signed in, but not as a learner.</response>
    [HttpGet("dashboard")]
    [Authorize(Policy = Policies.LearnerOnly)]
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
