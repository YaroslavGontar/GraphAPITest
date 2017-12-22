using System;
using System.Threading.Tasks;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System.Configuration;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace GraphWebAPITest.Authentication
{
    internal class AuthenticationHelper
    {
        public static string token;

        /// <summary>
        ///     Async task to acquire token for Application.
        /// </summary>
        /// <returns>Async Token for application.</returns>
        public static async Task<string> AcquireTokenAsync()
        {
            if (token == null || string.IsNullOrEmpty(token))
            {
                throw new Exception("Authorization Required.");
            }
            return token;
        }

        /// <summary>
        ///     Get Active Directory Client for Application.
        /// </summary>
        /// <returns>ActiveDirectoryClient for Application.</returns>
        public static ActiveDirectoryClient GetActiveDirectoryClient(string tenantId)
        {
            Uri baseServiceUri = new Uri(new Uri(Constants.ResourceUrl), tenantId);
            ActiveDirectoryClient activeDirectoryClient =
                new ActiveDirectoryClient(baseServiceUri, async () => await AcquireTokenAsync());
            return activeDirectoryClient;
        }

        public static async Task CheckToken(ClaimsIdentity identity, AzureAdOptions azureAdOptions)
        {
            if (!string.IsNullOrEmpty(token)) return;

            ClientCredential clientCred = new ClientCredential(azureAdOptions.ClientId, azureAdOptions.ClientSecret);

            string userAccessToken = identity.BootstrapContext as string;
            string userName = identity.Name;
            UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer", userName);

            string authority = $"{azureAdOptions.Instance}{azureAdOptions.Domain}";
            AuthenticationContext authContext = new AuthenticationContext(authority);

            var result = await authContext.AcquireTokenAsync(Constants.ResourceUrl, clientCred, userAssertion);
            token = result.AccessToken;
        }
    }
}
