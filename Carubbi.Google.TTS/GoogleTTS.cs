using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using WMPLib;

namespace Carubbi.Google.TTS
{

    public static class GoogleTTS
    {
        private const string GOOGLE_TTS_URL_BASE = "http://translate.google.com/translate_tts";
        private const string MOZILLA_USER_AGENT = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727)";

        private static WindowsMediaPlayer _wplayer;
        private static object _locker = new object();
        private static bool _idle = true;
        private static readonly List<string> _trackList = new List<string>();

        
        /// <summary>
        /// Generates a new file in the current temp path
        /// </summary>
        /// <param name="text">Content to be transformed</param>
        /// <param name="language">Language of the audio</param>
        /// <returns></returns>
        public static string GenerateFile(string text, Language language)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("the content was empty");

            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("user-agent", MOZILLA_USER_AGENT);
                webClient.UseDefaultCredentials = true;
                webClient.QueryString.Add("ie", "UTF-8");
                webClient.QueryString.Add("client", "tw-ob");
                webClient.QueryString.Add("tl", ParseCulture(language));
                webClient.QueryString.Add("q", text);
                
                var response = webClient.DownloadData(GOOGLE_TTS_URL_BASE);

                var tempFile = Path.GetTempFileName();
                tempFile = Path.Combine(Path.GetTempPath(), $"gtts_{Path.GetFileNameWithoutExtension(tempFile)}.mp3");
                using (var file = File.OpenWrite(tempFile))
                using (var memoryStream = new MemoryStream(response))
                {
                    memoryStream.CopyTo(file);
                    file.Flush();
                    file.Close();
                }
 
                return tempFile;
            }
        }

        private static void PlayFiles(List<string> trackList)
        {
            _idle = false;
 
            _wplayer = new WMPLib.WindowsMediaPlayer();
            var playlist = _wplayer.playlistCollection.newPlaylist("playlist");
            foreach (var track in trackList)
            {
                var media = _wplayer.newMedia(Path.Combine(Path.GetTempPath(), track));
                playlist.appendItem(media);
            }
            _wplayer.currentPlaylist = playlist;
            _wplayer.PlayStateChange += _wplayer_PlayStateChange;
            _wplayer.MediaError += _wplayer_MediaError;
            _wplayer.controls.play();
        }   

        

     

        private static string ParseCulture(Language language)
        {
            switch (language)
            {
                case Language.English:
                    return "en";
                case Language.BrazilianPortuguese:
                    return "pt";
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, null);
            }
        }

        private static void _wplayer_MediaError(object pMediaObject)
        {
            throw new ApplicationException("Error on play track");
        }

        private static void _wplayer_PlayStateChange(int newState)
        {
            var state = (WMPPlayState) newState;
            switch (state)
            {
                case WMPPlayState.wmppsMediaEnded:
                    File.Delete(_trackList.First());
                    _trackList.RemoveAt(0);
                    break;
                case WMPPlayState.wmppsStopped:
                case WMPPlayState.wmppsReady:
                    _idle = true;
                    break;
                case WMPPlayState.wmppsUndefined:
                    break;
                case WMPPlayState.wmppsPaused:
                    break;
                case WMPPlayState.wmppsPlaying:
                    break;
                case WMPPlayState.wmppsScanForward:
                    break;
                case WMPPlayState.wmppsScanReverse:
                    break;
                case WMPPlayState.wmppsBuffering:
                    break;
                case WMPPlayState.wmppsWaiting:
                    break;
                case WMPPlayState.wmppsTransitioning:
                    break;
                case WMPPlayState.wmppsReconnecting:
                    break;
                case WMPPlayState.wmppsLast:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Convert a text to audio and play synchronously
        /// </summary>
        /// <param name="text">Content to be transformed</param>
        /// <param name="language">Language of the audio</param>
        public static void Play(string text, Language language)
        {
            var chunks = SplitText(text, 12).ToList();
            _trackList.Clear();
            foreach (var chunk in chunks)
            {
                _trackList.Add(GenerateFile(chunk, language));
            }
            var playerProcess = new Thread(() => PlayFiles(_trackList));
            playerProcess.Start();
 
        }

        private static IEnumerable<string> SplitText(string text, int maxWords)
        {
            var words = text.Split(' ');
            var countMaxWords = 0;
            var stbChunk = new StringBuilder();
            foreach (var word in words)
            {
                stbChunk.Append(countMaxWords == 0 ? word : $" {word}");

                if (countMaxWords == maxWords || word == words[words.Count() - 1])
                {
                    countMaxWords = 0;
                    var chunk = stbChunk.ToString();
                    stbChunk = new StringBuilder();
                    yield return chunk;
                }
                countMaxWords++;
            }
        }
    }
}
