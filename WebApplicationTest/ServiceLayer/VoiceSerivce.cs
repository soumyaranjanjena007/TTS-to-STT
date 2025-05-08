using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebApplicationTest.ServiceLayer
{
    public class VoiceSerivce : IServiceVoice
    {
        private readonly string apiKey = "EJkZMEyoDmUqPhdfdD8e9tGkJQildCtCmKSVBdfVdB0RmXdRyp30JQQJ99BEACYeBjFXJ3w3AAAYACOGMBd1";
        private readonly string region = "eastus";
        private readonly string endpoint = "https://eastus.tts.speech.microsoft.com/cognitiveservices/v1";

        public async Task<byte[]> GetSpeechAsync(string text)
        {
            var accTok = await FetchTokenAsync();

            using(var client =  new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accTok);
                client.DefaultRequestHeaders.Add("User-Agent", "Test GatewayAISystems");
                client.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "audio-16khz-32kbitrate-mono-mp3");


                var body = $@"
                        <speak version='1.0' xml:lang='en-US'>
                        <voice xml:lang='en-US' xml:gender='Female' name='en-US-JennyNeural'>
                        {System.Net.WebUtility.HtmlEncode(text)}
                        </voice>
                        </speak>";

                using (var content = new StringContent(body, Encoding.UTF8, "application/ssml+xml"))
                using (var response = await client.PostAsync(endpoint, content))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
        }

        private async Task<string> FetchTokenAsync()
        {
            using(var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                var uri = $"https://{region}.api.cognitive.microsoft.com/sts/v1.0/issueToken";

                var response = await client.PostAsync(uri, null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
