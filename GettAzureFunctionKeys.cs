using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AzFunction
{
    public class KeyMap
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public LinkMap[] Links { get; set; }
    }

    public class LinkMap
    {
        public string Rel { get; set; }
        public string Href { get; set; }
    }

    public class AzureFunctions
    {
        public static string FetchKey()
        {
            var credentials = new AzureCredentials(new ServicePrincipalLoginInformation { ClientId = clientId, ClientSecret = secret }, tenantId, AzureEnvironment.AzureGlobalCloud);

            var azure = Azure
            .Configure()
            .Authenticate(credentials)
            .WithDefaultSubscription();

            var webFunctionApp = azure.AppServices.FunctionApps.GetByResourceGroup(resourceGroup, functionAppName);
            var ftpUsername = webFunctionApp.GetPublishingProfile().FtpUsername;
            var username = ftpUsername.Split('\\').ToList()[1];
            var password = webFunctionApp.GetPublishingProfile().FtpPassword;
            var base64Auth = Convert.ToBase64String(Encoding.Default.GetBytes($"{username}:{password}"));
            var apiUrl = new Uri($"https://{functionAppName}.scm.azurewebsites.net/api");
            var siteUrl = new Uri($"https://{functionAppName}.azurewebsites.net");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {base64Auth}");

                var result = client.GetAsync($"{apiUrl}/functions/admin/token").Result;
                JWT = result.Content.ReadAsStringAsync().Result.Trim('"'); 
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + JWT);
                var key = client.GetAsync($"{siteUrl}/admin/host/keys/default");  
                key.Wait();
                var res = key.Result.Content.ReadAsStringAsync().Result;

                var keyMap = JsonConvert.DeserializeObject<KeyMap>(res);
                
                if (keyMap != null)
                {
                    return keyMap.Value;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}