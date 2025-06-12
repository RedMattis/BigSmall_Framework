using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using static BigAndSmall.HumanlikeAnimalGenerator;

namespace BigAndSmall
{
    public class HumanlikeAnimal
    {
        public PawnKindDef animalKind;
        public ThingDef humanlikeThing;
        public ThingDef humanlike;
        public ThingDef animal;

        public int GetLifeStageIndex(Pawn pawn)
        {
            var aLifeStages = animal.race.lifeStageAges;
            int age = pawn.ageTracker.AgeBiologicalYears;
            for (int i = aLifeStages.Count - 1; i >= 0; i--)
            {
                if (age >= aLifeStages[i].minAge)
                {
                    return i;
                }
            }
            return 0;
        }
    }

    public static class HumanlikeAnimals
    {
        public static HumanlikeAnimal GetHumanlikeAnimalFor(ThingDef thingDef)
        {
            if (humanlikeAnimals.TryGetValue(thingDef, out HumanlikeAnimal humanlikeAnimal))
                return humanlikeAnimal;
            else if (reverseLookupHumanlikeAnimals.TryGetValue(thingDef, out HumanlikeAnimal humanlikeAnimal2))
                return humanlikeAnimal2;
            return null;
        }

        public static ThingDef HumanLikeAnimalFor(ThingDef td) => GetHumanlikeAnimalFor(td)?.humanlikeThing;
        public static ThingDef HumanLikeSourceFor(ThingDef td) => GetHumanlikeAnimalFor(td)?.humanlike;
        public static ThingDef AnimalSourceFor(ThingDef td) => GetHumanlikeAnimalFor(td)?.animal;
    }

    public static class HumanlikeAnimalGenerator
    {
        public static Dictionary<ThingDef, HumanlikeAnimal> humanlikeAnimals = [];
        public static Dictionary<ThingDef, HumanlikeAnimal> reverseLookupHumanlikeAnimals = [];

        public static void GenerateHumanlikeAnimals(bool hotReload)
        {
            if (BigSmall.BSSapientAnimalsActive)  // Just replace this with the actual mod's name later.
            {
                HashSet<ThingDef> thingDefsGenerated = [];

                var aniPawnKinds = DefDatabase<PawnKindDef>.AllDefs
                    .Where(x => x.race is ThingDef aniThing && aniThing?.race is RaceProperties race &&
                        race.Animal && race.intelligence == Intelligence.Animal)
                    .ToList();
                foreach (var aniPawnKind in aniPawnKinds)
                {
                    if (thingDefsGenerated.Contains(aniPawnKind.race)) continue;
                    GenerateAndRegisterHumanlikeAnimal(aniPawnKind, ThingDefOf.Human, hotReload);
                    thingDefsGenerated.Add(aniPawnKind.race);
                }

                var treatAsAnimal = DefDatabase<PawnKindDef>.AllDefs.Where(x=>x.ExtensionsOnDef<PawnKindExtension, PawnKindDef>()
                                                            .Any(y => y.generateHumanlikeAnimalFromThis)).ToList();
                foreach(var animalLikePK in treatAsAnimal)
                {
                    if (thingDefsGenerated.Contains(animalLikePK.race)) continue;
                    MakeDummySetupsForAlreadySapientAnimals(animalLikePK);
                    thingDefsGenerated.Add(animalLikePK.race);
                }

                foreach ((ThingDef th, HumanlikeAnimal hAnim) in humanlikeAnimals)
                {
                    reverseLookupHumanlikeAnimals[hAnim.animal] = hAnim;
                }
            }
        }

        private static void MakeDummySetupsForAlreadySapientAnimals(PawnKindDef animalLikePK)
        {
            var pawnExt = animalLikePK.ExtensionsOnDef<PawnKindExtension, PawnKindDef>().First(x => x.generateHumanlikeAnimalFromThis);
            HumanlikeAnimal hla = new()
            {
                animalKind = animalLikePK,
                humanlikeThing = animalLikePK.race,
                humanlike = animalLikePK.race,
                animal = animalLikePK.race
            };
            humanlikeAnimals[animalLikePK.race] = hla;
        }

        /// <summary>
        /// Generate a humanlike animal from an AnimalThing and HumanThing.
        /// 
        /// Generally we want to grab most stuff from the human, and transfer mostly the body and some traits from the animal.
        /// </summary>
        /// <param name="aniThing">ThingDef of an Animal.</param>
        /// <param name="humThing">ThingDef of a Humanlike (likely always the defautl "Human")</param>
        public static void GenerateAndRegisterHumanlikeAnimal(PawnKindDef aniPawnKind, ThingDef humThing, bool hotReload)
        {
            var aniThing = aniPawnKind.race;

            if (aniThing.race?.IsFlesh != true) return;

            string thingDefName = $"HL_{aniThing.defName}";
            RaceProperties aniRace = aniThing.race;
            RaceProperties humRace = humThing.race;

            ThingDef newThing = thingDefName.TryGetExistingDef<ThingDef>();
            RaceProperties newRace = new();

            newThing ??= humThing.GetType().GetConstructor([]).Invoke([]) as ThingDef;
            RaceFuser.CopyThingDefFields(humThing, newThing);
            newThing.defName = thingDefName;

            // Whitelist for comps.
            var compWhitelist = new HashSet<string>
            {
                "CompProperties_HoldingPlatformTarget",
                "CompProperties_Studiable",
                // Add more here when verified to work.
            };

            List<CompProperties> bothComps = [];
            if (aniThing.comps != null) { bothComps.AddRange(aniThing.comps); }
            foreach (var comp in humThing.comps)
            {
                if (!compWhitelist.Contains(comp.GetType().Name))
                {
                    bothComps.Add(comp);
                }
            }
            var filteredComps = bothComps.Where(x=> compWhitelist.Contains(x.GetType().ToString())).ToList();

            // From Human
            newThing.modExtensions = [];  // Doubt we want to load ModExtensions from either.
            newThing.comps = filteredComps;

            newThing.thingCategories = humThing.thingCategories != null ? [.. humThing.thingCategories] : [];
            newThing.inspectorTabs = humThing.inspectorTabs != null ? [.. humThing.inspectorTabs] : null;
            newThing.inspectorTabsResolved = humThing.inspectorTabsResolved != null ? [.. humThing.inspectorTabsResolved] : null;
            newThing.stuffCategories = humThing.stuffCategories != null ? [.. humThing.stuffCategories] : null;
            newThing.thingSetMakerTags = humThing.thingSetMakerTags != null ? [.. humThing.thingSetMakerTags] : null;
            newThing.virtualDefs = humThing.virtualDefs != null ? [.. humThing.virtualDefs] : null;

            // From Animal
            newThing.tools = aniThing.tools != null ? [.. aniThing.tools] : null;
            newThing.verbs = aniThing.verbs != null ? [.. aniThing.verbs] : null;
            newThing.butcherProducts = ([.. (humThing.butcherProducts ?? []), .. (aniThing.butcherProducts ?? [])]);
            if (newThing.butcherProducts.Empty()) newThing.butcherProducts = null;
            newThing.smeltProducts = ([.. (humThing.smeltProducts ?? []), .. (aniThing.smeltProducts ?? [])]);
            if (newThing.smeltProducts.Empty()) newThing.smeltProducts = null;

            // From Both.
            newThing.recipes = [.. (humThing.recipes ?? []), .. aniThing?.recipes ?? []];
            newThing.recipes = newThing.recipes.Distinct().ToList();
            newThing.tradeTags = ([.. (humThing.tradeTags ?? []), .. (aniThing.tradeTags ?? [])]);

            // Deduplicate inspector tabs
            if (newThing.inspectorTabs != null)
            {
                newThing.inspectorTabs = newThing.inspectorTabs.Distinct().ToList();
            }
            if (newThing.inspectorTabsResolved != null)
            {
                newThing.inspectorTabsResolved = newThing.inspectorTabsResolved.Distinct().ToList();
            }

            RaceFuser.CopyRaceProperties(aniRace, newRace);
            if (aniRace.lifeExpectancy < humRace.lifeExpectancy)
            {
                newRace.lifeExpectancy = humRace.lifeExpectancy;
                newRace.ageGenerationCurve = humRace.ageGenerationCurve;
            }
            else
            {
                // Patch aging rate instead. Messing with the life-stages is bug-prone.
                // Also the storyteller generating 3 year old adult sapient rats married to 300 year old dragons is kinda creepy.
            }
            newRace.thinkTreeConstant = humRace.thinkTreeConstant;
            newRace.thinkTreeMain = humRace.thinkTreeMain;
            newRace.intelligence = humRace.intelligence;
            newRace.foodType = humRace.foodType;    // Best handled by the Diet code, probably.
            newRace.lifeStageAges = humRace.lifeStageAges;
            newRace.lifeStageWorkSettings = humRace.lifeStageWorkSettings;
            newRace.trainability = humRace.trainability;
            newRace.petness = humRace.petness;
            //newRace.wildness = humRace.wildness;
            newRace.predator = humRace.predator;
            newRace.animalType = humRace.animalType;
            newRace.meatDef = humRace.meatDef;  // Can cause issues if they are mechanoid meatDef, etc.
            newRace.hideTrainingTab = humRace.hideTrainingTab;
            newRace.canReleaseToWild = humRace.canReleaseToWild;
            newRace.disableAreaControl = humRace.disableAreaControl;
            newRace.canBePredatorPrey = humRace.canBePredatorPrey;
            newRace.allowedOnCaravan = humRace.allowedOnCaravan;
            newRace.herdAnimal = humRace.herdAnimal;
            newRace.herdMigrationAllowed = humRace.herdMigrationAllowed;
            newRace.packAnimal = humRace.packAnimal;
            newRace.willNeverEat = humRace.willNeverEat;
            newRace.nameCategory = humRace.nameCategory;
            newRace.nameGenerator = humRace.nameGenerator;
            newRace.nameGeneratorFemale = humRace.nameGeneratorFemale;
            newRace.nameOnTameChance = humRace.nameOnTameChance;

            // Lets not generate a bunch of unnatural corpses. Set via reflection because of reports that the 
            // field is sometimes not present. Somehow.
            var hasUnnaturalCorpseField = newRace.GetType().GetField("hasUnnaturalCorpse");
            hasUnnaturalCorpseField?.SetValue(newRace, false);

            if (newRace.renderTree.defName == "Animal")
            {
                newRace.renderTree = DefDatabase<PawnRenderTreeDef>.GetNamed("BS_HumanlikeAnimal");
            }
            else if (newRace.renderTree.defName == "Human") { }  // Could be replaced by a whitelist.
            else
            {
                Log.WarningOnce($"Skipping generating humanlike-animal from {aniPawnKind.defName} as it has an unhandled type of renderTree: {newRace.renderTree.defName}.\n" +
                    $"No warning will be sent for any further animals skipped for humanlike-animal generation to avoid spamming the log.", 6661337);
            }

            

            string raceHediffName = $"HL_{aniThing.defName}_RaceHediff";    // This can be used to override the hediff of the race.
            var raceHediff = raceHediffName.TryGetExistingDef<HediffDef>();
            bool hasHands = false;
            if (raceHediff?.GetAllPawnExtensionsOnHediff().FirstOrDefault()?.forceHasHands == true) hasHands = true;

            if (raceHediff == null)
            {
                List<string> manipulatorBlackList = ["Mouth", "Jaw", "Beak", "Leg"];
                var allParts = newRace.body.corePart.GetAllBodyPartsRecursive();
                hasHands =
                    HasPartWithTag(allParts, BodyPartTagDefOf.ManipulationLimbCore, manipulatorBlackList) ||
                    HasPartWithTag(allParts, BodyPartTagDefOf.ManipulationLimbDigit, manipulatorBlackList) ||
                    HasPartWithTag(allParts, BodyPartTagDefOf.ManipulationLimbSegment, manipulatorBlackList);

                raceHediff = new HediffDef
                {
                    defName = raceHediffName,
                    hediffClass = typeof(RaceTracker),
                    isBad = false,
                    everCurableByItem = false,
                    initialSeverity = 1,
                    label = aniThing.label,
                    description = aniThing.description,
                    defaultLabelColor = new Color(0.5f, 1, 1),
                    generated = true
                };

                raceHediff.comps ??= [];
                raceHediff.comps.Add(new CompProperties_Race
                {
                    canSwapAwayFrom = false,
                });

                if (!hasHands)
                {
                    raceHediff.stages ??= [];
                    raceHediff.stages.Add(new HediffStage
                    {
                        disabledWorkTags = WorkTags.Crafting | WorkTags.Shooting | WorkTags.Animals
                    });
                }
                var pawnExt = new PawnExtension();
                PawnExtensionDef targetAnimalBase = hasHands ? DefDatabase<PawnExtensionDef>.GetNamed("BS_DefaultAnimal", true) : DefDatabase<PawnExtensionDef>.GetNamed("BS_DefaultAnimal_NoHands", true);
                PawnExtensionDef defaultHandlessSettings = DefDatabase<PawnExtensionDef>.GetNamed("BS_DefaultAnimal_NoHands", true);
                // Transfer via reflection.
                foreach (var field in typeof(PawnExtension)
                    .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Where(f => !f.IsStatic && !f.IsInitOnly))
                {
                    var value = field.GetValue(defaultHandlessSettings.pawnExtension);
                    if (value == null) continue;  // Skip null values.
                    field.SetValue(pawnExt, value);
                }
                if (pawnExt.traitIcon == null && aniPawnKind.lifeStages?.Any() == true)
                {
                    pawnExt.traitIcon = aniPawnKind.lifeStages.Last().bodyGraphicData.texPath + "_east";
                }
                pawnExt.forceHasHands = hasHands;
                raceHediff.modExtensions = [pawnExt];
            }

            var raceExt = new RaceExtension();
            raceExt.SetHediff(raceHediff);

            newThing.generated = true;
            newThing.defName = thingDefName;
            newThing.label = aniThing.label;
            newThing.description = aniThing.description;
            newThing.race = newRace;

            newThing.modExtensions = [raceExt];

            SetAnimalStatDefValues(humThing, aniThing, newThing, hasHands);

            DefGenerator.AddImpliedDef(newThing, hotReload: true);
            DefGenerator.AddImpliedDef(newRace.body, hotReload: true);
            DefGenerator.AddImpliedDef(raceHediff, hotReload: true);

            newThing.ResolveReferences();
            raceHediff.ResolveReferences();

            humanlikeAnimals[newThing] = new HumanlikeAnimal
            {
                humanlikeThing = newThing,
                animalKind = aniPawnKind,
                humanlike = humThing,
                animal = aniThing
            };
        }

        public static void SetAnimalStatDefValues(ThingDef humanThing, ThingDef animalThing, ThingDef newThing, bool hasHands)
        {
            newThing.statBases = [];
            foreach (var statBase in humanThing.statBases)
            {
                newThing.statBases.Add(new StatModifier
                {
                    stat = statBase.stat,
                    value = statBase.value
                });
            }

            // Animal
            newThing.SetStatBaseValue(StatDefOf.PsychicSensitivity, animalThing.GetStatValueAbstract(StatDefOf.PsychicSensitivity));
            newThing.SetStatBaseValue(StatDefOf.PawnBeauty, animalThing.GetStatValueAbstract(StatDefOf.PawnBeauty));
            newThing.SetStatBaseValue(StatDefOf.MoveSpeed, animalThing.GetStatValueAbstract(StatDefOf.MoveSpeed));
            newThing.SetStatBaseValue(StatDefOf.IncomingDamageFactor, animalThing.GetStatValueAbstract(StatDefOf.IncomingDamageFactor));
            newThing.SetStatBaseValue(StatDefOf.MarketValue, animalThing.GetStatValueAbstract(StatDefOf.MarketValue));
            newThing.SetStatBaseValue(StatDefOf.Nutrition, animalThing.GetStatValueAbstract(StatDefOf.Nutrition));
            newThing.SetStatBaseValue(StatDefOf.Mass, animalThing.GetStatValueAbstract(StatDefOf.Mass));
            newThing.SetStatBaseValue(StatDefOf.ToxicResistance, animalThing.GetStatValueAbstract(StatDefOf.ToxicResistance));
            newThing.SetStatBaseValue(StatDefOf.ToxicEnvironmentResistance, animalThing.GetStatValueAbstract(StatDefOf.ToxicEnvironmentResistance));
            newThing.SetStatBaseValue(StatDefOf.CarryingCapacity, animalThing.GetStatValueAbstract(StatDefOf.CarryingCapacity));

            // Averaged
            newThing.SetStatBaseValue(StatDefOf.DeepDrillingSpeed, (animalThing.GetStatValueAbstract(StatDefOf.DeepDrillingSpeed) + humanThing.GetStatValueAbstract(StatDefOf.DeepDrillingSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.MiningSpeed, (animalThing.GetStatValueAbstract(StatDefOf.MiningSpeed) + humanThing.GetStatValueAbstract(StatDefOf.MiningSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.MiningYield, (animalThing.GetStatValueAbstract(StatDefOf.MiningYield) + humanThing.GetStatValueAbstract(StatDefOf.MiningYield)) / 2);
            newThing.SetStatBaseValue(StatDefOf.ConstructionSpeed, (animalThing.GetStatValueAbstract(StatDefOf.ConstructionSpeed) + humanThing.GetStatValueAbstract(StatDefOf.ConstructionSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.SmoothingSpeed, (animalThing.GetStatValueAbstract(StatDefOf.SmoothingSpeed) + humanThing.GetStatValueAbstract(StatDefOf.SmoothingSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.PlantHarvestYield, (animalThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield) + humanThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield)) / 2);
            newThing.SetStatBaseValue(StatDefOf.PlantWorkSpeed, (animalThing.GetStatValueAbstract(StatDefOf.PlantWorkSpeed) + humanThing.GetStatValueAbstract(StatDefOf.PlantWorkSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.PlantHarvestYield, (animalThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield) + humanThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield)) / 2);
            newThing.SetStatBaseValue(StatDefOf.MeleeDodgeChance, (animalThing.GetStatValueAbstract(StatDefOf.MeleeDodgeChance) + humanThing.GetStatValueAbstract(StatDefOf.MeleeDodgeChance)) / 2);

            if (!hasHands)
            {
                newThing.SetStatBaseValue(StatDefOf.WorkSpeedGlobal, newThing.GetStatValueAbstract(StatDefOf.WorkSpeedGlobal) * 0.75f);
                newThing.SetStatBaseValue(StatDefOf.SurgerySuccessChanceFactor, newThing.GetStatValueAbstract(StatDefOf.SurgerySuccessChanceFactor) * 0.5f);
            }

            // No "Bee Movie" please.
            newThing.SetStatBaseValue(RomancePatches.FlirtChanceDef, 0);  // This prevents Lovin' from happening as well.
        }

        private static bool HasPartWithTag(List<BodyPartRecord> parts, BodyPartTagDef tag, List<string> blackListKeyword)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part.def.tags.Contains(tag))
                {
                    bool blacklisted = blackListKeyword.Any(part.def.defName.Contains);
                    if (!blacklisted) return true;
                }
            }

            return false;
        }
    }
}
