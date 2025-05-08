using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace WebApplicationTest.ServiceLayer
{
    public interface IServiceVoice
    {
        Task<byte[]> GetSpeechAsync(string text);
    }
}