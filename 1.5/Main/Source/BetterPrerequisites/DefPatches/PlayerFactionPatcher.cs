using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace BigAndSmall
{

    public class FactionExtension : DefModExtension
    {
        public class PawnKindSwap
        {
            //public List<string> pawnKindsToSwapFaction = new List<string>();
            public List<string> eventsToSwapPawnKind = new List<string>();
            public List<PawnkindChance> pawnKindSet = new List<PawnkindChance>();
            public bool forcePawnKindIdeology = false;
            //List<XenotypeChance> xenotypeChances = new List<XenotypeChance>();
        }

        public List<PawnKindSwap> pawnKindSwaps = new List<PawnKindSwap>();
    }

    public class PawnkindChance
    {
        public PawnKindDef pawnKind;
        public float chance = 1;

        public PawnkindChance()
        {
        }

        public PawnkindChance(PawnKindDef pawnKind, float chance)
        {
            this.pawnKind = pawnKind;
            this.chance = chance;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "pawnKind", xmlRoot.Name);
            chance = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
    }

    [HarmonyPatch]
    public static class PawnKindSwapPatches
    {
        [HarmonyPatch(typeof(QuestNode_Root_WandererJoin_WalkIn), nameof(QuestNode_Root_WandererJoin_WalkIn.GeneratePawn))]
        [HarmonyPrefix]
        public static bool GenerateWandererFactionPrefix(ref Pawn __result)
        {
            try
            {
                var playerFaction = Faction.OfPlayer;
                FactionExtension factionExtension = playerFaction.def.GetModExtension<FactionExtension>();
                if (factionExtension != null)
                {
                    // Check if QuestNode_Root_WandererJoin_WalkIn is in the eventsToSwapPawnKind list
                    if (factionExtension.pawnKindSwaps.Where(x => x.eventsToSwapPawnKind.Contains("QuestNode_Root_WandererJoin_WalkIn")).FirstOrDefault() is FactionExtension.PawnKindSwap pawnKindSwap)
                    {
                        Slate slate = QuestGen.slate;
                        Gender? fixedGender = null;
                        var pawnKind = pawnKindSwap.pawnKindSet.RandomElementByWeight(x => x.chance).pawnKind;
                        if (pawnKind.defName == "Villager") return true; // If we rolled a Villager we'll just let vanilla handle it.
                        Faction faction = Find.FactionManager.AllFactions.Where(x => x.def == pawnKind.defaultFactionType).RandomElement();
                        Ideo fixedIdeo = pawnKindSwap.forcePawnKindIdeology ? faction.ideos?.PrimaryIdeo : null;
                        if (!slate.TryGet<PawnGenerationRequest>("overridePawnGenParams", out var pgr))
                        {
                            pgr = new PawnGenerationRequest(pawnKind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false,
                                allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 20f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true,
                                allowPregnant: true, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist:
                                false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, fixedGender, null, null, null, fixedIdeo: fixedIdeo, forceNoIdeo: fixedIdeo == null,
                                forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: true);
                        }
                        if (Find.Storyteller.difficulty.ChildrenAllowed)
                        {
                            pgr.AllowedDevelopmentalStages |= DevelopmentalStage.Child;
                        }
                        var xenotypeChances = pawnKind.GetXenotypeChances();
                        //pgr.ForcedXenotype = xenotypeChances.GetRandomXenotype();

                        Pawn pawn = PawnGenerator.GeneratePawn(pgr);

                        if (pawn?.genes != null && xenotypeChances?.Count > 0)
                        {
                            for (int idx = pawn.genes.Endogenes.Count - 1; idx >= 0; idx--)
                            {
                                Gene gene = pawn.genes.Endogenes[idx];
                                pawn.genes.RemoveGene(gene);
                            }
                            pawn.genes.SetXenotype(xenotypeChances?.GetRandomXenotype());
                        }

                        if (!pawn.IsWorldPawn())
                        {
                            Find.WorldPawns.PassToWorld(pawn);
                        }
                        __result = pawn;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in Override of GenerateWandererFactionPrefix: {ex.Message}. Using Vanilla Method as a fallback.");
            }
            return true;
        }

        [HarmonyPatch(typeof(ThingSetMaker_RefugeePod), nameof(ThingSetMaker_RefugeePod.Generate), new Type[]
        {
            typeof(ThingSetMakerParams), typeof(List<Thing>)
        })]
        [HarmonyPrefix]
        public static bool GenerateRefugeePodPrefix(ref ThingSetMakerParams parms, ref List<Thing> outThings)
        {
            try
            {
                var playerFaction = Faction.OfPlayer;
                FactionExtension factionExtension = playerFaction.def.GetModExtension<FactionExtension>();
                if (factionExtension != null &&
                    factionExtension.pawnKindSwaps
                        .Where(x => x.eventsToSwapPawnKind
                        .Contains("ThingSetMaker_RefugeePod"))
                        .FirstOrDefault() is FactionExtension.PawnKindSwap pawnKindSwap)
                {
                    var pawnKind = pawnKindSwap.pawnKindSet.RandomElementByWeight(x => x.chance).pawnKind;
                    if (pawnKind.defName == "SpaceRefugee") return true; // If we rolled a SpaceRefugee we'll just let vanilla handle it.

                    // Find a random faction that matches the defaultFactions.
                    Faction pawnKindFaction = Find.FactionManager.AllFactions.Where(x => x.def == pawnKind.defaultFactionType).RandomElement();
                    Ideo fixedIdeo = pawnKindSwap.forcePawnKindIdeology ? pawnKindFaction.ideos?.PrimaryIdeo : null;

                    var targetFaction = DownedRefugeeQuestUtility.GetRandomFactionForRefugee();
                    if (pawnKindSwap.forcePawnKindIdeology)
                    {
                        targetFaction = pawnKindFaction;
                    }

                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, targetFaction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 20f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: true, fixedIdeo: fixedIdeo));
                    var xenoTypeChances = pawnKind.GetXenotypeChances();
                    if (pawn?.genes != null && xenoTypeChances.Count > 0)
                    {
                        for (int idx = pawn.genes.Endogenes.Count - 1; idx >= 0; idx--)
                        {
                            Gene gene = pawn.genes.Endogenes[idx];
                            pawn.genes.RemoveGene(gene);
                        }
                        pawn.genes.SetXenotype(xenoTypeChances.RandomElementByWeight(x => x.chance).xenotype);
                    }
                    outThings.Add(pawn);
                    HealthUtility.DamageUntilDowned(pawn);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in Override of GenerateRefugeePodPrefix: {ex.Message}. Using Vanilla Method as a fallback.");
            }
            return true;
        }
    }

}
