using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace WebApplicationTest.ServiceLayer
{
    public interface IServiceVoice
    {
        Task<byte[]> GetSpeechAsync(string text);

        Task<String> RecognizeSpeech(Stream audioStream);
        string GetAnswerForQuestion(string qsn);
        //Task<String> RecognizeSpeech(string audiofile);




    }
}