using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace fbchatbot.Controllers
{
    [RoutePrefix("fbhook")]
    public class FacebookWebHookController : ApiController
    {
        private const string ACCESSTOKEN = "REPLACE_BY_YOUR_FACEBOOK_PAGE_APPLICATION_TOKEN";

        [Route("")]
        [HttpGet]
        [HttpPost]
        public async Task<HttpResponseMessage> FacebookWebhookEndpoint(HttpRequestMessage request)
        {
            HttpResponseMessage response;

            // for auth sign only.
            if (request.Method == HttpMethod.Get)
            {
                var qs = request.GetQueryNameValuePairs().ToDictionary(i => i.Key, i => i.Value);
                var mode = qs["hub.mode"];
                var token = qs["hub.verify_token"];
                var challenge = qs["hub.challenge"];

                if (!string.IsNullOrEmpty(mode) && !string.IsNullOrEmpty(token))
                {
                    if (mode == "subscribe" && token == ACCESSTOKEN)
                    {
                        response = request.CreateResponse(HttpStatusCode.OK);
                        response.Content = new StringContent(challenge);
                        return response;
                    }
                    else
                    {
                        response = request.CreateResponse(HttpStatusCode.Forbidden);
                        return response;
                    }
                }
            }

            try
            {
                var requestContent = await request.Content.ReadAsStringAsync();
                var oRequest = JObject.Parse(requestContent);
                System.Diagnostics.Trace.WriteLine(requestContent);

                var objectType = oRequest.Property("object").Value.ToString();

                if (objectType != "page")
                {
                    response = request.CreateResponse(HttpStatusCode.NotFound);
                    return response;
                }

                var oEntries = oRequest.Property("entry").Value as JArray;

                foreach (JObject oEntry in oEntries)
                {
                    var oMessagings = oEntry.Property("messaging").Value as JArray;

                    foreach (JObject oMessaging in oMessagings)
                    {
                        var msg = (oMessaging.Property("message").Value as JObject).Property("text").Value.ToString();
                        var senderId = (oMessaging.Property("sender").Value as JObject).Property("id").Value.ToString();

                        // test to response message.
                        var oResponse = new JObject(
                            new JProperty("message_type", "RESPONSE"),
                            new JProperty("recipient", new JObject() { new JProperty("id", senderId) }),
                            new JProperty("message", new JObject() { new JProperty("text", "您好，我正在調校中。您傳遞的訊息是：" + msg) })
                            );

                        var client = new HttpClient();

                        var apiResponse = await client.PostAsync(
                            "https://graph.facebook.com/v4.0/me/messages?access_token=" + ACCESSTOKEN,
                            new StringContent(oResponse.ToString(), Encoding.UTF8, "application/json"));

                        if (!apiResponse.IsSuccessStatusCode)
                        {
                            var apiResponseBody = await apiResponse.Content.ReadAsStringAsync();
                            System.Diagnostics.Trace.WriteLine(apiResponseBody);
                        }
                    }
                }
                
                response = request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent("EVENT_RECEIVED");
                return response;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e.Message);
                System.Diagnostics.Trace.WriteLine(e.StackTrace);

                response = request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent("EVENT_RECEIVED");
                return response;
            }
        }
    }
}
