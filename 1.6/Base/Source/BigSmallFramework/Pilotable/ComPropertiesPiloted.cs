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

        public CompProperties_Piloted()
        {
            compClass = typeof(PilotedCompProps);
        }
    }
    public class PilotedCompProps : HediffComp
    {
        public CompProperties_Piloted Props => (CompProperties_Piloted)props;
    }


}
