using CoreEntities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
//using WebApplicationTest.Models;
using WebApplicationTest.ServiceLayer;

namespace WebApplicationTest.Controllers
{
    public class VoiceController : Controller
    {
        private readonly IServiceVoice _voiceService;
        private static byte[] _audioBytes;
        public VoiceController()
        {
            _voiceService = new VoiceSerivce();
        }
        // GET: Voice
        public ActionResult Index()
        {
            
            return View();
        }

        public ActionResult TTS()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> TTS(VoiceVM model)
        {
            if (model != null)
            {
                _audioBytes = await _voiceService.GetSpeechAsync(model.text ?? "Hello World, wellcome to gateway ai systems, how can i help you, i am soumya here to assist you");
                TempData["Audio"] = _audioBytes;
                ViewBag.AudioAvailable = true;
            }         
        

            return View();
        }

        public async Task<ActionResult> STT(){

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> STT(VoiceVM voice)
        {
            var file = Request.Files["audioFile"];
            if (file == null || file.ContentLength == 0)
                return Content("No audio uploaded");

            using (var client = new HttpClient())
            {
                var subscriptionKey = "";
                var region = "eastus"; // or your region
                var endpoint = $"https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US";

                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                using (var content = new StreamContent(file.InputStream))
                {
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

                    var response = await client.PostAsync(endpoint, content);
                    var json = await response.Content.ReadAsStringAsync();

                    dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    return Content((string)result.DisplayText ?? "Unable to transcribe");
                }
            }
        }


        public ActionResult PlayAudio()
        {
            
            if (_audioBytes == null)
                return HttpNotFound();

            return File(_audioBytes, "audio/mpeg");
        }
    }
}