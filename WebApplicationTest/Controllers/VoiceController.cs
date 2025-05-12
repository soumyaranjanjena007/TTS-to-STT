using CoreEntities.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
//using WebApplicationTest.Models;
using WebApplicationTest.ServiceLayer;
//using ActionResult = Microsoft.AspNetCore.Mvc.ActionResult;
//using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;
//using HttpPostAttribute = Microsoft.AspNetCore.Mvc.HttpPostAttribute;

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

        public async  Task<ActionResult>STT()
        {

            return View();
        }

        //[HttpPost]

        //public async Task<ActionResult> STTs()
        //{
        //    HttpPostedFileBase audioFile = Request.Files["audioFile"];
        //    Console.WriteLine($"Form count: {Request.Form.Count}, Form files count: {Request.Form.Count}");
        //    if (audioFile == null)
        //    {
        //        return new HttpStatusCodeResult(400, "No audio found");


        //    }

        //    try
        //    {
        //        var stream = audioFile.InputStream;
        //        var result = await _voiceService.RecognizeSpeech(stream);



        //        return Json(new { transcription = result }, JsonRequestBehavior.AllowGet);

        //    }
        //    catch(Exception ex)
        //    {
        //        Response.StatusCode = 500;
        //        return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}
        [HttpPost]
        public async Task<ActionResult> STTs()
        {
            HttpPostedFileBase audioFile = Request.Files["audioFile"];
            if (audioFile == null)
            {
                return new HttpStatusCodeResult(400, "No audio found"); 
            }

            try
            {
                var stream = audioFile.InputStream;
                var transcription = await _voiceService.RecognizeSpeech(stream); //  Speech-to-text

                var answer = _voiceService.GetAnswerForQuestion(transcription);  // Logic-based response

                _audioBytes = await _voiceService.GetSpeechAsync(answer);        

                return Json(new
                {
                    transcription = transcription,
                    answer = answer,
                    audioUrl = Url.Action("PlayAudio","Voice", new { id = DateTime.Now.Ticks }) 
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet); 
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