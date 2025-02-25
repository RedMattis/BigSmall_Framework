using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{

    public class ReturningSoul : Gene
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);

            // Get ModExt
            ReturningSoulModExt modExt = def.GetModExtension<ReturningSoulModExt>();

            if (modExt == null)
            {
                Log.Error("ReturningSoul gene is missing its ModExt. Please add it to the gene.");
                return;
            }

            Corpse corpse = pawn.Corpse;
            if (MakeCorpse_Patch.corpse == null)
            {
                corpse = MakeCorpse_Patch.corpse;
            }

            bool hasBurnDenial = pawn.health.hediffSet.HasHediff(BSDefs.BS_BurnReturnDenial);

            var pawnNameString = pawn.Name.ToString();
            // Check if dInfo is fire damage.
            if (hasBurnDenial || dinfo.HasValue == true && (dinfo?.Def == DamageDefOf.Flame || dinfo?.Def?.defName == "Burn"))
            {
                if (pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    Find.LetterStack.ReceiveLetter("BS_ReturningSoul_FireTitle".Translate(pawnNameString), "BS_ReturningSoul_Fire".Translate(pawnNameString), LetterDefOf.NegativeEvent, pawn);
                }

                return;
            }

            var soullessHediff = DefDatabase<HediffDef>.GetNamedSilentFail("BS_Soulless");
            if (pawn?.health?.hediffSet?.TryGetHediff(soullessHediff, out Hediff hediff) == true && soullessHediff != null)
            {
                if (pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    Find.LetterStack.ReceiveLetter("BS_WasKilled_Title".Translate(pawnNameString), "BS_WasKilledSoulless".Translate(pawnNameString), LetterDefOf.NegativeEvent, pawn);
                }
                return;
            }

            // Scatter filth on death.
            if (ModLister.CheckAnomaly("Returning soul"))
            {

                try
                {
                    FilthMaker.TryMakeFilth(corpse.Position, corpse.Map, ThingDefOf.Filth_Ash, 11);
                }
                catch { } // Likely the body was destroyed.
            }
            
            // We're really only bothering with colonists.
            // For other characters we'll just pretend they returned somewhere else with a new identity.
            if (pawn.IsColonist)
            {
                var soul = new ReturningSoulHolder
                {
                    pawn = pawn,
                    ticksToReturn = (int)Rand.Range(0.5f, 5f) * 60000,
                    corpseReturn = modExt.corpseReturn,
                    addCorpseGenes = modExt.addCorpseGenes,
                    addCorpseBionics = modExt.addCorpseBionics
                };
                // Run a tick just to make sure the data is cleaned of potential duplicates. 100 ticks won't make much of a difference anyway.
                ReturningSoulManager.instance.ProcessSouls();

                ReturningSoulManager.instance.returningSouls.Add(soul);
            }
            else
            {
                if (modExt.corpseReturn)
                {
                    try
                    {
                        // Just randomly change their apperance
                        pawn.story.hairDef = DefDatabase<HairDef>.AllDefsListForReading.RandomElement();
                        pawn.story.bodyType = DefDatabase<BodyTypeDef>.AllDefsListForReading.RandomElement();
                        pawn.story.bodyType = DefDatabase<BodyTypeDef>.AllDefsListForReading.RandomElement();
                    }
                    catch { }
                }
                try
                {
                    // Add a new weapon and armor from their pawnkind.
                    var pawnKind = pawn.kindDef;
                    var weapon = pawnKind.weaponTags.RandomElement();
                    // Get a weapon from the weapon tag.
                    var weaponDef = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.weaponTags != null && x.weaponTags.Contains(weapon)).RandomElement();
                    var weaponThing = ThingMaker.MakeThing(weaponDef);
                    pawn.equipment.AddEquipment((ThingWithComps)weaponThing);
                }
                catch
                {
                    Log.Warning($"Failed to add new weapon to {pawn.Name}");
                }

            }
            // Lets make them be alive first just to make sure no mod garbage-collects them or something.
            ResurrectionUtility.TryResurrect(pawn);
            pawn.DeSpawn();
        }
    }

    public class ReturningSoulModExt : DefModExtension
    {
        public bool corpseReturn = false;
        public bool addCorpseGenes = false;
        public bool addCorpseBionics = false;
    }

    public class ReturningSoulManager : GameComponent
    {
        public static ReturningSoulManager instance = null;
        public List<ReturningSoulHolder> returningSouls = [];
        public Game game;

        const int tickFrequency = 500;

        public ReturningSoulManager(Game game)
        {
            this.game = game;
            instance = this;
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref returningSouls, "BS_ReturningSouls", LookMode.Deep);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % tickFrequency != 0)
            {
                return;
            }
            ProcessSouls();
        }

        public void ProcessSouls()
        {
            if (returningSouls.Count != 0)
            {
                for (int idx = returningSouls.Count - 1; idx >= 0; idx--)
                {
                    ReturningSoulHolder soul = returningSouls[idx];
                    if (soul.pawn.Spawned)
                    {
                        returningSouls.RemoveAt(idx);
                        continue;
                    }
                    if (soul.Tick(tickFrequency))
                    {
                        returningSouls.RemoveAt(idx);
                    }

                }
            }
        }
    }

    public class ReturningSoulHolder : IExposable
    {
        public Pawn pawn;
        public int ticksToReturn = 9999;
        public int attempts = 0;
        public bool corpseReturn = false;
        public bool addCorpseGenes = false;
        public bool addCorpseBionics = false;

        public void ExposeData()
        {
            Scribe_Deep.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref ticksToReturn, "ticksToReturn");
            Scribe_Values.Look(ref attempts, "attempts");
            Scribe_Values.Look(ref corpseReturn, "corpseReturn");
            Scribe_Values.Look(ref addCorpseGenes, "addCorpseGenes");
            Scribe_Values.Look(ref addCorpseBionics, "addCorpseBionics");
        }

        public bool Tick(int tickCount)
        {
            ticksToReturn -= tickCount;
            if (ticksToReturn <= 0)
            {
                bool success = false;
                // Check every active map
                foreach (Map map in Find.Maps.Where(x => x.IsPlayerHome))
                {
                    success = TryRessurectFromCorpse(map);
                }
                if (success) return true;
                foreach (Map map in Find.Maps)
                {
                    success = TryRessurectFromCorpse(map);
                }
                if (success) return true;
                // If we're here, we couldn't find a suitable corpse.
                // Try again in 0.5 to 5 days minutes.
                ticksToReturn = (int)Rand.Range(0.5f, 5f) * 60000;
                attempts++;
                string pawnNameStr = pawn.Name.ToString();
                Messages.Message("BS_ReturningSoul_Failed".Translate(pawnNameStr), MessageTypeDefOf.NegativeEvent);
                if (attempts > Rand.Range(1, 6))
                {
                    Messages.Message("BS_ReturningSoul_FailedPermanent".Translate(pawnNameStr), MessageTypeDefOf.NegativeEvent);
                    pawn.Destroy();
                    return true;
                }
            }
            return false;
        }

        private bool TryRessurectFromCorpse(Map map)
        {
            var corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().InRandomOrder().ToList();

            if (pawn.Faction != Faction.OfPlayer)
                pawn.SetFaction(Faction.OfPlayer);

            if (!corpseReturn)
            {
                // Get any valid positon on the map.
                IntVec3 position = DropCellFinder.FindRaidDropCenterDistant(map, allowRoofed: true);
                // Try Place near
                GenSpawn.Spawn(pawn, position, map);
                FilthMaker.TryMakeFilth(position, map, ThingDefOf.Filth_Ash, 5);
                Messages.Message("BS_ReturningSoul_Success".Translate(pawn.Name.ToString()), MessageTypeDefOf.PositiveEvent);
                return true;
            }

            foreach (Corpse corpse in corpses)
            {
                // Check if human
                if (corpse.InnerPawn?.RaceProps?.Humanlike == true && corpse.InnerPawn.RaceProps.IsFlesh && !corpse.IsDessicated())
                {
                    // Spawn the pawn
                    GenSpawn.Spawn(pawn, corpse.Position, map);

                    // Remove all hediffs from the pawn.
                    pawn.health.hediffSet.hediffs.Clear();

                    // Transfer all bionics from the corpse to the pawn.
                    if (addCorpseBionics)
                    {
                        foreach (var bionic in corpse.InnerPawn.health.hediffSet.hediffs.Where(x => x.def.spawnThingOnRemoved != null && x.def.addedPartProps?.betterThanNatural == true))
                        {
                            var originalBodyPart = bionic.Part;

                            // Find the body part on the pawn
                            var bodyPart = pawn.health.hediffSet.GetNotMissingParts()
                                .FirstOrDefault(x => x.def.label == originalBodyPart.def.label || x.def.defName == originalBodyPart.def.defName);
                            if (bodyPart != null)
                            {
                                // Install the bionic
                                var bionicHediff = HediffMaker.MakeHediff(bionic.def, pawn, bodyPart) as Hediff_AddedPart;
                                pawn.health.AddHediff(bionicHediff);
                            }
                        }
                    }

                    // Resurrect the pawn
                    CompPropertiesMimicffect.DoMimic(pawn, corpse, [BSDefs.BS_ReturningSoul], spawnGibblets: false, addCorpseGenes: addCorpseGenes);
                    FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, ThingDefOf.Filth_Ash, 5);

                    Messages.Message("BS_ReturningSoulCorpse_Success".Translate(pawn.Name.ToString(), corpse.InnerPawn.Name.ToString()), MessageTypeDefOf.PositiveEvent);

                    // Remove the corpse
                    corpse.Destroy();
                    return true;
                }
            }
            return false;
        }
    }
}
