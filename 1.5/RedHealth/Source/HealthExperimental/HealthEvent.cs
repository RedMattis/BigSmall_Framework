using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RedHealth
{
    public class HealthEvent : IExposable
    {
        public string name;
        public int time;
        public HealthManager healthComp = null;
        public HealthEvent() { } // For the Scribe.
        public HealthEvent(HealthManager instance, string name, int time)
        {
            this.name = name;
            this.time = time;
            this.healthComp = instance;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref time, "time");
        }
    }
}
