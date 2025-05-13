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
        private readonly Dictionary<string, string> qaPairs;

        public VoiceSerivce()
        {
            //Intialize q&a pairs
            qaPairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "what is the weather", "The weather is sunny and pleasant today." },
                { "what are your storage rates", "Our storage rates start from $50 per month for a 5x5 unit." },
                { "do you have climate control", "Yes, we offer climate-controlled units to protect your belongings." },
                { "what are your hours", "We are open 24/7 for existing customers, and office hours are 9 AM to 6 PM." },
                { "is security available", "Yes, we have 24/7 video surveillance and gated access for your security." }
            };
        }

        public string GetAnswerForQuestion(string qsn)
        {
            if (string.IsNullOrWhiteSpace(qsn))
            {
                return "Please ask a valid qsn";
            }

            var bestMatch = qaPairs.Keys.OrderByDescending(k => CalculationSimilarity(k, qsn)).FirstOrDefault();

            return bestMatch != null ? qaPairs[bestMatch] : "I'm sorry babu, I dont have information about that shit";
        }

        private double CalculationSimilarity(string s1, string s2)
        {
            if(string.IsNullOrWhiteSpace(s1) || string.IsNullOrWhiteSpace(s2))
            {
                return 0;
            }

            var words1 = s1.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var words2 = s2.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            var intersectionCount = words1.Intersect(words2).Count();
            var unionCount = words1.Union(words2).Count();

            return unionCount == 0 ? 0 : (double)intersectionCount / unionCount;
        }

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
