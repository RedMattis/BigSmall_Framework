using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public class TakeDamagePatch
    {
        public static void Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            if (__instance is Pawn pawn == false)
                return;

            var sizeCache = HumanoidPawnScaler.GetCache(pawn, scheduleForce: 30);
            if (sizeCache == null)
                return;

            // Get damagedef by name
            var bombSuper = DefDatabase<DamageDef>.GetNamedSilentFail("BombSuper");

            // For bullet resistance.
            bool isBullet = dinfo.Def == DamageDefOf.Bullet;
            bool isArrow = dinfo.Def == BSDefs.Arrow;
            bool isStab = dinfo.Def == DamageDefOf.Stab;

            // For blunt force resistance.
            bool isBlunt = dinfo.Def == DamageDefOf.Blunt;
            bool isCrush = dinfo.Def == DamageDefOf.Crush;
            bool isExplosion = dinfo.Def == DamageDefOf.Bomb || dinfo.Def == bombSuper;
            bool isPoison = dinfo.Def == DamageDefOf.ToxGas || dinfo.Def.defName.ToLower().Contains("poison") || dinfo.Def.defName.ToLower().Contains("tox") || dinfo.Def.defName.ToLower().Contains("venom");
            bool isBurn = dinfo.Def == DamageDefOf.Flame || dinfo.Def.defName == "Burn" || dinfo.Def.defName == "Fire";

            // For Acid resistance
            bool isAcid = dinfo.Def.defName.ToLower().Contains("acid") || dinfo.Def.defName.ToLower().Contains("corro");



            // if instance is a pawn.
            if (isBullet || isArrow || isStab)
            {
                // Cache this!
                StatDef bulletDmgMultStat = StatDef.Named("SM_BulletDmgMult");
                float bulletDmgMult = pawn.GetStatValue(bulletDmgMultStat);
                // end cache-section

                if (isStab)
                {
                    // The multiplier is a value between 0 and 1. 1 is no reduction, 0 is full reduction. Halve the effect of the reduction for
                    // stab damage.
                    bulletDmgMult = 1 - (1 - bulletDmgMult) / 2;

                    //bulletDmgMult = bulletDmgMult / 2 + 0.5f;
                }

                if (bulletDmgMult != 1)
                {
                    dinfo.SetAmount(dinfo.Amount * bulletDmgMult);
                }
            }
            if (isBlunt || isCrush || isExplosion)
            {
                // Cache this!
                StatDef concDmgMultStat = StatDef.Named("SM_ConcussiveDmgMult");
                float concDmgMult = pawn.GetStatValue(concDmgMultStat);
                // end cache-section

                if (isBlunt)
                {
                    concDmgMult = 1 - (1 - concDmgMult) / 2;
                }

                if (concDmgMult != 1)
                {
                    dinfo.SetAmount(dinfo.Amount * concDmgMult);
                }
            }
            if (isAcid)
            {
                // Cache this!
                StatDef acidDmgMultStat = StatDef.Named("SM_AcidDmgMult");
                float acidDmgMult = pawn.GetStatValue(acidDmgMultStat);
                // end cache-section
                if (isPoison)
                {
                    acidDmgMult = 1 - (1 - acidDmgMult) / 2;
                }

                if (acidDmgMult != 1)
                {
                    acidDmgMult = Mathf.Clamp(acidDmgMult, 0, 99);
                    dinfo.SetAmount(dinfo.Amount * acidDmgMult);
                }
            }

            if (isBurn)
            {
                // Get Flame Damage Factor to check if the pawn is fire-immune.
                float fireDamageMult = pawn.genes?.FactorForDamage(dinfo) ?? 1;
                fireDamageMult *= pawn.health.FactorForDamage(dinfo);

                if (fireDamageMult > 0)
                {
                    // Check if they have the "BS_ReturningSoul" gene.
                    var validGenes = GeneHelpers.GetActiveGenesByName(pawn, "BS_ReturningSoul");
                    if (validGenes.Count() > 0)
                    {
                        // Add the BS_BurnReturnDenial hediff if they don't already have it.
                        if (pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.BS_BurnReturnDenial) == null)
                        {
                            pawn.health.AddHediff(BSDefs.BS_BurnReturnDenial);
                        }
                    }
                }
            }

        }
    }
}
