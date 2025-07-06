using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class PawnRenderNode_HAnimalBody : PawnRenderNode_HAnimalPart
    {
        public PawnRenderNode_HAnimalBody(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : base(pawn, props, tree)
        {
        }

        protected override IEnumerable<(GraphicStateDef state, Graphic graphic)> StateGraphicsFor(Pawn pawn)
        {
            foreach (var item in base.StateGraphicsFor(pawn))
            {
                yield return item;
            }

            HumanlikeAnimalGenerator.humanlikeAnimals.TryGetValue(pawn.def, out HumanlikeAnimal hueAni);
            if (hueAni == null)
            {
                Log.ErrorOnce("No HumanlikeAnimal found for " + pawn.def.defName, 123456333);
                yield break;
            }
            var animalKind = hueAni.animalKind;
            PawnKindLifeStage curKindLifeStage = animalKind.lifeStages[hueAni.GetLifeStageIndex(pawn)];

            if (curKindLifeStage.swimmingGraphicData != null)
            {
                Graphic graphic = curKindLifeStage.swimmingGraphicData.Graphic;
                if (pawn.gender == Gender.Female && curKindLifeStage.femaleSwimmingGraphicData != null)
                {
                    graphic = curKindLifeStage.femaleSwimmingGraphicData.Graphic;
                }
                if (pawn.TryGetAlternate(out var ag, out var _))
                {
                    graphic = ag.GetSwimmingGraphic(graphic);
                }
                yield return (GraphicStateDefOf.Swimming, graphic);
            }
        }
    }

}
