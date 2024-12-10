
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Automation.TestFramework
{

    public class ConnectionBridgeApi
    {

        //---------------------------------------------------------------------
        //  create with config 
        //      config should contain connection details ...
        
        //---------------------------------------------------------------------
        private readonly ConnectionBridgeConfiguration mConfiguration;

        //---------------------------------------------------------------------
        public ConnectionBridgeApi(ConnectionBridgeConfiguration configuration)
        {
            mConfiguration = configuration;
        }
        
        //---------------------------------------------------------------------
        public async Task<int> CreateBridgeAsync(Guid bridgeId, string accessKey)
        {
            HttpClient client = new HttpClient();

            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri  = new Uri($"{mConfiguration.Scheme}://{mConfiguration.Host}:{mConfiguration.Port}/v1/connection/{bridgeId}"),
                Method      = HttpMethod.Put,
                
            };

            Dictionary<string, object> content = new Dictionary<string, object>
            {
                { "name", "test" },
                { "description", "test" },
                { "access_key", accessKey }
            };
            
            request.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.SendAsync(request);

            //  need to decode response body

            return 0;
        }

        //---------------------------------------------------------------------
        public Task<int> DestroyBridgeAsync(Guid bridgeId)
        {
            return Task.FromResult(0);
        }

        //---------------------------------------------------------------------
        public Uri GetBridgeUrl(Guid bridgeId)
        {
            Uri connectionBridgeUri = new UriBuilder()
            {
                Scheme  = "ws",
                Host    = mConfiguration.Host,
                Port    = mConfiguration.Port,
                Path    = $"/ws/{bridgeId}"
            }.Uri;

            return connectionBridgeUri;
        }

    }

}
