using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Https;

namespace RemoteManagementLibrary
{
    public class RemoteManagementHTTPSClient
    {
        private HttpsClient httpsClient;
        public int debug;

        public RemoteManagementHTTPSClient()
        {
            httpsClient = new HttpsClient();
        }

        //Dispatch requests
        public void post(string serverUrl, string requestUrl, string body)
        {
            httpsClient.HostVerification = false;
            httpsClient.PeerVerification = false;
            HttpsClientRequest request = new HttpsClientRequest();            
            request.KeepAlive = false;
            request.Header.ContentType = "text/plain";
            request.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Post;
            request.ContentSource = ContentSource.ContentString;
            request.ContentString = body;
            String requestString =  serverUrl + requestUrl;
            request.Url.Parse(requestString);                        
            try
            {
                if (debug > 0)
                {
                    CrestronConsole.PrintLine("\nhttp trying get request: " + requestString);
                }                
                httpsClient.DispatchAsync(request, OnHttpsClientResponseCallback);
            }
            catch (Exception exception)
            {
                CrestronConsole.PrintLine("\nhttps exception with requestString: {0} and exception: {1}", requestString, exception.Message);
            }
        }

        //Handle client responses       
        public void OnHttpsClientResponseCallback(HttpsClientResponse response, HTTPS_CALLBACK_ERROR error)
        {
            if (debug > 0)
            {
                CrestronConsole.PrintLine("\nhttps OnHTTPClientResponseCallback: " + response.ContentString);
                if (error > 0)
                {
                    CrestronConsole.PrintLine("http error:\n" + error);
                }
            }      
            
        }        
    }
}