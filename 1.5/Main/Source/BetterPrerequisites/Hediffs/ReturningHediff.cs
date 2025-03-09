using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace BigAndSmall
{

    public class ReturningGC : GameComponent
    {
        public ReturningGC(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref VUReturning.lastCheckTick, "lastCheckTick");
            Scribe_Values.Look(ref VUReturning.deadRisingMode, "deadRisingMode");
            Scribe_Values.Look(ref VUReturning.zombieApocalypseMode, "zombieApocalypseMode");

            base.ExposeData();
        }
    }

    public class VUReturning : HediffWithComps
    {
        public int ticksToReanimate = 180000;
        public int timeOfDeath = 0;

        public static int lastCheckTick = -99999;
        public static bool deadRisingMode = false;
        public static bool zombieApocalypseMode = false;

        #region Not yet implemented.
        public static float durationDeadRisingMod = 1f; // In days
        public static float durationZombieApocalypseMod = 3f; // In days
        #endregion

        public static float ZombieApocalypseChance => 0.0f;
        public static float DeadRisingChance => 0.1f;

        public static float ReturnChance => 0.2f;
        public static float ReturnChanceApoc => 0.75f;

        public static float ReturnChanceColonist => 0.15f;

        public float PychoticWanderingChance => 0.5f;
        public float BerserkChance => 0.33f;
        public float BerserkChanceApoc => 0.75f;
        public float BerserkLoseFaction => 0.5f;
        public float BerserkLoseFactionApoc => 0.5f;

        public float ManhunterChance => 0.65f;
        public float PermanentBerserk => 0.0f;
        public float PermanentBerserkApoc => 0.0f;


        public override void ExposeData()
        {
            Scribe_Values.Look(ref ticksToReanimate, "ticksToReanimate");
            Scribe_Values.Look(ref timeOfDeath, "timeOfDeath");
            base.ExposeData();
        }


        public void TrySetReanimateTime()
        {

            if (timeOfDeath == 0)
            {
                timeOfDeath = Find.TickManager.TicksGame;
                int ticksPerDay = 60000;
                if (zombieApocalypseMode)
                {
                    if (Rand.Chance(0.1f))
                        ticksToReanimate = (int)(ticksPerDay * Rand.Range(-0.05f, 0.1f));
                    else if (Rand.Chance(0.1f))
                        ticksToReanimate = (int)(ticksPerDay * Rand.Range(0.001f, 0.1f));
                    else if (Rand.Chance(0.5f))
                        ticksToReanimate = (int)(ticksPerDay * Rand.Range(0.45f, 0.55f));
                    else if (Rand.Chance(0.5f))
                        ticksToReanimate = (int)(ticksPerDay * Rand.Range(0.9f, 1f));
                    else
                        ticksToReanimate = (int)(ticksPerDay * Rand.Range(1f, 40f));

                }
                else
                {

                    if (Rand.Chance(0.1f))
                        ticksToReanimate = (int)(ticksPerDay * Rand.Range(0.001f, 0.1f));
                    else if (Rand.Chance(0.5f))
                        ticksToReanimate = (int)(ticksPerDay * Rand.Range(0.9f, 1f));
                    else if (Rand.Chance(0.25f))
                        ticksToReanimate = (int)(ticksPerDay * Rand.Range(2.7f, 3.4f));
                    else if (Rand.Chance(0.50f))
                        ticksToReanimate = (int)(ticksPerDay * Rand.Range(3f, 10f));
                    else
                        ticksToReanimate = (int)(ticksPerDay * Rand.Range(10f, 80f));
                }
            }
        }

        public bool TryReanimate(bool forceTrigger=false)
        {
            // Check if the pawn is dead
            if (pawn.Dead)
            {
                if (pawn.IsUndead() || !pawn.RaceProps.IsFlesh)
                {
                    pawn.health.RemoveHediff(this);
                    return false;
                }


                var soullessHediff = DefDatabase<HediffDef>.GetNamedSilentFail("BS_Soulless");
                if (pawn.health.hediffSet.TryGetHediff(soullessHediff, out Hediff hediff) && soullessHediff != null)
                {
                    pawn.health.RemoveHediff(this);
                    return false; // Don't reanimate soulless pawns
                }

                TrySetReanimateTime();
                // Check if the time of death + the ticks to reanimate is less than the current tick
                if (timeOfDeath + ticksToReanimate < Find.TickManager.TicksGame)
                {
                    EjectCorpse();
                    // Check for faction of def BS_ZombieFaction. If it exists, we may want to set the pawn to that faction.
                    Faction zombieFaction = Find.FactionManager.AllFactions.FirstOrDefault(x => x.def.defName == "BS_ZombieFaction");

                    if (pawn.RaceProps.Humanlike)
                    {
                        // Remove the Dracul Vampirism Hediff
                        pawn.health.RemoveHediff(this);

                        // Apply Xenotype
                        var returnedXeno = GlobalSettings.GetRandomReturnedXenotype;
                        if (pawn.Faction == Faction.OfPlayerSilentFail)
                        {
                            returnedXeno = GlobalSettings.GetRandomReturnedColonistXenotype;
                        }
                        if (returnedXeno == null)
                        {
                            Log.Warning("Returned Xenotype is null!");
                            return false;
                        }
                        returnedXeno = ModifyReturnedByRotStage(pawn, returnedXeno);

                        GeneHelpers.AddAllXenotypeGenes(pawn, returnedXeno, name: $"{returnedXeno.label} {pawn.genes?.XenotypeLabel}");
                    }
                    else
                    {
                        pawn.health.AddHediff(GetAnimalReturnedHediff(pawn));
                    }

                    // Reanimate
                    GameUtils.UnhealingRessurection(pawn);

                    // 33% chance of entering berserk mental state
                    if (!zombieApocalypseMode && Rand.Chance(BerserkChance) || zombieApocalypseMode && Rand.Chance(BerserkChanceApoc))
                    {
                        // Get MentalStateDef of BS_ZombieBerserk
                        //MentalStateDef berserkDef = DefDatabase<MentalStateDef>.AllDefs.FirstOrDefault(x => x.defName == "BS_ZombieBerserk");

                        if (zombieFaction != null)
                        {
                            if (zombieApocalypseMode && Rand.Chance(BerserkLoseFactionApoc) || !zombieApocalypseMode && Rand.Chance(BerserkLoseFaction))
                            { 
                                pawn.SetFaction(zombieFaction);
                            }
                            else
                            {
                                pawn.SetFaction(zombieFaction);

                                // Check if pawn belongs to the player's faction
                                if (pawn.Faction == Faction.OfPlayer)
                                {
                                    if (pawn?.guest?.resistance != null)
                                    {
                                        pawn.guest.resistance = 0;
                                    }
                                    if (pawn?.guest?.will != null)
                                    {
                                        pawn.guest.will = Rand.Range(1, 5);
                                    }
                                }
                            }
                        }

                        float permanentBerserkChance = zombieApocalypseMode ? PermanentBerserkApoc : PermanentBerserk;
                        if (Rand.Chance(ManhunterChance))
                        {
                            if (Rand.Chance(permanentBerserkChance))
                                pawn.mindState.mentalStateHandler.TryStartMentalState(BSDefs.BS_LostManhunterPermanent, reason: "BS_ZombieReason".Translate(), forceWake: true);
                            else
                                pawn.mindState.mentalStateHandler.TryStartMentalState(BSDefs.BS_LostManhunter, reason: "BS_ZombieReason".Translate(), forceWake: true);
                        }
                        else
                        {
                            pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, reason: "BS_ZombieReason".Translate(), forceWake: true);
                        }
                        
                            
                        //if (!success)
                        //{
                        //    Log.Warning($"Failed to start mental state {berserkDef.defName} on {pawn.Name}.");
                        //}
                    }
                    // 50% chance of entering wandering mental state
                    else if (Rand.Chance(PychoticWanderingChance))
                    {
                        pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_Psychotic, reason: "BS_ZombieReason".Translate(), forceWake: true);
                    }

                    // Remove this hediff
                    pawn.health.RemoveHediff(this);
                    return true;
                }
            }
            else
            {
                // Remove this hediff
                pawn.health.RemoveHediff(this);
            }
            return false;
        }

        public static HediffDef GetAnimalReturnedHediff(Pawn pawn)
        {
            RotStage rotStage = pawn.GetRotStage();
            HediffDef targetDef = BSDefs.VU_AnimalReturned;
            if (rotStage == RotStage.Dessicated)
            {
                targetDef = BSDefs.VU_AnimalReturnedSkeletal;
            }
            else if (rotStage == RotStage.Rotting)
            {
                targetDef = BSDefs.VU_AnimalReturnedRotted;
            }

            return targetDef;
        }

        public static XenotypeDef ModifyReturnedByRotStage(Pawn pawn, XenotypeDef returnedXeno)
        {
            if (pawn.GetRotStage() == RotStage.Dessicated && returnedXeno == BSDefs.VU_Returned || returnedXeno == BSDefs.VU_Returned_Intact)
            {
                returnedXeno = BSDefs.VU_ReturnedSkeletal;
            }
            else if (pawn.GetRotStage() == RotStage.Fresh && returnedXeno == BSDefs.VU_ReturnedSkeletal)
            {
                returnedXeno = BSDefs.VU_Returned_Intact;
            }

            return returnedXeno;
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
    }

    public class MentalState_LostManhunter : MentalState_Manhunter
    {
        public override bool ForceHostileTo(Faction f)
        {
            return pawn.Faction != f || pawn.Faction.def.defName == "Zombies";
        }

        public override bool ForceHostileTo(Thing t)
        {
            return false;
        }
         
        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
    }

}
