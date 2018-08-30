using static System.Console;

namespace Carubbi.Google.TTS.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            WriteLine("Type in english:");
            var text = ReadLine();

            GoogleTTS.Play(text, Language.English);

            WriteLine("Escreva em português:");
            text = ReadLine();

            GoogleTTS.Play(text, Language.BrazilianPortuguese);

            WriteLine("Escreva em português (assíncrono):");
            text = ReadLine();

            GoogleTTS.AsyncPlay(text, Language.BrazilianPortuguese);

            WriteLine("Type in english (async):");
            text = ReadLine();
            GoogleTTS.AsyncPlay(text, Language.English);
            ReadKey();
        }   
    }
}
