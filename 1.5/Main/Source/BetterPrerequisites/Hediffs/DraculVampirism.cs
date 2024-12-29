using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class DraculStageProgression : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
        }

        public override void PostRemoved()
        {
            base.PostRemoved();

            //Log.Message($"Dracul Stage Progression Removed from {pawn.Name}");

            int ticksToDisappear = 0;
            foreach (var comp in comps)
            {
                if (comp is HediffComp_Disappears disappears)
                {
                    ticksToDisappear = disappears.ticksToDisappear;
                }
            }
            if (ticksToDisappear > 50000)
            {
                Log.Message($"Dracul Stage Progression Removed from {pawn.Name} with {ticksToDisappear} ticks remaining because it was insufficient to cause a change in stage.");
                return;
            }

            // Check the DeathrestCapacity of the pawn found on the Gene_Deathrest
            var deathrestGene = GeneHelpers.GetAllActiveGenes(pawn).Where(x => x is Gene_Deathrest);
            int deathRestCap = 0;
            if (deathrestGene.Count() > 0)
            {
                var drGene = (Gene_Deathrest)deathrestGene.First();
                deathRestCap = drGene.DeathrestCapacity;
            }


            // Check Dracul Stage of the Dracul Gene
            (int stage, _) = DraculStageExtension.TryGetDraculStage(pawn);
            //var draculGene = pawn.genes.GenesListForReading.Where(x => x.def.HasModExtension<DraculStageExtension>());

            var hemogenAmount = pawn.genes?.GetFirstGeneOfType<Gene_Hemogen>()?.Value;


            if (pawn.IsPrisoner || pawn.IsSlave || SanguophageUtility.ShouldBeDeathrestingOrInComaInsteadOfDead(pawn) || hemogenAmount < 0.75)
            {
                var draculStageProgression = (DraculStageProgression)HediffMaker.MakeHediff(BSDefs.VU_DraculAge, pawn);
                int ticksPerDay = 60000;
                int durationTicks = 1 * ticksPerDay;

                // Get the comps in the hediff and set the HediffCompProperties_Disappears disappars to the duration
                foreach (var comp in draculStageProgression.comps)
                {
                    if (comp is HediffComp_Disappears disappears)
                    {
                        disappears.ticksToDisappear = durationTicks;
                    }
                }
                pawn.health.AddHediff(draculStageProgression);
                return;
            }

            string targetXenoType = "VU_Dracul";
            switch (stage + 1)
            {
                case 1:
                    targetXenoType = "VU_Dracul_Spawn";
                    break;
                case 2:
                    targetXenoType = "VU_Dracul";
                    break;
                case 3:
                    targetXenoType = "VU_Dracul_Mature";
                    break;
                case 4:
                    targetXenoType = "VU_Dracul_Progenitor";
                    break;
                default:
                    Log.Warning($"Failed to set Dracul Stage. Defaulting to {targetXenoType}");
                    break;
            }


            
            var spawn = DefDatabase<XenotypeDef>.AllDefsListForReading.Where(x => x.defName == targetXenoType);
            if (spawn.Count() == 1)
            {
                pawn.genes.SetXenotype(spawn.First());
            }

            // Check the DeathrestCapacity of the pawn found on the Gene_Deathrest
            deathrestGene = GeneHelpers.GetAllActiveGenes(pawn).Where(x => x is Gene_Deathrest);
            if (deathrestGene.Count() > 0)
            {
                var drGene = (Gene_Deathrest)deathrestGene.First();
                if (stage == 3) deathRestCap++;
                if (stage == 4) deathRestCap++;
                drGene.OffsetCapacity(deathRestCap);
            }
        }
    }

    public class DraculVampirism : HediffWithComps
    {
        public int stageOfMostPowerfulDracul = 0;

        private bool checkedForHalfVampirism = false;
        private bool willBecomeHalfVampire = false;

        public int ticksToReanimate = 180000;
        public int timeOfDeath = 0;

        public bool checkedForMaster = false;

        public Faction factionOfMaster = null;
        private bool checkedForAnimalUndead = false;

        public override void PostTick()
        {
            if (pawn.IsBloodfeeder())
            {
                pawn.health.RemoveHediff(this);
            }
            else if (!pawn.RaceProps.Humanlike && !checkedForAnimalUndead)
            {
                // If the pawn is an animal and has the VU_DraculAnimalVampirism or VU_AnimalReturned, if so remove the vampirism. VU_AnimalReturned
                foreach (Hediff hediff in pawn.health?.hediffSet.hediffs)
                {
                    var defName = hediff.def.defName;
                    if (defName == "VU_DraculAnimalVampirism" || hediff.def.GetAllPawnExtensionsOnHediff().Any(x=>x.isUnliving||x.isMechanical))
                    {
                        pawn.health.RemoveHediff(this);
                        break;
                    }
                }
                checkedForAnimalUndead = true;
            }
            base.PostTick();
            if (checkedForHalfVampirism == false && Severity > 0.8f)
            {
                if (Rand.Chance(0.66f))
                {
                    willBecomeHalfVampire = true;
                }
                checkedForHalfVampirism = true;
            }
            if (willBecomeHalfVampire && HediffUtility.FullyImmune(this))
            {
                // Apply all the genes from the Half-Vampire Xenotype

                // Get all the xenohuman defs
                foreach (var def in DefDatabase<XenotypeDef>.AllDefsListForReading.Where(x => x.defName == "VU_Dracul_HalfVampire"))
                {
                    // Get all the genes from the xenohuman defs
                    GeneHelpers.AddAllXenotypeGenes(pawn, def, "Half-Vampire");
                    // Set the name of the pawns xenotype to "Half Vampire"
                }
            }
            TryAssignFactionOfMasterIfMissing();
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref factionOfMaster, "factionOfMaster");
            Scribe_Values.Look(ref ticksToReanimate, "ticksToReanimate");
            Scribe_Values.Look(ref checkedForHalfVampirism, "checkedForHalfVampirism");
            base.ExposeData();
        }

        private void TryAssignFactionOfMasterIfMissing()
        {
            if (factionOfMaster == null && checkedForMaster == false)
            {
                checkedForMaster = true;
                // If we have no master, but there is a Dracul vampire on the map, set it to that, prefering player dracul, followed by neutral dracul.
                Faction enemyFaction = null;

                foreach (var otherPawn in pawn?.Map?.mapPawns?.AllPawns.Where(x => !x.Dead && x.genes != null && DraculStageExtension.TryGetDraculStage(x).draculGene != null))
                {
                    // If found vampire is hostile to the player faction and it was a player faction pawn, set that as a possible master.
                    if (otherPawn.Faction.HostileTo(Faction.OfPlayer))
                    {
                        enemyFaction = otherPawn.Faction;
                    }
                    // If the found pawn belongs to the player, set the player as the master.
                    else if (pawn.Faction == Faction.OfPlayer)
                    {
                        factionOfMaster = pawn.Faction;
                        break;
                    }
                }
                if (factionOfMaster == null && enemyFaction != null)
                {
                    factionOfMaster = enemyFaction;
                }
            }
        }

        //public override void Notify_PawnDied()
        //{
        //    base.Notify_PawnDied();
        //    TrySetTimeOfDeath();
        //}

        public void TrySetReanimateTime()
        {

            if (timeOfDeath == 0)
            {
                timeOfDeath = Find.TickManager.TicksGame;
                int ticksPerDay = 60000;
                if (!pawn.RaceProps.Humanlike)
                {

                    ticksToReanimate = (int)(ticksPerDay / (stageOfMostPowerfulDracul + 1) * Verse.Rand.Range(0.5f, 1.5f));
                }
                else
                {
                    //int level = GetFinalVampireLevel();
                    ticksToReanimate = (int)(ticksPerDay / (stageOfMostPowerfulDracul + 1) * Verse.Rand.Range(0.5f, 3.0f));
                    // Set death tick time to the current tick.
                    // The higher the severity the longer it will take, but also the more powerful the Dracul will tend to be.
                }
                ticksToReanimate = Mathf.Clamp(ticksToReanimate, ticksPerDay / 3, ticksPerDay * 2);
            }
        }

        public int GetFinalVampireLevel()
        {
            // The higher the severity and powerful the sire the more powerful the Dracul will tend to be
            int level = 0;
            if (Severity < 0.5f) level = -3;
            else if (Severity < 0.8f) level = -2;
            else if (Severity >= 0.8f) level = -1;

            level += stageOfMostPowerfulDracul;
            return level;
        }

        public bool TryReanimate()
        {
            // Check if the pawn is dead
            if (pawn.Dead && Severity > 0.15)
            {
                TrySetReanimateTime();
                // Check if the time of death + the ticks to reanimate is less than the current tick
                if (timeOfDeath + ticksToReanimate < Find.TickManager.TicksGame)
                {
                    EjectCorpse();
                    if (!pawn.RaceProps.Humanlike)
                    {
                        // Apply hediff to animal.
                        var animalHediff = HediffMaker.MakeHediff(HediffDef.Named("VU_DraculAnimalVampirism"), pawn);
                        pawn.health.AddHediff(animalHediff);
                    }
                    else
                    {
                        // Get the final vampire level
                        int vampireLevel = GetFinalVampireLevel();

                        // Remove the Dracul Vampirism Hediff
                        pawn.health.RemoveHediff(this);

                        // Apply Dracul Xenotype
                        string targetXenotype;
                        if (vampireLevel <= 0) targetXenotype = "VU_Dracul_Feral";
                        else if (vampireLevel <= 2) targetXenotype = "VU_Dracul_Spawn";
                        else targetXenotype = "VU_Dracul";

                        foreach (var def in DefDatabase<XenotypeDef>.AllDefsListForReading.Where(x => x.defName == targetXenotype))
                        {
                            // Set pawn xenotype to the target xenotype
                            pawn.genes.SetXenotype(def);
                        }
                        // Reanimate
                    }
                    GameUtils.UnhealingRessurection(pawn);
                    if (pawn.RaceProps.Humanlike)
                    {
                        SetFactionOfVampire();
                    }
                    // If master is not of the player colony the berserkchance is 75%
                    if (factionOfMaster != Faction.OfPlayer)
                    {
                        if (Rand.Chance(0.75f))
                        {
                            pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                        }
                    }
                    else
                    {
                        if (Rand.Chance(0.25f))
                        {
                            pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                        }
                    }

                    return true;
                }
            }
            return false;
        }

        private void EjectCorpse()
        {
            // If the pawn is in a container, remove it from the container
            var container = pawn.ParentHolder;
            while (container is Corpse)
            {
                container = container.ParentHolder;
            }
            if (container is Building_Casket grave)
            {
                grave.EjectContents();
            }
        }

        public void SetFactionOfVampire()
        {
            if (factionOfMaster != null)
            {
                if (pawn.Faction.IsPlayer)
                {
                    if (factionOfMaster.IsPlayer || factionOfMaster.AllyOrNeutralTo(Faction.OfPlayer))
                    {
                        // No change.
                    }
                    else
                    {
                        // Change the faction of the pawn to the faction of the master.
                        pawn.SetFaction(factionOfMaster);
                    }
                }
                else
                {
                    if (factionOfMaster.IsPlayer)
                    {
                        // Actually, let's not do this maybe? It seems like a cheesy way to tame animals.
                        if (!pawn.RaceProps.Humanlike)
                        {
                            //pawn.SetFaction(factionOfMaster);
                        }
                        else
                        {

                            // Enslave
                            if (ModsConfig.IdeologyActive)
                                GenGuest.EnslavePrisoner(pawn.Map.mapPawns.FreeColonists.RandomElement(), pawn);
                            else
                            {
                                // Recruit them as a fallback.
                                pawn.SetFaction(Faction.OfPlayer);
                            }
                        }
                    }
                    else
                    {
                        // Change the faction of the pawn to the faction of the master.
                        pawn.SetFaction(factionOfMaster);
                    }
                }
            }
        }
    }

    //Postfix Pawn.TickRare with Harmony to run the DraculVampirism Hediff's TryReanimate function.
    [HarmonyPatch(typeof(Corpse), nameof(Pawn.TickRare))]
    public static class Pawn_TickRare_Patch
    {
        public static void Postfix(Corpse __instance)
        {
            if (__instance == null || __instance.InnerPawn == null) return;
            Pawn pawn = __instance.InnerPawn;
            if (pawn.Dead)
            {
                bool hdiffFound = false;
                // Get all hediffs on pawn
                // for loop counting backwards
                try
                {
                    //if (pawn.health.hediffSet.hediffs != null)

                    for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                    {
                        // Get the current hediff
                        var hediff = pawn.health.hediffSet.hediffs[i];
                        // If the hediff class is DraculVampirism
                        if (hediff is DraculVampirism)
                        {
                            // Run the TryReanimate function
                            var result = (hediff as DraculVampirism).TryReanimate();
                            hdiffFound = true;

                            if (result) break;
                        }
                        else if (hediff is VUReturning vUReturning)
                        {
                            // Run the TryReanimate function
                            var result = vUReturning.TryReanimate();
                            hdiffFound = true;
                            if (result) break;
                        }
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    // Don't think this should happen anymore, but just to be safe.
                }

                if (!hdiffFound)
                {
                    //Log.Message($"[DraculVampirism] {__instance} is dead, but has no DraculVampirism Hediff.");
                }
            }
            else
            {
                //Log.Message($"[DraculVampirism] {__instance} is not dead.");
            }
        }
    }
}
