using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList))]
    public static class GetUpdatedAvailableVerbsList_Patch
    {
        public static void Postfix(ref List<VerbEntry> __result, Pawn ___pawn, bool terrainTools)
        {
            Pawn pawn = ___pawn;

            if (!terrainTools)
            {
                var sizeCache = HumanoidPawnScaler.GetCache(pawn);
                if (sizeCache != null && sizeCache.unarmedOnly)
                {
                    // Basically go through the equipment again, and remove any verbs that were added from them.
                    // In practice this means weapons (equippment) won't work, but armour and hediff-sourced verbs will.
                    if (pawn.equipment != null)
                    {
                        List<ThingWithComps> allEquipmentListForReading = pawn.equipment.AllEquipmentListForReading;
                        for (int j = 0; j < allEquipmentListForReading.Count; j++)
                        {
                            CompEquippable comp = allEquipmentListForReading[j].GetComp<CompEquippable>();
                            if (comp == null)
                            {
                                continue;
                            }
                            List<Verb> allVerbs2 = comp.AllVerbs;
                            if (allVerbs2 == null)
                            {
                                continue;
                            }
                            foreach(var verb in allVerbs2)
                            {
                                for (int veIdx = __result.Count - 1; veIdx >= 0; veIdx--)
                                {
                                    VerbEntry verbEntry = __result[veIdx];
                                    if (verbEntry.verb.EquipmentSource == allEquipmentListForReading[j])
                                    {
                                        __result.Remove(verbEntry);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

