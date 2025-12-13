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
        public ThingDef humanlikeAnimal;
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

        public static ThingDef HumanLikeAnimalFor(ThingDef td) => GetHumanlikeAnimalFor(td)?.humanlikeAnimal;
        public static ThingDef HumanLikeSourceFor(ThingDef td) => GetHumanlikeAnimalFor(td)?.humanlike;
        public static ThingDef AnimalSourceFor(ThingDef td) => GetHumanlikeAnimalFor(td)?.animal;
        public static bool IsHumanlikeAnimal(this ThingDef td) => humanlikeAnimals.ContainsKey(td);
    }

    public static class HumanlikeAnimalGenerator
    {
        public static bool HasHumanlikeAnimals { get; private set; }
        public static Dictionary<ThingDef, HumanlikeAnimal> humanlikeAnimals = [];
        public static Dictionary<ThingDef, HumanlikeAnimal> reverseLookupHumanlikeAnimals = [];
        public static HashSet<BodyDef> modifiedBodies = [];

        public static void GenerateHumanlikeAnimals(bool hotReload)
        {
            if (BigSmall.BSSapientAnimalsActive || BigSmall.BSSapientMechanoidsActive)  // Just replace this with the actual mod's name later.
            {
                HasHumanlikeAnimals = true;
                modifiedBodies.Clear();
                HashSet<ThingDef> thingDefsGenerated = [];

                var aniPawnKinds = DefDatabase<PawnKindDef>.AllDefs
                    .Where(x => x.race is ThingDef aniThing && aniThing?.race is RaceProperties race &&
                        race.Animal && race.intelligence == Intelligence.Animal)
                    .ToList();

                if (BigSmall.BSSapientMechanoidsActive)
                {
                    var mechPawnKinds = DefDatabase<PawnKindDef>.AllDefs
                        .Where(x => x.race is ThingDef mechThing && mechThing?.race is RaceProperties mechRace &&
                            mechRace.IsMechanoid &&
                            mechRace.intelligence >= Intelligence.Animal && mechRace.intelligence != Intelligence.Humanlike);
                    Log.Message($"Found {mechPawnKinds.Count()} mechanoid pawn kinds to generate humanlike animals from.");
                    aniPawnKinds.AddRange(mechPawnKinds);
                    SapienatorCanTargetMechsHack();
                }
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
                modifiedBodies.Clear();
            }
        }

        private static void SapienatorCanTargetMechsHack()
        {
            // Hack to change the Sapienator.
            var sapienator = DefDatabase<ThingDef>.GetNamedSilentFail("BS_Sapienator");
            sapienator?.comps.Where(x => x is CompProperties_TargetableExtended).ToList().ForEach(x =>
            {
                var comp = (CompProperties_TargetableExtended)x;
                comp.targetInfo.canTargetMechs = true;
            });
        }

        private static void MakeDummySetupsForAlreadySapientAnimals(PawnKindDef animalLikePK)
        {
            var pawnExt = animalLikePK.ExtensionsOnDef<PawnKindExtension, PawnKindDef>().First(x => x.generateHumanlikeAnimalFromThis);
            HumanlikeAnimal hla = new()
            {
                animalKind = animalLikePK,
                humanlikeAnimal = animalLikePK.race,
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
		/// <param name="aniPawnKind">ThingKindDef of an Animal.</param>
		/// <param name="humThing">ThingDef of a Humanlike (likely always the defautl "Human")</param>
		/// <param name="hotReload">Whether or not this is in context of a hotreload.</param>
		public static void GenerateAndRegisterHumanlikeAnimal(PawnKindDef aniPawnKind, ThingDef humThing, bool hotReload)
        {
            var aniThing = aniPawnKind.race;

            bool forceHands = BigSmallMod.settings.allAnimalsHaveHands;

            //if (aniThing.race?.IsFlesh != true) return;

            string thingDefName = $"HL_{aniThing.defName}";
            RaceProperties aniRace = aniThing.race;
            RaceProperties humRace = humThing.race;

            ThingDef newThing = thingDefName.TryGetExistingDef<ThingDef>();
            RaceProperties newRace = new();

            newThing ??= humThing.GetType().GetConstructor([]).Invoke([]) as ThingDef;
            RaceFuser.CopyThingDefFields(humThing, newThing);
            newThing.defName = thingDefName;

            HashSet<string> compWhitelist = [];
            HashSet<string> tabWhiteList = [];
            HashSet<string> extWhiteList = [];
            HashSet<RomanceTags> romanceTags = [];
            RomanceTags romanceOverride = null;
            foreach (var setting in HumanlikeAnimalSettings.AllHASettings)
            {
                compWhitelist.AddRange(setting.compWhitelist);
                tabWhiteList.AddRange(setting.tabWhitelist);
                extWhiteList.AddRange(setting.modExtensionWhitelist);
                romanceTags.AddRange(setting.animalFamilySettings
                    .Where(x =>
                        x.members.Any(x => aniThing.defName.Contains(x, StringComparison.OrdinalIgnoreCase)) ||
                        x.membersExact.Any(x => aniThing.defName.Equals(x, StringComparison.OrdinalIgnoreCase)))
                    .Select(x => x.romanceTags));
            }
            if (romanceTags.Any())
            {
                romanceOverride = romanceTags.GetMerged();
            }

            // Lowercase comparer
            List<CompProperties> bothComps = [];
            if (aniThing.comps != null) { bothComps.AddRange(aniThing.comps); }
            foreach (var comp in humThing.comps)
            {
                // Check if the comp is already in the list, to avoid duplicates.
                if (!bothComps.Any(x => x.GetType() == comp.GetType() && x.compClass == comp.compClass))
                {
                    bothComps.Add(comp);
                }
            }
            var filteredComps = bothComps
                .Where(x =>
                    compWhitelist.Contains(x.GetType().ToString(), StringComparer.OrdinalIgnoreCase) ||
                    compWhitelist.Contains(x.compClass.ToString(), StringComparer.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            newThing.comps = filteredComps;
            newThing.thingClass = aniThing.thingClass;

            // From Human

            newThing.thingCategories = humThing.thingCategories != null ? [.. humThing.thingCategories] : [];
            newThing.stuffCategories = humThing.stuffCategories != null ? [.. humThing.stuffCategories] : null;
            newThing.thingSetMakerTags = humThing.thingSetMakerTags != null ? [.. humThing.thingSetMakerTags] : null;
            newThing.virtualDefs = humThing.virtualDefs != null ? [.. humThing.virtualDefs] : null;

            // From Animal
            newThing.modExtensions = [];  // Doubt we want to load ModExtensions from either.
            if (aniThing.modExtensions?.Count > 0)
            {
                newThing.modExtensions.AddRange(aniThing.modExtensions.Where(x => extWhiteList.Contains(x.GetType().ToString(), StringComparer.OrdinalIgnoreCase)));
            }

            newThing.tools = aniThing.tools != null ? [.. aniThing.tools] : null;
            newThing.verbs = aniThing.verbs != null ? [.. aniThing.verbs] : null;
            newThing.butcherProducts = ([.. (humThing.butcherProducts ?? []), .. (aniThing.butcherProducts ?? [])]);
            if (newThing.butcherProducts.Empty()) newThing.butcherProducts = null;
            newThing.smeltProducts = ([.. (humThing.smeltProducts ?? []), .. (aniThing.smeltProducts ?? [])]);
            if (newThing.smeltProducts.Empty()) newThing.smeltProducts = null;

            // From Both.
            List<RecipeDef> recipes = new();
            if (humThing.recipes != null)
                recipes.AddRange(humThing.recipes);

            if (aniThing.recipes != null)
                recipes.AddRange(aniThing.recipes);

            newThing.recipes = recipes.Distinct().ToList();
            newThing.tradeTags = ([.. (humThing.tradeTags ?? []), .. (aniThing.tradeTags ?? [])]);


            // Transfer filtered inspector tabs from both human and animal.
            List<Type> bothInspectTabs = [];
            if (humThing.inspectorTabs != null)
                bothInspectTabs.AddRange(humThing.inspectorTabs);

            if (aniThing.inspectorTabs != null)
                bothInspectTabs.AddRange(aniThing.inspectorTabs);

            newThing.inspectorTabs = bothInspectTabs
                .Where(x => tabWhiteList.Contains(x.ToString(), StringComparer.OrdinalIgnoreCase))
                .Distinct()
                .ToList();


            RaceFuser.CopyRaceProperties(aniRace, newRace);
            if (aniRace.lifeExpectancy < humRace.lifeExpectancy)
            {
                newRace.lifeExpectancy = humRace.lifeExpectancy;
                newRace.ageGenerationCurve = humRace.ageGenerationCurve;
            }
            else
            {
                // Patch aging rate instead. Messing with the life-stages is bug-prone.
                // Also the storyteller generating 3 year old adult sapient rats married to 300 year old dragons would be kinda creepy.
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
            newRace.fleshType = newRace.fleshType == FleshTypeDefOf.Mechanoid ? humRace.FleshType : newRace.FleshType;
            newRace.meatDef = humRace.meatDef;
            //newRace.flightStartChanceOnJobStart = aniRace.flightStartChanceOnJobStart > 0 ? 1 : 0;
            if (aniRace.hasMeat)
            {
                if (aniRace.specificMeatDef != null)
                {
                    newRace.specificMeatDef = aniRace.specificMeatDef;
                }
                else if (aniRace.useMeatFrom != null)
                {
                    newRace.useMeatFrom = aniRace.useMeatFrom;
                }
                else
                {
                    newRace.useMeatFrom = aniThing;
                }
            }
            else
            {
                newRace.specificMeatDef = BSDefs.BS_MeatGeneric;
            }
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
            newRace.roamMtbDays = null;

            // Lets not generate a bunch of unnatural corpses. Set via reflection because of reports that the 
            // field is sometimes not present. Somehow.
            var hasUnnaturalCorpseField = newRace.GetType().GetField("hasUnnaturalCorpse");
            hasUnnaturalCorpseField?.SetValue(newRace, false);
            SetRenderTree(aniPawnKind, aniThing, aniRace, humRace, newRace);

            // Fix animal body so animals can equip stuff. This also caches the parts if this is not already done.
            SetupBodyTags(newThing, newRace);

            string raceHediffName = $"HL_{aniThing.defName}_RaceHediff";    // This can be used to override the hediff of the race.

            var raceHediff = raceHediffName.TryGetExistingDef<HediffDef>();
            PawnExtension racePawnExtension = raceHediff?.GetAllPawnExtensionsOnHediff().FirstOrDefault();
            float fineManipulation = racePawnExtension?.animalFineManipulation ?? 0f;
            if (raceHediff == null)
            {
                bool hasHands = false;

                List<string> manipulatorBlackList = ["Mouth", "Jaw", "Beak", "Leg"];
                var allParts = newRace.body.corePart.GetAllBodyPartsRecursive();

                if (HumanlikeAnimalSettings.AllHASettings.Any(x => x.hasHandsWildcards.Any(wc => aniThing.defName.Contains(wc, StringComparison.OrdinalIgnoreCase))))
                {
                    hasHands = true;
                    fineManipulation = 1.0f;
                }
                else if (aniThing.race.IsMechanoid)
                {
                    // Mechanoids are assume to always be somewhat decent at finemanipulation.
                    hasHands = true;
                    fineManipulation = 0.65f;
                }
                else if (HumanlikeAnimalSettings.AllHASettings.Any(x => x.hasPoorHandsWildcards.Any(wc => aniThing.defName.Contains(wc, StringComparison.OrdinalIgnoreCase))))
                {
                    hasHands = true;
                    fineManipulation = 0.5f;
                }
                else
                {
                    hasHands =
                        HasPartWithTag(allParts, BodyPartTagDefOf.ManipulationLimbCore, manipulatorBlackList) ||
                        HasPartWithTag(allParts, BodyPartTagDefOf.ManipulationLimbDigit, manipulatorBlackList) ||
                        HasPartWithTag(allParts, BodyPartTagDefOf.ManipulationLimbSegment, manipulatorBlackList);
                    fineManipulation = hasHands ? 1.0f : 0f;
                }

                if (forceHands)
                {
                    fineManipulation = 1.0f;
                    hasHands = true;
                }

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


                if (VanillaExpanded.VEActive)
                {
                    var aniComps = aniThing.comps ?? [];
                    foreach (var extraAbility in VEF_InitialAbility_Helper.TryGetAbilities(aniComps))
                    {
                        raceHediff.abilities ??= [];
                        raceHediff.abilities.Add(extraAbility);
                    }
                }

                raceHediff.comps ??= [];
                raceHediff.comps.Add(new CompProperties_Race
                {
                    canSwapAwayFrom = false,
                });

                var pawnExt = new PawnExtension();
                PawnExtensionDef targetAnimalBase = fineManipulation > 0.75f ? BSDefs.BS_DefaultAnimal : fineManipulation > 0.35f ? BSDefs.BS_DefaultAnimal_PoorHands : BSDefs.BS_DefaultAnimal_NoHands;
                if (targetAnimalBase.pawnExtension.animalFineManipulation != null)
                {
                    // Normally this will be null, but if a modder has set values then we use those instead for any values calculated based of it.
                    fineManipulation = targetAnimalBase.pawnExtension.animalFineManipulation.Value;
                    if (fineManipulation > 0.45f)
                    {
                        hasHands = true;
                    }
                }
                if (!hasHands && !BigSmallMod.settings.animalsLowSkillPenalty)
                {
                    raceHediff.stages ??= [];
                    raceHediff.stages.Add(new HediffStage
                    {
                        disabledWorkTags = WorkTags.Crafting | WorkTags.Shooting | WorkTags.Animals
                    });
                }

                if (aniPawnKind.RaceProps.IsMechanoid)
                {
                    targetAnimalBase = BSDefs.BS_DefaultMechanoid;
                }

                foreach (var field in typeof(PawnExtension)
                    .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Where(f => !f.IsStatic && !f.IsInitOnly))
                {
                    var value = field.GetValue(targetAnimalBase.pawnExtension);
                    if (value == null) continue;  // Skip null values.
                    field.SetValue(pawnExt, value);
                }

                if (BigSmallMod.settings.animalsLowSkillPenalty && pawnExt.aptitudes != null)
                {
                    foreach (var skill in pawnExt.aptitudes)
                    {
                        if (skill.level < -8)
                        {
                            skill.level = -4;
                        }
                        else if (skill.level < -4)
                        {
                            skill.level = -2;
                        }
                        else if (skill.level < 0)
                        {
                            skill.level = 0;
                        }
                    }
                }
                if (pawnExt.traitIcon == null && aniPawnKind.lifeStages?.Any() == true)
                {
                    pawnExt.traitIcon = GetTraitIcon(aniPawnKind);
                }
                pawnExt.animalFineManipulation = fineManipulation;
                pawnExt.romanceTags = romanceOverride;
                raceHediff.modExtensions = [pawnExt];
                racePawnExtension = pawnExt;
            }

            GenerateProductionCompsFromAnimal(aniThing, raceHediff);

            // At this point if racePawnExtension didn't exist then we have now created it.
            // Same goes for raceHediff.
            fineManipulation = racePawnExtension.animalFineManipulation ?? fineManipulation;
            if ((racePawnExtension.traitIcon == null || racePawnExtension.traitIcon == "BS_Traits/robot") &&
                aniPawnKind.lifeStages?.Any() == true)
            {
                racePawnExtension.traitIcon = GetTraitIcon(aniPawnKind);
            }
            if (racePawnExtension.animalFineManipulation < 0.45)
            {
                racePawnExtension.canWieldThings = false;
            }
            racePawnExtension.bodyTypes.Add(
                new GenderBodyType()
                {
                    bodyType = BSDefs.BS_AnimalBodyType,
                    isDefault = true,
                }
            );

            racePawnExtension.nullsThoughts ??= [];
            var allUncoveredThoughts = DefDatabase<ThoughtDef>.AllDefs.Where(x => x.defName.Contains("uncovered", StringComparison.OrdinalIgnoreCase));
            var allSweatThoughts = DefDatabase<ThoughtDef>.AllDefs.Where(x => x.defName.Contains("sweat", StringComparison.OrdinalIgnoreCase));
            var tableThoughts = DefDatabase<ThoughtDef>.AllDefs.Where(x => x.defName.Contains("table", StringComparison.OrdinalIgnoreCase));
            racePawnExtension.nullsThoughts.AddDistinctRange(allUncoveredThoughts);
            racePawnExtension.nullsThoughts.AddDistinctRange(allSweatThoughts);
            racePawnExtension.nullsThoughts.AddDistinctRange(tableThoughts);

            if (aniPawnKind.abilities != null)
            {
                raceHediff.abilities ??= [];
                raceHediff.abilities.AddRange(aniPawnKind.abilities);
            }

            var raceExt = new RaceExtension();
            raceExt.SetHediff(raceHediff);

            newThing.generated = true;
            newThing.label = aniThing.label;
            newThing.description = aniThing.description;
            newThing.race = newRace;

            newThing.modExtensions.Add(raceExt);

            SetAnimalStatDefValues(humThing, aniThing, newThing, fineManipulation, racePawnExtension);
            if (newRace.gestationPeriodDays == -1)
            {
                newRace.gestationPeriodDays = humRace.gestationPeriodDays;
                if (ModsConfig.BiotechActive)  // If you want egg-laying sapient animals you'll have to figure it out yourselves.
                {
                    newThing.SetStatBaseValue(StatDefOf.Fertility, 0);
                }
            }

            DefGenerator.AddImpliedDef(newThing, hotReload: true);
            DefGenerator.AddImpliedDef(newRace.body, hotReload: true);
            DefGenerator.AddImpliedDef(raceHediff, hotReload: true);

            newThing.ResolveReferences();
            raceHediff.ResolveReferences();

            humanlikeAnimals[newThing] = new HumanlikeAnimal
            {
                humanlikeAnimal = newThing,
                animalKind = aniPawnKind,
                humanlike = humThing,
                animal = aniThing
            };

            static string GetTraitIcon(PawnKindDef aniPawnKind)
            {
                return aniPawnKind.lifeStages.Last().bodyGraphicData.texPath + "_east";
            }
        }

        private static void GenerateProductionCompsFromAnimal(ThingDef aniThing, HediffDef raceHediff)
        {
            if (raceHediff.comps?.Any(x=>x is ProductionHediffSettings) == true)
            {
                // User has likely defined their own production hediffs, so don't auto-generate any.
                return;
            }
            foreach (var mComp in aniThing.comps?.Where(x => x is CompProperties_Milkable) ?? [])
            {
                var milkComp = (CompProperties_Milkable)mComp;
                var product = milkComp.milkDef;
                var quantity = milkComp.milkAmount;
                var frequency = milkComp.milkIntervalDays;
                bool femaleOnly = milkComp.milkFemaleOnly;
                GenerateProductionComp(raceHediff, product, quantity, frequency, femaleOnly: femaleOnly);
            }
            foreach (var sComp in aniThing.comps?.Where(x => x is CompProperties_Shearable) ?? [])
            {
                var shearComp = (CompProperties_Shearable)sComp;
                var product = shearComp.woolDef;
                var quantity = shearComp.woolAmount;
                var frequency = shearComp.shearIntervalDays;
                GenerateProductionComp(raceHediff, product, quantity, frequency);
            }
            foreach (var eComp in aniThing.comps?.Where(x => x is CompProperties_EggLayer) ?? [])
            {
                var eggComp = (CompProperties_EggLayer)eComp;
                var product = eggComp.eggUnfertilizedDef; // >_>;;
                var quantity = Mathf.CeilToInt(eggComp.eggCountRange.Average);
                var frequency = Mathf.CeilToInt(eggComp.eggLayIntervalDays);
                bool femaleOnly = eggComp.eggLayFemaleOnly;
                GenerateProductionComp(raceHediff, product, quantity, frequency, femaleOnly: femaleOnly);
            }
            foreach (var apComp in aniThing.comps.Where(x => x.GetType().Name == "CompProperties_AnimalProduct"))
            {
                var productField = apComp.GetType().GetField("resourceDef");
                var amountField = apComp.GetType().GetField("resourceAmount");
                var intervalField = apComp.GetType().GetField("gatheringIntervalDays");
                if (productField != null && amountField != null && intervalField != null)
                {
                    var quantity = (int)amountField.GetValue(apComp);
                    var frequency = (int)intervalField.GetValue(apComp);
                    if (productField.GetValue(apComp) is ThingDef product)
                    {
                        GenerateProductionComp(raceHediff, product, quantity, frequency);
                    }

                    var ramdomItems = apComp.GetType().GetField("randomItems");
                    if (ramdomItems?.GetValue(apComp) is List<string> itemList)
                    {
                        var listLength = itemList.Count;
                        List<ThingDef> possibleProducts = [.. itemList.Select(DefDatabase<ThingDef>.GetNamedSilentFail).Where(def => def != null)];
                        if (possibleProducts.Count == 0) continue;

                        GenerateProductionComp(raceHediff, null, quantity, frequency, rngOptions: possibleProducts);
                    }
                }
            }
        }

        private static void GenerateProductionComp(HediffDef raceHediff, ThingDef product, int quantity, int frequency, bool femaleOnly=false, List<ThingDef> rngOptions = null, float chance=1)
        {
            rngOptions ??= [];

            string saveKeyEnd = product != null ? product.defName : "RandomProduct";

            ProductionHediffSettings pSettings = new()
            {
                frequencyInDays = frequency,
                progressName = "ResourceGrowth",
                saveKey = $"{raceHediff.defName}_ResourceGrowth_{saveKeyEnd}",
                activationAge = 13,
                chance = chance,
                femaleOnly = femaleOnly,
                products =
                [
                    new()
                    {
                        product = product,
                        randomProduct = rngOptions,
                        baseAmount = quantity,
                    }
                ],
            };
            while (raceHediff.comps.Any(x=>x.compClass == pSettings.compClass))
            {
                Type compClass = pSettings.NextFromThis();
                pSettings.compClass = compClass;
                if (pSettings == null)
                {
                    Log.Warning($"Could not add production hediff for {product?.defName} to {raceHediff?.defName} due to too many comp class name collisions.");
                    return;
                }
            }

            raceHediff.comps.Add(pSettings);
        }

        private static void SetRenderTree(PawnKindDef aniPawnKind, ThingDef aniThing, RaceProperties aniRace, RaceProperties humRace, RaceProperties newRace)
        {
            bool renderTreeOverriden = false;
            List<RenderTreeOverride> renderTreeOverrides = [];
            foreach (var setting in HumanlikeAnimalSettings.AllHASettings)
            {
                renderTreeOverrides.AddRange(setting.renderTreeWhitelist);
            }
            foreach (var overrideDef in renderTreeOverrides)
            {
                if (overrideDef.thingDefNames.Contains(aniThing.defName, StringComparer.OrdinalIgnoreCase))
                {
                    newRace.renderTree = DefDatabase<PawnRenderTreeDef>.GetNamed(overrideDef.renderTreeDefName);
                    renderTreeOverriden = true;
                    break;
                }
            }

            if (!renderTreeOverriden)
            {
                if (newRace.renderTree.defName == "Animal" || newRace.renderTree.defName == "Misc")
                {
                    newRace.renderTree = DefDatabase<PawnRenderTreeDef>.GetNamed("BS_HumanlikeAnimal");
                }
                else if (newRace.renderTree.defName == "Human") { }  // Could be replaced by a whitelist.
                else
                {

                    if (aniRace.Humanlike)
                    {
                        Log.WarningOnce($"Unhandled Render-Tree: {aniPawnKind.defName} has an unhandled render tree: {newRace.renderTree.defName}. It will likely not render as expected if made sapient. Keeping previous.\n" +
                        $"No warning of this type will be sent to avoid spamming the log.", 6661337);
                        newRace.renderTree = humRace.renderTree;
                    }
                    else
                    {
                        Log.WarningOnce($"Unhandled Render-Tree: {aniPawnKind.defName} has an unhandled render tree: {newRace.renderTree.defName}. It will likely not render as expected if made sapient. Defaulting to BS_HumanlikeAnimal.\n" +
                        $"No warning of this type will be sent to avoid spamming the log.", 6661338);
                        newRace.renderTree = DefDatabase<PawnRenderTreeDef>.GetNamed("BS_HumanlikeAnimal");
                    }
                }
            }
        }

        private static void GetPartsRecursive(BodyPartRecord part, List<BodyPartRecord> parts)
        {
            parts.Add(part);
            foreach (var child in part.parts)
            {
                GetPartsRecursive(child, parts);
            }
        }

        private static void SetupBodyTags(ThingDef newThing, RaceProperties newRace)
        {
            if (modifiedBodies.Add(newRace.body) == false)
            {
                return;
            }
            bool foundUtilitySlot = false;

            // Waist. Now without being... alive.
            var bodyPartDefWaist = DefDatabase<BodyPartDef>.AllDefs.FirstOrDefault(x => x.defName == "BS_InorganicWaist");
            //var bodyPartDefWaist = DefDatabase<BodyPartDef>.AllDefs.FirstOrDefault(x => x.defName == "Waist");
            var armGrp = DefDatabase<BodyPartGroupDef>.AllDefs.FirstOrDefault(x => x.defName == "Arms");
            var shouldGrp = DefDatabase<BodyPartGroupDef>.AllDefs.FirstOrDefault(x => x.defName == "Shoulders");
            var waistGrp = DefDatabase<BodyPartGroupDef>.AllDefs.FirstOrDefault(x => x.defName == "Waist");

            List<string> multiUseTorso = ["snakebody"];
            List<string> legAndArmParts = ["tentacle"];
            List<string> legParts = ["leg", "snakeBody"];
            List<string> armParts = ["arm", "wing"];
            List<BodyPartRecord> allParts = [];
            GetPartsRecursive(newRace.body.corePart, allParts);
            allParts.ForEach(part =>
            {
                if (part.def.defName == "Waist")
                {
                    foundUtilitySlot = true;
                }
                if (part == newRace.body.corePart)
                {
                    TryAddTags(newThing, part, multiUseTorso, [BodyPartGroupDefOf.Torso, BodyPartGroupDefOf.Legs]);
                    AddGroupIfNoneDefined(part, [BodyPartGroupDefOf.Torso], newThing);
                }
                else if (TryAddTags(newThing, part, legAndArmParts, [BodyPartGroupDefOf.Legs, armGrp])) { }
                else if (TryAddTags(newThing, part, legParts, [BodyPartGroupDefOf.Legs])) { }
                else if (TryAddTags(newThing, part, armParts, [armGrp])) { }
                
            });
            if (!foundUtilitySlot)
            {
                var corePart = newRace.body.corePart;
                var utilityPart = new BodyPartRecord
                {
                    def = bodyPartDefWaist,
                    coverage = 0.0f,
                    parent = corePart,
                    groups = [waistGrp],
                };
                corePart.parts.Add(utilityPart);
            }
        }

        private static bool TryAddTags(ThingDef newThing, BodyPartRecord part, List<string> tags, List<BodyPartGroupDef> grp)
        {
            bool partHasTag = tags.Any(tag => part.def.defName.ToLower().Contains(tag) == true);
            bool parentHasTag = part.parent != null && tags.Any(tag => part.parent.def.defName.ToLower().Contains(tag) == true);
            if (partHasTag || parentHasTag)
            {
                AddGroupIfNoneDefined(part, grp, newThing);
                return true;
            }
            return false;
        }

        private static void AddGroupIfNoneDefined(BodyPartRecord part, List<BodyPartGroupDef> groupToAdd, ThingDef thingDef)
        {
            if (part.groups.NullOrEmpty())
            {
                part.groups = groupToAdd;
                //Log.Message($"Adding group {groupToAdd.First().defName} to {thingDef.defName} body part {part.def.defName}.");
            }
        }

        public static void SetAnimalStatDefValues(ThingDef humanThing, ThingDef animalThing, ThingDef newThing, float fineManipulation, PawnExtension pExt)
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
            var animalThingSize = animalThing.race.baseBodySize;

            // Animal
            newThing.SetStatBaseValue(StatDefOf.PsychicSensitivity, animalThing.GetStatValueAbstract(StatDefOf.PsychicSensitivity));
            newThing.SetStatBaseValue(StatDefOf.PawnBeauty, animalThing.GetStatValueAbstract(StatDefOf.PawnBeauty));
            newThing.SetStatBaseValue(StatDefOf.MoveSpeed, animalThing.GetStatValueAbstract(StatDefOf.MoveSpeed));
            newThing.SetStatBaseValue(StatDefOf.IncomingDamageFactor, animalThing.GetStatValueAbstract(StatDefOf.IncomingDamageFactor));
            newThing.SetStatBaseValue(StatDefOf.MarketValue, animalThing.GetStatValueAbstract(StatDefOf.MarketValue) * 1.5f);
            newThing.SetStatBaseValue(StatDefOf.Mass, animalThing.GetStatValueAbstract(StatDefOf.Mass));
            newThing.SetStatBaseValue(StatDefOf.ToxicResistance, animalThing.GetStatValueAbstract(StatDefOf.ToxicResistance));
            newThing.SetStatBaseValue(StatDefOf.ToxicEnvironmentResistance, animalThing.GetStatValueAbstract(StatDefOf.ToxicEnvironmentResistance));
            newThing.SetStatBaseValue(StatDefOf.CarryingCapacity, animalThing.GetStatValueAbstract(StatDefOf.CarryingCapacity));
            newThing.SetStatBaseValue(StatDefOf.ComfyTemperatureMax, animalThing.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax));
            newThing.SetStatBaseValue(StatDefOf.ComfyTemperatureMin, animalThing.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin));
            newThing.SetStatBaseValue(StatDefOf.LeatherAmount, animalThing.GetStatValueAbstract(StatDefOf.LeatherAmount));
            newThing.SetStatBaseValue(StatDefOf.MeatAmount, animalThing.GetStatValueAbstract(StatDefOf.MeatAmount));
            newThing.SetStatBaseValue(StatDefOf.FlightCooldown, animalThing.GetStatValueAbstract(StatDefOf.FlightCooldown));
            newThing.SetStatBaseValue(StatDefOf.MaxFlightTime, animalThing.GetStatValueAbstract(StatDefOf.MaxFlightTime));
            newThing.SetStatBaseValue(StatDefOf.ArmorRating_Sharp, animalThing.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp));
            newThing.SetStatBaseValue(StatDefOf.ArmorRating_Blunt, animalThing.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt));
            newThing.SetStatBaseValue(StatDefOf.ArmorRating_Heat, animalThing.GetStatValueAbstract(StatDefOf.ArmorRating_Heat));


            // Most animal eat far less than a human of equivalent size in RimWorld for game mechanical reasons.
            // As a colonist a giant sapient Thumbo needs  eat more than a human or its gets rather silly (and OP).
            //
            // This is still very generous compared to sized-up humans.
            var scaledHumanNutrition = humanThing.GetStatValueAbstract(StatDefOf.Nutrition) * animalThingSize;
            var animalNutrition = animalThing.GetStatValueAbstract(StatDefOf.Nutrition);
            newThing.SetStatBaseValue(StatDefOf.Nutrition, Math.Max(animalNutrition, scaledHumanNutrition));

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
            newThing.SetStatBaseValue(StatDefOf.FilthRate, animalThing.GetStatValueAbstract(StatDefOf.FilthRate) / 3 + (humanThing.GetStatValueAbstract(StatDefOf.FilthRate) / 3 * 2));

            // Half
            newThing.SetStatBaseValue(StatDefOf.FlightCooldown, animalThing.GetStatValueAbstract(StatDefOf.FlightCooldown)/2);

            if (fineManipulation < 0.99)
            {
                float workspeedMult = Mathf.Lerp(1.0f, 0.65f, fineManipulation);
                newThing.SetStatBaseValue(StatDefOf.WorkSpeedGlobal, newThing.GetStatValueAbstract(StatDefOf.WorkSpeedGlobal) * workspeedMult);
                float surgeryMult = Mathf.Lerp(1.0f, 0.5f, fineManipulation);
                newThing.SetStatBaseValue(StatDefOf.SurgerySuccessChanceFactor, newThing.GetStatValueAbstract(StatDefOf.SurgerySuccessChanceFactor) * surgeryMult);
            }
            if (pExt.isMechanical || animalThing.race.IsMechanoid)
            {
                if (ModsConfig.BiotechActive)
                {
                    newThing.SetStatBaseValue(StatDefOf.Fertility, 0);
                }
                // 1.6 is just a random value. Might need tweaking. Probably better to make it too cheap than too expensive though while we're experimenting.
                newThing.SetStatBaseValue(BSDefs.BS_BatteryCharging, Mathf.Max(animalThing.GetStatValueAbstract(BSDefs.BS_BatteryCharging), 1.6f));
            }


            if (animalThing.race.Animal && animalThing.race.Insect && pExt.romanceTags == null)
            {
                pExt.romanceTags = new RomanceTags()
                {
                    compatibilities = new()
                    {
                        ["BS_Insect".Translate()] = new RomanceTags.Compatibility { chance = 1.0f, factor = 1.0f }
                    }
                };
                if (BigSmallMod.settings.animalOnAnimal)
                {
                    pExt.romanceTags.compatibilities["BS_SapientAnimal".Translate()] = new RomanceTags.Compatibility { chance = 0.75f, factor = 1.0f };
                }
            }
            // No "Bee Movie" please.
            else if (animalThing.race.Animal)
            {
                pExt.romanceTags ??= new RomanceTags()
                {
                    compatibilities = new()
                    {
                        [animalThing.label] = new RomanceTags.Compatibility { chance = 1.0f, factor = 1.0f }
                    }
                };
                if (BigSmallMod.settings.animalOnAnimal)
                {
                    pExt.romanceTags.compatibilities["BS_SapientAnimal".Translate()] = new RomanceTags.Compatibility { chance = 0.75f, factor = 1.0f };
                }
            }

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
