using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public partial class PawnKindExtension : DefModExtension
    {
        
        public void TryChangeApparel(Pawn pawn)
        {
            if (blockAllApparel)
            {
                if (pawn.apparel?.WornApparel != null && pawn.apparel.WornApparel.Any())
                {
                    pawn.apparel.DestroyAll();
                }
            }
            else if (blockAllNonNudityApparel)
            {
                if (pawn.apparel?.WornApparel != null && pawn.apparel.WornApparel.Any())
                {
                    var toDestroy = new List<Apparel>();
                    foreach (var apparel in pawn.apparel.WornApparel)
                    {
                        if (!apparel.def.apparel.countsAsClothingForNudity || apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
                        {
                            toDestroy.Add(apparel);
                        }
                    }
                    foreach (var apparel in toDestroy)
                    {
                        pawn.apparel.Remove(apparel);
                        apparel.Destroy();
                    }
                }
            }
            else if (preventPantless || preventShirtless)
            {
                if (pawn.story?.traits?.HasTrait(TraitDefOf.Nudist) == true) return;

                Color? pawnKindColor = pawn.kindDef.apparelColor == Color.white ? null : pawn.kindDef.apparelColor;

                // Check Legs are covered.
                if (pawn.apparel?.WornApparel != null)
                {
                    bool legsCovered = false;
                    bool torsoCovered = false;
                    List<Apparel> newApparelItems = [];
                    foreach (var apparel in pawn.apparel.WornApparel)
                    {
                        if (apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs))
                        {
                            legsCovered = true;
                        }
                        if (apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
                        {
                            torsoCovered = true;
                        }
                    }
                    if (!legsCovered && preventPantless)
                    {
                        // Check so the pawn actually has legs.
                        if (pawn.RaceProps.body.AllParts.Any(x => x.groups.Contains(BodyPartGroupDefOf.Legs)))
                        {
                            var techTier = pawn.Faction?.def.techLevel ?? TechLevel.Neolithic;
                            if (techTier == TechLevel.Neolithic && DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_TribalA") is ThingDef tribalWear
                                && tribalWear.apparel.PawnCanWear(pawn) && pawn.apparel?.CanWearWithoutDroppingAnything(tribalWear) == true)
                            {
                                Apparel newApparel = (Apparel)ThingMaker.MakeThing(tribalWear, GenStuff.RandomStuffFor(tribalWear));
                                newApparelItems.Add(newApparel);
                                pawn.apparel.Wear(newApparel);
                            }
                            else if (techTier > TechLevel.Neolithic && DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_Pants") is ThingDef pantsDef
                                && pantsDef.apparel.PawnCanWear(pawn) && pawn.apparel?.CanWearWithoutDroppingAnything(pantsDef) == true)
                            {
                                Apparel newApparel = (Apparel)ThingMaker.MakeThing(pantsDef, GenStuff.RandomStuffFor(pantsDef));
                                newApparelItems.Add(newApparel);
                                pawn.apparel.Wear(newApparel, false);
                            }
                        }
                    }
                    if (!torsoCovered)
                    {
                        if (pawn.RaceProps.body.AllParts.Any(x => x.groups.Contains(BodyPartGroupDefOf.Torso)))
                        {
                            var techTier = pawn.Faction?.def.techLevel ?? TechLevel.Neolithic;
                            if (techTier == TechLevel.Neolithic && DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_TribalA") is ThingDef tribalWear
                                && tribalWear.apparel.PawnCanWear(pawn) && pawn.apparel?.CanWearWithoutDroppingAnything(tribalWear) == true)
                            {
                                Apparel newApparel = (Apparel)ThingMaker.MakeThing(tribalWear, GenStuff.RandomStuffFor(tribalWear));
                                newApparelItems.Add(newApparel);
                                pawn.apparel.Wear(newApparel);
                            }
                            else if (techTier > TechLevel.Neolithic && DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_BasicShirt") is ThingDef shirtDef
                                && shirtDef.apparel.PawnCanWear(pawn) && pawn.apparel?.CanWearWithoutDroppingAnything(shirtDef) == true)
                            {
                                Apparel newApparel = (Apparel)ThingMaker.MakeThing(shirtDef, GenStuff.RandomStuffFor(shirtDef));
                                newApparelItems.Add(newApparel);
                                pawn.apparel.Wear(newApparel, false);
                            }
                        }
                    }
                    if (pawnKindColor is Color pColor)
                    {
                        foreach (var apparel in newApparelItems)
                        {
                            apparel.DrawColor = pColor;
                        }
                    }
                }
            }
        }

        
    }
}
