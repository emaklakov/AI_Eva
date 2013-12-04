using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    /// <summary>
    /// Память
    /// </summary>
    internal class Memory
    {
        private string[] _phrasesWhenHearMyName = { "Я Вас слушаю!", "Чем могу быть полезна?", "Вся во внимании!", "Да!", "Вы меня звали?" };
        private string _whatHeard = "";
        private string _whatSay = "";
        private int lastIndexWhatSay = 0;

        /// <summary>
        /// Фразы, которые произносит Ева, когда слышет свое имя
        /// </summary>
        public string[] PhrasesWhenHearMyName
        {
            get 
            {
                return _phrasesWhenHearMyName;
            }
        }

        /// <summary>
        /// Что последнее слышала Ева
        /// </summary>
        public string WhatHeard
        {
            get
            {
                return _whatHeard;
            }
            set
            {
                _whatHeard = value;
            }
        }

        /// <summary>
        /// Что последнее сказала Ева
        /// </summary>
        public string WhatSay
        {
            get
            {
                return _whatSay;
            }
            set
            {
                _whatSay = value;
            }
        }

        public Memory()
        { 
        }
    }
}
