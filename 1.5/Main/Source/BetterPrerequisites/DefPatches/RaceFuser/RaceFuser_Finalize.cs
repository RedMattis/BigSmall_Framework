using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace BigAndSmall
{
    public static partial class RaceFuser
    {
        private static void GenerateAndRegisterRaceDefs(bool hotReload)
        {
            // Generate ThingDefs for all BodyDets as long as they don't have "discardNonRobotFusions"
            foreach (var fusedBody in FusedBody.FusedBodies.Values)
            //.Select(x => (x.generatedBody, x.SourceBody, x)))
            {
                var body = fusedBody.generatedBody;
                var source = fusedBody.SourceBody;
                var fSetBody = fusedBody.fuseSetBody;
                body.generated = true;
                body.ResolveReferences();
                if (doDebug) Log.Message(GetPartsStringRecursive(body.corePart));

                var sThing = source.thingDef;
                var sRace = sThing.race;
                var fSetRace = fSetBody != null ? fSetBody.thingDef.race : sRace;

                // Make the new Thing using reflection, in case it is a subclass. (Har Har Har... HAR!)
                var newThing = sThing.GetType().GetConstructor([]).Invoke([]) as ThingDef;
                var newRace = new RaceProperties();

                var allThingDefSources = fusedBody.mergableBodies.Select(x => x.thingDef).ToList();
                var raceExtensions = allThingDefSources.SelectMany(x => x.ExtensionsOnDef<RaceExtension, ThingDef>()).ToList();

                var newRaceExtension = new RaceExtension(raceExtensions)
                {
                    isFusionOf = fusedBody.mergableBodies.Select(x => x.thingDef).ToList()
                };

                // Use reflection to copy all fields from the source ThingDef to the new ThingDef.
                foreach (var field in sThing.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                {
                    try
                    {
                        field.SetValue(newThing, field.GetValue(sThing));
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to copy field {field.Name} from {sThing.defName} to {newThing.defName}.");
                        Log.Error(e.ToString());
                    }
                }
                // So we don't append to the original lists...
                newThing.modExtensions = sThing.modExtensions != null ? [.. sThing.modExtensions] : [];
                newThing.comps = sThing.comps != null ? [.. sThing.comps] : null;
                newThing.thingCategories = sThing.thingCategories != null ? [.. sThing.thingCategories] : [];
                newThing.recipes = sThing.recipes != null ? [.. sThing.recipes] : null;
                newThing.tools = sThing.tools != null ? [.. sThing.tools] : null;
                newThing.inspectorTabs = sThing.inspectorTabs != null ? [.. sThing.inspectorTabs] : null;
                newThing.inspectorTabsResolved = sThing.inspectorTabsResolved != null ? [.. sThing.inspectorTabsResolved] : null;
                //newThing.tradeTags = sThing.tradeTags != null ? [.. sThing.tradeTags] : null;
                newThing.verbs = sThing.verbs != null ? [.. sThing.verbs] : null;
                //newThing.stuffCategories = sThing.stuffCategories != null ? [.. sThing.stuffCategories] : null;
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

                // Same for Race
                foreach (var field in fSetRace.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                {
                    try
                    {
                        field.SetValue(newRace, field.GetValue(sRace));
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
                newRace.corpseDef = sRace.corpseDef;
                newRace.linkedCorpseKind = sRace.linkedCorpseKind;

                bool makeCorpse = newRace.hasCorpse && newRace.linkedCorpseKind == null;
                if (makeCorpse && sThing?.race?.corpseDef != null)
                {
                    // And Corpse

                    ThingDef newCorpse = new();
                    //if (fields == null)
                    //{
                    //    Log.Error($"Failed to get fields from corpseDef of \"{sThing?.defName}\".\n");
                    //    Log.Error($"{sThing?.race?.corpseDef}");
                    //    if (sThing?.race?.corpseDef != null)
                    //    {
                    //        Log.Error($"{sThing?.race?.corpseDef?.GetType()}");
                    //        Log.Error($"{sThing?.race?.corpseDef?.GetType()?.GetFields()}");
                    //        Log.Error($"{sThing}.{sThing?.race}.{sThing?.race?.corpseDef}.{sThing?.race?.corpseDef?.GetType()}, {sThing?.race?.corpseDef?.GetType()?.GetFields()}");
                    //    }
                    //    fields = [];
                    //}

                    foreach (var field in sThing.race.corpseDef.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                    {
                        try
                        {
                            field.SetValue(newCorpse, field.GetValue(newRace.corpseDef));
                        }
                        catch (Exception e)
                        {
                            Log.Error($"Failed to copy field {field.Name} from corpseDef.");
                            Log.Error(e.ToString());
                        }
                    }
                    newRace.corpseDef = newCorpse;
                    newCorpse.defName = $"{body.defName}_Corpse";
                    newCorpse.label = "BS_CorpseOf".Translate(body.LabelCap);
                    newCorpse.race = newRace;

                    newCorpse.modExtensions ??= [];
                    newCorpse.modExtensions.RemoveAll(x => x is RaceExtension);
                    newCorpse.modExtensions.Add(newRaceExtension);

                    Log.Message($"[Big and Small]: Generated CorpseDef for {newThing?.defName} with the name {newCorpse.label} ({newCorpse.defName}).");
                }




                // Lets not generate a bunch of unnatural corpses. Set via reflection because of reports that the 
                // field is sometimes not present.
                var hasUnnaturalCorpseField = newRace.GetType().GetField("hasUnnaturalCorpse");
                hasUnnaturalCorpseField?.SetValue(newRace, false);

                newThing.generated = true;
                newThing.defName = $"{body.defName}";
                newThing.label = body.LabelCap;
                newThing.race = newRace;
                newRace.body = body;

                newThing.modExtensions ??= [];
                newThing.modExtensions.RemoveAll(x => x is RaceExtension);
                newThing.modExtensions.Add(newRaceExtension);

                if (fusedBody.isMechanical)
                {

                }
                if (fusedBody.fuseSetBody is MergableBody fSBody)
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

                // Copy over the recipes, styles, and categories, ...
                newThing.recipes = [.. allThingDefSources.Where(x => x.recipes != null).SelectMany(x => x?.recipes).Where(x => x != null).ToList().Distinct()];
                newThing.thingCategories = [.. allThingDefSources.Where(x => x.thingCategories != null).SelectMany(x => x?.thingCategories).Where(x => x != null).ToList().Distinct()];

                // Hmm... doesn't seem worth the mess.
                //newThing.tools = [.. allThingDefSources.Where(x => x.tools != null).SelectMany(x => x?.tools).Where(x => x != null).ToList().Distinct()];

                fusedBody.SetThing(newThing);

                // Add the things to the game.
                DefGenerator.AddImpliedDef(newThing, hotReload: hotReload);
                DefGenerator.AddImpliedDef(newRace.body, hotReload: hotReload);

                bodyDefsAdded.Add(newRace.body);
                thingDefsAdded.Add(newThing);
                //DefDatabase<ThingDef>.Add(newThing);
                //DefDatabase<BodyDef>.Add(body);

                // Make sure we generate new Hashes for the new ThingDefs.
                GenerateShortHashes(hotReload, newThing, newRace);
                //if (makeCorpse)
                //{
                //    ShortHashWrapper.GiveShortHash(newThing.race.corpseDef);
                //}
            }
        }
        private static void GenerateShortHashes(bool hotReload, ThingDef newThing, RaceProperties newRace)
        {
            if (!hotReload)
            {
                newThing.shortHash = 0;
                ShortHashWrapper.GiveShortHash(newRace.body);
                ShortHashWrapper.GiveShortHash(newThing);
            }
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
