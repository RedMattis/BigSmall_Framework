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

        private string GeneShouldBeActive(Gene gene, List<PawnExtension> genePawnExts, List<PawnExtension> hediffPawnExts, List<PawnExtension> allPawnExts, List<Gene> activeGenes)
        {
            if (genePawnExts.Count != 0 && !ConditionalManager.TestConditionals(gene, genePawnExts))
            {
                return "BS_ConditionForActivationNotMet".Translate().CapitalizeFirst();
            }
            if (hediffPawnExts.Count != 0 && hediffPawnExts.Select(ext => ext.IsGeneLegal(gene.def, removalCheck: false)).FuseNoNullCheck().Denied())
            {
                return "BS_DisabledByHediff".Translate().CapitalizeFirst();
            }
            if (PrerequisiteValidator.Validate(gene.def, pawn) is string pFailReason && pFailReason != "")
            {
                return pFailReason;
            }
            if (allPawnExts.Count != 0 && allPawnExts.Select(ext=>ext.IsGeneLegal(gene.def, removalCheck:false)).FuseNoNullCheck().Denied())
            {
                return "BS_DisabledByFilter".Translate().CapitalizeFirst();
            }
            return "";
        }

        private void UpdateGeneOverrideStates(List<PawnExtension> allPawnExts)
        {
            if (pawn.genes == null)
            {
                return;
            }
            
            var allGenes = pawn.genes.GenesListForReading;
            if (allGenes.NullOrEmpty() || allPawnExts.Count == 0)
            {
                return;
            }

            IOrderedEnumerable<Gene> orderedGenes = allGenes
                .OrderByDescending(gene => (gene.def.GetAllPawnExtensionsOnGene() is List<PawnExtension> genePawnExts && genePawnExts.Any())
                ? genePawnExts.Max(x => x.priority + (x.HasGeneFilters ? 0.5f : 0))
                : 0);

            var hediffPawnExts = pawn.GetHediffExtensions<PawnExtension>();
            foreach (var gene in orderedGenes)
            {
                if (!GeneCache.globalCache.TryGetValue(gene, out var geneCache))
                {
                    GeneCache.globalCache[gene] = geneCache = new GeneCache(gene);
                }
                var allActiveGenes = GeneHelpers.GetAllActiveGenes(pawn).ToList();
                var genePawnExts = gene.def.GetAllPawnExtensionsOnGene();
                
                string failReason = GeneShouldBeActive(gene, genePawnExts, hediffPawnExts, allPawnExts, allActiveGenes);
                if (failReason != "")
                {
                    //Log.Message($"Debug: {gene.def.defName} failed to activate: {failReason}");
                    if (!geneCache.isOverriden) genesDeactivated.Add(gene);
                    geneCache.isOverriden = true;
                    if (gene.Active)
                    {
                        // In case it wasn't already disabled, just do it here.
                        gene.OverrideBy(GeneCache.DummyGene);
                    }
                }
                else
                {
                    
                    if (geneCache.isOverriden)
                    {
                        //Log.Message($"Debug: Activated {gene.def.defName}");
                        geneCache.isOverriden = false;
                        genesActivated.Add(gene);
                        gene.OverrideBy(null);
                    }
                    //foreach (var supressor in gene.def.ExtensionsOnDef<GeneSuppressor_Gene, GeneDef>())
                    //{
                    //    foreach (string supressedGene in supressor.supressedGenes)
                    //    {
                    //        allGenes.Where(x => x.def.defName == supressedGene).ToList().ForEach(x => x.overriddenByGene = gene);
                    //    }
                    //}
                }
            }
        }
    }

    public class GeneCache
    {
        public static Dictionary<Gene, GeneCache> globalCache = [];

        public bool initialized = false;
        public bool isOverriden = false;
        public string disabledMessage = "BS_RequirementNotMet".Translate().CapitalizeFirst();
        public Gene gene = null;

        private static Gene dummyGene = null;
        //private static Gene dummyGeneRace = null;
        public static Gene MakeDummyGene()
        {
            dummyGene ??= new Gene
            {
                def = new GeneDef()
                {
                    defName = "BS_PDummyGene",
                    label = "BS_RequirementNotMet".Translate().CapitalizeFirst(),
                    description = "System gene.",
                    displayCategory = GeneCategoryDefOf.Miscellaneous,
                    canGenerateInGeneSet = false,
                    selectionWeight = 0,
                    selectionWeightCultist = 0,
                }
            };
            return dummyGene;
        }
        public static Gene DummyGene => dummyGene ??= MakeDummyGene();
        public GeneCache(Gene gene)
        {
            MakeDummyGene(); // Make sure it is set up.

            this.gene = gene;
            globalCache.Add(gene, this);
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
