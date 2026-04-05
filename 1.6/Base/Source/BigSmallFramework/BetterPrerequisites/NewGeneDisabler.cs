using BigAndSmall;
using BigAndSmall.FilteredLists;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Verse;


namespace BigAndSmall
{
    public partial class BSCache
    {
        private readonly List<Gene> genesActivated = [];
        private readonly List<Gene> genesDeactivated = [];

        private string GeneShouldBeActive(Gene gene, List<PawnExtension> genePawnExts, List<PawnExtension> hediffPawnExts, List<PawnExtension> allPawnExts)
        {
            if (genePawnExts.Count != 0 && !ConditionalManager.TestConditionals(gene, genePawnExts))
            {
                return "BS_ConditionForActivationNotMet".Translate().CapitalizeFirst();
            }

            if (hediffPawnExts.Count != 0)
            {
				for (int i = hediffPawnExts.Count - 1; i >= 0; i--)
				{
					PawnExtension ext = hediffPawnExts[i];
					if (ext.IsGeneLegal(gene.def, removalCheck: false).Denied())
						return "BS_DisabledByHediff".Translate().CapitalizeFirst();
				}
            }

            if (PrerequisiteValidator.Validate(gene.def, pawn) is string pFailReason && pFailReason != "")
            {
                return pFailReason;
            }

            if (allPawnExts.Count != 0)
            {
				for (int i = allPawnExts.Count - 1; i >= 0; i--)
				{
					PawnExtension ext = allPawnExts[i];
					if (ext.IsGeneLegal(gene.def, removalCheck: false).Denied())
						return "BS_DisabledByFilter".Translate().CapitalizeFirst();
				}
            }

            return "";
        }

        private bool UpdateGeneOverrideStates(List<PawnExtension> allPawnExts)
        {
            if (pawn.genes == null)
            {
                return false;
            }
            
            var allGenes = pawn.genes.GenesListForReading;
            if (allGenes.NullOrEmpty() || allPawnExts.Count == 0)
            {
                return false;
            }
            bool change = false;

            var orderedGenes = allGenes.Select(gene => (gene, extensions: gene.def.GetAllPawnExtensionsOnGene()))
                .OrderByDescending(gene => gene.extensions.Count > 0 
					? gene.extensions.Max(x => x.priority + (x.HasGeneFilters ? 0.5f : 0))
					: -1);

            var hediffPawnExts = pawn.GetHediffExtensions<PawnExtension>();
            foreach (var geneEntry in orderedGenes)
            {
				Gene gene = geneEntry.gene;
				if (!GeneCache.globalCache.TryGetValue(gene, out var geneCache))
                {
                    GeneCache.globalCache[gene] = geneCache = new GeneCache(gene);
                }

                bool overridenByDummy = gene.overriddenByGene == GeneCache.DummyGene;
                if ((gene.Overridden == false) || overridenByDummy)
                {
                    string failReason = GeneShouldBeActive(gene, geneEntry.extensions, hediffPawnExts, allPawnExts);
                    bool activeState = gene.Active;
                    if (failReason != "")
                    {
                        if (!geneCache.isOverriden) genesDeactivated.Add(gene);
                        geneCache.isOverriden = true;
                        if (activeState)
                        {
                            gene.OverrideBy(GeneCache.DummyGene);
                            change = true;
                        }
                    }
                    else
                    {
                        if (geneCache.isOverriden || overridenByDummy)
                        {
                            if (geneCache.isOverriden)
                            {
                                geneCache.isOverriden = false;
                                genesActivated.Add(gene);
                            }
                            if (activeState == false)
                            {
                                gene.OverrideBy(null);
                                change |= gene.Active;
                            }
                        }
                    }
                }
            }
            return change;
        }
    }

    public class DummyGeneClass : Gene
    {
        public override string Label => "BS_RequirementNotMet".Translate().CapitalizeFirst();
        public override void PostAdd() { }
        public override void PostRemove() { }
        public override void TickInterval(int delta) { }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null) { }
        public override void ExposeData() { }
        public override bool Active => true;
    }

    public class GeneCache
    {
        public static Dictionary<Gene, GeneCache> globalCache = [];
        public static void ClearCaches()
        {
            globalCache.Clear();
        }

        public bool initialized = false;
        public bool isOverriden = false;
        public string disabledMessage = "BS_RequirementNotMet".Translate().CapitalizeFirst();
        public Gene gene = null;

        private static Gene dummyGene = null;
        //private static Gene dummyGeneRace = null;
        public static Gene MakeDummyGene()
        {
            dummyGene ??= new Gene { def = BSDefs.BS_OverrideDummyGene };
            return dummyGene;
        }

        public static Gene DummyGene => dummyGene ??= MakeDummyGene();
        public GeneCache(Gene gene)
        {
            MakeDummyGene(); // Make sure it is set up.
            this.gene = gene;
        }

        public Gene OverridenBy()
        {
            if (isOverriden)
            {
                return dummyGene;
            }
            return null;
        }
    }

    [HarmonyPatch]
    public static class OverrideAllConflictingTranspiler
    {
        public static Gene IsOverridenBy(Gene gene)
        {
            if (GeneCache.globalCache.TryGetValue(gene, out var cache))
            {
                return cache.OverridenBy();
            }
            else
            {
                GeneCache.globalCache[gene] = new GeneCache(gene);  // Won't have a value yet, so don't bother returning.
            }
            return null;
        }
        [HarmonyPatch(typeof(Gene), nameof(Gene.OverrideBy))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);
            var label = il.DefineLabel();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ret)
                {
                    var newInstructions = new List<CodeInstruction>
                    {
                        // Abort if the gene is overriden
                        new(OpCodes.Ldarg_1),
                        new(OpCodes.Brtrue_S, label),

                        // Set to override (if any)
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Call, typeof(OverrideAllConflictingTranspiler).GetMethod(nameof(IsOverridenBy))),
                        new(OpCodes.Stfld, typeof(Gene).GetField(nameof(Gene.overriddenByGene))),

                        // Jump here if the gene is already overriden
                        new CodeInstruction(OpCodes.Nop).WithLabels(label),
                    };
                    codes.InsertRange(i, newInstructions);
                    break;
                }
            }

            return codes.AsEnumerable();
        }
    }
}
