using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace BigAndSmall
{

    [HarmonyPatch]
    public class Piloted : HediffWithComps, IThingHolder
    {
        private static bool forcePilotableUpdate = false;

        public bool removeIfNoPilot = false;
        public bool defaultEnterable = true;

        private CompProperties_Piloted props = null;
        protected ThingOwner innerContainer = null;

        private int startPilotTime = 0;
        protected Ideo cachedIdeology = null;
        protected Faction cachedFaction = null;


        public CompProperties_Piloted Props => GetProperties();
        public float BaseCapacity => Props.baseCapacity;
        public IThingHolder ParentHolder => pawn;

        public int PilotCapacity => Props.pilotCapacity;
        public int PilotCount => innerContainer.Where(x => x is Pawn).Count();

        public float TotalMass =>
            innerContainer.Where(x => x is Pawn).Sum(x => ((Pawn)x).BodySize) +
            innerContainer.Where(x => x is Corpse).Sum(x => ((Corpse)x).InnerPawn.BodySize);
        public float Fullness => TotalMass / pawn.BodySize;
        public float MaxCapacity { get => BaseCapacity * (pawn.BodySize + (pawn.story.traits.allTraits.Any(x => x.def.defName == "VFEP_WarcasketTrait_Mech") ? 1.05f : 0)); }

        public ThingOwner InnerContainer
        {
            get
            {
                innerContainer ??= new ThingOwner<Thing>(this, oneStackOnly: false);
                return innerContainer;
            }
            set => innerContainer = value;
        }

        public CompProperties_Piloted GetProperties()
        {
            if (props != null) return props;
            if (!(comps.Where(x => x is PilotedCompProps).FirstOrDefault() is PilotedCompProps pcp))
            {
                Log.Error("BS_Piloted: No PilotedCompProps found on hediff " + def.defName);
                return null;
            }
            props = pcp.Props;
            return props;
        }

        public override string LabelInBrackets
        {
            get
            {
                // Get the name of the first pilot.
                Pawn pilot = InnerContainer.Where(x => x is Pawn).FirstOrDefault() as Pawn;
                if (pilot != null)
                {
                    if (pilotEjectCountdown != -1)
                    {
                        return "BS_PilotEjectCoutndown".Translate(); ;
                    }

                    return "BS_PilotedBy".Translate() + " " + pilot.Name.ToStringShort;
                }
                return base.LabelInBrackets;
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return InnerContainer;
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            //InnerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
        }

        public void AddPilot(Thing thing)
        {
            try
            {
                // Log potential issues
                if (pawn == null) { Log.Warning($"BS_Piloted: Pilotable entity was null"); }
                if (thing == null) { Log.Warning($"BS_Piloted: Tried to add null pilot to {pawn.Name}."); }
                if (InnerContainer == null) { Log.Warning($"BS_Piloted: InnerContainer was null for {pawn.Name}."); }

                thing.DeSpawnOrDeselect();
                if (thing.holdingOwner != null)
                {
                    int result = thing.holdingOwner.TryTransferToContainer(thing, InnerContainer, thing.stackCount);
                    if (result == 0) { Log.Warning($"Failed to transfer pilot to piloted hediff of {pawn.Name}."); }
                }
                else
                {
                    bool result = InnerContainer.TryAdd(thing);
                    if (!result) { Log.Warning($"Failed to add pilot to piloted hediff of {pawn.Name}."); }
                }

            }
            catch (Exception e)
            {
                Log.Warning($"Failed to add pilot to piloted hediff.{e.Message}\n{e.StackTrace}");
            }
            try
            { 
                if (Props.removeIfNoPilot)
                {
                    removeIfNoPilot = true;
                }

                // Check if the is a pilot there already:
                var otherPilot = InnerContainer.Where(x => x is Pawn && x != thing).FirstOrDefault();
                if (otherPilot == null && thing is Pawn pilot)
                {
                    pawn.guest?.SetGuestStatus(null);

                    if (ModsConfig.IdeologyActive && Props.temporarilySwapIdeology && pilot.Ideo != null)
                    {
                        cachedIdeology = pawn.Ideo;
                        pawn.ideo.SetIdeo(pilot.Ideo);
                    }
                    if (Props.temporarilySwapFaction && pilot.Faction != null)
                    {
                        // Reduce odds of getting killed if downed. Keep doing so for each repeat.
                        pawn.health.overrideDeathOnDownedChance = pawn.health.overrideDeathOnDownedChance > 0
                            ? pawn.health.overrideDeathOnDownedChance / 2
                            : Find.Storyteller.difficulty.enemyDeathOnDownedChanceFactor / 2;
                        
                        cachedFaction = pawn.Faction;
                        pawn.SetFaction(pilot.Faction);
                    }

                    try { InheritPilotSkills(pilot, pawn); } catch (Exception e) { Log.Warning($"Failed to transfer pilot skills:\n{e.Message}\n{e.StackTrace}"); }
                    try { InheritPilotTraits(pilot); } catch (Exception e) { Log.Warning($"Failed to transfer pilot traits:\n{e.Message}\n{e.StackTrace}"); }
                    try { InheritRelationships(pilot, pawn); } catch (Exception e) { Log.Warning($"Failed to transfer pilot relationships:\n{e.Message}\n{e.StackTrace}"); }
                    startPilotTime = Find.TickManager.TicksGame;
                }
            }
            catch (Exception e)
            {
                Log.Warning("Failed to transfer all pilot properties to pilotable: " + e.Message + e.StackTrace);
            }
            forcePilotableUpdate = true;
            pawn.health.Notify_HediffChanged(this);
            return;
        }

        public static readonly List<string> PhysicalTraitList =
        [
            "speedoffset", "beauty", "gigantism", "large", "small", "dwarfism", "bs_giant", "tough"
        ];

        public void InheritRelationships(Pawn pilot, Pawn target)
        {
            if (pilot == null || target == null) return;
            // Transfer faction
            if (pilot.Faction != null && pilot.Faction != target.Faction)
            {
                target.SetFaction(pilot.Faction);
            }

            // Transfer ideology
            if (pilot.Ideo != null)
            {
                target.ideo.SetIdeo(pilot.Ideo);
            }

            if (Props.inheritRelationShips == false)
            {
                return;
            }

            // Transfer resistance values
            target.guest.resistance = pilot.guest.resistance;

            // Transfer Will
            target.guest.will = pilot.guest.will;  

            // Get the pilot's relations
            List<DirectPawnRelation> pilotRelations = pilot.relations?.DirectRelations.ToList();

            // Get the pilot's thoughts
            List<Thought_Memory> pilotThoughts = pilot.needs?.mood?.thoughts?.memories?.Memories?.ToList();

            // Clear the target's relations and thoughts
            target.relations?.ClearAllRelations();
            target.needs?.mood?.thoughts?.memories?.Memories?.Clear();
            target.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();

            var literallyAllPawns = Find.WorldPawns.AllPawnsAliveOrDead.ToList();
            literallyAllPawns = [.. literallyAllPawns.Concat(Find.Maps.SelectMany(x => x.mapPawns.AllPawns)).Distinct()];

            // Add all other relations-pawns that show up in the pilot's thoughts to "literallyAllPawns".
            if (pilotThoughts != null)
            {
                foreach (var thought in pilotThoughts)
                {
                    if (thought.otherPawn != null)
                    {
                        literallyAllPawns.Add(thought.otherPawn);
                    }
                }
            }

            var directPawnRelationsToSource = new Dictionary<DirectPawnRelation, Pawn>();
            // Fetch all relations to the pilot.
            foreach (var somePawn in literallyAllPawns)
            {
                if (somePawn == pilot)
                {
                    continue;
                }
                var relations = somePawn.relations?.DirectRelations?.ToList();
                if (relations != null)
                {
                    try
                    {
                        foreach (var relation in relations)
                        {
                            if (relation?.otherPawn != null && relation.otherPawn == pilot)
                            {
                                directPawnRelationsToSource.Add(relation, somePawn);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to fetch relations to pilot {pilot.Name} from {somePawn.Name} with error: \n{e.Message}\n{e.StackTrace}");
                    }
                }
            }

            // Add all direct relations of the pilot to the piloted.
            for (int idx = pilotRelations.Count - 1; idx >= 0; idx--)
            {
                DirectPawnRelation relation = pilotRelations[idx];
                try
                {
                    if (!target.relations?.DirectRelationExists(relation.def, relation.otherPawn) == true)
                    {
                        target?.relations?.AddDirectRelation(relation.def, relation.otherPawn);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to add relation {relation.def.defName} to {target.Name} from {pilot.Name} with error: \n{e.Message}\n{e.StackTrace}");
                }
            }

            // Add direct relations from other pawns to the pilot.
            foreach (var relation in directPawnRelationsToSource)
            {
                var otherPawn = relation.Value;
                var relationDef = relation.Key.def;
                try
                {
                    otherPawn.relations.AddDirectRelation(relationDef, target);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to add relation {relationDef.defName} to {otherPawn.Name} from {target.Name} with error: \n{e.Message}\n{e.StackTrace}");
                }
            }

            // Add all thoughts of the pilot to the piloted.
            for (int idx = pilotThoughts.Count - 1; idx >= 0; idx--)
            {
                Thought_Memory thought = pilotThoughts[idx];
                try
                {
                    var newThought = ThoughtMaker.MakeThought(thought.def, thought.sourcePrecept);
                    newThought.CopyFrom(thought);
                    thought.pawn = target;
                    target.needs.mood.thoughts.memories.TryGainMemory(newThought, thought.otherPawn);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to add thought {thought.def.defName} to {target.Name} from {pilot.Name} with error: \n{e.Message}\n{e.StackTrace}");
                }
            }

            if (pawn.needs?.mood?.thoughts != null)
            {
                pawn?.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();
            }

            if (ModsConfig.RoyaltyActive)
            {
                target.royalty = new Pawn_RoyaltyTracker(pawn);
                // Transfer titles
                var titles = pilot.royalty?.AllTitlesForReading;
                if (titles != null)
                {
                    foreach (var title in titles)
                    {
                        target.royalty?.SetTitle(title.faction, title.def, grantRewards: false, sendLetter: false);
                        if (pawn?.royalty?.GetFavor(title.faction) is int favorAmount)
                        {
                            target.royalty.SetFavor(title.faction, favorAmount);
                        }
                    }
                }
            }

            //// Remove all relations and thoughts of the source.
            pilot.relations?.ClearAllRelations();
            pilot.needs?.mood?.thoughts?.memories?.Memories?.Clear();
            pilot.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();
            target.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();

        }

        public void InheritPilotSkills(Pawn source, Pawn target)
        {
            if (!Props.inheritPilotSkills)
            {
                return;
            }
            // Get the pilot skills
            List<SkillRecord> pilotSkills = source.skills.skills;
            List<SkillRecord> targetSkills = target.skills.skills;
            // Loop through the pilot skills
            for (int i = 0; i < pilotSkills.Count; i++)
            {
                SkillRecord pilotSkill = pilotSkills[i];
                SkillRecord targetSkill = targetSkills[i];

                // Set skills & passions
                targetSkill.levelInt = pilotSkill.levelInt;
                targetSkill.passion = pilotSkill.passion;
                targetSkill.xpSinceLastLevel = pilotSkill.xpSinceLastLevel;
                targetSkill.xpSinceMidnight = pilotSkill.xpSinceMidnight;
            }
        }
        public void InheritPilotTraits(Pawn pilot)
        {
            if (!Props.inheritPilotMentalTraits)
            {
                return;
            }
            // Remove all traits from the pilotable which are not physical.
            var traitsToRemove = pawn.story.traits.allTraits.Where(x => !PhysicalTraitList.Any(y => x.def.defName.ToLower().StartsWith(y))).ToList();
            foreach (Trait trait in traitsToRemove)
            {
                pawn.story.traits.allTraits.Remove(trait);
            }

            // Add all traits from the pilot (that are not physical).
            var traitsToAdd = pilot.story.traits.allTraits.Where(x => !PhysicalTraitList.Any(y => x.def.defName.ToLower().StartsWith(y))).ToList();
            foreach (Trait trait in traitsToAdd)
            {
                pawn.story.traits.GainTrait(trait);
            }
        }


        public void RemovePilots(bool mayRemoveHediff = true)
        {
            IList<Thing> content = InnerContainer;

            if (content.Count == 0)
            {
                return;
            }
            foreach (Thing thing in content.Where(x => x is Pawn))
            {
                // Push skills improvements back from the pilotable entity to the pilot.
                try { InheritPilotSkills(pawn, thing as Pawn); } catch (Exception e)
                {
                    Log.Error($"Failed to inherit skills from {pawn.Name} with error:\n{e.Message}\n{e.StackTrace}");
                }

                // Restore relationships from the pilotable entity to the pilot.
                try { InheritRelationships(pawn, thing as Pawn); }
                catch (Exception e)
                {
                    Log.Error($"Failed to inherit relationships from {pawn.Name} with error:\n{e.Message}\n{e.StackTrace}");
                }
                break; // Never add skills from more than one pilot, or we might overwrite some other pawn's skills.
            }

            
            // Remove everything in innerContainer
            for (int i = content.Count - 1; i >= 0; i--)
            {
                Thing thing = content[i];
                TryLearnSkill(thing);

                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.MapHeld, ThingPlaceMode.Near);
            }

            if (!pawn.Dead && Props.temporarilySwapIdeology && cachedIdeology != null)
            {
                pawn.ideo.SetIdeo(cachedIdeology);
                cachedIdeology = null;
            }
            if (!pawn.DestroyedOrNull() && Props.temporarilySwapFaction && cachedFaction != null)
            {
                pawn.SetFaction(cachedFaction);
                cachedFaction = null;
            }

            pilotEjectCountdown = -1; // Reset the eject countdown.
            pawn.health.Notify_HediffChanged(this);
            forcePilotableUpdate = true;
            

            // mayRemoveHediff is false when called from PostRemoved or Pawn.Kill to avoid recursion.
            if (mayRemoveHediff && removeIfNoPilot && pawn?.health?.hediffSet?.HasHediff(def) == true)
            {
                try
                {
                    if (!pawn.Dead && props.injuryOnRemoval is int injuryAmount && injuryAmount > 0)
                    {
                        var mainBodyPart = pawn.RaceProps.body.corePart;
                        if (mainBodyPart != null)
                        {
                            var damageInfo = new DamageInfo(DamageDefOf.Cut, injuryAmount * pawn.BodySize, 300, -1, null, mainBodyPart);
                            pawn.TakeDamage(damageInfo);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to apply removal injury to {pawn.Name} with error:\n{e.Message}\n{e.StackTrace}");
                }
                pawn.health.RemoveHediff(this);
            }
        }

        private void TryLearnSkill(Thing thing)
        {
            if (thing is Pawn pilot && Props.pilotLearnSkills is float skillLearnFactor)
            {
                if (pilot?.skills?.skills == null || pawn?.skills?.skills == null)
                {
                    return;
                }

                try
                {
                    int ticksPiloting = Find.TickManager.TicksGame - startPilotTime;
                    float daysPiloting = ticksPiloting / 60000f;
                    float skillGain = daysPiloting * skillLearnFactor / 40;
                    float totalLearningFactor = pilot.GetStatValue(StatDefOf.GlobalLearningFactor);
                    foreach (SkillRecord pilotSkill in pilot.skills.skills)
                    {
                        foreach (SkillRecord pawnSkill in pawn.skills.skills)
                        {
                            if (pawnSkill.def == pilotSkill.def)
                            {
                                var xpDifference = pawnSkill.XpTotalEarned - pilotSkill.XpTotalEarned;

                                if (xpDifference > 0)
                                {
                                    float totalGain = xpDifference * skillGain;
                                    if (xpDifference < 4000)
                                    {
                                        totalGain *= 0.2f;
                                    }
                                    else if (xpDifference < 6000)
                                    {
                                        totalGain *= 0.35f;
                                    }
                                    else if (xpDifference < 8000)
                                    {
                                        totalGain *= 0.5f;
                                    }
                                    else if (xpDifference < 12000)
                                    {
                                        totalGain *= 0.75f;
                                    }

                                    totalGain = Mathf.Min(totalGain, xpDifference / totalLearningFactor);
                                    pilotSkill.Learn(totalGain);
                                    if (totalGain > 4000)
                                    {
                                        Messages.Message("BS_GainedSkillFromPawnAmount".Translate(
                                            pilot.Name.ToStringShort, $"{totalGain:f0}",
                                            pawnSkill?.def?.label, pawn.Name?.ToStringShort), pawn, MessageTypeDefOf.PositiveEvent);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to transfer learned skills from {pawn.Name} to {pilot.Name} with error:\n{e.Message}\n{e.StackTrace}");
                }
            }
        }

        public override void PostRemoved()
        {
            try
            {
                base.PostRemoved();
                RemovePilots(mayRemoveHediff:false);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to remove pilot from {pawn.Name} with error:\n{e.Message}\n{e.StackTrace}");
            }
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
        }

        public float CalculateConsciousnessOffset()
        {
            // Get the pilot
            var thing = InnerContainer.FirstOrDefault();
            if (thing is Pawn pilot)
            {

                // Get Pilot conciousness
                float pilotConsciousness = pilot.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
                // Get the multiplier
                float offset = (pilotConsciousness - 1) * Props.pilotConsciousnessOffset + Props.flatBonusIfPiloted;
                return offset;
            }
            return 0f;
        }

        /// <summary>
        /// To avoid recursion from setting it in a method that will trigger a refresh hitting that very same method.
        /// </summary>
        float severity = 0.1f;

        public bool HasNoPilotAndRequiresPilot()
        {

            Thing thing = InnerContainer.FirstOrDefault();
            // If the pilot is required and there is no pilot, return 0.01
            if (thing == null || !(thing is Pawn))
            {
                severity = 0.1f;
                if (Props.pilotRequired)
                {
                    return true;
                }
            }
            else
            {
                severity = 1;
            }
            return false;
        }


        public List<PawnCapacityModifier> cachedCapMods = [];
        [HarmonyPatch(typeof(Hediff), nameof(CapMods), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CapMods_Postfix(Hediff __instance, ref List<PawnCapacityModifier> __result)
        {
            if (__instance is Piloted piloted)
            {
                var otherCapacityMods = __result.Where(x => x.capacity != PawnCapacityDefOf.Consciousness);

                if (!forcePilotableUpdate)
                {
                    if (Find.TickManager.TicksGame % tickRate != 0 && piloted.cachedCapMods.Count() > 0)
                    {
                        __result = piloted.cachedCapMods;
                        forcePilotableUpdate = false;
                        return;
                    }
                }
                else
                {
                    forcePilotableUpdate = false;
                }

                PawnCapacityModifier consciousness = new PawnCapacityModifier()
                {
                    capacity = PawnCapacityDefOf.Consciousness,
                };

                if (piloted.HasNoPilotAndRequiresPilot())
                {
                    consciousness.setMax = 0.01f;

                    // Down the pawn.
                }

                consciousness.offset = +piloted.CalculateConsciousnessOffset();
                __result = [consciousness, .. otherCapacityMods];
                piloted.cachedCapMods = __result;
            }
        }

        int pilotEjectCountdown = -1;
        static readonly int tickRate = 550;
        public override void Tick()
        {
            base.Tick();

            if (Find.TickManager.TicksGame % tickRate == 0 || forcePilotableUpdate)
            {
                // Use up more food if there is a pilot.
                if (InnerContainer.Count() > 0)
                {
                    pawn.needs.food.CurLevel -= pawn.needs.food.FoodFallPerTick * tickRate * 0.5f;
                }
                if (Severity != severity)
                {
                    Severity = severity;
                    pawn.health.Notify_HediffChanged(this);
                }

                if (PilotCount > 0 && pawn.Downed && (Props.canAutoEjectIfColonist || !pawn.IsColonist))
                {
                    if (pilotEjectCountdown == -1)
                    {
                        pilotEjectCountdown = 2;
                    }
                    else
                    {
                        pilotEjectCountdown--;
                        if (pilotEjectCountdown == 0)
                        {
                            pilotEjectCountdown = -1;
                            RemovePilots();
                        }
                    }
                }
                else
                {
                    pilotEjectCountdown = -1;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            forcePilotableUpdate = true;
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look(ref removeIfNoPilot, "removeIfNoPilot", defaultValue: false);
            Scribe_Values.Look(ref defaultEnterable, "defaultEnterable", defaultValue: true);
            Scribe_References.Look(ref cachedFaction, "cachedFaction");
            Scribe_References.Look(ref cachedIdeology, "cachedIdeology");
            Scribe_Values.Look(ref startPilotTime, "timeSpentPiloting", 0);
        }
    }

    


}
