using RimWorld;
using UnityEngine;
using Verse;
namespace BigAndSmall
{
    public class PawnRenderNodeWorker_HumanlikeAnimalBody : PawnRenderNodeWorker
    {
        private HumanlikeAnimal humanlikeAnimal;
        public HumanlikeAnimal GetHumanlikeAnimal(Pawn pawn)
        {
            if (humanlikeAnimal != null) return humanlikeAnimal;
            if (HumanlikeAnimalGenerator.humanlikeAnimals.TryGetValue(pawn.def, out HumanlikeAnimal hueAni))
            {
                humanlikeAnimal = hueAni;
                return humanlikeAnimal;
            }
            Log.ErrorOnce("No HumanlikeAnimal found for " + pawn.def.defName, 123456333);
            return null;
        }
        protected override GraphicStateDef GetGraphicState(PawnRenderNode node, PawnDrawParms parms)
        {
            if (node.tree.currentAnimation != null || !DrawNonHumanlikeSwimmingGraphic(parms.pawn))
            {
                return base.GetGraphicState(node, parms);
            }
            return GraphicStateDefOf.Swimming;
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            return base.OffsetFor(node, parms, out pivot) + node.PrimaryGraphic.DrawOffset(parms.facing);
        }

        // Not a fan of how this method looks, but Ludeon runs about the same code, so if it ends up expensive then
        // Ludeon's work probably is as well...
        public bool DrawNonHumanlikeSwimmingGraphic(Pawn pawn)
        {
            if (!pawn.Spawned || !pawn.WaterCellCost.HasValue)
            {
                return false;
            }
            var hla = GetHumanlikeAnimal(pawn);
            var animalKind = hla.animalKind;
            var idx = hla.GetLifeStageIndex(pawn);
            var curKindLifeStage = animalKind.lifeStages[idx];
            if (curKindLifeStage.swimmingGraphicData != null)
            {
                return pawn.Position.GetTerrain(pawn.Map).IsWater;
            }
            return false;
        }
    }
}