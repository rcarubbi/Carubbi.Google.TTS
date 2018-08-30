using static System.Console;

namespace Carubbi.Google.TTS.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            while (true)
            {
                WriteLine("Type in english:");
                var text = ReadLine();

                GoogleTTS.Play(text, Language.English);

                WriteLine("Escreva em português:");
                text = ReadLine();

                GoogleTTS.Play(text, Language.BrazilianPortuguese);
            }

        }
    }
}
