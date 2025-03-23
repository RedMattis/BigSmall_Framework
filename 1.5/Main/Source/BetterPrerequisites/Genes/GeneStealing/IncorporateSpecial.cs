using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class MutantToGeneset
    {
        public static List<GeneDef> GetGenesFromAnomalyCreature(Pawn pawn)
        {
            var undeadSet = new List<GeneDef>
            {
                BSDefs.ToxResist_Total,
                BSDefs.Sterile,
                BSDefs.VU_NoBlood,
                BSDefs.VU_Unliving_Resilience,
                BSDefs.VU_Unliving,
                BSDefs.Beauty_Ugly,
                BSDefs.BS_DessicatedBodyWithGreyHair,
                BSDefs.BS_DessicatedBodyWithHair,
                BSDefs.BS_DessicatedBody,
                BSDefs.BS_GhoulHead,
            };
            if (pawn.IsGhoul)
            {
                return
                [
                    BSDefs.BS_Instability_Catastrophic,
                    BSDefs.WoundHealing_SuperFast,
                    BSDefs.VU_UltraRapidAging,
                    BSDefs.BS_VeryFastAging,
                    BSDefs.BS_FastAging,
                    BSDefs.BS_PainNumb,
                    BSDefs.BS_ToughSkin,
                    BSDefs.BS_NaturalArmor, ..undeadSet
                ];
            }
            else if (pawn.IsShambler)
            {
                return undeadSet;
            }
            else if (pawn.def.race.FleshType == FleshTypeDefOf.Fleshbeast)
            {
                return
                [
                    BSDefs.BS_Instability_Catastrophic,
                    BSDefs.BS_CellPandemonium,
                    BSDefs.WoundHealing_SuperFast,
                    BSDefs.VU_UltraRapidAging,
                    BSDefs.BS_VeryFastAging,
                    BSDefs.BS_FastAging,
                    BSDefs.BS_PainNumb,
                    BSDefs.BS_ToughSkin,
                    BSDefs.BS_NaturalArmor,
                    BSDefs.VU_Hermaphromorph,
                ];
            }
            else if (pawn.def.race.FleshType == FleshTypeDefOf.EntityFlesh)
            {
                if (pawn.def.defName == "Devourer")
                {
                    return
                    [
                        BSDefs.BS_Instability_Catastrophic,
                        BSDefs.BS_SnekEngulf,
                        BSDefs.BS_PainNumb,
                        BSDefs.BS_GeneEater,
                        BSDefs.BS_ToughSkin,
                        BSDefs.BS_NaturalArmor,
                        BSDefs.BS_FeedingFrenzy
                    ];
                }
                return
                [
                    ..undeadSet
                ];
            }
            else
            {
                return [];
            }
        }
    }
}

