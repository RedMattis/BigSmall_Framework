using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    //[HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.StyleItemAllowed))]
    //public static class StyleItemAllowed_Patch
    //{
    //    public static void Postfix(bool __result, StyleItemDef styleItem, Pawn_GeneTracker __instance)
    //    {

    //        if (__instance != null && styleItem != null && __instance.pawn != null && __instance.pawn.RaceProps?.Humanlike != null && __instance.pawn.genes != null)
    //        {
    //            if (styleItem is TattooDef)
    //            {
    //                var pawn = __instance.pawn;
    //                // Get all genes for the pawn
    //                var genes = Helpers.GetAllActiveGenes(pawn);
    //                foreach (var gene in genes)
    //                {
    //                    // If the gene has a modextension for tattoos
    //                    if (gene.def.GetModExtension<TattooStyleGeneExtension>() is TattooStyleGeneExtension tattooGene)
    //                    {
    //                        if(tattooGene.noTattoos)
    //                        {
    //                            __result = false;
    //                            return;
    //                        }
    //                        // If the tattoo gene is the same as the style item
    //                        if (tattooGene.whitelist.Contains(styleItem.))
    //                        {
    //                            // Allow the tattoo
    //                            __result = true;
    //                            return;
    //                        }
    //                        else if (tattooGene.blacklist.Contains(styleItem))
    //                        {
    //                            // Disallow the tattoo
    //                            __result = false;
    //                            return;
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

    //public class TattooStyleGeneExtension : DefModExtension
    //{
    //    // Whitelist of permitted styles
    //    public List<StyleItemDef> whitelist;
    //    public List<StyleItemDef> blacklist;
    //    public bool noTattoos = false;
    //}
}
