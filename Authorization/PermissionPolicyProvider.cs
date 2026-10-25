using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace RbacApi.Authorization
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }
        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // check if policy exists
            var policy = await base.GetPolicyAsync(policyName);
            if(policy != null)
            {
                return policy;
            }

            // create policy dynamically for this permission requirement
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
        }
    }
}
