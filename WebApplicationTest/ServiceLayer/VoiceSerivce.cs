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
        private readonly string apiKey = "8w2ynEDIjk8I9I8MbC13hd02jQOQnFKY6pkxT9bdDG9GubNbeulTJQQJ99BGACHYHv6XJ3w3AAAYACOGt067";
        private readonly string region = "eastus2";
        private readonly string endpoint = "https://eastus2.tts.speech.microsoft.com/cognitiveservices/v1"; //"https://eastus2.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US"
        private readonly Dictionary<string, string> qaPairs;
        private readonly string avatarEndpoint = "https://eastus2.api.cognitive.microsoft.com/avatar/batchsyntheses";

        public VoiceSerivce()
        {
            //Intialize q&a pairs
            qaPairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"Hi", "Hii i am Nova" },
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

            return bestMatch != null ? qaPairs[bestMatch] : "I'm sorry babu, I dont have information about that ";
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

        /// <summary>
        /// Initiate a batch  synthesis jon for generating an AI avatar
        /// text = the text the avatar should speak
        /// voiceName = the name of the voice to use (e.g., "en-US-JennyNeural")
        /// avatarCharacter = the name of the avatar character (e.g., "lisa")
        /// avatarStyle = the style of the avatar (e.g., "sitting")
        

        public async Task<string> StartAvatarSynthesisAsync( string text, string voiceName, string avatarCharacter, string avatarStyle)
        {
             using (var client = new HttpClient())
             {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                client.DefaultRequestHeaders.Add("User-Agent", "TestAvatar GatewayAISystems");

                string synthesisId = $"batchavatar-{Guid.NewGuid()}";

                var ssmlContent = $@"
                <speak version='1.0' xml:lang='en-US'>
                <voice name='{voiceName}'>
                {System.Net.WebUtility.HtmlEncode(text)}
                </voice>
                </speak>";

                var requestBody = new
                {
                    inputKind = "SSML",

                    inputs = new[] {
                         new { content = ssmlContent 
                         }
                     },
                    synthesisConfig = new
                    {
                        voice = voiceName
                    },
                    avatarConfig = new
                    {
                        talkingAvatarCharacter = avatarCharacter,
                        talkingAvatarStyle = avatarStyle
                    },

                    videoFormat = "Mp4",
                    videoCodec = "H264",
                    properties = new
                    {
                        timeToLiveInHours = 1
                    }
                };

                var jsonBody = JsonConvert.SerializeObject(requestBody);
                using(var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    var requestUri = $"{avatarEndpoint}/{synthesisId}?api-version=2024-08-01";

                    using(var response = await client.PutAsync(requestUri, content))
                    {
                        response.EnsureSuccessStatusCode();
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(responseBody);

                        return jsonDoc.RootElement.GetProperty("id").GetString();
                    }
                }
             }
        }

        //public async Task<string> GetAvatarVideoUrlAsync(string synthesisId, int timeoutSeconds = 300, int pollIntervalSeconds = 5)
        //    {
        //    using(var client = new HttpClient())
        //    {
        //       client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
        //        client.DefaultRequestHeaders.Add("User-Agent", "Test GatewayAISystems");

        //        var startTime = DateTime.UtcNow;

        //        string requesturi = $"{avatarEndpoint}/{synthesisId}?api-version=2024-08-01";

        //        while((DateTime.UtcNow - startTime).TotalSeconds < timeoutSeconds)
        //        {
        //            using (var response = await client.GetAsync(requesturi))
        //            {
        //                response.EnsureSuccessStatusCode();
        //                var responseBody = await response.Content.ReadAsStringAsync();
        //                var jsonDoc = System.Text.Json.JsonDocument.Parse(responseBody);

        //                var status = jsonDoc.RootElement.GetProperty("status").GetString();

        //                if(status == "Succeeded")
        //                {
        //                    if(jsonDoc.RootElement.TryGetProperty("outputs", out var outputsElement) && outputsElement.TryGetProperty("result", out var resultElement))
        //                    {
        //                        return resultElement.GetString();
        //                    }
        //                    else
        //                    {
        //                        throw new Exception("Avatar synthesis succeeded but no video URL found in response.");
        //                    }
        //                }
        //                else if(status == "Running" || status == "NotStarted")
        //                {
        //                    await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds));

        //                }
        //                else
        //                {
        //                    throw new Exception($"Avatar synthesis failed with status: {status}. Response: {responseBody}");
        //                }


        //            }
        //        }
        //        throw new TimeoutException($"Avatar synthesis job {synthesisId} did not complete within the timeout of {timeoutSeconds} seconds.");
        //    }
        //}



        public async Task<string> GetAvatarVideoUrlAsync(string synthesisId, int timeoutSeconds = 600, int pollIntervalSeconds = 5)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                client.DefaultRequestHeaders.Add("User-Agent", "TestAvatar GatewayAISystems");

                var startTime = DateTime.UtcNow;
                string requesturi = $"{avatarEndpoint}/{synthesisId}?api-version=2024-08-01";

                while ((DateTime.UtcNow - startTime).TotalSeconds < timeoutSeconds)
                {
                    using (var response = await client.GetAsync(requesturi))
                    {
                        response.EnsureSuccessStatusCode();
                        var responseBody = await response.Content.ReadAsStringAsync();

                        var jsonDoc = System.Text.Json.JsonDocument.Parse(responseBody);
                        var root = jsonDoc.RootElement;

                        string status = root.GetProperty("status").GetString();
                        Console.WriteLine($"Polling Status: {status}");

                        if (status == "Succeeded")
                        {
                            if (root.TryGetProperty("outputs", out var outputsElement) &&
                                outputsElement.TryGetProperty("result", out var resultElement))
                            {
                                if(resultElement.ValueKind == JsonValueKind.String)
                            {
                                return resultElement.GetString();
                            }
                            else if(resultElement.ValueKind == JsonValueKind.Object && resultElement.TryGetProperty("videoUrl", out var videoUrlElement))
                            {
                                return videoUrlElement.GetString();
                            }
                                else
                                {
                                    throw new Exception("Could not find valid video url");
                                }
                            }

                        }
                        else if (status == "Failed")
                        {
                            throw new Exception("Synthesis job failed: " + responseBody);
                        }

                        await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds));
                    }
                }

                throw new TimeoutException($"Avatar synthesis job {synthesisId} did not complete within {timeoutSeconds} seconds.");
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
            //  ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var endpoint = $"https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US";

            using (var httpClient = new HttpClient())
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
