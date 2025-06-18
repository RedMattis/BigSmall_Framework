using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using static Verse.DamageWorker;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public class DamageWorker_BiteDevourDmg
    {
        [HarmonyPatch(typeof(DamageWorker_AddInjury), "FinalizeAndAddInjury", new Type[] { typeof(Pawn), typeof(Hediff_Injury), typeof(DamageInfo), typeof(DamageResult) })]
        [HarmonyPostfix]
        public static void FinalizeAndAddInjury_Postfix(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo, DamageResult result, DamageWorker_AddInjury __instance)
        {
            if (!BigSmall.BSGenesActive) { return; }

            if (dinfo.Def.defName.ToLower().Contains("devourdmg") //BSDefs.BS_BiteDevourDmg.defName
                && dinfo.Instigator is Pawn instigator && !instigator.Dead
                && pawn.RaceProps?.IsFlesh == true)
            {
                var nutritionAmount = pawn.BodySize;

                bool didKill = false;
                bool killDowned = Rand.Chance(0.5f) && pawn.Downed;
                // Check if victim should be dead
                if (!pawn.Dead && (pawn.health.ShouldBeDead() || killDowned))
                {
                    pawn.Kill(dinfo);
                    didKill = true;

                }
                else if (pawn.Dead)
                {
                    didKill = true;
                }

                if (didKill && pawn?.RaceProps?.IsMechanoid == false && instigator.BodySize > pawn.BodySize * 2 && Rand.Chance(0.7f))
                {
                    Gibblets.SpawnGibblets(pawn, instigator.Position, instigator.Map, randomOrganChance: 0.1f, skullChance: 0.4f);

                    if (pawn?.apparel?.WornApparel != null)
                    {
                        // Destroy apparel and avoid flooding the area with stuff. Drop other items.
                        for (int i = pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
                        {
                            pawn.apparel.WornApparel[i].Destroy();
                        }
                        pawn.inventory.DropAllNearPawn(instigator.Position, forbid: true, unforbid: false);
                    }
                    nutritionAmount *= 6;
                    IngestTarget(pawn, instigator, nutritionAmount);
                    if (MakeCorpse_Patch.corpse?.Destroyed == false)
                    {
                        MakeCorpse_Patch.corpse.Destroy();
                        MakeCorpse_Patch.corpse = null;
                    }
                    // Stun the attacker
                    instigator.stances.stunner.StunFor(100, instigator);
                }
                else if (didKill)
                {
                    Gibblets.SpawnGibblets(pawn, pawn.Position, instigator.Map, bloodMin: 7, bloodMax: 18, gibbletMin: 1, gibbletMax: 1, gibbletChance: 0.7f);

                    float sizeDifference = (instigator.BodySize - (pawn.BodySize * 0.8f)) * 2;
                    float rotChance = Mathf.Clamp(sizeDifference / 2, 0, 0.4f);

                    if (Rand.Chance(rotChance) && MakeCorpse_Patch.corpse is Corpse corpse)
                    {
                        // Set pawn to dessicated.
                        CompRottable rottable = corpse.TryGetComp<CompRottable>();
                        
                        nutritionAmount *= 5;
                        instigator.stances.stunner.StunFor(100, instigator);
                        Gibblets.SpawnGibblets(pawn, instigator.Position, instigator.Map, bloodMin: 7, bloodMax: 30, gibbletMin: 1, gibbletMax: 2, gibbletChance: 0.7f, randomOrganChance: 0.1f);
                        IngestTarget(pawn, instigator, nutritionAmount);

                        if (corpse.Destroyed == false && rottable != null)
                        {
                            rottable.RotProgress = rottable.PropsRot.TicksToDessicated + 10;
                        }
                    }
                    else
                    {
                        Gibblets.SpawnGibblets(pawn, pawn.Position, instigator.Map, bloodMin: 7, bloodMax: 18, gibbletMin: 1, gibbletMax: 1, gibbletChance: 0.7f);
                        IngestTarget(pawn, instigator, nutritionAmount);
                    }
                }

                if (!pawn.Dead)
                {
                    float partMaxHealth = injury.Part.def.GetMaxHealth(pawn);
                    // Check coverage of bodypart
                    nutritionAmount *= injury.Part?.coverage ?? 0;
                    // Get damage amount inflicted to the part.
                    nutritionAmount *= Mathf.Min(result.totalDamageDealt, partMaxHealth) / partMaxHealth;

                    if (result.totalDamageDealt > pawn.BodySize * 10 && Rand.Chance(0.1f))
                    {
                        Gibblets.SpawnGibblets(pawn, pawn.Position, instigator.Map, bloodMin: 1, bloodMax: 4, gibbletMin: 1, gibbletMax: 1, gibbletChance: 1f);
                    }
                    IngestTarget(pawn, instigator, nutritionAmount);
                }

                
            }
        }

        private static void IngestTarget(Pawn target, Pawn eater, float nutritionMax, float maxPercentOfFoodBar=0.25f)
        {
            if (eater?.needs?.food == null || target == null)
            {
                return;
            }
            var corpse = target.Corpse;
            var maxFromFoodBar = eater.needs.food.MaxLevel * maxPercentOfFoodBar;
            var nutritionWanted = eater.needs.food.NutritionWanted;

            var nutritionToEat = Mathf.Min(maxFromFoodBar, Mathf.Min(nutritionMax, nutritionWanted));
            if (nutritionMax > 0 && target.Dead && corpse != null)
            {
                if (target.IngestibleNow == true)
                {
                    eater.needs.food.CurLevel += target.Ingested(eater, nutritionToEat);
                }
                else if (target.Corpse?.IngestibleNow == true)
                {
                    eater.needs.food.CurLevel += target.Corpse.Ingested(eater, nutritionToEat);
                }
            }
            else if (nutritionMax > 0)
            {
                var meatThingDef = target.RaceProps?.meatDef;
                if (meatThingDef != null && meatThingDef.ingestible?.CachedNutrition is float nutriPStack && nutriPStack > 0)
                {
                    Thing meat = ThingMaker.MakeThing(meatThingDef);
                    meat.stackCount = FoodUtility.StackCountForNutrition(nutritionToEat, nutriPStack);
                    meat.stackCount = meat.stackCount < 1 ? 1 : meat.stackCount;
                    eater.needs.food.CurLevel += meat.Ingested(eater, nutritionToEat);
                }
            }
        }
    }
}
