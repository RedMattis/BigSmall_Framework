using BetterPrerequisites;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static RimWorld.ColonistBar;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill), new Type[]
    {
        typeof(DamageInfo),
        typeof(Hediff),
        })]
    public static class OnKillPatch
    {
        [HarmonyPostfix]
        public static void OnKillPostfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            const int ticksPerDay = 60000;

            // Check if the mod RedMattis.Undead is enabled. Otherwise abort.
            if (ModsConfig.IsActive("RedMattis.Undead") || ModsConfig.IsActive("RedMattis.Yokai"))
            {
                ReturnedReanimation(__instance, ticksPerDay);
            }

            if (dinfo?.Instigator is Pawn instigator && __instance is Pawn victim)
            {
                Corpse corpse = MakeCorpse_Patch.corpse != null && MakeCorpse_Patch.corpse.InnerPawn == victim ? MakeCorpse_Patch.corpse : null;

                // Check if distnace is less than 2 cells.
                var position = corpse?.Position ?? instigator?.Position;

                if (position == null)
                {
                    return;
                }

                bool targetFar = (corpse?.Spawned == true || victim?.Spawned == true) && (instigator.Position.DistanceTo(position.Value) > 2);

                var genExt = Helpers.GetAllActiveGenes(instigator).Select(x => x.def.GetModExtension<GeneExtension>()).Where(x => x != null).ToList();
                if (!targetFar && genExt.Any(x=>x.consumeSoulOnHit != null))
                {
                    var consumeSoulOnHit = genExt.First(x => x.consumeSoulOnHit != null).consumeSoulOnHit;
                    var scHediff = CompAbilityEffect_ConsumeSoul.MakeGetSoulCollectorHediff(instigator);
                    scHediff.AddPawnSoul(__instance, true, consumeSoulOnHit.gainMultiplier, consumeSoulOnHit.exponentialFalloff, consumeSoulOnHit.gainSkillMultiplier);
                    CompAbilityEffect_ConsumeSoul.ApplySoulless(victim);
                }
            }
        }

        private static void ReturnedReanimation(Pawn __instance, int ticksPerDay)
        {

            // Check if corpse is available
            Corpse corpse = null;
            if (MakeCorpse_Patch.corpse != null)
            {
                corpse = MakeCorpse_Patch.corpse;
            }
            // Log State
            //Log.Message($"[BigAndSmall] ReturnedReanimation: lastCheckTick: {VUReturning.lastCheckTick}, deadRisingMode: {VUReturning.deadRisingMode}, zombieApocalypseMode: {VUReturning.zombieApocalypseMode}");

            // Check if undead
            if (__instance == null || corpse == null || __instance.IsUndead())
            {
                return;
            }

            var soullessHediff = DefDatabase<HediffDef>.GetNamedSilentFail("BS_Soulless");
            if (__instance?.health?.hediffSet?.TryGetHediff(soullessHediff, out Hediff hediff) == true && soullessHediff != null)
            {
                return; // Don't trigger on death of soulless pawns.
            }

            float zombieApocChance = VUReturning.ZombieApocalypseChance;
            float deadRisingChance = VUReturning.DeadRisingChance;


            if (!ModsConfig.IsActive("RedMattis.Undead"))
            {
                // This means we're only using the Yokai mod. Don't cause undead to rise unless specifically triggered.
                zombieApocChance = 0;
                deadRisingChance = 0;
            }

            var map = __instance.Map ?? corpse.Map;

            // Check if victim is humanlike
            if (__instance.RaceProps.Humanlike || VUReturning.zombieApocalypseMode)
            {
                // If lastCheckTick was one day ago.
                if (Find.TickManager.TicksGame - VUReturning.lastCheckTick > ticksPerDay)
                {
                    if (Rand.Chance(zombieApocChance))
                    {
                        VUReturning.lastCheckTick = Find.TickManager.TicksGame + ticksPerDay * 2;
                        VUReturning.zombieApocalypseMode = true;
                        TriggerZombieApocalypse(map);
                    }
                    else if (Rand.Chance(deadRisingChance))
                    {
                        VUReturning.deadRisingMode = true;
                        VUReturning.zombieApocalypseMode = false;
                        VUReturning.lastCheckTick = Find.TickManager.TicksGame;
                    }
                    else
                    {
                        VUReturning.deadRisingMode = false;
                        VUReturning.zombieApocalypseMode = false;
                        VUReturning.lastCheckTick = Find.TickManager.TicksGame;
                    }
                }

                // Check if the hediff is already applied
                if (__instance.health.hediffSet.HasHediff(HediffDef.Named("BS_ReturnedReanimation")))
                {
                    return;
                }

                var validFleshType = __instance.RaceProps.FleshType == FleshTypeDefOf.Normal || __instance.RaceProps.FleshType == FleshTypeDefOf.Insectoid;
                // Check if the pawn is a robot.
                if(!validFleshType)
                {
                    return;
                }

                if (VUReturning.zombieApocalypseMode && Rand.Chance(VUReturning.ReturnChanceApoc))
                {
                    // Apply Returned Reanimation hediff
                    Hediff returnedHediff = HediffMaker.MakeHediff(HediffDef.Named("BS_ReturnedReanimation"), __instance);
                    __instance.health.AddHediff(returnedHediff);
                }
                else if (VUReturning.deadRisingMode && Rand.Chance(VUReturning.ReturnChance))
                {
                    // Apply Returned Reanimation hediff
                    Hediff returnedHediff = HediffMaker.MakeHediff(HediffDef.Named("BS_ReturnedReanimation"), __instance);
                    __instance.health.AddHediff(returnedHediff);
                }

                // 10% chance of reanimation if Colonist
                if (__instance.Faction == Faction.OfPlayer && ModsConfig.IsActive("RedMattis.Undead"))
                {
                    if (Rand.Chance(VUReturning.ReturnChanceColonist))
                    {
                        // Apply Returned Reanimation hediff
                        Hediff returnedHediff = HediffMaker.MakeHediff(HediffDef.Named("BS_ReturnedReanimation"), __instance);
                        __instance.health.AddHediff(returnedHediff);
                    }
                }
            }
        }

        public static void TriggerZombieApocalypse(Map targetMap,bool sendMessage=true)
        {
            if (sendMessage)
            {
                Messages.Message("BS_ZombieApocalypse".Translate(), MessageTypeDefOf.ThreatSmall);
                //Find.LetterStack.ReceiveLetter(label: "BS_ZombieApocalypse".Translate(), text: "BS_ZombieApocalypse".Translate(), textLetterDef: LetterDefOf.NeutralEvent);
            }

            if (targetMap?.listerThings == null)
            {
                // This can trigger when the map generates in which case this might not even be initialized yet.
                return;
            }

            // Get all dead bodies which are not dessicated and not mechanoids
            IEnumerable<Corpse> corpses = targetMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).Cast<Corpse>()
                .Where(x => !x.IsDessicated() && x.InnerPawn.RaceProps.IsFlesh && !x.InnerPawn.IsUndead());

            // 50% chance of adding the hediff to each corpse
            foreach (Corpse c in corpses)
            {
                // Check if the corpse has "flesh" type skin
                if (Rand.Chance(VUReturning.ReturnChanceApoc / 2))
                {
                    // Apply Returned Reanimation hediff
                    Hediff returnedHediff = HediffMaker.MakeHediff(HediffDef.Named("BS_ReturnedReanimation"), c.InnerPawn);
                    c.InnerPawn.health.AddHediff(returnedHediff);
                }
            }
        }
    }

    //public class MentalState_ReturnedBerserk : MentalState
    //{
    //    public static Faction zombieFaction = null;
    //    public override bool ForceHostileTo(Thing t)
    //    {
    //        if (BigSmall.performScaleCalculations && pawn != null && pawn.needs != null && BigSmall.humnoidScaler != null)
    //        {
    //            var sizeCache = HumanoidPawnScaler.GetPawnBSDict(pawn);
    //            if (sizeCache != null)
    //            {
    //                if (sizeCache.deathlike)
    //                {
    //                    return false;
    //                }
    //            }
    //        }
    //        return true;
    //    }

    //    public override bool ForceHostileTo(Faction f)
    //    {
    //        if (zombieFaction == null)
    //            zombieFaction = Find.FactionManager.AllFactions.FirstOrDefault(x => x.def.defName == "BS_ZombieFaction");
    //        if (f == zombieFaction)
    //        {
    //            return false;
    //        }
    //        return true;
    //    }

    //    public override RandomSocialMode SocialModeMax()
    //    {
    //        return RandomSocialMode.Off;
    //    }
    //}


}
