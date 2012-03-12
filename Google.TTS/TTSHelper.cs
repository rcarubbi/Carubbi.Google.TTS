using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Runtime.InteropServices;
using Microsoft.DirectX.AudioVideoPlayback;
namespace Google.TTS
{
    public class TTSHelper
    {
        private static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }

        private static HttpWebRequest _request;
        private const string URL_TTS_GOOGLE = "http://translate.google.com/translate_tts?tl=pt&q=";
        private static string caminhoTemp;

        public static string ProxyPath
        {
            get;
            set;
        }

        public static string ProxyUserName
        {
            get;
            set;
        }

        public static string ProxyPassword
        {
            get;
            set;
        }

        public static string ProxyDomain
        {
            get;
            set;
        }



        public static string GerarArquivo(string texto)
        {
            Uri url = new Uri(string.Concat(URL_TTS_GOOGLE, texto));

            _request = (HttpWebRequest)HttpWebRequest.Create(url);
            _request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727)";
            _request.UseDefaultCredentials = true;
           
            if (!string.IsNullOrEmpty(ProxyPath))
            {
                _request.Proxy = WebRequest.GetSystemWebProxy();
                _request.Proxy.Credentials = new NetworkCredential(ProxyUserName, ProxyPassword, ProxyDomain);
            }
            
            WebResponse response = _request.GetResponse();
            Stream fileContent = response.GetResponseStream();
            caminhoTemp = Path.ChangeExtension(Path.GetTempFileName(), ".mp3");

            using (Stream file = File.OpenWrite(caminhoTemp))
            {
                CopyStream(fileContent, file);
                file.Flush();
                file.Close();
            }

            fileContent.Close();
            fileContent.Dispose();

            return caminhoTemp;
        }

        public static void ReproduzirArquivo(string arquivo, bool excluir, bool isAsync)
        {
            Audio player = new Audio(caminhoTemp);

            player.Play();

            if (!isAsync)
            {
                while (player.CurrentPosition < player.Duration) { }

                if (excluir)
                {
                    File.Delete(caminhoTemp);
                }
            }
        }

        public static void ReproduzirSincrono(string texto)
        {
            ReproduzirSincrono(texto, true);
        }

        public static void ReproduzirSincrono(string texto, bool excluir)
        {
            try
            {
                ReproduzirArquivo(GerarArquivo(texto), excluir, false);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static void ReproduzirAsincrono(string texto)
        {
            ReproduzirArquivo(GerarArquivo(texto), false, true);
        }
    }
}
