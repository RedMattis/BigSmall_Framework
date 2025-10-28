using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    

    public class CompProperties_Piloted : HediffCompProperties
    {
        public bool pilotRequired = true;
        public int pilotCapacity = 1;
        public float baseCapacity = 0.51f;
        public float pilotConsciousnessOffset = 0.25f;
        public bool inheritPilotSkills = false;
        public bool inheritPilotMentalTraits = false;
        public float flatBonusIfPiloted = 0f;
        public bool inheritRelationShips = false;

        public bool removeIfNoPilot = false;
        public bool temporarilySwapIdeology = false;
        public bool temporarilySwapFaction = false;
        public bool temporarilySwapName = false;
        public int? injuryOnRemoval = null;
        public bool canAutoEjectIfColonist = true;
        
        public XenotypeDef xenotypeToApplyOnApply = null;
        public bool restoreXenotypeOnRemove = false;
        public XenotypeDef xenotypeToApplyOnRemove = null;
        
        public bool pilotInheritMentalTraitsOnRemove = false;
        
        public bool killOnRemove = false;
        

        // Only works for a single pilot.
        public float? pilotLearnSkills = null;

        public CompProperties_Piloted()
        {
            compClass = typeof(PilotedCompProps);
        }


        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string configError in base.ConfigErrors(parentDef))
            {
                yield return configError;
            }

            if (xenotypeToApplyOnRemove != null && restoreXenotypeOnRemove)
            {
                yield return "Cannot apply xenotype on remove and restore xenotype on remove at the same time.";
            }
        }
    }
    public class PilotedCompProps : HediffComp
    {
        public CompProperties_Piloted Props => (CompProperties_Piloted)props;
    }


}
