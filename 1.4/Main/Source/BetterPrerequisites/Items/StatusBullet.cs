﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class BS_StatusBullet : Bullet
    {
        public ModExtension_StatusAfflicter Props => def.GetModExtension<ModExtension_StatusAfflicter>();

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield: false);
            if (Props != null && hitThing != null && hitThing is Pawn pawn)
            {
                float severity = Props.severity;
                float severityPerBodySize = Props.severityPart;
                if(Props.softScaleSeverityByBodySize && pawn.BodySize > 1)
                {
                    severity /= UnityEngine.Mathf.Sqrt(pawn.BodySize);
                    severityPerBodySize /= UnityEngine.Mathf.Sqrt(pawn.BodySize);
                }

                float prevBodySize = pawn.BodySize;
                if (Props.hediffToAdd != null)
                {
                    Hediff oldHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(Props.hediffToAdd);
                    if (oldHediff != null)
                    {
                        oldHediff.Severity += severity;
                    }
                    else
                    {
                        Hediff hediff = HediffMaker.MakeHediff(Props.hediffToAdd, pawn);
                        hediff.Severity = severity;
                        pawn.health.AddHediff(hediff);
                    }

                }
                if (Props.hediffToAddToPart != null)
                {
                    BodyPartRecord bodyPartRecord = pawn.health.hediffSet.GetNotMissingParts().RandomElement();
                    Hediff hediff2 = HediffMaker.MakeHediff(Props.hediffToAddToPart, pawn, bodyPartRecord);
                    hediff2.Severity = severityPerBodySize;
                    pawn.health.AddHediff(hediff2, bodyPartRecord);
                }
            }
        }
    }

    public class ModExtension_StatusAfflicter : DefModExtension
    {
        public HediffDef hediffToAdd = null;
        public float severity = 0.05f;
        public HediffDef hediffToAddToPart = null;
        public float severityPart = 0.05f;
        public bool softScaleSeverityByBodySize = false;
    }

}
