using Microsoft.Identity.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AzureADConsole
{
    class Program
    {
        private static IPublicClientApplication app = null;
        static void Main(string[] args)
        {


            var tenantId = ConfigurationManager.AppSettings["tenantID"];
            if (string.IsNullOrEmpty(tenantId))
            {
                Console.WriteLine("Please enter the tenantID");
                tenantId = Console.ReadLine();
            }
            var clientId = ConfigurationManager.AppSettings["clientID"];
            if (string.IsNullOrEmpty(clientId))
            {
                Console.WriteLine("Please enter the clientID");
                clientId = Console.ReadLine();
            }

            Console.WriteLine("Please enter the username");
            var userName = Console.ReadLine();
            string[] scopes = { "User.Read" };
            var graphUrl = "https://graph.microsoft.com/v1.0/me";

            app = GetApplication(clientId, $"https://login.microsoftonline.com/{tenantId}");

            AuthenticationResult result = null;
            try
            {
                var resultTask = AcquireTokenDeviceCode(scopes);
                resultTask.Wait();
                result = resultTask.Result;
            }
            catch (MsalUiRequiredException)
            {
                var resultTask = AcquireTokenInteractive(scopes, userName);
                resultTask.Wait();
                result = resultTask.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }

            if (result != null)
            {
                Console.WriteLine("Successfully authenticated");
                var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, graphUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                var responseTask = httpClient.SendAsync(request);
                responseTask.Wait();
                var resultTask = responseTask.Result.Content.ReadAsStringAsync();
                resultTask.Wait();
                Console.WriteLine($"{resultTask.Result}");
            }
            else
            {
                Console.WriteLine("The authentication process failed");
            }
            Console.WriteLine("Process complete!. Press any key to exit");
            Console.ReadLine();


        }

        static async Task<AuthenticationResult> AcquireTokenDeviceCode(string[] scopes)
        {
            return await app.AcquireTokenWithDeviceCode(scopes, DeviceAuthenticationStatus).ExecuteAsync();
        }

        static IPublicClientApplication GetApplication(string clientId, string authority)
        {
            IPublicClientApplication app = PublicClientApplicationBuilder
                                                .Create(clientId)
                                                .WithAuthority(authority)
                                                .WithDefaultRedirectUri()
                                                .Build();

            return app;
        }

        static async Task DeviceAuthenticationStatus(DeviceCodeResult code)
        {
            Console.WriteLine($"{code.Message}");
            await Task.Delay(1000);
        }

        static async Task<AuthenticationResult> AcquireTokenInteractive(string[] scopes, string userName)
        {
            try
            {
                return await app.AcquireTokenInteractive(scopes)
                                .WithLoginHint(userName)
                                .WithPrompt(Prompt.SelectAccount)
                                .ExecuteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                throw;
            }
        }
    }
}
