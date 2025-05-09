using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

        public async Task<string> RecognizeSpeech(Stream audioStream)
        {
            var endpoint = $"https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US";


            using(var httpClient = new HttpClient())
            using(var content = new StreamContent(audioStream))
            {
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                var response = await httpClient.PostAsync(endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Azure STT failed: {response.StatusCode}, {responseBody}");

                }

                var json = System.Text.Json.JsonDocument.Parse(responseBody);
                var res = json;

                return json.RootElement.GetProperty("DisplayText").GetString();
            }
            //using (var httpClient = new HttpClient())
            //using (var audioStream = System.IO.File.OpenRead(audioStream))
            //using (var content = new StreamContent(audioStream))
            //{
            //    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

            //    httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            //    var response = await httpClient.PostAsync(endpoint, content);
            //    var responseString = await response.Content.ReadAsStringAsync();

            //    if (!response.IsSuccessStatusCode)
            //    {
            //        throw new Exception($"STT request failed: {response.StatusCode}, {responseString}");
            //    }

            //    // Parse the JSON to extract the transcription text
            //    var json = System.Text.Json.JsonDocument.Parse(responseString);
            //    var transcription = json.RootElement.GetProperty("DisplayText").GetString();

            //    return transcription;


            //}

        }
        //public async Task<string> RecognizeSpeech(Stream audioStream)
        //{
        //    var speechConfig = SpeechConfig.FromSubscription(apiKey, region);
        //    speechConfig.SpeechRecognitionLanguage = "en-us";

        //    using(var audioInputStrem= AudioInputStream.CreatePushStream())
        //    {
        //        using(var audioInput = AudioConfig.FromStreamInput(audioInputStrem))
        //        {
        //            using(var speechRecognizer = new SpeechRecognizer(speechConfig, audioInput))
        //            {
        //                byte[] buffer = new byte[4096];
        //                int bytesRead;

        //                while((bytesRead = audioStream.Read(buffer, 0, buffer.Length)) > 0)
        //                {
        //                    audioInputStrem.Write(buffer, bytesRead);

        //                }

        //                audioInputStrem.Close();

        //                var result = await speechRecognizer.RecognizeOnceAsync();

        //                switch (result.Reason)
        //                {
        //                    case ResultReason.RecognizedSpeech:
        //                        return result.Text;

        //                    case ResultReason.NoMatch:
        //                        return "No speech could be recognozed";
        //                    case ResultReason.Canceled:
        //                        var cancellation = CancellationDetails.FromResult(result);
        //                        Console.WriteLine($"Canceled: Reaspn = {cancellation.Reason}");
        //                        if(cancellation.Reason == CancellationReason.Error)
        //                        {
        //                            Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
        //                            Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
        //                            throw new ApplicationException($"Speech recognition canceled: {cancellation.ErrorDetails}");
        //                        }

        //                        return "Speech recognition canceled.";
        //                    default:
        //                        return "Unknown speech recognition result.";

        //                }

        //            }
        //        }

        //    }

        //}

       


    }
}
