using Microsoft.AspNetCore.Authorization;

namespace RbacApi.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            PermissionRequirement requirement)
        {
            // if user is not authenticated, fail
            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                return Task.CompletedTask;
            }

            // check if the user has the required permission
            var hasPermission = context.User.Claims.Any(c => c.Type == "permission" && c.Value == requirement.Permission);
            
            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
