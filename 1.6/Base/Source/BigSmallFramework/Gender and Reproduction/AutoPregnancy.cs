using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static UnityEngine.Networking.UnityWebRequest;

namespace BigAndSmall
{
    public class AutoPregnancySettings : DefModExtension
    {
        public float randomExtraParentChance = 0;
        public float randomExtraParentChanceArchites = 0;
    }


    public class AutoPregnancy : TickdownGene
    {
        private bool? _isFemale = null;
        protected bool IsFemale => _isFemale ??= pawn.gender != Gender.Male;
        protected bool autoPregDisabled = false;

        public override void ResetCountdown()
        {
            tickDown = Rand.Range(60000, 300000);
        }

        public override void TickEvent()
        {
            if (autoPregDisabled) return;

            var settings = def.GetModExtension<AutoPregnancySettings>();
            var heSet = pawn.health.hediffSet;
            if (pawn?.ageTracker.Adult != true || pawn.GetStatValue(StatDefOf.Fertility) <= 0 || !IsFemale || heSet.HasHediff(HediffDefOf.Lactating))
            {
                return;
            }
            
            Pawn fakeFather = null;
            if (Rand.Chance(settings.randomExtraParentChance))
            {
                bool canHaveArchiteFather = Rand.Chance(settings.randomExtraParentChanceArchites);
                fakeFather = PawnsFinder.All_AliveOrDead
                    .Where(x => x?.IsMechanical() != true
                        && x?.IsUndead() != true
                        && x?.genes?.GenesListForReading?.Any() == true
                        && x.genes.GenesListForReading.Count > 3
                        && (canHaveArchiteFather || !x.genes.GenesListForReading.Any(x=>x.def.biostatArc > 1))) // Okay, ONE archite point is fine.
                    .RandomElementByWeight(x=>x.genes?.xenotype == XenotypeDefOf.Baseliner ? 0.001f : x.genes?.hybrid == true ? 0.1f : 1);
                if (fakeFather == null)
                {
                    Log.Message($"[AutoPregnancy] Could not find a valid random father for {pawn.Name}");
                }
            }
            var genes = PregnancyUtility.GetInheritedGeneSet(fakeFather, pawn, out bool success);
            if (success)
            {
                Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, pawn);
                hediff_Pregnant.SetParents(pawn, null, genes);
                pawn.health.AddHediff(hediff_Pregnant);
            }
        }
        public override void PostAdd()
        {
            tickDown = Rand.Range(10000, 60000);
            base.PostAdd();
        }

        public void ToggleAutoPregnancy()
        {
            autoPregDisabled = !autoPregDisabled;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            yield return new Command_Action()
            {
                defaultLabel = autoPregDisabled ? "BS_RestoreAutoPregnancy".Translate() : "BS_EnoughAutoPregnancy".Translate(),
                defaultDesc = autoPregDisabled ? "BS_RestoreAutoPregnancyDesc" : "BS_EnoughAutoPregnancyDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get(autoPregDisabled ? "GeneIcons/BS_AutoPregnancyGizmo" : "GeneIcons/BS_AutoPregnancyGizmo_STHAP", true),
                action = ToggleAutoPregnancy
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref autoPregDisabled, "BS_AutoPregDisabled");
        }
    }

    public class ShortPregnancy : TickdownGene
    {
        public override void ResetCountdown()
        {
            tickDown = Rand.Range(6000, 120000);
        }

        public override void TickEvent()
        {
            var heSet = pawn.health.hediffSet;
            if (heSet.TryGetHediff(HediffDefOf.PregnantHuman, out Hediff pregnantHediff))
            {
                if (pregnantHediff.Severity > 0.65)
                {
                    pregnantHediff.Severity = 0.98f;
                }
            }
        }
    }
}
