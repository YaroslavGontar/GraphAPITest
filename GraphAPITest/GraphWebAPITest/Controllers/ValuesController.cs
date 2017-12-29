using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphWebAPITest.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;

namespace GraphWebAPITest.Controllers
{

    [Authorize(Policy = "Admin")]
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        AzureAdOptions _azureAdOptions;
        private ILogger<ValuesController> _logger;

        public ValuesController(ILogger<ValuesController> logger, IOptions<AzureAdOptions> azureAdOptions)
        {
            _azureAdOptions = azureAdOptions.Value ?? throw new Exception("AzureAdOptions is not initialized for class ValuesController."); ;
            _logger = logger ?? throw new Exception("Logger is not initialized for class ValuesController.");
        }

        [HttpGet]
        public async Task<IEnumerable<string>> Get()
        {
            _logger.LogInformation("Check Token from logged user.");
            await AuthenticationHelper.CheckToken(User.Identity as ClaimsIdentity, _azureAdOptions);

            _logger.LogInformation("Get Assigned roles to me.");

            try
            {
                //_azureAdOptions.TenantId
                ActiveDirectoryClient client = AuthenticationHelper.GetActiveDirectoryClient(_azureAdOptions.TenantId);
                var roles = await client.Me.AppRoleAssignments.ExecuteAsync();

                return roles.CurrentPage.Select(ARole => $"{ARole.Id} - PrincipalId:{ARole.PrincipalId} - PrincipalDisplayName:{ARole.PrincipalDisplayName} - PrincipalType:{ARole.PrincipalType} - ResourceDisplayName:{ARole.ResourceDisplayName} - ResourceId:{ARole.ResourceId}");
            }
            catch (WebException ex)
            {
                _logger.LogError($"WebException:{ex}");
                throw ex;
            }
            //return users.CurrentPage.Select(user => $"{user.GivenName} {user.ObjectId}");
            //return new string[] { me.GivenName, me.ObjectId };
        }

        [HttpGet]
        [Route("Roles")]
        public async Task<IEnumerable<string>> Roles()
        {
            _logger.LogInformation("Check Token from logged user.");
            await AuthenticationHelper.CheckToken(User.Identity as ClaimsIdentity, _azureAdOptions);

            _logger.LogInformation("Get Application Roles.");
            try
            {
                ActiveDirectoryClient client = AuthenticationHelper.GetActiveDirectoryClient(_azureAdOptions.TenantId);
                var app = await client.Applications.GetByObjectId(_azureAdOptions.ClientId).ExecuteAsync();

                return app.AppRoles.Select(appRole => $"{appRole.Id} {appRole.Value}");
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("GetAllClaims")]
        public IEnumerable<string> GetAllClaims()
        {
            return User.Claims.Select(role => $"{role.Issuer}-{role.OriginalIssuer}-{role.Subject}-{role.Type}-{role.Value}-{role.ValueType}");
        }

        [Authorize(Policy = "Admin", Roles = "Admin")]
        [HttpGet]
        [Route("TestAdminRole")]
        public IEnumerable<string> TestAdminRole()
        {
            if (!User.IsInRole("Admin")) return new string[] { "Current user is not in Admin role." };

            var adminRole = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
            if(adminRole != null)
            {
                return new string[] { $"{adminRole.Issuer}-{adminRole.OriginalIssuer}-{adminRole.Subject}-{adminRole.Type}-{adminRole.Value}-{adminRole.ValueType}" };
            } else
            {
                _logger.LogWarning("Admin role not found!");
                return new string[] { "Admin role not found!" };
            }
        }

        [Authorize(Policy = "Admin", Roles = "Viewer")]
        [HttpGet]
        [Route("TestViewerRole")]
        public IEnumerable<string> TestViewerRole()
        {
            if (!User.IsInRole("Viewer")) return new string[] { "Current user is not in Viewer role." };

            var viewerRole = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Role && claim.Value == "Viewer");
            if (viewerRole != null)
            {
                return new string[] { $"{viewerRole.Issuer}-{viewerRole.OriginalIssuer}-{viewerRole.Subject}-{viewerRole.Type}-{viewerRole.Value}-{viewerRole.ValueType}" };
            }
            else
            {
                _logger.LogWarning("Viewer role not found!");
                return new string[] { "Viewer role not found!" };
            }
        }

        // POST api/values
        [HttpPost]
        public async Task Post([FromBody]string value)
        {
            _logger.LogInformation("Check Token from logged user.");
            await AuthenticationHelper.CheckToken(User.Identity as ClaimsIdentity, _azureAdOptions);

            _logger.LogInformation("Try to add Admin role for me.");

            ActiveDirectoryClient client = AuthenticationHelper.GetActiveDirectoryClient(_azureAdOptions.TenantId);
            IAppRoleAssignment appRoleAssignment = new AppRoleAssignment()
            {
                CreationTimestamp = DateTime.Now,
                Id = Guid.Parse(value),
                PrincipalDisplayName = "ygontar@objectivity.co.uk Gontar",
                PrincipalId = Guid.Parse("c3e1f4c6-b4f9-45c1-a4f7-280346295994"),
                PrincipalType = "User",
                ResourceDisplayName = "GrathWebAPITest",
                ResourceId = Guid.Parse("bfa79360-7eac-4bc3-81f2-459ea1ff9f3f")
            };
            await client.Me.AppRoleAssignments.AddAppRoleAssignmentAsync(appRoleAssignment);
        }
    }
}
