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
using GrathWebAPITest.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;

namespace GrathWebAPITest.Controllers
{
    
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

        [Authorize(Policy = "Admin")]
        [HttpGet]
        public async Task<IEnumerable<string>> Get()
        {
            _logger.LogInformation("Check Token from logged user.");
            await CheckToken();

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

        [Authorize(Policy = "Admin")]
        [HttpGet]
        [Route("Roles")]
        public async Task<IEnumerable<string>> Roles()
        {
            await CheckToken();
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

        [HttpGet]
        [Route("Version")]
        public string Version()
        {
            //RuntimeInformation.FrameworkDescription
            return RuntimeInformation.FrameworkDescription;
        }


        private async Task CheckToken()
        {
            if (!string.IsNullOrEmpty(AuthenticationHelper.token)) return;

            ClientCredential clientCred = new ClientCredential(_azureAdOptions.ClientId, _azureAdOptions.ClientSecret);
            var identity = User.Identity as ClaimsIdentity;
            string userAccessToken = identity.BootstrapContext as string;
            string userName = identity.Name;
            UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer", userName);

            string authority = $"{_azureAdOptions.Instance}{_azureAdOptions.Domain}";
            //string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            AuthenticationContext authContext = new AuthenticationContext(authority);

            var result = await authContext.AcquireTokenAsync(Authentication.Constants.ResourceUrl, clientCred, userAssertion);
            AuthenticationHelper.token = result.AccessToken;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [Authorize(Policy = "Admin")]
        [HttpPost]
        public async Task Post([FromBody]string value)
        {
            await CheckToken();

            //_azureAdOptions.TenantId
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

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
            // For more information on protecting this API from Cross Site Request Forgery (CSRF) attacks, see https://go.microsoft.com/fwlink/?LinkID=717803
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            // For more information on protecting this API from Cross Site Request Forgery (CSRF) attacks, see https://go.microsoft.com/fwlink/?LinkID=717803
        }
    }
}
