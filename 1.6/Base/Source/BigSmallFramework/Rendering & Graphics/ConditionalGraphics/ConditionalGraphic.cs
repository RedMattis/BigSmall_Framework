using BigAndSmall.FilteredLists;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public abstract class ConditionalGraphic
    {
        public class PartRecord
        {
            public BodyPartDef bodyPartDef;
            public bool mirrored = false;
            public bool partMissing = false;
            public bool mustBeReplacement = false;
            public bool implant = false;
            public bool mustBeBetterThanNatural = false;
            public HediffDef hasHediff = null;
        }
        public enum AltTrigger
        {
            Colonist,
            Male,
            Female,
            SlaveOfColony,
            PrisonerOfColony,
            SlaveOrPrisoner,
            OfColony,
            Unconcious,
            Dead,
            Rotted,
            Dessicated,
            HasForcedSkinColorGene,
			HasForcedHairColorGene,
			IsRecolored,  // Only works for AdvancedColor.
            BiotechDLC,
            IdeologyDLC,
            AnomalyDLC,
            CustomColorAIsSet,
            CustomColorBIsSet,
            CustomColorCIsSet,
        }
        public List<PartRecord> triggerBodyPart = [];

        // If the filter evaluates to TRUE the graphic will be used.
        public FilterListSet<string> triggerGeneTag = new();
        public FilterListSet<GeneDef> triggerGene = new();
        public FilterListSet<FlagString> triggerFlags = new();
        public int randSeed = 0;
        

        private List<AltTrigger> triggers = [];
        private List<AltTrigger> triggerConditions = [];
        public float? chanceTrigger = null;
        public SimpleCurve chanceByAge = null; // 1.0 means 100% chance at age 100.
        public ChanceByStat chanceByStat = null;

        public List<FlagString> replaceFlags = [];
        public List<FlagString> replaceFlagsAndInactive = [];

        public bool HasGeneTriggers => triggerGeneTag.AnyItems() || triggerGene.AnyItems();

        public HashSet<AltTrigger> Triggers { get => [.. triggerConditions, .. triggers]; }

        public List<GraphicsOverride> GetGraphicOverrides(Pawn pawn)
        {
            var overrides = ModExtHelper.GetAllExtensions<GraphicsOverride>(pawn);
            var overridesInactive = ModExtHelper.GetAllExtensionsPlusInactive<GraphicsOverride>(pawn);
            HashSet<GraphicsOverride> allOverrides = [.. overrides, .. overridesInactive];

            List<FlagString> allFlags = [.. replaceFlags, .. replaceFlagsAndInactive];
            if (allOverrides.Any())
            {
                List<GraphicsOverride> sortedListOne = [.. overrides.SelectMany(x => x.Overrides).Where(x => x.replaceFlags.Any(t => replaceFlags.Contains(t))).OrderByDescending(x => x.priority)];
                List<GraphicsOverride> sortedListTwo = [.. overridesInactive.SelectMany(x => x.Overrides).Where(x => x.replaceFlags.Any(t => allFlags.Contains(t))).OrderByDescending(x => x.priority)];
                return [.. sortedListOne, .. sortedListTwo];
            }
            return [];
        }
        
        private bool PartTriggersIsValid(Pawn pawn)
        {
            if (triggerBodyPart.Count > 0)
            {
                var allParts = pawn.RaceProps.body.AllParts;
                foreach (var partRequire in triggerBodyPart)
                {
                    bool partFound = false;
                    if (partRequire.bodyPartDef == null) throw new Exception("PartRecord is missing a part definition.");

                    bool betterThanNatural = false;
                    bool spawnThingOnRemoval = false;
                    bool isReplacement = false;
                    bool isMissingPart = false;
                    bool hasTheHediff = false;

                    foreach (var hediff in pawn.health.hediffSet.hediffs.Where(x => x?.Part?.def == partRequire.bodyPartDef && x.Part?.flipGraphic == partRequire.mirrored))
                    {
                        partFound = true;
                        if (hediff.def.addedPartProps?.betterThanNatural == true) betterThanNatural = true;
                        if (hediff is Hediff_AddedPart) { isReplacement = true; }
                        if (hediff.def.spawnThingOnRemoved != null) spawnThingOnRemoval = true;
                        if (hediff is Hediff_MissingPart) isMissingPart = true;
                        if (hediff.def == partRequire.hasHediff) hasTheHediff = true;
                    }
                    if (partRequire.partMissing && !partFound && !allParts.Any(x => x.def == partRequire.bodyPartDef))
                    {
                        isMissingPart = true;
                    }
                    if (partRequire.bodyPartDef != null && !partFound) return false;
                    if (partRequire.mustBeBetterThanNatural && !betterThanNatural) return false;
                    if (partRequire.mustBeReplacement && !isReplacement) return false;
                    if (partRequire.implant && !spawnThingOnRemoval) return false;
                    if (partRequire.partMissing && !isMissingPart) return false;
                    if (partRequire.hasHediff != null && !hasTheHediff) return false;

                    return true;
                }
            }
            
            return true;
        }

        private bool TriggerTagsValid(Pawn pawn)
        {
            if (triggerFlags.AnyItems())
            {
                var tags = Flagger.GetTagStrings(pawn, includeInactive:false);
                var filterResult = triggerFlags.GetFilterResultFromItemList(tags);
                if (filterResult.Denied()) return false;
            }
            return true;
        }

        private bool GeneTriggersValid(Pawn pawn)
        {
            if (HasGeneTriggers)
            {
                FilterResult filterResult = FilterResult.None;
                if (!triggerGeneTag.IsEmpty())
                {
                    var genes = GeneHelpers.GetAllActiveGenes(pawn);
                    var allTags = genes.Where(x => !x.def.exclusionTags.NullOrEmpty()).SelectMany(x => x.def.exclusionTags).ToList();
                    filterResult = triggerGeneTag.GetFilterResultFromItemList(allTags);
                }
                if (!triggerGene.IsEmpty())
                {
                    var geneDefs = GeneHelpers.GetAllActiveGeneDefs(pawn).ToList();
                    filterResult = triggerGene.GetFilterResultFromItemList(geneDefs).Fuse(filterResult);
                }
                if (!filterResult.ExplicitlyAllowed()) return false;
            }
            return true;
        }


        /// <summary>
        /// "True" means use this graphic (if not chlidren are valid),
        /// "False" means skip and keep looking.
        /// "Null" is a valid result, but means that the graphic should be hidden.
        /// </summary>
        /// <returns></returns>
        public bool GetState(Pawn pawn, PawnRenderNode node=null)
        {
            int seed = pawn.thingIDNumber + pawn.def.defName.GetHashCode() + randSeed;
            if (chanceTrigger != null)
            {
                using (new RandBlock(seed))
                {
                    if (Rand.Value > chanceTrigger.Value)
                    {
                        return false;
                    }
                }
            }
            if (chanceByAge != null)
            {
                float age = pawn.ageTracker.AgeBiologicalYearsFloat;
                float chance = chanceByAge.Evaluate(age);
                using (new RandBlock(seed))
                {
                    if (Rand.Value > chance)
                    {
                        return false;
                    }
                }
            }
            if (chanceByStat != null && chanceByStat.Evaluate(pawn, seed) == false)
            {
                return false;
            }
            if (!TriggerTagsValid(pawn))
            {
                return false;
            }
            if (!PartTriggersIsValid(pawn))
            {
                return false;
            }
            if (!GeneTriggersValid(pawn))
            {
                return false;
            }


            if (Triggers.Count == 0) return true;
            var apparel = node?.GetApparelFromNode();
            Thing customTarget = apparel ?? (Thing)pawn;
            return Triggers.All(x => x switch
            {
                AltTrigger.Colonist => pawn.Faction == Faction.OfPlayer,
                AltTrigger.SlaveOfColony => pawn.HostFaction == Faction.OfPlayer && pawn.IsSlave,
                AltTrigger.Male => pawn.gender == Gender.Male,
                AltTrigger.Female => pawn.gender == Gender.Female,
                AltTrigger.PrisonerOfColony => pawn.HostFaction == Faction.OfPlayer && pawn.IsPrisoner,
                AltTrigger.SlaveOrPrisoner => pawn.IsSlave || pawn.IsPrisoner,
                AltTrigger.OfColony => pawn.HostFaction == Faction.OfPlayer || pawn.Faction == Faction.OfPlayer,
                AltTrigger.Unconcious => pawn.Downed && !pawn.health.CanCrawl,
                AltTrigger.Dead => pawn.Dead,
                AltTrigger.Rotted => pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Rotting,
                AltTrigger.Dessicated => pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated,
                AltTrigger.HasForcedSkinColorGene => GeneHelpers.GetAllActiveGenes(pawn).Any(x => x.def.skinColorOverride != null),
				AltTrigger.HasForcedHairColorGene => GeneHelpers.GetAllActiveGenes(pawn).Any(x => x.def.hairColorOverride != null),
				AltTrigger.IsRecolored => node?.GetApparelFromNode()?.GetComp<CompColorable>()?.Active == true,
                AltTrigger.BiotechDLC => ModsConfig.BiotechActive,
                AltTrigger.IdeologyDLC => ModsConfig.IdeologyActive,
                AltTrigger.AnomalyDLC => ModsConfig.AnomalyActive,
                AltTrigger.CustomColorAIsSet => CustomizableGraphic.Get(customTarget)?.colorA != null,
                AltTrigger.CustomColorBIsSet => CustomizableGraphic.Get(customTarget)?.colorB != null,
                AltTrigger.CustomColorCIsSet => CustomizableGraphic.Get(customTarget)?.colorC != null,
                _ => false,
            });
        }
    }
}
