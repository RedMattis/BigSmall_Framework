using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RedHealth
{
    public class HealthAspectTracker : IExposable
    {
        public HealthAspect def;
        public SimpleCurve curve;
        public float scaleMultiplier = 1;

        public HealthAspectTracker()
        {
        }

        public HealthAspectTracker(HealthAspect def, bool mainHealthCurve)
        {
            this.def = def;

            var curvesShapes = HealthCurve.GetAllCruves();

            var curve1 = curvesShapes.Where(x => !def.lucky || (x.score >= 0)).RandomElementByWeight(x => x.weight);
            if (mainHealthCurve)
            {
                // The main health curve always uses the exponential curve for one of the blended curves so that most pawns only get health issues towards the end
                // of their lifespan. ...unless they get very unlocky with the interpolation and curve2.
                curve1 = HDefs.RED_ExponentialCurve;
            }
            else
            {
                scaleMultiplier = Rand.Range(0.9f, 1.1f);
            }
            var curve2 = curvesShapes.Where(x => x != curve1 && (!def.lucky || (x.score >= 0))).RandomElementByWeight(x => x.weight);
            float interpolation = Rand.Value;

            // Create a new curve that is a blend of the two curves. The purpose of all this is to create a curve that is unique to this pawn.
            curve = [];
            for (int i = 0; i < curve1.curve.PointsCount; i++)
            {
                curve.Add(new CurvePoint(curve1.curve[i].x * interpolation + curve2.curve[i].x * (1 - interpolation),
                                       curve1.curve[i].y * interpolation + curve2.curve[i].y * (1 - interpolation)));
            }
            if (mainHealthCurve)
            {
                // The deathknell of the pawn. Make sure the finale angle of the curve is propper steep.
                curve.Add(new CurvePoint(1.2f, 1.5f));
            }

            if (Main.loggingV) Log.Message($"Created HealthAspectTracker for {def.defName} with the curves {curve1.defName} ({interpolation * 100:f0}%), {curve2.defName} ({(1 - interpolation) * 100:f0}%).");
        }

        public bool IsApplicable(Pawn pawn, out float vulnerablePartsPercent)
        {
            vulnerablePartsPercent = 1;

            if (Main.settings.disableForAll) return false;
            if (Main.settings.aspectsDisabled.Any(x => x.defName == def.defName && x.active is false)) return false;

            if (def.nullifyingRaces.Count > 0 && def.nullifyingRaces.Contains(pawn.def))
            {
                if (Main.loggingV) Log.Message($"Nullifying race found for {def.defName} on {pawn.Name} {pawn.def}");
                return false;
            }
            if (def.validFleshTypes.Count > 0 && !def.validFleshTypes.Contains(pawn.RaceProps.FleshType))
            {
                if (Main.loggingV) Log.Message($"FleshType not valid for {def.defName} on {pawn.Name}");
                return false;
            }
            if (def.nullifyingHediffs.Count > 0 && def.nullifyingHediffs.Any(x => pawn.health.hediffSet.hediffs.Any(y => y.def == x)))
            {
                if (Main.loggingV) Log.Message($"Nullifying hediff found for {def.defName} on {pawn.Name}");
                return false;
            }
            if (pawn?.genes != null && def.nullifyingGenes.Count > 0 && pawn.genes.GenesListForReading.Where(x => x.Active && def.nullifyingGenes.Contains(x.def)).Any())
            {
                if (Main.loggingV) Log.Message($"Nullifying gene found for {def.defName} on {pawn.Name}");
                return false;
            }
            if (pawn?.story?.traits != null && def.nullifyingTraits.Count > 0 && def.nullifyingTraits.Any(x => pawn.story.traits.HasTrait(x)))
            {
                if (Main.loggingV) Log.Message($"Nullifying trait found for {def.defName} on {pawn.Name}");
                return false;
            }
            //def.associatedPartsTags.Any(x => pawn.health.hediffSet.GetNotMissingParts().Any(y => y.def.tags.Contains(x)));
            if (def.associatedPartsTags.Count > 0)
            {
                int normalParts = 0;
                int replacedParts = 0;
                foreach (var tag in def.associatedPartsTags)
                {
                    // pawn.def.race.body.AllParts.Where(x => x.def.tags.Contains(tag)))
                    foreach (var validParts in pawn.health.hediffSet.GetNotMissingParts().Where(x => x.def.tags.Contains(tag)))
                    {
                        if (def.nullifyIfPartsReplaced && pawn.health.hediffSet.hediffs
                            .Where(x => x.Part == validParts && x is Hediff_AddedPart).Any())
                        {
                            replacedParts++;
                        }
                        else if (def.nullifyIfPartsReplacedBetterThanNatural && pawn.health.hediffSet.hediffs
                            .Where(x => x.Part == validParts && x is Hediff_AddedPart ap && ap.def.addedPartProps.partEfficiency >= 1).Any())
                        {
                            replacedParts++;
                        }
                        else
                        {
                            normalParts++;
                        }
                    }
                }
                if (normalParts == 0)
                {
                    if (Main.loggingV) Log.Message($"No parts found for {def.defName} on {pawn.Name}");
                    return false;  // No parts/valid of the required type were found. Not applicable.
                }
                else if (replacedParts > 0)
                {
                    vulnerablePartsPercent = (float)normalParts / (normalParts + replacedParts);
                    if (Main.loggingV) Log.Message($"Replaced parts found for {def.defName} on {pawn.Name}. The pawn is {vulnerablePartsPercent * 100}% vulnerable.");
                }
            }
            return true;
        }

        /// <summary>
        /// High score... is a bad thing here.
        /// </summary>
        public float? GetScore(Pawn pawn, float mainCurveValue)
        {
            bool isApplicable = IsApplicable(pawn, out float vulnerablePartsPercent);
            if (!isApplicable)
            {
                //if (Main.logging) Log.Message($"HealthAspect {def.defName} is not applicable for {pawn.Name}");
                return null;
            }
            float baseValue = curve.Evaluate(mainCurveValue);
            if (def.offsetFromCapacity.Count > 0)
            {
                foreach (var wCapacity in def.offsetFromCapacity)
                {
                    // Get the value of the capacity for the pawn
                    float capacityLevel = PawnCapacityUtility.CalculateCapacityLevel(pawn.health.hediffSet, wCapacity.capacity);
                    float capacityOffset = (capacityLevel - 1) * wCapacity.weight;
                    capacityOffset = Math.Min(wCapacity.clamp.max, Math.Max(wCapacity.clamp.min, capacityOffset));
                    baseValue += capacityOffset;
                }
            }
            return baseValue * vulnerablePartsPercent * scaleMultiplier;
        }

        public string AsHealthDisplay(Pawn pawn, float mainCurveValue)
        {
            float? rating = GetScore(pawn, mainCurveValue);
            if (rating == null) return "100%";
            return $"{(rating.Value) * 100f:0}%";
            //return $"{(1 - rating.Value)*100f:0}%";
        }
        public string AsHealthDisplay(float? finalRating)
        {
            if (finalRating == null) return "0%";
            //return $"{(finalRating.Value) * 100f:0}%";
            return $"{(1 - finalRating.Value) * 100f:0}%";
        }

        public HealthThreshold GetThreshold(float? rating)
        {
            rating ??= 0;
            // Get the highest possible threshold that is still below the rating.
            return def.thresholds.Where(x => x.threshold <= rating).OrderByDescending(x => x.threshold).FirstOrDefault();
        }

        public HealthThreshold GetThresholdFromScore(Pawn pawn, float rating)
        {
            float score = GetScore(pawn, rating).Value;
            return GetThreshold(score);
        }

        public void SetSeverity(Pawn pawn, Hediff hediff, float? severityInput, bool set, BodyPartRecord part = null, float? severityPerDayMultiplier = null)
        {
            if (Main.loggingV) Log.Message($"Setting severity of {hediff.def.defName} ({hediff}) on {pawn.Name} (part:{part} to {severityInput} (set: {set})...");
            if (severityInput == null) return;
            float severity = severityInput.Value;
            float previousSeverity = hediff.Severity;
            // Check if it is an injury. If so scale it to the remaining health of the part.
            if (hediff is Hediff_Injury injury && part != null)
            {
                float currentHealth = pawn.health.hediffSet.GetPartHealth(part);  // Gets exact current part health in HP.
                float severityToAdd = currentHealth * severity;
                injury.Severity += severityToAdd;
                if (Main.loggingV) Log.Message($"Set severity of {injury.def.defName} on {pawn.Name} to {injury.Severity} ({previousSeverity} - {severity} * {currentHealth}).");
            }
            else if (set)  // Ignored for injuries.
            {
                hediff.Severity = severity > hediff.Severity ? severity : hediff.Severity;
            }
            else
            {
                float severityOfMax = Mathf.Min(hediff.def.maxSeverity, 1) - hediff.Severity;
                severity = Math.Max(Mathf.Min(severityOfMax * severity), 0.05f);
                hediff.Severity += severity;
            }
        }

        public int HealthEvent(Pawn pawn, float? baseRating, bool forceEvent = false)
        {
            bool PartInvalidTargetForHediff(HediffDef hediffDef, BodyPartRecord part, bool permitExisting)
            {
                if (HealthHelpers.PartIsBionic(pawn, part))
                    return false;
                if (!permitExisting && pawn.health.hediffSet.hediffs.Any(x => x.Part == part && x.def == hediffDef))
                    return false;
                return true;
            }

            int nextEventTime = Rand.Range(60000, HealthThreshold.defaultMaxMeanTime);
            float? adjustedRating = baseRating != null ? GetScore(pawn, baseRating.Value) : null;
            if (baseRating == null || adjustedRating == null)
            {
                if (forceEvent)
                {
                    if (Main.loggingV) Log.Message($"Forced event for {def.defName} on {pawn.Name}. No rating or adjusted rate ({baseRating}->{adjustedRating}). The Health Aspect is likely not applicable to the pawn due to an immunity.");
                }
                return nextEventTime;
            }
            var threshold = GetThreshold(adjustedRating.Value);
            if (threshold == null)
            {
                if (forceEvent)
                {
                    if (Main.loggingV) Log.Message($"Forced event for {def.defName} on {pawn.Name}. No threshold at {adjustedRating}.");
                }
                return nextEventTime;
            }
            else
            {
                nextEventTime = threshold.GetNextEventTime();
            }
            float rng = Rand.Value;
            if (rng < threshold.odds || forceEvent)
            {
                if (forceEvent)
                {
                    if (Main.logging) Log.Message($"Forcing event for {def.defName} on {pawn.Name}. Threshold was {threshold} ({baseRating}->{adjustedRating}), Real chance was {threshold.odds * 100:f2}%");
                }
                var possibleEffects = threshold.effects.ToList();
                var validBodyParts = pawn.health.hediffSet.GetNotMissingParts()?.Where(x => !HealthHelpers.PartIsBionic(pawn, x)).ToList();
                try
                {
                    for (int idx = possibleEffects.Count - 1; idx >= 0; idx--)
                    {
                        var eff = possibleEffects[idx];
                        if (eff.hediff == null || !eff.ShouldFilterIfExisting()) continue;
                        if (eff.AppliesToSpecificParts() && validBodyParts.Count > 0)
                        {
                            if (validBodyParts.All(part => PartInvalidTargetForHediff(eff.hediff, part, false)))
                            {
                                if (Main.loggingV) Log.Message($"Removing the effect {eff.hediff.defName} from the list of possible effects since it has already been applied to all valid parts.");
                                possibleEffects.Remove(eff);
                            }
                        }
                        else
                        {
                            if (pawn.health.hediffSet.hediffs.Any(x => x.def == eff.hediff))
                            {
                                if (Main.loggingV) Log.Message($"Removing the effect {eff.hediff.defName} from the list of possible effects since it has already been applied to the pawn.");
                                possibleEffects.Remove(eff);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Error while filtering effects for {def.defName} on {pawn.Name} at Threshold {threshold.label} ({baseRating:f2}->{adjustedRating:f2}). {e.Message}");
                }

                if (possibleEffects.Count == 0)
                {
                    if (Main.loggingV) Log.Message($"No applicable effects for {def.defName} on {pawn.Name} at Threshold {threshold.label} ({baseRating:f2}->{adjustedRating}:f2).");
                    return nextEventTime;
                }

                var effect = possibleEffects.RandomElementByWeight(x => x.weight);
                bool filterIfExisting = effect.ShouldFilterIfExisting();

                // Check if worldpawn.
                if (pawn.IsWorldPawn() && (effect.killWorldPawn || effect.killPawn) && pawn.Faction != Faction.OfPlayerSilentFail)
                {
                    pawn.Kill(null, null);
                    if (Main.loggingV) Log.Message($"Killed world pawn {pawn.Name} due to failing {def.label}");
                    return nextEventTime;
                }

                if (Main.loggingV) Log.Message($"Time to apply stuff. We've got Hediff {effect.hediff}.");

                if (Main.logging) Log.Message($"Event for {def.LabelCap} on {pawn.Name}. Applying: \"{effect.hediff.LabelCap}\".\n" +
                    $"Threshold was {threshold.label} ({baseRating:f2}->{adjustedRating:f2}), Chance was {threshold.odds * 100f:f2}%. Rolled {rng * 100:f2}%. Forced: {forceEvent}.\n" +
                    $"Extras: FilterIfExisting: {effect.ShouldFilterIfExisting()}. Probability Weight: {effect.weight}. Custom Severity Range {effect.severityRange != null}. Custom Severity Rate: {effect.severityPerDayRange != null}\n" +
                    $"Next Event of this type in {nextEventTime / 60000.0:f2} days.\n");

                List<Hediff> newHediffs = [];
                List<Hediff> hediffsModified = [];
                if (!pawn.IsWorldPawn() && effect.hediff != null)
                {
                    if (effect.canAffectAnyLivePart)
                    {
                        var validBodyPart = validBodyParts.FirstOrDefault();
                        if (validBodyPart != null)
                        {
                            // Check if the part has the hediff. If it already exists we're skipping. We're probably adding cancers,
                            // and with how many valid parts there are it shouldn't be much of an issue. 
                            var anyExistingHediff = pawn.health.hediffSet.hediffs.Any(x => x.def == effect.hediff && x.Part == validBodyPart);
                            if (anyExistingHediff)
                            {
                                // Add it.
                                Hediff newHediff = HediffMaker.MakeHediff(effect.hediff, pawn);
                                SetSeverity(pawn, newHediff, effect.severityRange?.RandomInRange, set: true, part: validBodyPart);
                                pawn.health.AddHediff(newHediff, validBodyPart);
                                newHediffs.Add(newHediff);
                            }
                        }
                    }
                    else if (effect.partsToAffect.Count() == 0)
                    {
                        var existingHediff = pawn.health.hediffSet.hediffs.FirstOrDefault(x => x.def == effect.hediff);
                        if (existingHediff != null && effect.stackWithExisting)
                        {
                            // Default to 20% severity if nothing is specified.
                            SetSeverity(pawn, existingHediff, effect.severityRange?.RandomInRange ?? 0.5f, set: false);
                            hediffsModified.Add(existingHediff);
                            if (Main.loggingV) Log.Message($"Stacked {effect.hediff.defName} on {pawn.Name}");
                        }
                        else
                        {
                            Hediff newHediff = HediffMaker.MakeHediff(effect.hediff, pawn);
                            SetSeverity(pawn, existingHediff, effect.severityRange?.RandomInRange, set: true);
                            pawn.health.AddHediff(newHediff);
                            newHediffs.Add(newHediff);
                            if (Main.loggingV) Log.Message($"Added {effect.hediff.defName} to {pawn.Name}");
                        }
                    }
                    else
                    {
                        if (Main.loggingV) Log.Message($"Applying {effect.hediff.defName} to {pawn.Name} on specific parts.");
                        if (validBodyParts == null || validBodyParts.Count == 0)
                        {
                            if (Main.loggingV) Log.Message($"No valid body parts found for {def.defName} on {pawn.Name}");
                            return nextEventTime;
                        }
                        foreach (var part in effect.partsToAffect.InRandomOrder())
                        {
                            bool partFound = false;
                            var bodyPartsToAffect = validBodyParts?.Where(x => x.def == part);
                            if (filterIfExisting)
                            {
                                bodyPartsToAffect = bodyPartsToAffect?.Where(x => PartInvalidTargetForHediff(effect.hediff, x, false)).ToList();
                            }
                            if (bodyPartsToAffect == null || bodyPartsToAffect.Count() == 0)
                            {
                                continue;
                            }
                            // Get a corresponding body part
                            foreach (var bodyPart in bodyPartsToAffect)
                            {
                                var existingHediff = pawn.health.hediffSet.hediffs.Where(x => x.def == effect.hediff && x.Part == bodyPart).FirstOrDefault();

                                //Hediff existingHediff = null;
                                if (existingHediff != null && effect.stackWithExisting)
                                {
                                    if (Main.loggingV) Log.Message($"Stacking {effect.hediff.defName} on {pawn.Name} on {bodyPart.def.defName}");
                                    hediffsModified.Add(existingHediff);
                                    SetSeverity(pawn, existingHediff, effect.severityRange?.RandomInRange ?? 0.5f, set: false, part: bodyPart);
                                }
                                else
                                {
                                    Hediff newHediff = HediffMaker.MakeHediff(effect.hediff, pawn, partRecord: bodyPart);
                                    SetSeverity(pawn, newHediff, effect.severityRange?.RandomInRange ?? 0.2f, set: true, part: bodyPart);
                                    pawn.health.AddHediff(newHediff, bodyPart);
                                    newHediffs.Add(newHediff);
                                    if (Main.loggingV) Log.Message($"Added {effect.hediff.defName} to {pawn.Name} on {bodyPart.def.defName}");
                                }
                                partFound = true;
                                break;
                            }
                            if (partFound)
                            {
                                break;
                            }
                        }
                    }
                    //foreach (var hediffAdded in newHediffs)
                    //{
                    //    if (hediffAdded is Hediff_Injury injury && injury.TryGetComp<HediffComp_GetsPermanent>() is HediffComp_GetsPermanent comp)
                    //    {
                    //        comp.IsPermanent = true;
                    //        if (Main.logging) Log.Message($"Set {hediffAdded.def.defName} to permanent");
                    //    }
                    //}
                    foreach (var hediffsModifiedOrAdded in hediffsModified.Concat(newHediffs))
                    {
                        if (hediffsModifiedOrAdded is Hediff_Injury injury && injury.TryGetComp<HediffComp_GetsPermanent>() is HediffComp_GetsPermanent permanentComp)
                        {
                            permanentComp.IsPermanent = true;
                            if (Main.loggingV) Log.Message($"Set {hediffsModifiedOrAdded.def.defName} to permanent");
                        }
                        if (effect.severityPerDayRange != null && hediffsModifiedOrAdded.TryGetComp<HediffComp_SeverityPerDay>() is HediffComp_SeverityPerDay severityComp)
                        {
                            var svpr = effect.severityPerDayRange.Value;
                            severityComp.severityPerDay = Rand.Range(svpr.min, svpr.max);
                        }
                    }
                    if (newHediffs.Count > 0 && pawn.Faction == Faction.OfPlayerSilentFail)
                    {
                        // Notify about any NEW hediffs added.
                        TaggedString label = "LetterLabelNewDisease".Translate() + ": " + newHediffs.First().def.LabelCap;
                        TaggedString text = "BirthdayBiologicalAgeInjuries".Translate(pawn);
                        text += ":\n\n" + newHediffs.Select(h => h.def.LabelCap.Resolve()).ToLineList("- ");
                        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, pawn);
                    }

                }


                if (effect.killPawn)
                {
                    if (Main.loggingV) Log.Message($"Killing {pawn.Name}");
                    pawn.Kill(null, newHediffs.FirstOrDefault());
                    return nextEventTime;
                }
                if (effect.killWorldPawn && pawn.IsWorldPawn() && pawn.Faction != Faction.OfPlayerSilentFail)
                {
                    if (Main.loggingV) Log.Message($"Killing {pawn.Name}");
                    pawn.Kill(null, newHediffs.FirstOrDefault());
                    return nextEventTime;
                }

                return nextEventTime;
            }
            return nextEventTime;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Deep.Look(ref curve, "curve");
            Scribe_Values.Look(ref scaleMultiplier, "scaleMultiplier");
            //Scribe_Values.Look(ref randomCurveSize, "randomCurveSize");
        }
    }

}
