using System;
using System.Threading.Tasks;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System.Configuration;
using System.IO;

namespace GrathWebAPITest.Authentication
{
    internal class Constants
    {
        public static string ResourceUrl = "https://graph.windows.net";
        public static string AuthString = "https://login.microsoftonline.com/{0}";
        public static string OAuth2Auth = "/oauth2/authorize";
    }

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

            activeDirectoryClient.Context.SendingRequest2 += Context_SendingRequest2;
            activeDirectoryClient.Context.ReceivingResponse += Context_ReceivingResponse;
            return activeDirectoryClient;
        }

        private static void Context_ReceivingResponse(object sender, System.Data.Services.Client.ReceivingResponseEventArgs e)
        {
            var strReader = new StreamReader(e.ResponseMessage.GetStream());
            var res = strReader.ReadToEnd();
            Console.WriteLine(res);
        }

        private static void Context_SendingRequest2(object sender, System.Data.Services.Client.SendingRequest2EventArgs e)
        {
            Console.WriteLine(e.RequestMessage.Url);
        }
    }
}
