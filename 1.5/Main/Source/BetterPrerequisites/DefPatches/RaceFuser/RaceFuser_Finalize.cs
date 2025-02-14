using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;
using static HarmonyLib.AccessTools;

namespace BigAndSmall
{
    public static partial class RaceFuser
    {
        private static void GenerateAndRegisterRaceDefs(bool hotReload)
        {
            foreach (var fused in FusedBody.FusedBodies.Values)
            {
                var generateBody = fused.generatedBody;
                var source = fused.SourceBody;

                if (doDebug) Log.Message(GetPartsStringRecursive(generateBody.corePart));

                var sThing = source.thingDef;
                var sRace = sThing.race;

                string thingDefName = $"{generateBody.defName}";

                string bodyDefName = generateBody.defName;
                ThingDef newThing = thingDefName.TryGetExistingDef<ThingDef>();
                BodyDef newBody = bodyDefName.TryGetExistingDef<BodyDef>();

                var newRace = new RaceProperties();

                // Make the new Thing using reflection, in case it is a subclass. (Har most likely)
                newThing ??= sThing.GetType().GetConstructor([]).Invoke([]) as ThingDef;

                // Actually doesn't seem to work
                //newThing ??= new ThingDef();

                foreach (var field in sThing.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                //foreach (var field in typeof(ThingDef).GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                {
                    try
                    {
                        if (field.FieldType.IsClass && field.GetValue(sThing) != null && field.GetType().Name.Contains("ThingDef_AlienRace.AlienSettings"))
                        {
                            field.SetValue(newThing, field.GetType().GetConstructor([]).Invoke([]));
                            foreach (var alienField in field.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                            {
                                try
                                {
                                    alienField.SetValue(field.GetValue(newThing), alienField.GetValue(field.GetValue(sThing)));
                                }
                                catch (Exception e)
                                {
                                    Log.Error($"Failed to access field {field.Name}.");
                                    Log.Error(e.ToString());
                                }
                            }
                        }
                        else
                        {
                            field.SetValue(newThing, field.GetValue(sThing));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to copy field {field.Name} from thingDef.");
                        Log.Error(e.ToString());
                    }
                }

                var allThingDefSources = fused.mergableBodies.Select(x => x.thingDef).ToList();
                newThing.recipes = [.. allThingDefSources.Where(x => x.recipes != null).SelectMany(x => x?.recipes).Where(x => x != null).ToList().Distinct()];
                newThing.thingCategories = [.. allThingDefSources.Where(x => x.thingCategories != null).SelectMany(x => x?.thingCategories).Where(x => x != null).ToList().Distinct()];

                // So we don't append to the original lists...
                newThing.modExtensions = sThing.modExtensions != null ? [.. sThing.modExtensions] : [];
                newThing.comps = sThing.comps != null ? [.. sThing.comps] : null;
                newThing.thingCategories = sThing.thingCategories != null ? [.. sThing.thingCategories] : [];
                newThing.recipes = sThing.recipes != null ? [.. sThing.recipes] : null;
                newThing.tools = sThing.tools != null ? [.. sThing.tools] : null;
                newThing.inspectorTabs = sThing.inspectorTabs != null ? [.. sThing.inspectorTabs] : null;
                newThing.inspectorTabsResolved = sThing.inspectorTabsResolved != null ? [.. sThing.inspectorTabsResolved] : null;
                newThing.tradeTags = sThing.tradeTags != null ? [.. sThing.tradeTags] : null;
                newThing.verbs = sThing.verbs != null ? [.. sThing.verbs] : null;
                newThing.stuffCategories = sThing.stuffCategories != null ? [.. sThing.stuffCategories] : null;
                newThing.thingSetMakerTags = sThing.thingSetMakerTags != null ? [.. sThing.thingSetMakerTags] : null;
                newThing.butcherProducts = sThing.butcherProducts != null ? [.. sThing.butcherProducts] : null;
                newThing.smeltProducts = sThing.smeltProducts != null ? [.. sThing.smeltProducts] : null;
                newThing.virtualDefs = sThing.virtualDefs != null ? [.. sThing.virtualDefs] : null;


                // Deduplicate inspector tabs
                if (newThing.inspectorTabs != null)
                {
                    newThing.inspectorTabs = newThing.inspectorTabs.Distinct().ToList();
                }
                if (newThing.inspectorTabsResolved != null)
                {
                    newThing.inspectorTabsResolved = newThing.inspectorTabsResolved.Distinct().ToList();
                }

                foreach (var field in sRace.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                {
                    try
                    {
                        if (field.FieldType.IsGenericType && field.GetValue(sRace) != null)
                        {
                            var sourceList = field.GetValue(sRace);
                            var newList = (IList)Activator.CreateInstance(field.FieldType);
                            foreach (var item in (IEnumerable)sourceList)
                            {
                                newList.Add(item);
                            }
                            field.SetValue(newRace, newList);
                        }
                        else
                        {
                            field.SetValue(newRace, field.GetValue(sRace));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to copy field {field.Name} from race.");
                        Log.Error(e.ToString());
                    }

                }


                // Set the body of the base race rather than that of the Fuse Set.
                newRace.body = sRace.body;
                newRace.renderTree = sRace.renderTree;

                newThing.generated = true;
                newThing.defName = thingDefName;
                newThing.label = generateBody.LabelCap;
                newThing.race = newRace;
                newRace.body = generateBody;
                generateBody.generated = true;

                var raceExtensions = allThingDefSources.SelectMany(x => x.ExtensionsOnDef<RaceExtension, ThingDef>()).ToList();
                var newRaceExtension = new RaceExtension(raceExtensions)
                {
                    isFusionOf = fused.mergableBodies.Select(x => x.thingDef).ToList()
                };

                newThing.modExtensions ??= [];
                newThing.modExtensions.RemoveAll(x => x is RaceExtension);
                newThing.modExtensions.Add(newRaceExtension);

                if (fused.fuseSetBody is MergableBody fSBody)
                {
                    var fsThing = fSBody.thingDef;
                    var fsRace = fsThing.race;
                    newThing.statBases = [.. fsThing.statBases];
                    foreach (var statBase in fsThing.statBases)
                    {
                        newThing.SetStatBaseValue(statBase.stat, statBase.value);
                    }
                    newThing.butcherProducts = [.. fsThing.butcherProducts];
                    newThing.ingredient = fsThing.ingredient;
                    // Averaged
                    newThing.SetStatBaseValue(StatDefOf.PsychicSensitivity, (sThing.GetStatValueAbstract(StatDefOf.PsychicSensitivity) + fsThing.GetStatValueAbstract(StatDefOf.PsychicSensitivity)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.DeepDrillingSpeed, (sThing.GetStatValueAbstract(StatDefOf.DeepDrillingSpeed) + fsThing.GetStatValueAbstract(StatDefOf.DeepDrillingSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.MiningSpeed, (sThing.GetStatValueAbstract(StatDefOf.MiningSpeed) + fsThing.GetStatValueAbstract(StatDefOf.MiningSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.MiningYield, (sThing.GetStatValueAbstract(StatDefOf.MiningYield) + fsThing.GetStatValueAbstract(StatDefOf.MiningYield)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.ConstructionSpeed, (sThing.GetStatValueAbstract(StatDefOf.ConstructionSpeed) + fsThing.GetStatValueAbstract(StatDefOf.ConstructionSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.SmoothingSpeed, (sThing.GetStatValueAbstract(StatDefOf.SmoothingSpeed) + fsThing.GetStatValueAbstract(StatDefOf.SmoothingSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.PlantHarvestYield, (sThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield) + fsThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.PlantWorkSpeed, (sThing.GetStatValueAbstract(StatDefOf.PlantWorkSpeed) + fsThing.GetStatValueAbstract(StatDefOf.PlantWorkSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.PlantHarvestYield, (sThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield) + fsThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield)) / 2);

                    newThing.SetStatBaseValue(StatDefOf.MoveSpeed, (sThing.GetStatValueAbstract(StatDefOf.MoveSpeed) + fsThing.GetStatValueAbstract(StatDefOf.MoveSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.IncomingDamageFactor, (sThing.GetStatValueAbstract(StatDefOf.IncomingDamageFactor) + fsThing.GetStatValueAbstract(StatDefOf.IncomingDamageFactor)) / 2);

                    // Averaged Ceil
                    newThing.SetStatBaseValue(StatDefOf.PawnBeauty, Mathf.Ceil((sThing.GetStatValueAbstract(StatDefOf.PawnBeauty) + fsThing.GetStatValueAbstract(StatDefOf.PawnBeauty)) / 2));

                    // Max
                    newThing.SetStatBaseValue(StatDefOf.MarketValue, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.MarketValue), fsThing.GetStatValueAbstract(StatDefOf.MarketValue)));
                    newThing.SetStatBaseValue(StatDefOf.Nutrition, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.Nutrition), fsThing.GetStatValueAbstract(StatDefOf.Nutrition)));
                    newThing.SetStatBaseValue(StatDefOf.Mass, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.Mass), fsThing.GetStatValueAbstract(StatDefOf.Mass)));
                    newThing.SetStatBaseValue(StatDefOf.ToxicResistance, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.ToxicResistance), fsThing.GetStatValueAbstract(StatDefOf.ToxicResistance)));
                    newThing.SetStatBaseValue(StatDefOf.ToxicEnvironmentResistance, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.ToxicEnvironmentResistance), fsThing.GetStatValueAbstract(StatDefOf.ToxicEnvironmentResistance)));
                    newThing.SetStatBaseValue(StatDefOf.MeleeDodgeChance, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.MeleeDodgeChance), fsThing.GetStatValueAbstract(StatDefOf.MeleeDodgeChance)));
                }

                newRace.corpseDef = null;
                newRace.linkedCorpseKind = null;
                newRace.hasCorpse = false;
                // Lets not generate a bunch of unnatural corpses. Set via reflection because of reports that the 
                // field is sometimes not present.
                var hasUnnaturalCorpseField = newRace.GetType().GetField("hasUnnaturalCorpse");
                hasUnnaturalCorpseField?.SetValue(newRace, false);

                fused.SetThing(newThing);

                // Clear the caches from newRace.body.
                newRace.body.cachedPartsByTag.Clear();
                newRace.body.cachedPartsByDef.Clear();

                bodyDefsAdded.Add(newRace.body);
                thingDefsAdded.Add(newThing);

                DefGenerator.AddImpliedDef(newThing, hotReload: hotReload);
                DefGenerator.AddImpliedDef(newRace.body, hotReload: hotReload);
            }
        }

        public static void PostSaveLoadedSetup()
        {
            bool partsWereChanged = false;
            foreach (var td in thingDefsAdded)
            {
                if (td.race.body.cachedAllParts.NullOrEmpty())
                {
                    td.race.body.CacheDataRecursive(td.race.body.corePart);
                    partsWereChanged = true;
                }
            }
            if (partsWereChanged)
            {
                ModPatches.FacialAnim_PatchDynamicRaces.PatchFaceAdjustmentDict([.. thingDefsAdded]);
            }
        }

        public static void GenerateCorpses(bool hotReload)
        {
            foreach (var (newThingDef, fused) in FusedBody.FusedBodyByThing.Select(x=> (x.Key, x.Value)))
            {
                var sThing = fused.SourceBody.thingDef;
                GenerateCorpse(
                    fused: fused,
                    sThing: sThing,
                    newThing: newThingDef,
                    hotReload: hotReload
                    );
            }
        }

        private static void GenerateCorpse(FusedBody fused,ThingDef sThing, ThingDef newThing, bool hotReload)
        {
            RaceProperties newRace = newThing.race;
            BodyDef generatedBody = newThing.race.body;
            RaceProperties sRace = sThing.race;

            ThingDef sCorpse = sThing.race.corpseDef;
            sCorpse ??= sThing?.race?.linkedCorpseKind;

            string corpseDefName = $"{generatedBody.defName}_Corpse";
            ThingDef newCorpse = corpseDefName.TryGetExistingDef<ThingDef>();
            newCorpse ??= new();
            bool makeCorpse = sRace.hasCorpse;

            if (makeCorpse && sCorpse == null)
            {
                Log.Warning($"{sThing}.hasCorpse is True, but no ThingDef for corpse was found. Aborting corpse generation for fused race things.");
                return;
            }
            if (!makeCorpse) return;

            foreach (var field in sCorpse.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
            {
                try
                {
                    if (field.FieldType.IsGenericType && field.GetValue(sCorpse) != null)
                    {
                        var sourceList = field.GetValue(sCorpse);
                        var newList = (IList)Activator.CreateInstance(field.FieldType);
                        foreach (var item in (IEnumerable)sourceList)
                        {
                            newList.Add(item);
                        }
                        field.SetValue(newCorpse, newList);
                    }
                    else
                    {
                        field.SetValue(newCorpse, field.GetValue(sCorpse));
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to copy field {field.Name} from race.");
                    Log.Error(e.ToString());
                }

            }
            //Log.Clear();

            newCorpse.defName = corpseDefName;
            newCorpse.label = "CorpseLabel".Translate(generatedBody.label);

            newCorpse.description = "CorpseDesc".Translate(newThing.label);
           

            newCorpse.race = newRace;
            newCorpse.recipes = [.. sCorpse.recipes];
            newCorpse.thingCategories = [.. sCorpse.thingCategories];
            newCorpse.inspectorTabs = [.. sCorpse.inspectorTabs];
            newThing.race.corpseDef = newCorpse;
            newThing.race.hasCorpse = true;


            DirectXmlCrossRefLoader.RegisterListWantsCrossRef(newCorpse.thingCategories, !fused.isMechanical ? ThingCategoryDefOf.CorpsesHumanlike.defName : BSDefs.BS_RobotCorpses.defName, newCorpse);

            //thingDefsAdded.Add(newCorpse);
            DefGenerator.AddImpliedDef(newCorpse, hotReload: hotReload);
        }

        private static void GenerateShortHashes(bool hotReload, ThingDef newThing, RaceProperties newRace)
        {
            if (!hotReload)
            {
                newThing.shortHash = 0;
                newRace.body.shortHash = 0;
            }
        }
        private static string OutputFullClassAsString(object obj)
        {
            if (obj == null) return "null";

            var type = obj.GetType();
            var fields = type.GetFields();
            var sb = new StringBuilder();

            sb.AppendLine($"Class: {type?.Name} ({obj})");
            sb.AppendLine("{");

            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                if (value == null) continue;
                sb.AppendLine($"  {field?.Name}: {value}");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        internal static class ShortHashWrapper
        {
            private static Action<Def, Type, HashSet<ushort>> giveHashDelegate;
            private static FieldRef<Dictionary<Type, HashSet<ushort>>> takenHashesFieldRef;

            static ShortHashWrapper()
            {
                giveHashDelegate = MethodDelegate<Action<Def, Type, HashSet<ushort>>>(Method(typeof(ShortHashGiver), "GiveShortHash",
                [typeof(Def), typeof(Type), typeof(HashSet<ushort>)], null), null, true);
                takenHashesFieldRef = StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(Field(typeof(ShortHashGiver), "takenHashesPerDeftype"));
            }
            internal static void GiveShortHash<T>(T def) where T : Def
            {
                Dictionary<Type, HashSet<ushort>> dictionary = takenHashesFieldRef.Invoke();
                if (!dictionary.ContainsKey(typeof(T)))
                {
                    dictionary[typeof(T)] = [];
                }
                HashSet<ushort> arg = dictionary[typeof(T)];
                giveHashDelegate(def, null, arg);
            }
        }
    }
}
