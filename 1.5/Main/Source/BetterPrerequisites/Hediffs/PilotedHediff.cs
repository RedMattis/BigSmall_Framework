using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    public class CompProperties_RemovePilot : CompProperties_AbilityEffect
    {
        public CompProperties_RemovePilot()
        {
            compClass = typeof(RemovePilotComp);
        }
    }

    public class RemovePilotComp : CompAbilityEffect
    {

        // When the ability is activated remove the piloted Hediff.
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            RemovePilotedHediff(parent.pawn);
        }

        // Remove the piloted Hediff.
        public void RemovePilotedHediff(Pawn pawn)
        {
            // Get first hediff matching name BS_Piloted
            var pilotedHediffs = pawn.health.hediffSet.hediffs.Where(x => x is Piloted);
            foreach (var pilotedHediff in pilotedHediffs.ToArray())
            {
                // Removed the pilot from the hediff.
                if (pilotedHediff is Piloted piloted)
                {
                    piloted.RemovePilots();
                    return;
                }
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return true;
        }
    }

    public class CompProperties_Piloted : HediffCompProperties
    {
        public bool pilotRequired = true;
        public int pilotCapacity = 1;
        public float baseCapacity = 0.51f;
        public float pilotConsciousnessOffset = 0.25f;
        public bool inheritPilotSkills = false;
        public bool inheritPilotMentalTraits = false;
        public float flatBonusIfPiloted = 0f;
        public bool inheritRelationShips = false;

        public CompProperties_Piloted()
        {
            compClass = typeof(PilotedCompProps);
        }
    }
    public class PilotedCompProps : HediffComp
    {
        public CompProperties_Piloted Props => (CompProperties_Piloted)props;
    }

    [HarmonyPatch]
    public class Piloted : HediffWithComps, IThingHolder
    {
        public CompProperties_Piloted Props => GetProperties();
        private CompProperties_Piloted props = null;

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
                if (innerContainer == null)
                {
                    innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
                }
                return innerContainer;
            }
            set => innerContainer = value;
        }

        private ThingOwner innerContainer = null;

        private static bool forcePilotableUpdate = false;

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
                Log.Warning("Failed to add pilot to piloted hediff: " + e.Message);
            }
            try
            { 
                // Check if the is a pilot there already:
                var otherPilot = InnerContainer.Where(x => x is Pawn && x != thing).FirstOrDefault();
                if (otherPilot == null && thing is Pawn pilot)
                {
                    try { InheritPilotSkills(pilot, pawn); } catch (Exception e) { Log.Warning("Failed to transfer pilot skills: " + e.Message); }
                    try { InheritPilotTraits(pilot); } catch (Exception e) { Log.Warning("Failed to transfer pilot traits: " + e.Message); }
                    try {InheritRelationships(pilot, pawn); } catch (Exception e) { Log.Warning("Failed to transfer pilot relationships: " + e.Message); }
                }
            }
            catch (Exception e)
            {
                Log.Warning("Failed to transfer all pilot properties to pilotable: " + e.Message);
            }
            forcePilotableUpdate = true;
            pawn.health.Notify_HediffChanged(this);
            return;
        }

        public static readonly List<string> PhysicalTraitWhitelist = new List<string>
        {
            "speedoffset", "beauty", "gigantism", "large", "small", "dwarfism", "bs_giant", "tough"
        };

        public void InheritRelationships(Pawn source, Pawn target)
        {
            if (Props.inheritRelationShips == false)
            {
                return;
            }

            // Get the pilot's relations
            List<DirectPawnRelation> pilotRelations = source.relations.DirectRelations.ToList();

            // Get the pilot's thoughts
            List<Thought_Memory> pilotThoughts = source.needs.mood.thoughts.memories.Memories.ToList();

            // Clear the target's relations and thoughts
            target.relations.ClearAllRelations();
            target.needs.mood.thoughts.memories.Memories.Clear();

            var literallyAllPawns = Find.WorldPawns.AllPawnsAliveOrDead.ToList();

            // Add all other relations-pawns that show up in the pilot's thoughts to "literallyAllPawns".
            foreach (var thought in pilotThoughts)
            {
                if (thought.otherPawn != null)
                {
                    literallyAllPawns.Add(thought.otherPawn);
                }
            }


            var directPawnRelationsToSource = new Dictionary<DirectPawnRelation, Pawn>();
            // Fetch all relations to the pilot.
            foreach (var somePawn in literallyAllPawns)
            {
                if (somePawn == source)
                {
                    continue;
                }
                var relations = somePawn.relations.DirectRelations.ToList();
                foreach (var relation in relations)
                {
                    if (relation.otherPawn == source)
                    {
                        directPawnRelationsToSource.Add(relation, somePawn);
                    }
                }
            }


            // Add all direct relations of the pilot to the piloted.
            for (int idx = pilotRelations.Count - 1; idx >= 0; idx--)
            {
                DirectPawnRelation relation = pilotRelations[idx];
                try
                {
                    target.relations.AddDirectRelation(relation.def, relation.otherPawn);
                    //Log.Message($"Added (source->other) relation {relation.def.defName} from {target.Name} to {relation.otherPawn}.");
                }
                catch (Exception e)
                {
                    Log.Error("Failed to add relation " + relation.def.defName + " to " + target.Name + " from " + source.Name + " with error: " + e.Message);
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
                    //Log.Message($"Added (other->source) relation {relationDef.defName} from {otherPawn.Name} to {target.Name}.");
                }
                catch (Exception e)
                {
                    Log.Error("Failed to add relation " + relationDef.defName + " to " + otherPawn.Name + " from " + target.Name + " with error: " + e.Message);
                }
            }

            // Add all thoughts of the pilot to the piloted.
            for (int idx = pilotThoughts.Count - 1; idx >= 0; idx--)
            {
                Thought_Memory thought = pilotThoughts[idx];
                try
                {
                    target.needs.mood.thoughts.memories.TryGainMemory(thought.def, thought.otherPawn);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to add thought " + thought.def.defName + " to " + target.Name + " from " + source.Name + " with error: " + e.Message);
                }
            }

            // Transfer ideology
            if (source.Ideo != null)
            {
                target.ideo.SetIdeo(source.Ideo);
            }

            // Transfer faction
            if (source.Faction != null && source.Faction != target.Faction)
            {
                target.SetFaction(source.Faction);
            }

            // Transfer resistance values
            target.guest.resistance = source.guest.resistance;
            
            // Transfer Will
            target.guest.will = source.guest.will;



            if (pawn.needs?.mood?.thoughts != null)
            {
                pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
            }

            if (ModsConfig.RoyaltyActive)
            {
                target.royalty = new Pawn_RoyaltyTracker(pawn);
                // Transfer titles
                foreach (var title in source.royalty.AllTitlesForReading)
                {
                    target.royalty.SetTitle(title.faction, title.def, grantRewards: false, sendLetter: false);
                    int favorAmount = pawn.royalty.GetFavor(title.faction);
                    target.royalty.SetFavor(title.faction, favorAmount);
                }
            }

            //// Remove all relations and thoughts of the source.
            source.relations.ClearAllRelations();
            source.needs.mood.thoughts.memories.Memories.Clear();

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
            // Remove all traits which do not contain or start with a string in the whitelist.
            var traitsToRemove = pawn.story.traits.allTraits.Where(x => !PhysicalTraitWhitelist.Any(y => x.def.defName.ToLower().StartsWith(y))).ToList();
            foreach (Trait trait in traitsToRemove)
            {
                pawn.story.traits.allTraits.Remove(trait);
            }

            // Add all traits from the pilot that are NOT on the whitelist.
            var traitsToAdd = pilot.story.traits.allTraits.Where(x => !PhysicalTraitWhitelist.Any(y => x.def.defName.ToLower().StartsWith(y))).ToList();
            foreach (Trait trait in traitsToAdd)
            {
                pawn.story.traits.GainTrait(trait);
            }
        }


        public void RemovePilots()
        {
            IList<Thing> content = InnerContainer;

            if(content.Count == 0)
            {
                return;
            }
            foreach (Thing thing in content.Where(x => x is Pawn))
            {
                // Push skills improvements back from the pilotable entity to the pilot.
                try { InheritPilotSkills(pawn, thing as Pawn); } catch (Exception e) { Log.Error("Failed to inherit skills from " + pawn.Name + " with error: " + e.Message); }

                // Restore relationships from the pilotable entity to the pilot.
                try { InheritRelationships(pawn, thing as Pawn); } catch (Exception e) { Log.Error("Failed to inherit relationships from " + pawn.Name + " with error: " + e.Message); }
                break; // Never add skills from more than one pilot, or we might overwrite some other pawn's skills.
            }


            // Remove everything in innerContainer
            for (int i = content.Count - 1; i >= 0; i--)
            {
                Thing thing = content[i];
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.MapHeld, ThingPlaceMode.Near);
            }

            pilotEjectCountdown = -1; // Reset the eject countdown.
            pawn.health.Notify_HediffChanged(this);
            forcePilotableUpdate = true;
        }
        public override void PostRemoved()
        {
            try
            {
                base.PostRemoved();
                RemovePilots();
            }
            catch (Exception e)
            {
                Log.Error("Failed to remove pilot from " + pawn.Name + " with error: " + e.Message);
            }
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            // Done via patch instead.
            //try
            //{
            //    RemovePilot();
            //    base.Notify_PawnDied();
            //}
            //catch (Exception e)
            //{
            //    Log.Error("Failed to remove pilot from " + pawn.Name + " with error: " + e.Message);
            //}
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


        public List<PawnCapacityModifier> cachedCapMods = new List<PawnCapacityModifier>();
        [HarmonyPatch(typeof(Hediff), nameof(CapMods), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CapMods_Postfix(Hediff __instance, ref List<PawnCapacityModifier> __result)
        {
            if (__instance is Piloted piloted)
            {
                //PawnCapacityModifier oldConsciousness = __result.FirstOrDefault(x => x.capacity == PawnCapacityDefOf.Consciousness);
                //if (oldConsciousness != null)
                //    __result.Remove(oldConsciousness);

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
                __result = new List<PawnCapacityModifier>() { consciousness };
                __result.AddRange(otherCapacityMods);
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

                if (PilotCount > 0 && pawn.Downed)
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
            //Scribe_Collections.Look(ref cachedCapMods, "cachedCapMods", lookMode: LookMode.Value);
        }
        //public override string GetTooltip(Pawn pawn, bool showHediffsDebugInfo)
        //{
        //    var baseTooltip = base.GetTooltip(pawn, showHediffsDebugInfo);
        //    var stringBuilder = new StringBuilder();
        //    stringBuilder.Append(baseTooltip);
        //    stringBuilder.AppendLine();
        //    stringBuilder.AppendLine();
        //    stringBuilder.Append("BS_ConsciousnessFromPilot".Translate());
        //    return stringBuilder.ToString().TrimEndNewlines();
        //}
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    public static class FloatMenuMakerMap_AddHumanlikeOrders_Patch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {
            List<Thing> thingList = IntVec3.FromVector3(clickPos).GetThingList(pawn.Map);
            var pawnList = thingList.Where(x => x is Pawn).Select(x => (Pawn)x);
            foreach (var pilotable in pawnList)
            {
                // Check if pawn has a piloted hediff
                var pilotedHediff = pilotable?.health?.hediffSet?.hediffs?.Where(x => x is Piloted).FirstOrDefault();
                if (pilotedHediff != null && pilotedHediff is Piloted piloted)
                {
                    string errorMsg = "";
                    if (piloted.pawn.Faction != pawn.Faction && !piloted.Props.pilotRequired)
                    {
                        errorMsg = $"{pawn.Label} {"BS_CannotEnterEnemyAsOperator".Translate()}";
                    }
                    else if (piloted.pawn.Faction != pawn.Faction && !piloted.pawn.Downed && piloted.InnerContainer.Count > 0)
                    {
                        errorMsg = $"{pawn.Label} {"BS_CannotPilotNonDownedEnemy".Translate()}";
                    }
                    else if (piloted.PilotCount + 1 > piloted.PilotCapacity)
                    {
                        errorMsg = $"{pawn.Label} {"BS_PilotCapReached".Translate()}";
                    }
                    else if (piloted.MaxCapacity < pawn.BodySize)
                    {
                        errorMsg = $"{pawn.Label} {"BS_TooLargeToPilot".Translate()}";
                    }
                    else if (piloted.TotalMass + pawn.BodySize > piloted.MaxCapacity)
                    {
                        errorMsg = $"{pawn.Label} {"BS_NotEnoughRoomForPilot".Translate()}";
                    }

                    var pilotJobDef = DefDatabase<JobDef>.AllDefsListForReading.Where(x => x.defName == "BS_EnteringPilotablePawn").FirstOrDefault();
                    if (pilotJobDef != null)
                    {
                        var action = new FloatMenuOption("BS_EnterPilotable".Translate(), delegate
                        {
                            Job job = JobMaker.MakeJob(pilotJobDef, pilotable);
                            //job.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
                        });
                        if (errorMsg != "")
                        {
                            action.Disabled = true;
                            action.Label = errorMsg;
                        }

                        opts.Add(action);
                    }

                    //string errorMsg2 = "";
                    if (piloted.pawn.Downed && piloted.PilotCount > 0)
                    {
                        var ejectJobDef = DefDatabase<JobDef>.AllDefsListForReading.Where(x => x.defName == "BS_EjectPilotablePawn").FirstOrDefault();
                        if (ejectJobDef != null)
                        {
                            var action = new FloatMenuOption("BS_EjectPilots".Translate(), delegate
                            {
                                Job job = JobMaker.MakeJob(ejectJobDef, pilotable);
                                //job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
                            });

                            opts.Add(action);
                        }
                    }
                }
            }
        }
    }

    public class JobDriver_EnterPilotable : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(150).WithProgressBarToilDelay(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate
                {
                    // Get the Target as a Pawn
                    Pawn pilotable = TargetA.Thing as Pawn;
                    if (pilotable != null)
                    {
                        var pilotedHediff = pilotable?.health?.hediffSet?.hediffs?.Where(x => x is Piloted).FirstOrDefault();
                        if (pilotedHediff != null && pilotedHediff is Piloted piloted)
                        {
                            piloted.AddPilot(pawn);
                        }
                    }
                }
            };
        }
    }

    public class JobDriver_EjectPilotable : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(150).WithProgressBarToilDelay(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate
                {
                    // Get the Target as a Pawn
                    Pawn pilotable = TargetA.Thing as Pawn;
                    if (pilotable != null)
                    {
                        var pilotedHediff = pilotable?.health?.hediffSet?.hediffs?.Where(x => x is Piloted).FirstOrDefault();
                        if (pilotedHediff != null && pilotedHediff is Piloted piloted)
                        {
                            piloted.RemovePilots();
                        }
                    }
                }
            };
        }
    }

}
