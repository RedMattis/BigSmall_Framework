using BigAndSmall.FilteredLists;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class FlagString : IEquatable<FlagString>
    {
        private const string DEFAULT = "default";

        public string mainTag;
        public string subTag = DEFAULT;
        public List<string> extraTags = [];

        public bool Equals(FlagString other) => mainTag == other.mainTag && subTag == other.subTag && extraTags.SequenceEqual(other.extraTags);
        public override bool Equals(object obj)
        {
            if (obj is FlagString other)
            {
                return mainTag == other.mainTag && subTag == other.subTag && extraTags.SequenceEqual(other.extraTags);
            }
            return false;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (mainTag?.GetHashCode() ?? 0);
                hash = hash * 23 + (subTag?.GetHashCode() ?? 0);
                return hash;
            }
        }
        public override string ToString() => $"{mainTag}/{subTag}" + (extraTags.Any() ? $"[{string.Join(",", extraTags)}]" : "");

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            mainTag = xmlRoot.Name;
            if (xmlRoot.InnerText != "")
            {
                subTag = xmlRoot.InnerText;
            }
            extraTags = xmlRoot.Attributes?.OfType<XmlAttribute>().Select(x => x.Value).ToList() ?? [];
        }
    }

    public class GraphicsOverride : DefModExtension
    {
        public List<FlagString> replaceFlags = [];

        public List<GraphicsOverride> overrideList = [];
        public float priority = 0;
        public List<ConditionalGraphic> graphics = [];
        public Vector2 drawSize = Vector2.one;

        public List<GraphicsOverride> Overrides
        {
            get
            {
                if (overrideList.Any())
                {
                    return [.. overrideList.SelectMany(x => x.Overrides).OrderByDescending(x => x.priority)];
                }
                return [this];
            }
        }
    }

    public class Flagger : DefModExtension
    {
        public float priority = 0;
        public List<FlagString> flags = [];

        public static List<FlagString> GetTagStrings(Pawn pawn, bool includeInactive)
        {
            var flagList = includeInactive ? ModExtHelper.GetAllExtensionsPlusInactive<Flagger>(pawn) : ModExtHelper.GetAllExtensions<Flagger>(pawn);
            if (flagList.Any())
            {
                return [.. flagList.OrderByDescending(x => x.priority).SelectMany(x => x.flags)];
            }
            return [];
        }
    }

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
        public FilterListSet<FlagString> triggerFlags = new();
        

        private List<AltTrigger> triggers = []; // Obsolete. It is too cryptic.
        private List<AltTrigger> triggerConditions = [];
        public float? chanceTrigger = null;

        public List<FlagString> replaceFlags = [];
        public List<FlagString> replaceFlagsAndInactive = [];

        public bool HasGeneTriggers => triggerGeneTag.AnyItems() || triggerGene.AnyItems();

        public List<AltTrigger> Triggers { get => [.. triggerConditions, .. triggers]; set => triggerConditions = value; }

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
            return Triggers.All(x => x switch
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
