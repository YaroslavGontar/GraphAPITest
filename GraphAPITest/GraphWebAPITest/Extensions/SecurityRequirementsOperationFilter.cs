using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace GraphWebAPITest
{
    // SecurityRequirementsOperationFilter.cs
    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        private readonly IOptions<AuthorizationOptions> authorizationOptions;

        public SecurityRequirementsOperationFilter(IOptions<AuthorizationOptions> authorizationOptions)
        {
            this.authorizationOptions = authorizationOptions;
        }

        public void Apply(Operation operation, OperationFilterContext context)
        {
            var controllerPolicies = context.ApiDescription.ControllerAttributes()
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy);
            var actionPolicies = context.ApiDescription.ActionAttributes()
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy);
            var policies = controllerPolicies.Union(actionPolicies).Distinct();
            var listPol = policies.ToList();
            var requiredClaimTypes = policies
                .Select(x => this.authorizationOptions.Value.GetPolicy(x))
                .SelectMany(x => x.Requirements)
                .OfType<ClaimsAuthorizationRequirement>()
                .Select(x => x.ClaimType);

            if (requiredClaimTypes.Any())
            {
                operation.Responses.Add("401", new Response { Description = "Unauthorized" });
                operation.Responses.Add("403", new Response { Description = "Forbidden" });

                operation.Security = new List<IDictionary<string, IEnumerable<string>>>();
                operation.Security.Add(
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "oauth2", requiredClaimTypes }
                    });
            }
        }
    }
}
