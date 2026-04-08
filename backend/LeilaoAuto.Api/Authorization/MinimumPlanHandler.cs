using System.Security.Claims;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace LeilaoAuto.Api.Authorization;

public sealed class MinimumPlanHandler : AuthorizationHandler<MinimumPlanRequirement>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<MinimumPlanHandler> _logger;

    public MinimumPlanHandler(IUserRepository userRepository, ILogger<MinimumPlanHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumPlanRequirement requirement)
    {
        var userIdRaw = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return;
        }

        var cancellationToken = GetCancellationToken(context.Resource);
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: false, cancellationToken);
        if (user is null)
        {
            return;
        }

        if (user.Plan.IsAtLeast(requirement.MinimumPlan))
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogInformation(
            "Access denied for user {UserId}. CurrentPlan={CurrentPlan}, RequiredPlan={RequiredPlan}.",
            user.Id,
            user.Plan,
            requirement.MinimumPlan);
    }

    private static CancellationToken GetCancellationToken(object? resource)
    {
        return resource switch
        {
            HttpContext httpContext => httpContext.RequestAborted,
            Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext filterContext => filterContext.HttpContext.RequestAborted,
            _ => CancellationToken.None
        };
    }
}
