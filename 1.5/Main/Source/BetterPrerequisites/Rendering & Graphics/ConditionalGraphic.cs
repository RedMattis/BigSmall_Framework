using BigAndSmall.FilteredLists;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public abstract class ConditionalGraphic
    {
        //public enum TriggerResult
        //{
        //    False,
        //    True,
        //    Error,
        //}
        public class PartRecord
        {
            public BodyPartDef bodyPartDef;
            public bool mirrored = false;
            public bool partMissing = false;
            public bool mustBeReplacement = false;
            public bool mustBeBetterThanNatural = false;
            public HediffDef hasHediff = null;
        }
        public enum AltTrigger
        {
            Colonist,
            SlaveOfColony,
            PrisonerOfColony,
            SlaveOrPrisoner,
            OfColony,
            Unconcious,
            Dead,
            Rotted,
            Dessicated,
            HasForcedSkinColorGene,
            BiotechDLC,
            IdeologyDLC,
            AnomalyDLC,
        }
        public List<PartRecord> triggerBodyPart = [];

        // If the filter evaluates to TRUE the graphic will be used.
        public FilterListSet<string> triggerGeneTag = new();
        public FilterListSet<GeneDef> triggerGene = new();

        public bool HasGeneTriggers => triggerGeneTag.AnyItems() || triggerGene.AnyItems();

        public List<AltTrigger> triggers = [];
        public float? chanceTrigger = null;
        private bool PartTriggersIsValid(Pawn pawn)
        {
            if (triggerBodyPart.Count > 0)
            {
                
                var allParts = pawn.RaceProps.body.AllParts;
                foreach (var partRequire in triggerBodyPart)
                {
                    bool requirementValid = false;
                    bool partFound = false;
                    if (partRequire.bodyPartDef == null) throw new Exception("PartRecord is missing a part definition.");
                    foreach (var hediff in pawn.health.hediffSet.hediffs.Where(x=>x?.Part?.def == partRequire.bodyPartDef && x.Part?.flipGraphic == partRequire.mirrored))
                    {
                        partFound = true;
                        bool partValid = true;
                        bool betterThanNatural = hediff.def.addedPartProps?.betterThanNatural == true;
                        bool spawnThingOnRemoval = hediff.def.spawnThingOnRemoved != null;
                        bool isMissingPart = hediff is Hediff_MissingPart;
                        if (partRequire.mustBeReplacement && !spawnThingOnRemoval) { partValid = false; }
                        if (partRequire.mustBeBetterThanNatural && !betterThanNatural) { partValid = false; }
                        if (isMissingPart && !partRequire.partMissing) { partValid = false; }
                        if (partRequire.hasHediff != null && hediff.def != partRequire.hasHediff) { partValid = false; }

                        if (partValid)
                        {
                            requirementValid = true;
                        }
                    }
                    if (partRequire.partMissing && !partFound && !allParts.Any(x => x.def == partRequire.bodyPartDef))
                    {
                        requirementValid = true;
                    }
                    if (!requirementValid)
                    {
                        return false;
                    }
                }
            }
            if (HasGeneTriggers)
            {
                FilterResult filterResult = FilterResult.None;
                if (!triggerGeneTag.IsEmpty())
                {
                    var genes = GeneHelpers.GetAllActiveGenes(pawn);
                    var allTags = genes.Where(x=>!x.def.exclusionTags.NullOrEmpty()).SelectMany(x => x.def.exclusionTags).ToList();
                    filterResult = triggerGeneTag.GetFilterResultFromItemList(allTags).Fuse(filterResult);
                }
                if (!triggerGene.IsEmpty())
                {
                    var genes = GeneHelpers.GetAllActiveGenes(pawn);
                    var allGenes = genes.Where(x => !x.def.exclusionTags.NullOrEmpty()).Select(x => x.def).ToList();
                    filterResult = triggerGene.GetFilterResultFromItemList(allGenes).Fuse(filterResult);
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
        /// <param name="pawn"></param>
        /// <returns></returns>
        public bool GetState(Pawn pawn)
        {
            if (chanceTrigger != null)
            {
                using (new RandBlock(pawn.thingIDNumber + pawn.def.defName.GetHashCode()))
                {
                    if (Rand.Value > chanceTrigger.Value)
                    {
                        return false;
                    }
                }
            }
            if (!PartTriggersIsValid(pawn))
            {
                return false;
            }


            if (triggers.Count == 0) return true;
            return triggers.All(x => x switch
            {
                AltTrigger.Colonist => pawn.Faction == Faction.OfPlayer,
                AltTrigger.SlaveOfColony => pawn.HostFaction == Faction.OfPlayer && pawn.IsSlave,
                AltTrigger.PrisonerOfColony => pawn.HostFaction == Faction.OfPlayer && pawn.IsPrisoner,
                AltTrigger.SlaveOrPrisoner => pawn.IsSlave || pawn.IsPrisoner,
                AltTrigger.OfColony => pawn.HostFaction == Faction.OfPlayer || pawn.Faction == Faction.OfPlayer,
                AltTrigger.Unconcious => pawn.Downed && !pawn.health.CanCrawl,
                AltTrigger.Dead => pawn.Dead,
                AltTrigger.Rotted => pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Rotting,
                AltTrigger.Dessicated => pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated,
                AltTrigger.HasForcedSkinColorGene => GeneHelpers.GetAllActiveGenes(pawn).Any(x => x.def.skinColorOverride != null),
                AltTrigger.BiotechDLC => ModsConfig.BiotechActive,
                AltTrigger.IdeologyDLC => ModsConfig.IdeologyActive,
                AltTrigger.AnomalyDLC => ModsConfig.AnomalyActive,
                _ => false,
            });
        }
    }
}
