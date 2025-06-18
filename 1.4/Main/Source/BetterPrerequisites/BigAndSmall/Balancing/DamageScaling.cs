using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall.Balancing
{
    //[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.GetDamageFactorFor), new Type[]
    //    {
    //    typeof(Tool),
    //    typeof(Pawn),
    //    typeof(HediffComp_VerbGiver)
    //    })
    //]
    //public static class VerbProperties_GetDamageFactorFor_Patch
    //{
    //    //public static void Postfix(ref float __result, Tool tool, Pawn attacker, VerbProperties __instance)
    //    //{
    //    //    if (BigSmall.performScaleCalculations &&
    //    //        __instance.IsMeleeAttack && attacker != null
    //    //        && BigSmall.humnoidScaler != null)
    //    //    {
    //    //        var sizeCache = HumanoidPawnScaler.GetPawnBSDict(attacker);
    //    //        if (sizeCache != null)
    //    //        {
    //    //            float damageMultiplier = sizeCache.scaleMultiplier.linear;
    //    //            if (damageMultiplier > 1)
    //    //            {
    //    //                // Make giants a bit less prone to instant-killing.
    //    //                // Mostly for balance reasons, too much instant-death otherwise.
    //    //                damageMultiplier = Mathf.Pow(damageMultiplier, BigSmallMod.settings.dmgExponent);

    //    //                // Halve increase since we're adding flat damage elsewhere now.
    //    //                //damageMultiplier = (damageMultiplier - 1) / 2 + 1;


    //    //                //    damageMultiplier *= damageAdjustment;
    //    //                //}
    //    //            }

    //    //            //TEMPORARILY TURNED OFF SO IT DOESN'T INTERFERE WITH TESTING!

    //    //            __result *= damageMultiplier;
    //    //        }
    //    //    }
    //    //}
    //}

    // AdjustedArmorPenetration
    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedArmorPenetration), new Type[]
        {
        typeof(Tool),
        typeof(Pawn),
        typeof(Thing),
        typeof(HediffComp_VerbGiver),
        })
    ]
    public static class VerbProperties_AdjustedArmorPenetration_Patch
    {
        public static void Postfix(ref float __result, Pawn attacker, VerbProperties __instance)
        {
            //float oldValue = __result;
            if (__instance.IsMeleeAttack && attacker != null)
            {
                var sizeCache = HumanoidPawnScaler.GetBSDict(attacker);
                if (sizeCache != null)
                {
                    float extraArmourPen;
                    if (sizeCache.scaleMultiplier.linear > 1)
                    {
                        // Note that each point of damage also gives 1.5% armour pen, so functionally 0.20 gives ~ 30% armour pen.
                        extraArmourPen = sizeCache.scaleMultiplier.linear * 0.20f - 0.20f;
                    }
                    else
                    {
                        extraArmourPen = sizeCache.scaleMultiplier.linear * 0.1f - 0.1f;
                    }
                    extraArmourPen = Mathf.Min(1, extraArmourPen); // Don't add more than 100% armour pen from size.

                    __result += extraArmourPen;
                }
            }
        }
    }

    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedMeleeDamageAmount),
        new Type[]
        {
        typeof(Tool),
        typeof(Pawn),
        typeof(Thing),
        typeof(HediffComp_VerbGiver),
        })
    ]
    public static class AdjustedMeleeDamageAmount_Patch
    {
        public static void Postfix(ref float __result, Tool tool, Pawn attacker, Thing equipment, HediffComp_VerbGiver hediffCompSource, VerbProperties __instance)
        //public static void Postfix(ref float __result, Verb ownerVerb, Pawn attacker, VerbProperties __instance)
        {
            __result = GetSizeAdjustedBaseDamage(__result, attacker, tool, __instance);
            //__result = GetSizeAdjustedBaseDamage(__result, attacker, ownerVerb.tool, __instance);
        }

        public static float GetSizeAdjustedBaseDamage(float __result, Pawn attacker, Tool tool, VerbProperties verbProperties)
        {
            if (attacker != null)
            {
                var sizeCache = HumanoidPawnScaler.GetBSDict(attacker);
                if (sizeCache != null)
                {
                    float sizeScale = sizeCache.scaleMultiplier.linear;
                    float maxMultipler = sizeScale;
                    if (sizeScale > 1)
                    {
                        float flatDmg = (sizeScale - 1) * BigSmallMod.settings.flatDamageIncrease;
                        float flatResult = __result + flatDmg;
                        maxMultipler = Mathf.Pow(maxMultipler, BigSmallMod.settings.dmgExponent);
                        float multipliedResult = __result * maxMultipler;

                        // Limit the damage increase base x multiplier of the base damage so we don't get Jotuns biting for 30 damage.
                        float result = Mathf.Min(multipliedResult, flatResult);
                        
                        //Log.Warning($"{attacker.Name.ToStringShort}'s {tool.label} dmg is {result} ({__result} + {sizeScale} * {8}) ({__result} * {maxMultipler}) (was {debugOldValue})");

                        __result = result;
                    }
                    else
                    {
                        __result *= sizeScale;
                    }
                }
            }
            return __result;
        }
    }

    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedMeleeDamageAmount),
        new Type[]
        {
        typeof(Tool),
        typeof(Pawn),
        typeof(ThingDef),
        typeof(ThingDef),
        typeof(HediffComp_VerbGiver),
        })
    ]
    public static class AdjustedMeleeDamageAmount_Patch_2
    {
        public static void Postfix(ref float __result, Tool tool, Pawn attacker, ThingDef equipment,
            ThingDef equipmentStuff, HediffComp_VerbGiver hediffCompSource, VerbProperties __instance)
        {
            __result = AdjustedMeleeDamageAmount_Patch.GetSizeAdjustedBaseDamage(__result, attacker, tool, __instance);
        }
    }
}
