using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    internal class Voice
    {
        SpeechSynthesizer ss = new SpeechSynthesizer();

        public Voice()
        {
            ss.Volume = 20; // от 0 до 100
            ss.Rate = 0; //от -10 до 10
        }

        public void Start()
        {
            ss.SpeakAsyncCancelAll();
            ss.Resume();
            ss.SpeakAsync("Я Вас слушаю!");
        }

        public void Say(string Text)
        {
 
        }
    }
}
