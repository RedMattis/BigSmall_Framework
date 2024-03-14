using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    Gibblets.SpawnGibblets(pawn, instigator.Position, instigator.Map, randomOrganChance:0.1f, skullChance:0.4f);

                    // Delete Armor apparel, but keep regular clothes.
                    for (int i = pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
                    {
                        // Check the apparel has the armor tags or had trade tags armor.
                        if (pawn.apparel.WornApparel[i].def.thingCategories.Contains(ThingCategoryDefOf.ApparelArmor)
                        || pawn.apparel.WornApparel[i].def.tradeTags.Any(x => x.ToLower().Contains("armor")))
                        {
                            pawn.apparel.WornApparel[i].Destroy();
                        }
                    }
                    // Drop other items on the ground
                    pawn.inventory.DropAllNearPawn(instigator.Position, forbid: true, unforbid: false);

                    if (MakeCorpse_Patch.corpse != null)
                    {
                        MakeCorpse_Patch.corpse.Destroy();
                        MakeCorpse_Patch.corpse = null;
                    }
                    nutritionAmount *= 6;

                    // Stun the attacker
                    instigator.stances.stunner.StunFor(100, instigator);
                }
                else if (didKill)
                {
                    Gibblets.SpawnGibblets(pawn, pawn.Position, instigator.Map, bloodMin: 7, bloodMax: 18, gibbletMin: 1, gibbletMax: 1, gibbletChance: 0.7f);

                    float sizeDifference = (instigator.BodySize - (pawn.BodySize*0.8f))*2;
                    float rotChance = Mathf.Clamp(sizeDifference / 2, 0, 0.4f);

                    if (Rand.Chance(rotChance) && MakeCorpse_Patch.corpse != null)
                    {
                        // Set pawn to dessicated.
                        CompRottable rottable = MakeCorpse_Patch.corpse.TryGetComp<CompRottable>();
                        if (rottable != null)
                        {
                            rottable.RotProgress = rottable.PropsRot.TicksToDessicated + 10;
                        }
                        Log.Message($"Set rottable to {rottable.RotProgress}");
                        nutritionAmount *= 5;
                        instigator.stances.stunner.StunFor(100, instigator);
                        Gibblets.SpawnGibblets(pawn, instigator.Position, instigator.Map, bloodMin: 7, bloodMax: 30, gibbletMin: 1, gibbletMax: 2, gibbletChance: 0.7f, randomOrganChance: 0.1f);
                    }
                    else
                    {
                        Gibblets.SpawnGibblets(pawn, pawn.Position, instigator.Map, bloodMin: 7, bloodMax: 18, gibbletMin: 1, gibbletMax: 1, gibbletChance: 0.7f);
                    }
                }

                if (!pawn.Dead)
                {
                    float partMaxHealth = injury.Part.def.GetMaxHealth(pawn);
                    // Check coverage of bodypart
                    nutritionAmount *= injury.Part?.coverage ?? 0;
                    // Get damage amount inflicted to the part.
                    nutritionAmount *= Mathf.Min(result.totalDamageDealt, partMaxHealth) / partMaxHealth;

                    if (result.totalDamageDealt > pawn.BodySize*10 && Rand.Chance(0.1f))
                    {
                        Gibblets.SpawnGibblets(pawn, pawn.Position, instigator.Map, bloodMin: 1, bloodMax: 4, gibbletMin: 1, gibbletMax: 1, gibbletChance: 1f);
                    }
                }

                if (nutritionAmount > 0)
                {
                    instigator.needs.food.CurLevel += nutritionAmount;
                    EngulfHediff.GetEatenCorpseMeatThoughts(instigator, pawn);
                }

            }
        }

        
    }
}
