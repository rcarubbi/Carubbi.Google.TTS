using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Google.TTS
{
    public enum Idioma
    {
        Portugues,
        Ingles
    }

    public class TtsHelper
    {
        private static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[8 * 1024];
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



        public static string GerarArquivo(string texto, Idioma idioma)
        {
            var strIdioma = idioma == Idioma.Portugues ? "pt" : "en";
            var url = new Uri(string.Format(URL_TTS_GOOGLE, texto, strIdioma));
            PrepareRequest(url);
            WebResponse response;
            try
            {
                response = _request.GetResponse();
            }
            catch
            {
                PrepareRequest(url);
                response = _request.GetResponse();
            }

            var fileContent = response.GetResponseStream();
            var caminhoTemp = Path.ChangeExtension(Path.GetTempFileName(), ".mp3");

            using (Stream file = File.OpenWrite(caminhoTemp))
            {
                CopyStream(fileContent, file);
                file.Flush();
                file.Close();
            }

            if (fileContent == null) return caminhoTemp;
            fileContent.Close();
            fileContent.Dispose();

            return caminhoTemp;

        }

        private static void PrepareRequest(Uri url)
        {
            _request = (HttpWebRequest)WebRequest.Create(url);
            _request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727)";
            _request.UseDefaultCredentials = true;
        }

        private static WMPLib.WindowsMediaPlayer _wplayer;

        public static void ReproduzirArquivo(object parameter)
        {
            _idle = false;

            var faixas = (List<string>)parameter;

            _wplayer = new WMPLib.WindowsMediaPlayer();
            var playlist = _wplayer.playlistCollection.newPlaylist("playlist");
            foreach (var faixa in faixas)
            {
                var media = _wplayer.newMedia(Path.Combine(Path.GetTempPath(), faixa));
                playlist.appendItem(media);
            }
            _wplayer.currentPlaylist = playlist;
            _wplayer.PlayStateChange += _wplayer_PlayStateChange;
            _wplayer.MediaError += _wplayer_MediaError;
            _wplayer.controls.play();


        }

        private static void _wplayer_MediaError(object pMediaObject)
        {
            throw new Exception("Erro ao tentar reproduzir arquivo");
        }

        private static object _locker = new object();
        private static bool _idle = true;

        private static void _wplayer_PlayStateChange(int newState)
        {

            if ((WMPLib.WMPPlayState)newState == WMPLib.WMPPlayState.wmppsMediaEnded)
            {
                File.Delete(Arquivos.First());
                Arquivos.RemoveAt(0);
            }
            else if ((WMPLib.WMPPlayState)newState == WMPLib.WMPPlayState.wmppsStopped || (WMPLib.WMPPlayState)newState == WMPLib.WMPPlayState.wmppsReady)
            {
                _idle = true;
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

        private static readonly List<string> Arquivos = new List<string>();
        private static void Reproduzir(string texto, Idioma idioma, bool esperar)
        {
            var trechos = SepararTrechos(texto, 12).ToList();
            Arquivos.Clear();
            foreach (var trecho in trechos)
            {
                Arquivos.Add(GerarArquivo(trecho, idioma));
            }
            var playerProcess = new Thread(ReproduzirArquivo);

            playerProcess.Start(Arquivos);
            Thread.Sleep(1000);
            while (esperar && !_idle)
            {
                Thread.Sleep(1000);
            }
        }

        private static IEnumerable<string> SepararTrechos(string texto, int maxWords)
        {
            var palavras = texto.Split(' ');
            var countMaxWords = 0;
            var stbTrecho = new StringBuilder();
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
