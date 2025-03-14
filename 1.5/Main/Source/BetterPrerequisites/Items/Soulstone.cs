﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_ApplySoulstone : CompProperties
    {
        public float factor = 1.0f;
        public float falloff = 2.5f;


        public CompProperties_ApplySoulstone()
        {
            compClass = typeof(CompTargetEffect_ApplySoulstone);
        }
    }

    public class CompTargetEffect_ApplySoulstone : CompTargetEffect
    {
        public CompProperties_ApplySoulstone Props => (CompProperties_ApplySoulstone)props;
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target is Pawn pawn)
            {
                var scHediff = CompAbilityEffect_ConsumeSoul.MakeGetSoulCollectorHediff(pawn);
                scHediff.AddSoulPowerDirect(Props.factor, Props.falloff);

                // Spread blood filith around the area.
                if (pawn?.Map != null)
                {
                    var bloodFilth = ThingDefOf.Filth_Blood;
                    for (int i = 0; i < 2; i++)
                    {
                        IntVec3 randomCell = pawn.Position + GenRadial.RadialPattern[i];
                        if (randomCell.InBounds(pawn.Map))
                        {
                            FilthMaker.TryMakeFilth(randomCell, pawn.Map, bloodFilth, 1);
                        }
                    }
                }
            }
        }
    }
}
