using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class Gibblets
    {
        public static void SpawnGibblets(Pawn pawn, IntVec3 centerPos, Map map, int bloodMin = 10, int bloodMax = 20, int gibbletMin = 1, int gibbletMax = 3, float bloodChance = 1, float gibbletChance = 1,
            float randomOrganChance = 0, float skullChance = 0)
        {
            bool spawnBlood = Rand.Chance(bloodChance);
            bool spawnGibblets = Rand.Chance(gibbletChance) && spawnBlood;
            bool spawnRandomOrgans = Rand.Chance(randomOrganChance);
            bool spawnSkull = Rand.Chance(skullChance);

            if (spawnBlood)
            {
                int randomCount = (int)(Rand.RangeInclusive(bloodMin, bloodMax) * pawn.BodySize);
                var bloodType = pawn?.RaceProps?.BloodDef; // Scatter blood over the place
                if (bloodType != null)
                {
                    FilthMaker.TryMakeFilth(centerPos, map, bloodType, randomCount);
                }
            }

            if (spawnGibblets)
            {
                int randomMeatChunkCount = Rand.RangeInclusive(gibbletMin, gibbletMax);
                if (pawn.RaceProps?.meatDef != null)
                {
                    for (int i = 0; i < randomMeatChunkCount; i++)
                    {
                        Thing gib;
                        if (pawn.RaceProps.IsMechanoid)
                        {
                            if (Rand.Chance(0.20f))
                            {
                                gib = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
                            }
                            else
                            {
                                gib = ThingMaker.MakeThing(ThingDefOf.Steel);
                            }
                        }
                        else
                        {
                            gib = ThingMaker.MakeThing(pawn.RaceProps.meatDef);
                        }
                        gib.stackCount = (int)Mathf.Max(1, (Rand.RangeInclusive(-1, 2) * pawn.BodySize));
                        // Offset position randomly one square
                        var position = centerPos + new IntVec3(Rand.Range(-1, 1), 0, Rand.Range(-1, 1));

                        GenSpawn.Spawn(gib, position, map);
                    }
                }
            }
            if (spawnRandomOrgans)
            {
                List<ThingDef> whiteList =
                [
                    BSDefs.Heart, BSDefs.Liver, BSDefs.Lung, BSDefs.Kidney
                ];

                // Check if the pawn has any of the organs in the white list, if so, select the whitlist entry.
                // Get all not-missing parts
                var bodyOrgans = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null);
                var organThings = whiteList.Where(x => bodyOrgans.Any(y => y.def.defName == x.defName));

                // Spawn a random organ from the list
                if (organThings.Any())
                {
                    var organ = organThings.RandomElement();
                    var organThing = ThingMaker.MakeThing(organ);
                    GenSpawn.Spawn(organThing, centerPos, map);

                    //// Remove the organ from the pawn Pointless at the moment since this is only used if the pawn is gibbed into nothingness.
                    //var bodyPartRecord = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null).Where(x => x.def.defName == organ.defName).FirstOrDefault();

                    //// Add Hediff_MissingPart to the part.
                    //var hediff = HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, bodyPartRecord);
                }
            }
            if (spawnSkull && ModsConfig.IdeologyActive)
            {
                var skull = ThingMaker.MakeThing(ThingDefOf.Skull);
                GenSpawn.Spawn(skull, centerPos, map);
            }
        }
    }
}
