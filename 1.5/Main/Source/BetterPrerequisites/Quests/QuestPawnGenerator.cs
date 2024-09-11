using RimWorld.QuestGen;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld.Planet;
using HarmonyLib;

namespace BigAndSmall
{
    public class QuestNode_PawnGenerator : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> storeAs;

        [NoTranslate]
        public SlateRef<string> addToList;

        [NoTranslate]
        public SlateRef<IEnumerable<string>> addToLists;

        public SlateRef<bool> playerFaction;
        public SlateRef<List<XenotypeDef>> forcedXenotypes = null;
        public SlateRef<PawnKindDef> kindDef;
        public SlateRef<Faction> faction;
        public SlateRef<bool> forbidAnyTitle;
        public SlateRef<bool> ensureNonNumericName;
        public SlateRef<List<TraitDef>> forceOneTraitOf;
        public SlateRef<IEnumerable<TraitDef>> forcedTraits;
        public SlateRef<IEnumerable<TraitDef>> prohibitedTraits;
        public SlateRef<Pawn> extraPawnForExtraRelationChance;
        public SlateRef<float> relationWithExtraPawnChanceFactor;
        public SlateRef<bool?> allowAddictions;
        public SlateRef<float> biocodeWeaponChance;
        public SlateRef<float> biocodeApparelChance;
        public SlateRef<bool> mustBeCapableOfViolence;
        public SlateRef<bool> isChild;
        public SlateRef<bool> allowPregnant;
        public SlateRef<Gender?> fixedGender;

        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }

        protected override void RunInt()
        {
            XenotypeDef forcedXeno = forcedXenotypes.GetValue(QuestGen.slate)?.RandomElement();
            Slate slate = QuestGen.slate;
            Faction faction = this.faction.GetValue(slate);
            var forcedTraits = this.forcedTraits.GetValue(slate);
            var rngForcedTrait = forceOneTraitOf.GetValue(slate)?.RandomElement();
            //if (rngForcedTrait != null)
            //{
            //    forcedTraits.AddItem(rngForcedTrait);
            //}
            if (playerFaction.GetValue(slate))
            {
                faction = Faction.OfPlayer;
            }
            //Faction ofPlayer = Faction.OfPlayer;
            PawnGenerationRequest request = new PawnGenerationRequest(
                kindDef.GetValue(slate),
                faction: faction,
                context: PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: false,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: true,
                mustBeCapableOfViolence: mustBeCapableOfViolence.GetValue(slate),
                colonistRelationChanceFactor: 1f,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: true,
                allowPregnant: allowPregnant.GetValue(slate),
                allowFood: true,
                allowAddictions: allowAddictions.GetValue(slate) ?? true,
                forcedTraits: forcedTraits,
                prohibitedTraits: prohibitedTraits.GetValue(slate),
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                worldPawnFactionDoesntMatter: false,
                forceRedressWorldPawnIfFormerColonist: false,
                biocodeWeaponChance: biocodeWeaponChance.GetValue(slate),
                biocodeApparelChance: biocodeApparelChance.GetValue(slate),
                forceNoIdeo: false,
                forceNoBackstory: false,
                forbidAnyTitle: forbidAnyTitle.GetValue(slate),
                validatorPreGear: null,
                validatorPostGear: null,
                minChanceToRedressWorldPawn: 0f,
                fixedBiologicalAge: null,
                fixedChronologicalAge: null,
                fixedGender: fixedGender.GetValue(slate),
                forcedXenotype: forcedXeno,
                forceRecruitable: false,
                biologicalAgeRange: null,
                extraPawnForExtraRelationChance: extraPawnForExtraRelationChance.GetValue(slate),
                relationWithExtraPawnChanceFactor: relationWithExtraPawnChanceFactor.GetValue(slate)
            );

            Pawn pawn = PawnGenerator.GeneratePawn(request);
            // If the pawn does not have the forced trait, add it. For some reason the forced traits weren't working?
            if (rngForcedTrait != null && !pawn.story.traits.HasTrait(rngForcedTrait))
            {
                pawn.story.traits.GainTrait(new Trait(rngForcedTrait, 0, true));
            }

            if (ensureNonNumericName.GetValue(slate) && (pawn.Name == null || pawn.Name.Numerical))
            {
                pawn.Name = PawnBioAndNameGenerator.GeneratePawnName(pawn);
            }
            if (storeAs.GetValue(slate) != null)
            {
                QuestGen.slate.Set(storeAs.GetValue(slate), pawn);
            }
            if (addToList.GetValue(slate) != null)
            {
                QuestGenUtility.AddToOrMakeList(QuestGen.slate, addToList.GetValue(slate), pawn);
            }
            if (addToLists.GetValue(slate) != null)
            {
                foreach (string item in addToLists.GetValue(slate))
                {
                    QuestGenUtility.AddToOrMakeList(QuestGen.slate, item, pawn);
                }
            }
            QuestGen.AddToGeneratedPawns(pawn);
            if (!pawn.IsWorldPawn())
            {
                Find.WorldPawns.PassToWorld(pawn);
            }
        }
    }
}
