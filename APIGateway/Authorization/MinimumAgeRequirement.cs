using Microsoft.AspNetCore.Authorization;

namespace APIGateway.Authorization;

public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge { get; }

    public MinimumAgeRequirement(int minimumAge)
    {
        MinimumAge = minimumAge;
    }
}

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement)
    {
        var dateOfBirthClaim = context.User.FindFirst(c => c.Type == "date_of_birth")?.Value;
        if (dateOfBirthClaim != null && DateTime.TryParse(dateOfBirthClaim, out var dateOfBirth))
        {
            var userAge = DateTime.Today.Year - dateOfBirth.Year;
            if (dateOfBirth > DateTime.Today.AddYears(-userAge)) userAge--;

            if (userAge >= requirement.MinimumAge)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

public class ResourceAccessRequirement : IAuthorizationRequirement
{
}

public class ResourceAccessHandler : AuthorizationHandler<ResourceAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceAccessRequirement requirement)
    {
        var resourceId = context.Resource as HttpContext;
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var resourceOwnerId = resourceId?.Request.RouteValues["userId"]?.ToString();

        if (userId == resourceOwnerId || context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}