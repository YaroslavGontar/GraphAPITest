using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GrathWebAPITest.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace GrathWebAPITest.Controllers
{
    [Authorize(Policy = "Admin")]
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        AzureAdOptions _azureAdOptions;

        public ValuesController(IOptions<AzureAdOptions> azureAdOptions)
        {
            _azureAdOptions = azureAdOptions.Value;
        }

        [HttpGet]
        public async Task<IEnumerable<string>> Get()
        {
            await CheckToken();

            ActiveDirectoryClient client = AuthenticationHelper.GetActiveDirectoryClient(_azureAdOptions.TenantId);
            var users = await client.Users.ExecuteAsync();

            return users.CurrentPage.Select(user => $"{user.GivenName} {user.ObjectId}");
            //return new string[] { me.GivenName, me.ObjectId };
        }

        [HttpGet]
        [Route("Applications")]
        public async Task<IEnumerable<string>> Applications()
        {
            await CheckToken();
            try
            {
                ActiveDirectoryClient client = AuthenticationHelper.GetActiveDirectoryClient(_azureAdOptions.TenantId);
                var apps = await client.Applications.ExecuteAsync();

                return apps.CurrentPage.Select(app => $"{app.DisplayName} ({string.Concat(app.AppRoles.Select(appRole => $"{appRole.Id} {appRole.Value}"))}");
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }

        private async Task CheckToken()
        {
            if (!string.IsNullOrEmpty(AuthenticationHelper.token)) return;

            ClientCredential clientCred = new ClientCredential(_azureAdOptions.ClientId, _azureAdOptions.ClientSecret);
            var identity = User.Identity as ClaimsIdentity;
            string userAccessToken = identity.BootstrapContext as string;
            string userName = identity.Name;
            UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer", userName);

            string authority = $"{_azureAdOptions.Instance}{_azureAdOptions.TenantId}";
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
        [HttpPost]
        public void Post([FromBody]string value)
        {
            // For more information on protecting this API from Cross Site Request Forgery (CSRF) attacks, see https://go.microsoft.com/fwlink/?LinkID=717803
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
