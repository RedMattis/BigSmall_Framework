using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigAndSmall
{
    // Assembly-CSharp, Version=1.5.9102.32373, Culture=neutral, PublicKeyToken=null
    // Verse.PawnRenderNode_AnimalPart
    using RimWorld;
    using UnityEngine;
    using Verse;

    public class PawnRenderNode_HAnimalPack : PawnRenderNode
    {
        public bool isPackAnimal = false;

        public PawnRenderNode_HAnimalPack(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            // kinda ugly, but the props don't get set up properly from the XML when loaded from a PawnRenderTree.
            props.pawnType = PawnRenderNodeProperties.RenderNodePawnType.Any;
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            Graphic graphic = GraphicFor(pawn);
            if (graphic != null)
            {
                return MeshPool.GetMeshSetForSize(graphic.drawSize.x, graphic.drawSize.y);
            }
            return null;
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            HumanlikeAnimalGenerator.humanlikeAnimals.TryGetValue(pawn.def, out HumanlikeAnimal hueAni);
            if (hueAni == null)
            {
                Log.ErrorOnce("No HumanlikeAnimal found for " + pawn.def.defName, 123456333);
                return null;
            }
            if (!hueAni.animal.race.packAnimal)
            {
                isPackAnimal = false;
                return null;
            }
            isPackAnimal = true;
            var animalKind = hueAni.animalKind;
            PawnKindLifeStage curKindLifeStage = animalKind.lifeStages[hueAni.GetLifeStageIndex(pawn)];

            // All the code below is copy-pasta from PawnRenderNode_AnimalPack.

            Graphic graphic = ((pawn.gender == Gender.Female && curKindLifeStage.femaleGraphicData != null) ? curKindLifeStage.femaleGraphicData.Graphic : curKindLifeStage.bodyGraphicData.Graphic);

            return GraphicDatabase.Get<Graphic_Multi>(graphic.path + "Pack", ShaderDatabase.Cutout, graphic.drawSize, Color.white);
        }
    }

    public class PawnRenderNodeWorker_HAnimalPack : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (node is PawnRenderNode_HAnimalPack hAnimalPack && hAnimalPack.isPackAnimal == false)
            {
                return false;
            }

            if (base.CanDrawNow(node, parms) && !parms.Portrait && parms.pawn.inventory != null)
            {
                return parms.pawn.inventory.innerContainer.Count > 0;
            }
            return false;
        }
    }

}
