using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigAndSmall
{
    using RimWorld;
    // Assembly-CSharp, Version=1.4.8385.39127, Culture=neutral, PublicKeyToken=null
    // Verse.Gene_Healing

    public class CompAbilityEffect_Xenoregenerate : CompProperties_AbilityEffect
    {
        public CompAbilityEffect_Xenoregenerate()
        {
            compClass = typeof(CompAbilityEffect_FixWorstHealthCondition);
        }
    }

}
