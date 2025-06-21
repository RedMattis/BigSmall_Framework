using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
                //foreach (var field in typeof(ThingDef).GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                CopyThingDefFields(sThing, newThing);

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

                CopyRaceProperties(sRace, newRace);


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
                    newThing.butcherProducts = [.. fsThing.butcherProducts];
                    newThing.ingredient = fsThing.ingredient;
                    MergeStatDefValues(fsThing, sThing, newThing);
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

                DefGenerator.AddImpliedDef(newThing, hotReload: true);
                DefGenerator.AddImpliedDef(newRace.body, hotReload: true);

                newThing.ResolveReferences();
            }
        }

        public static void MergeStatDefValues(ThingDef priorityThing, ThingDef secondaryThing, ThingDef newThing)
        {
            newThing.statBases = [.. priorityThing.statBases];
            foreach (var statBase in priorityThing.statBases)
            {
                newThing.SetStatBaseValue(statBase.stat, statBase.value);
            }

            // Averaged
            newThing.SetStatBaseValue(StatDefOf.PsychicSensitivity, (secondaryThing.GetStatValueAbstract(StatDefOf.PsychicSensitivity) + priorityThing.GetStatValueAbstract(StatDefOf.PsychicSensitivity)) / 2);
            newThing.SetStatBaseValue(StatDefOf.DeepDrillingSpeed, (secondaryThing.GetStatValueAbstract(StatDefOf.DeepDrillingSpeed) + priorityThing.GetStatValueAbstract(StatDefOf.DeepDrillingSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.MiningSpeed, (secondaryThing.GetStatValueAbstract(StatDefOf.MiningSpeed) + priorityThing.GetStatValueAbstract(StatDefOf.MiningSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.MiningYield, (secondaryThing.GetStatValueAbstract(StatDefOf.MiningYield) + priorityThing.GetStatValueAbstract(StatDefOf.MiningYield)) / 2);
            newThing.SetStatBaseValue(StatDefOf.ConstructionSpeed, (secondaryThing.GetStatValueAbstract(StatDefOf.ConstructionSpeed) + priorityThing.GetStatValueAbstract(StatDefOf.ConstructionSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.SmoothingSpeed, (secondaryThing.GetStatValueAbstract(StatDefOf.SmoothingSpeed) + priorityThing.GetStatValueAbstract(StatDefOf.SmoothingSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.PlantHarvestYield, (secondaryThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield) + priorityThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield)) / 2);
            newThing.SetStatBaseValue(StatDefOf.PlantWorkSpeed, (secondaryThing.GetStatValueAbstract(StatDefOf.PlantWorkSpeed) + priorityThing.GetStatValueAbstract(StatDefOf.PlantWorkSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.PlantHarvestYield, (secondaryThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield) + priorityThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield)) / 2);

            newThing.SetStatBaseValue(StatDefOf.MoveSpeed, (secondaryThing.GetStatValueAbstract(StatDefOf.MoveSpeed) + priorityThing.GetStatValueAbstract(StatDefOf.MoveSpeed)) / 2);
            newThing.SetStatBaseValue(StatDefOf.IncomingDamageFactor, (secondaryThing.GetStatValueAbstract(StatDefOf.IncomingDamageFactor) + priorityThing.GetStatValueAbstract(StatDefOf.IncomingDamageFactor)) / 2);

            // Averaged Ceil
            newThing.SetStatBaseValue(StatDefOf.PawnBeauty, Mathf.Ceil((secondaryThing.GetStatValueAbstract(StatDefOf.PawnBeauty) + priorityThing.GetStatValueAbstract(StatDefOf.PawnBeauty)) / 2));

            // Max
            newThing.SetStatBaseValue(StatDefOf.MarketValue, Mathf.Max(secondaryThing.GetStatValueAbstract(StatDefOf.MarketValue), priorityThing.GetStatValueAbstract(StatDefOf.MarketValue)));
            newThing.SetStatBaseValue(StatDefOf.Nutrition, Mathf.Max(secondaryThing.GetStatValueAbstract(StatDefOf.Nutrition), priorityThing.GetStatValueAbstract(StatDefOf.Nutrition)));
            newThing.SetStatBaseValue(StatDefOf.Mass, Mathf.Max(secondaryThing.GetStatValueAbstract(StatDefOf.Mass), priorityThing.GetStatValueAbstract(StatDefOf.Mass)));
            newThing.SetStatBaseValue(StatDefOf.ToxicResistance, Mathf.Max(secondaryThing.GetStatValueAbstract(StatDefOf.ToxicResistance), priorityThing.GetStatValueAbstract(StatDefOf.ToxicResistance)));
            newThing.SetStatBaseValue(StatDefOf.ToxicEnvironmentResistance, Mathf.Max(secondaryThing.GetStatValueAbstract(StatDefOf.ToxicEnvironmentResistance), priorityThing.GetStatValueAbstract(StatDefOf.ToxicEnvironmentResistance)));
            newThing.SetStatBaseValue(StatDefOf.MeleeDodgeChance, Mathf.Max(secondaryThing.GetStatValueAbstract(StatDefOf.MeleeDodgeChance), priorityThing.GetStatValueAbstract(StatDefOf.MeleeDodgeChance)));
        }

        public static void CopyRaceProperties(RaceProperties sRace, RaceProperties newRace)
        {
            foreach (var field in GetDeclaredFields(sRace.GetType()).Where(x => !x.IsStatic))
            {
                try
                {
                    var value = field.GetValue(sRace);
                    if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        field.SetValue(newRace, value);
                    }
                    else
                    {
                        field.SetValue(newRace, value);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to copy field {field.Name} from race.");
                    Log.Error(e.ToString());
                }
            }
        }

        public static void CopyThingDefFields(ThingDef sThing, ThingDef newThing)
        {
            foreach (var field in sThing.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
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
                        var value = field.GetValue(sThing);
                        if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            field.SetValue(newThing, value);
                        }
                        else
                        {
                            field.SetValue(newThing, value);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to copy field {field.Name} from thingDef.");
                    Log.Error(e.ToString());
                }
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
                    sThing: sThing,
                    newThing: newThingDef,
                    isMechanical: fused.isMechanical,
                    hotReload: hotReload
                    );
            }
        }

        private static void GenerateCorpse(ThingDef sThing, ThingDef newThing, bool isMechanical, bool hotReload)
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


            DirectXmlCrossRefLoader.RegisterListWantsCrossRef(newCorpse.thingCategories, !isMechanical ? ThingCategoryDefOf.CorpsesHumanlike.defName : BSDefs.BS_RobotCorpses.defName, newCorpse);

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
                // Updated to use the new overload of MethodDelegate with Type[] parameter
                giveHashDelegate = MethodDelegate<Action<Def, Type, HashSet<ushort>>>(
                    Method(typeof(ShortHashGiver), "GiveShortHash", new[] { typeof(Def), typeof(Type), typeof(HashSet<ushort>) }, null),
                    null,
                    true
                );

                takenHashesFieldRef = StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(
                    Field(typeof(ShortHashGiver), "takenHashesPerDeftype")
                );
            }

            internal static void GiveShortHash<T>(T def) where T : Def
            {
                Dictionary<Type, HashSet<ushort>> dictionary = takenHashesFieldRef.Invoke();
                if (!dictionary.ContainsKey(typeof(T)))
                {
                    dictionary[typeof(T)] = new HashSet<ushort>();
                }
                HashSet<ushort> arg = dictionary[typeof(T)];
                giveHashDelegate(def, null, arg);
            }
        }
    }
}
