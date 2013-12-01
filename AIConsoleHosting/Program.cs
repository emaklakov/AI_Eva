using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI;
using Microsoft.Speech.Recognition;

namespace AIConsoleHosting
{
    class Program
    {
        static void Main(string[] args)
        {
            Eva eva = new Eva();
            eva.Listen();

            Console.WriteLine("Скажите: Ева!");
            Console.ReadLine();
        }
    }
}
