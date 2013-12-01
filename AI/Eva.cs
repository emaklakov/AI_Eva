using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class Eva
    {
        Hearing hearing; // Слух
        Voice voice; // Голос

        public Eva()
        {
            hearing = new Hearing();
            voice = new Voice();
        }

        public void Listen()
        {
            hearing.HeardHerName += hearing_HeardHerName;
            hearing.Start();
        }

        void hearing_HeardHerName()
        {
            voice.Start();
        }
    }
}
