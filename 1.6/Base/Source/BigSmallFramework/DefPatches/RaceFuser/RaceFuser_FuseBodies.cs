using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static BigAndSmall.BodyDefFusionsHelper;


namespace BigAndSmall
{
    public static partial class RaceFuser
    {
        private static Dictionary<BodyPartDef, List<BodyPartDef>> _mechanicalVersionOf = null;

        private static Dictionary<BodyPartDef, List<BodyPartDef>> GetMechanicalVersionsOf()
        {
            if (_mechanicalVersionOf != null) return _mechanicalVersionOf;
            _mechanicalVersionOf = [];

            List<(BodyPartDef, BodyPartExtension)> parts = [];
            foreach (var def in DefDatabase<BodyPartDef>.AllDefsListForReading)
            {
                var extensions = def.ExtensionsOnDef<BodyPartExtension, BodyPartDef>();
                if (extensions != null)
                {
                    foreach (var extension in extensions)
                    {
                        if (extension.mechanicalVersionOf != null && extension.mechanicalVersionOf.Any())
                        {
                            parts.Add((def, extension));
                            break;
                        }
                    }
                }
            }

            foreach (var part in parts)
            {
                foreach (var mechanicalPart in part.Item2.mechanicalVersionOf)
                {
                    if (!_mechanicalVersionOf.ContainsKey(mechanicalPart))
                    {
                        _mechanicalVersionOf[mechanicalPart] = [];
                    }
                    _mechanicalVersionOf[mechanicalPart].Add(part.Item1);
                }
            }
            return _mechanicalVersionOf;
        }


        private static BodyPartRecord ClonePartsRecursive(BodyPartRecord genPartParent, BodyPartRecord source, BodyDef genBody, MergableBody bodyOne, List<BodyPartRecord> unTransfereredParts, bool makeMechanical)
        {
            if (bodyOne.ShouldRemovePart(source.def))
            {
                //Log.Message($"Skipping {source.def.defName} as it should be removed.");
                return null;
            }

            var partDef = source.def;
            string customLabel = source.customLabel;
            if (makeMechanical && GetMechanicalVersionsOf() is Dictionary<BodyPartDef, List<BodyPartDef>> mechVersionList &&
                mechVersionList.TryGetValue(source.def, out var mechVersions) && !mechVersions.NullOrEmpty())
            {
                partDef = mechVersions.First();
                customLabel = customLabel ?? (source.def.IsMirroredPart ? (source.flipGraphic ? "BS_Left".Translate() : "BS_Right".Translate() + " " + partDef.label) : partDef.label);
            }

            var nGenPart = new BodyPartRecord
            {
                body = genBody,
                parent = genPartParent,
                def = partDef,
                customLabel = customLabel,
                untranslatedCustomLabel = source.untranslatedCustomLabel,
                coverage = source.coverage,
                depth = source.depth,
                height = source.height,
                woundAnchorTag = source.woundAnchorTag,
                flipGraphic = source.flipGraphic,
                groups = source.groups == null ? null : [.. source.groups],
                visibleHediffRots = source.visibleHediffRots == null ? null : [.. source.visibleHediffRots],
            };
            //if (doDebug) Log.Message($"{genPartParent?.LabelCap}->{nGenPart?.LabelCap}, " +
            //            $"({nGenPart.coverage * 100:f0}%)");

            if (unTransfereredParts.Contains(source)) unTransfereredParts.Remove(source);
            if (genPartParent == null)
            {
                genBody.corePart = nGenPart;
            }
            else
            {
                genPartParent.parts.Add(nGenPart);
            }
            foreach (var child in source.parts)
            {
                ClonePartsRecursive(nGenPart, child, genBody, bodyOne, unTransfereredParts, makeMechanical);
            }
            return nGenPart;
        }

        private static float? Similarity(BodyPartRecord partOne, BodyPartRecord partTwo)
        {
            float similarity = 0;
            if (partOne.def == partTwo.def)
            {
                similarity += 1000000;
            }
            else if (Equavalence(partOne.def, partTwo.def) is float simMult)
            {
                similarity += 1000000 * simMult;
            }
            else return null; // Parts are not similar.
            //if (ImportsRecipesFromOrSame(partOne.def, partTwo.def))
            //{
            //    similarity += 20000;
            //}
            if (partOne.groups == partTwo.groups)
            {
                similarity += 10000;
            }
            if (partOne.flipGraphic == partTwo.flipGraphic)
            {
                similarity += 1000;
            }
            if (partOne.height == partTwo.height)
            {
                similarity += 100;
            }
            if (partOne.customLabel == partTwo.customLabel)
            {
                similarity += 10;
            }
            if (partOne.coverage == partTwo.coverage)
            {
                similarity += 1;
            }
            if (partOne.customLabel?.Split(' ').Intersect(partTwo.customLabel?.Split(' ')).Any() == true)
            {
                similarity += 0.5f;
            }
            if (partOne.depth == partTwo.depth)
            {
                similarity += 0.1f;
            }
            if (partOne.woundAnchorTag == partTwo.woundAnchorTag)
            {
                similarity += 0.05f;
            }
            if (partOne.visibleHediffRots == partTwo.visibleHediffRots)
            {
                similarity += 0.01f;
            }
            return similarity;
        }
        private static void MergeRecursively(BodyPartRecord genPart, BodyPartRecord partTwo, List<BodyPartRecord> unTransfereredParts, MergableBody mergeOne)
        {
            //bool doDebug = mergeOne?.bodyDef?.LabelCap == "Snake-person";
            var partTwoParts = partTwo.parts.Where(x => !(mergeOne.ShouldRemovePart(x.def))).ToList();
            genPart.parts = genPart.parts.Where(x => !(mergeOne.ShouldRemovePart(x.def))).ToList();

            foreach (var child in genPart.parts)
            {
                var similarPart = partTwoParts
                    .Where(x => Similarity(x, child) != null)?
                    .Select(x => (x, Similarity(x, child)))?
                    .OrderByDescending(x => x.Item2).FirstOrDefault();
                if (similarPart.HasValue && similarPart.Value.x != null)
                {
                    var similarBodyRec = similarPart.Value.x;
                    MergeRecursively(child, similarBodyRec, unTransfereredParts, mergeOne);
                    unTransfereredParts.Remove(similarBodyRec);
                    partTwoParts.Remove(similarBodyRec);
                }
            }

            // Total Coverage of the body parts.
            float genTotalCoverage = genPart.parts.Any() ? genPart.parts.Sum(x => x.coverage) : 0;
            float partTwoCoverage = partTwoParts.Any() ? partTwoParts.Sum(x => x.coverage) : 0;
            float coverageMultiplier = 1;
            if (genTotalCoverage != 0)
            {
                coverageMultiplier = genTotalCoverage / (genTotalCoverage + partTwoCoverage);
                foreach (var part in genPart.parts)
                {
                    part.coverage *= coverageMultiplier;
                }
            }

            foreach (var part in partTwoParts)
            {
                if (unTransfereredParts.Contains(part))
                {
                    unTransfereredParts.Remove(part);

                    if (PartsToSkip.Contains(part.def))
                    {
                        continue;
                    }
                    var newPart = ClonePartsRecursive(genPart, part, genPart.body, mergeOne, unTransfereredParts, makeMechanical: false);
                    newPart.coverage *= coverageMultiplier;
                    newPart.parent = genPart;
                }
                //else
                //{
                //    Log.Message($"Part {part.LabelCap} was already transferred. Skipped.");
                //}
            }
        }
    }
}
