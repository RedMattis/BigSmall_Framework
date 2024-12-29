using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BigAndSmall.DefAltNamer;
using Verse;

namespace BigAndSmall
{
    public static class HotReloadExtension
    {
        public static T TryGetExistingDef<T>(this string defName) where T : Def
        {
            return DefDatabase<T>.GetNamed(defName, errorOnFail: false);
        }

    }
}
