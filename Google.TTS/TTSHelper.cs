using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Runtime.InteropServices;
using System.Threading;
namespace Google.TTS
{
    public enum Idioma
    {
        Portugues,
        Ingles
    }

    public enum ProxyMethod
    {
        ItauProxy
    }

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
        private const string URL_TTS_GOOGLE = "http://translate.google.com/translate_tts?ie=UTF&tl={1}&q={0}&client=tw-ob";


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

        public static ProxyMethod? ProxyMethod
        {
            get;
            set;
        }

        public static string GerarArquivo(string texto, Idioma idioma)
        {
            string strIdioma = idioma == Idioma.Portugues ? "pt" : "en";
            Uri url = new Uri(string.Format(URL_TTS_GOOGLE, texto, strIdioma));
            PrepareRequest(url);
            WebResponse response = null;
            try
            {
                response = _request.GetResponse();
            }
            catch
            {
                if (ProxyMethod.HasValue && ProxyMethod.Value == Google.TTS.ProxyMethod.ItauProxy)
                {
                    ProxyMethod = null;
                    PrepareRequest(url);
                    response = _request.GetResponse();
                }
            }
            Stream fileContent = response.GetResponseStream();
            var caminhoTemp = Path.ChangeExtension(Path.GetTempFileName(), ".mp3");

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

        private static void PrepareRequest(Uri url)
        {
            if (ProxyMethod.HasValue && ProxyMethod.Value == Google.TTS.ProxyMethod.ItauProxy)
            {
                byte[] urlBytes = Encoding.UTF8.GetBytes(url.AbsolutePath.ToCharArray());
                _request = (HttpWebRequest)HttpWebRequest.Create(string.Format(ProxyPath, Convert.ToBase64String(urlBytes)));
                _request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727)";

                byte[] authBytes = Encoding.UTF8.GetBytes(string.Format("{0}:{1}", ProxyUserName, ProxyPassword).ToCharArray());
                _request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(authBytes);
                _request.KeepAlive = true;
                _request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                _request.Headers["Accept-Encoding"] = "gzip,deflate,sdch";
                _request.Headers["Cookie"] = "BCSI-CS-578f1ddf35ea416c=2";

            }
            else
            {
                _request = (HttpWebRequest)HttpWebRequest.Create(url);
                _request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727)";
                _request.UseDefaultCredentials = true;
            }
        }
        private static WMPLib.WindowsMediaPlayer _wplayer;

        public static void ReproduzirArquivo(object parameter)
        {
            idle = false;

            List<String> arquivos = (List<String>)parameter;

            _wplayer = new WMPLib.WindowsMediaPlayer();
            var _playlist = _wplayer.playlistCollection.newPlaylist("playlist");
            foreach (var arquivo in arquivos)
            {
                WMPLib.IWMPMedia media = _wplayer.newMedia(Path.Combine(Path.GetTempPath(), arquivo));
                _playlist.appendItem(media);
            }
            _wplayer.currentPlaylist = _playlist;
            _wplayer.PlayStateChange += new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(_wplayer_PlayStateChange);
            _wplayer.MediaError += new WMPLib._WMPOCXEvents_MediaErrorEventHandler(_wplayer_MediaError);
            _wplayer.controls.play();


        }

        static void _wplayer_MediaError(object pMediaObject)
        {
            throw new Exception("Erro ao tentar reproduzir arquivo");
        }

        static object locker = new object();
        static bool idle = true;
        static void _wplayer_PlayStateChange(int NewState)
        {
            Console.WriteLine(((WMPLib.WMPPlayState)NewState).ToString());
            if ((WMPLib.WMPPlayState)NewState == WMPLib.WMPPlayState.wmppsMediaEnded)
            {
                File.Delete(arquivos.First());
                arquivos.RemoveAt(0);
            }
            else if ((WMPLib.WMPPlayState)NewState == WMPLib.WMPPlayState.wmppsStopped || (WMPLib.WMPPlayState)NewState == WMPLib.WMPPlayState.wmppsReady)
            {
                idle = true;
            }
        }

        public static void ReproduzirSincrono(string texto, Idioma idioma)
        {
            Reproduzir(texto, idioma, true);
        }

        public static void ReproduzirAsincrono(string texto, Idioma idioma)
        {
            Reproduzir(texto, idioma, false);
        }

        private static List<String> arquivos = new List<string>();
        private static void Reproduzir(string texto, Idioma idioma, bool esperar)
        {
            List<String> trechos = SepararTrechos(texto, 12).ToList();
            arquivos.Clear();
            foreach (var trecho in trechos)
            {
                arquivos.Add(GerarArquivo(trecho, idioma));
            }
            Thread playerProcess = new Thread(new ParameterizedThreadStart(ReproduzirArquivo));

            playerProcess.Start(arquivos);
            Thread.Sleep(1000);
            while (esperar && !idle)
            {
                Thread.Sleep(1000);
            }
        }

        private static IEnumerable<string> SepararTrechos(string texto, int maxWords)
        {

            var palavras = texto.Split(' ');
            int countMaxWords = 0;
            StringBuilder stbTrecho = new StringBuilder();
            foreach (var palavra in palavras)
            {
                if (countMaxWords == 0)
                    stbTrecho.Append(palavra);
                else
                    stbTrecho.Append(" " + palavra);

                if (countMaxWords == maxWords || palavra == palavras[palavras.Count() - 1])
                {
                    countMaxWords = 0;
                    var retorno = stbTrecho.ToString();
                    stbTrecho = new StringBuilder();
                    yield return retorno;
                }
                countMaxWords++;
            }
        }
    }
}
