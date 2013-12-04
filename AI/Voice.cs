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
        bool _IsStart = false;
        Random rnd = new Random();
        Memory memory = new Memory(); 

        public bool IsStart
        {
            get { return _IsStart; }
        }

        public Voice()
        {
            ss.Volume = 30; // от 0 до 100
            ss.Rate = 0; //от -10 до 10
        }

        public void Start()
        {
            //ss.SpeakAsyncCancelAll();
            ss.Resume();
            //ss.SpeakAsync(memory.PhrasesWhenHearMyName[rnd.Next(memory.PhrasesWhenHearMyName.Length-1)]);
            string Phrase = memory.PhrasesWhenHearMyName[rnd.Next(memory.PhrasesWhenHearMyName.Length - 1)];
            ss.Speak(Phrase);
            memory.WhatSay = Phrase;
            _IsStart = true;
        }

        public void Say(string Text)
        {
            ss.SpeakAsyncCancelAll();
            ss.Resume();
            //ss.SpeakAsync(Text);
            ss.Speak(Text);
            memory.WhatSay = Text;
            _IsStart = true;
        }
    }
}
