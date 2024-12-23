using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static RimWorld.PsychicRitualRoleDef;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class MechanicalColonistPatches
    {
        [HarmonyPatch(typeof(HealthUtility), "TryAnesthetize")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        public static bool TryAnesthetizePatch(Pawn pawn)
        {
            if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache && cache.isMechanical)
            {
                return false;
            }
            return true;
        }

        //public ResolvedWound ChooseWoundOverlay(Hediff hediff)
        [HarmonyPatch(typeof(FleshTypeDef), nameof(FleshTypeDef.ChooseWoundOverlay))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        public static bool ChooseWoundOverlayPatch(ref FleshTypeDef.ResolvedWound __result, FleshTypeDef __instance, Hediff hediff)
        {
            if (__instance != FleshTypeDefOf.Mechanoid && HumanoidPawnScaler.GetCacheUltraSpeed(hediff.pawn) is BSCache cache && cache.isMechanical)
            {
                __result = FleshTypeDefOf.Mechanoid.ChooseWoundOverlay(hediff);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CompRottable), nameof(CompRottable.Active), MethodType.Getter)]
        [HarmonyPriority(int.MaxValue)]
        public static bool Deactivate_CompRottable(CompRottable __instance, ref bool __result)
        {
            if (__instance.parent is Corpse corpse &&
                HumanoidPawnScaler.GetCacheUltraSpeed(corpse.InnerPawn) is BSCache cache && cache.isMechanical)
            {
                __result = false;
                return false;
            }
            return true;
        }

        //[HarmonyPatch(typeof(ThoughtWorker_TranshumanistAppreciation), "CurrentSocialStateInternal", [typeof(Pawn), typeof(Pawn)])]
        //[HarmonyPriority(Priority.Low)]
        //public static class TranshumanistAppreciation_Patch
        //{
        //    public static void Postfix(ref ThoughtState __result, Pawn pawn, Pawn other)
        //    {
        //        if (HumanoidPawnScaler.GetCacheUltraSpeed(other) is BSCache cache && cache.isMechanical)
        //        {
        //            __result = ThoughtState.ActiveAtStage(99);
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(ThoughtWorker_BodyPuristDisgust), "CurrentSocialStateInternal", [typeof(Pawn), typeof(Pawn)])]
        //[HarmonyPriority(Priority.Low)]
        //public static class BodyPuristDisgust_Patch
        //{
        //    public static void Postfix(ref ThoughtState __result, Pawn pawn, Pawn other)
        //    {
        //        if (HumanoidPawnScaler.GetCacheUltraSpeed(other) is BSCache cache && cache.isMechanical)
        //        {
        //            __result = ThoughtState.ActiveAtStage(99);
        //        }
        //    }


        //}
        //[HarmonyPatch(typeof(ThoughtWorker_HasAddedBodyPart), "CurrentStateInternal", [typeof(Pawn)])]
        //[HarmonyPriority(Priority.Low)]
        //public static class HasAddedBodyPart_Patch
        //{
        //    public static void Postfix(ref ThoughtState __result, Pawn pawn)
        //    {
        //        if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache && cache.isMechanical)
        //        {
        //            __result = ThoughtState.ActiveAtStage(99);
        //        }
        //    }
        //}
        [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.AddedAndImplantedPartsWithXenogenesCount), [typeof(Pawn)])]
        [HarmonyPriority(Priority.Low)]
        public static class AddedAndImplantedPartsWithXenogenesCount_Patch
        {
            public static void Postfix(ref int __result, Pawn pawn)
            {
                if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache && cache.isMechanical)
                {
                    __result += 2;
                }
            }
        }


        public static Dictionary<BodyDef, Dictionary<BodyPartDef, List<BodyPartRecord>>> cachedRecordsPerPartDefDefPerBodydef = []; // Haha
        [HarmonyPatch(typeof(BodyDef), nameof(BodyDef.GetPartsWithDef))]
        [HarmonyPriority(Priority.Low)]
        [HarmonyPostfix]
        public static void GetPartsWithDef_Postfix(ref IEnumerable<BodyPartRecord> __result, BodyDef __instance, BodyPartDef def)
        {
            if (HumanPatcher.partImportsFromDictReverse.TryGetValue(def, out var partDefList))
            {
                if (!cachedRecordsPerPartDefDefPerBodydef.TryGetValue(__instance, out var cachedResult))
                {
                    cachedResult = [];
                    cachedRecordsPerPartDefDefPerBodydef[__instance] = cachedResult;
                }

                if (!cachedResult.TryGetValue(def, out var cachedParts))
                {
                    cachedParts = [];
                    foreach (var partDef in partDefList)
                    {
                        for (int i = 0; i < __instance.AllParts.Count; i++)
                        {
                            BodyPartRecord bodyPartRecord = __instance.AllParts[i];
                            if (bodyPartRecord.def == partDef && !cachedParts.Contains(bodyPartRecord))
                            {
                                cachedParts.Add(bodyPartRecord);
                            }
                        }
                    }
                    cachedRecordsPerPartDefDefPerBodydef[__instance][def] = cachedParts;
                }

                var resultList = __result.ToList();
                resultList.AddRange(cachedParts);
                __result = resultList;
            }
        }

        [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool MakeRecipeProducts(ref IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver, Precept_ThingStyle precept = null, ThingStyleDef style = null, int? overrideGraphicIndex = null)
        {
            if (recipeDef?.ExtensionsOnDef<RecipeExtension, RecipeDef>()?.FirstOrDefault() is RecipeExtension re && re?.pawnKindDef is PawnKindDef pkd)
            {
                __result = [];
                Faction playerFaction = Faction.OfPlayerSilentFail;
                playerFaction ??= FactionUtility.DefaultFactionFrom(pkd.defaultFactionType);

                Pawn pawn = PawnGenerator.GeneratePawn(pkd, playerFaction);
                if (pawn != null)
                {
                    GenSpawn.Spawn(pawn, worker.Position, worker.Map);
                }
                return false;
            }

            return true;
        }

        static List<string> blackListMechanical = ["PsychophagyTarget", "ChronophagyTarget", "PhilophagyTarget"];
        static List<string> blackListTrulyAgeless = ["ChronophagyTarget"];
        [HarmonyPatch(
            typeof(PsychicRitualRoleDef),
            nameof(PsychicRitualRoleDef.PawnCanDo),
            [typeof(Context), typeof(Pawn), typeof(TargetInfo), typeof(AnyEnum)],
            [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out]
        )]
        [HarmonyPostfix]
        public static void PawnCanDo_Prefix(ref bool __result, PsychicRitualRoleDef __instance, Context context, Pawn pawn, TargetInfo target, ref AnyEnum reason)
        {
            BSCache cache = HumanoidPawnScaler.GetCacheUltraSpeed(pawn);
            if (blackListMechanical.Contains(__instance.defName) && cache?.isMechanical == true)
            {
                __result = false;
                reason = AnyEnum.FromEnum(Condition.NoPsychicSensitivity);
            }

            if (blackListTrulyAgeless.Contains(__instance.defName) && GeneHelpers.GetAllActiveGenes(pawn).Any(x=>x is TrulyAgeless))
            {
                __result = false;
                reason = AnyEnum.FromEnum(Condition.NoPsychicSensitivity);
            }
        }

        [HarmonyPatch(
            typeof(Corpse),
            nameof(Corpse.ButcherProducts),
             [ typeof(Pawn), typeof(float) ]
        )]
        [HarmonyPrefix]
        public static bool ButcherProducts_Prefix(ref IEnumerable<Thing> __result, Corpse __instance, Pawn butcher, float efficiency)
        {
            if (__instance?.InnerPawn?.def?.IsMechanicalDef() == true)
            {
                IEnumerable<Thing> EnumerableFromLambda()
                {
                    foreach (var item in __instance.InnerPawn.ButcherProducts(butcher, efficiency * __instance.InnerPawn.BodySize))
                    {
                        yield return item;
                    }
                }
                __result = EnumerableFromLambda();
                return false;
            }
            return true;
        }
    }

    public class UnfinishedMechanicalRace : UnfinishedThing
    {
        public override string LabelNoCount
        {
            get
            {
                return def.LabelCap;
            }
        }
    }
}