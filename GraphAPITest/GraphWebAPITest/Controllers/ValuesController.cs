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
        public async Task<IEnumerable<IAppRoleAssignment>> Get()
        {
            _logger.LogInformation("Check Token from logged user.");
            await AuthenticationHelper.CheckToken(User.Identity as ClaimsIdentity, _azureAdOptions);

            _logger.LogInformation("Get Assigned roles to me.");

            try
            {
                ActiveDirectoryClient client = AuthenticationHelper.GetActiveDirectoryClient(_azureAdOptions.TenantId);
                var roles = await client.Me.AppRoleAssignments.ExecuteAsync();

                return roles.CurrentPage;
            }
            catch (WebException ex)
            {
                _logger.LogError($"WebException:{ex}");
                throw ex;
            }
        }

        [HttpGet]
        [Route("Me")]
        public async Task<IEnumerable<AssignRole>> Me()
        {
            _logger.LogInformation("Check Token from logged user.");
            await AuthenticationHelper.CheckToken(User.Identity as ClaimsIdentity, _azureAdOptions);

            _logger.LogInformation("Get Application Me.");
            ActiveDirectoryClient client = AuthenticationHelper.GetActiveDirectoryClient(_azureAdOptions.TenantId);
            var me = await client.Me.ExecuteAsync();

            var obj = new AssignRole
            {
                PrincipalId = me.ObjectId,
                PrincipalDisplayName = me.DisplayName,
                PrincipalType = me.UserType
            };

            return new[] { obj };
        }

        [HttpGet]
        [Route("Groups")]
        public async Task<IEnumerable<AssignRole>> Groups()
        {
            _logger.LogInformation("Check Token from logged user.");
            await AuthenticationHelper.CheckToken(User.Identity as ClaimsIdentity, _azureAdOptions);

            _logger.LogInformation("Get Application Groups.");
            ActiveDirectoryClient client = AuthenticationHelper.GetActiveDirectoryClient(_azureAdOptions.TenantId);
            var groups = await client.Groups.ExecuteAsync();

            return groups.CurrentPage.Select(group => new AssignRole { PrincipalId = group.ObjectId, PrincipalDisplayName = group.DisplayName, PrincipalType = group.ObjectType });
        }

        [HttpGet]
        [Route("Roles")]
        public async Task<IEnumerable<AppRole>> Roles()
        {
            _logger.LogInformation("Check Token from logged user.");
            await AuthenticationHelper.CheckToken(User.Identity as ClaimsIdentity, _azureAdOptions);

            _logger.LogInformation("Get Application Roles.");
            ActiveDirectoryClient client = AuthenticationHelper.GetActiveDirectoryClient(_azureAdOptions.TenantId);
            var apps = await client.Applications.ExecuteAsync();

            return apps.CurrentPage.SelectMany(app => app.AppRoles);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("GetAllClaims")]
        public IEnumerable<SimpleClaim> GetAllClaims()
        {
            return User.Claims.Select(claim => new SimpleClaim
            {
                Issuer = claim.Issuer,
                OriginalIssuer = claim.OriginalIssuer,
                Type = claim.Type,
                Value = claim.Value,
                ValueType = claim.ValueType
            });
        }

        [Authorize(Policy = "Admin", Roles = "Admin")]
        [HttpGet]
        [Route("TestAdminRole")]
        public IEnumerable<string> TestAdminRole()
        {
            if (!User.IsInRole("Admin")) return new string[] { "Current user is not in Admin role." };

            var adminRole = User.Claims.FirstOrDefault(claim => claim.Type == "roles" && claim.Value == "Admin");
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

            var viewerRole = User.Claims.FirstOrDefault(claim => claim.Type == "roles" && claim.Value == "Viewer");
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
        public async Task Post([FromBody]AssignRole value)
        {
            _logger.LogInformation("Check Token from logged user.");
            await AuthenticationHelper.CheckToken(User.Identity as ClaimsIdentity, _azureAdOptions);

            _logger.LogInformation("Try to add Admin role for me.");

            ActiveDirectoryClient client = AuthenticationHelper.GetActiveDirectoryClient(_azureAdOptions.TenantId);

            IAppRoleAssignment appRoleAssignment = new AppRoleAssignment()
            {
                CreationTimestamp = DateTime.Now,
                Id = Guid.Parse(value.RoleId),
                PrincipalDisplayName = value.PrincipalDisplayName,
                PrincipalId = Guid.Parse(value.PrincipalId),
                PrincipalType = value.PrincipalType,
                ResourceDisplayName = "GrathWebAPITest",
                ResourceId = Guid.Parse("bfa79360-7eac-4bc3-81f2-459ea1ff9f3f")
            };

            if (value.PrincipalType == "Group")
            {
                await client.Groups.GetByObjectId(value.PrincipalId).AppRoleAssignments.AddAppRoleAssignmentAsync(appRoleAssignment);
            }
            else
            {
                await client.Users.GetByObjectId(value.PrincipalId).AppRoleAssignments.AddAppRoleAssignmentAsync(appRoleAssignment);
            }
        }
    }

    public class SimpleClaim
    {
        public string Type { get; set; }
        public string OriginalIssuer { get; set; }
        public string Issuer { get; set; }
        public string ValueType { get; set; }
        public string Value { get; set; }
    }

    public class AssignRole
    {
        public string PrincipalId { get; set; }
        public string PrincipalType { get; set; }
        public string PrincipalDisplayName { get; set; }
        public string RoleId { get; set; }
    }
}
