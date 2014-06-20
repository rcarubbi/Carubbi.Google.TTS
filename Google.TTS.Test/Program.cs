using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Google.TTS.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Digite a frase:");
            string texto = Console.ReadLine();

            TTSHelper.ReproduzirSincrono(texto, Idioma.Portugues);
        }   
    }
}
