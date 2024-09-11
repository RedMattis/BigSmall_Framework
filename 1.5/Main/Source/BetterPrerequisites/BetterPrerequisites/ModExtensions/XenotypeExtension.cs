using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BetterPrerequisites
{
    public class XenotypeExtension : DefModExtension
    {
        public float morphWeight = 1;
        public bool morphIgnoreGender = false;
        //public int? maxAge = null;
        //public Gender? morphGender = null;

        public List<List<string>> genePickPriority = null;

        //public class GenePicker
        //{
        //    public List<string> priority = new();
        //}
    }

    public static class XenoTypeDefExtensions
    {
        public static float GetMorphWeight(this XenotypeDef def)
        {
            if (def.HasModExtension<XenotypeExtension>())
            {
                return def.GetModExtension<XenotypeExtension>().morphWeight;
            }
            return 1;
        }
    }

}
