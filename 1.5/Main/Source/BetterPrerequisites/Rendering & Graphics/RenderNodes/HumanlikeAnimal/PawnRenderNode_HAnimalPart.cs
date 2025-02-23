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

    public class PawnRenderNode_HAnimalPart : PawnRenderNode
    {
        public PawnRenderNode_HAnimalPart(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
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
            var animalKind = hueAni.animalKind;
            PawnKindLifeStage curKindLifeStage = animalKind.lifeStages[hueAni.GetLifeStageIndex(pawn)];

            // All the code below is copy-pasta from PawnRenderNode_AnimalPart.

            AlternateGraphic ag;
            int index;
            Graphic graphic = (pawn.TryGetAlternate(out ag, out index) ? ag.GetGraphic(curKindLifeStage.bodyGraphicData.Graphic) : ((pawn.gender == Gender.Female && curKindLifeStage.femaleGraphicData != null) ? curKindLifeStage.femaleGraphicData.Graphic : curKindLifeStage.bodyGraphicData.Graphic));
            if ((pawn.Dead || (pawn.IsMutant && pawn.mutant.Def.useCorpseGraphics)) && curKindLifeStage.corpseGraphicData != null)
            {
                graphic = ((pawn.gender == Gender.Female && curKindLifeStage.femaleCorpseGraphicData != null) ? curKindLifeStage.femaleCorpseGraphicData.Graphic.GetColoredVersion(curKindLifeStage.femaleCorpseGraphicData.Graphic.Shader, graphic.Color, graphic.ColorTwo) : curKindLifeStage.corpseGraphicData.Graphic.GetColoredVersion(curKindLifeStage.corpseGraphicData.Graphic.Shader, graphic.Color, graphic.ColorTwo));
            }
            switch (pawn.Drawer.renderer.CurRotDrawMode)
            {
                case RotDrawMode.Fresh:
                    if (ModsConfig.AnomalyActive && pawn.IsMutant && pawn.mutant.HasTurned)
                    {
                        return graphic.GetColoredVersion(ShaderDatabase.Cutout, MutantUtility.GetSkinColor(pawn, graphic.Color).Value, MutantUtility.GetSkinColor(pawn, graphic.ColorTwo).Value);
                    }
                    return graphic;
                case RotDrawMode.Rotting:
                    return graphic.GetColoredVersion(ShaderDatabase.Cutout, PawnRenderUtility.GetRottenColor(graphic.Color), PawnRenderUtility.GetRottenColor(graphic.ColorTwo));
                case RotDrawMode.Dessicated:
                    if (curKindLifeStage.dessicatedBodyGraphicData != null)
                    {
                        Graphic graphic2;
                        if (pawn.RaceProps.FleshType != FleshTypeDefOf.Insectoid)
                        {
                            graphic2 = ((pawn.gender == Gender.Female && curKindLifeStage.femaleDessicatedBodyGraphicData != null) ? curKindLifeStage.femaleDessicatedBodyGraphicData.GraphicColoredFor(pawn) : curKindLifeStage.dessicatedBodyGraphicData.GraphicColoredFor(pawn));
                        }
                        else
                        {
                            Color dessicatedColorInsect = PawnRenderUtility.DessicatedColorInsect;
                            graphic2 = ((pawn.gender == Gender.Female && curKindLifeStage.femaleDessicatedBodyGraphicData != null) ? curKindLifeStage.femaleDessicatedBodyGraphicData.Graphic.GetColoredVersion(ShaderDatabase.Cutout, dessicatedColorInsect, dessicatedColorInsect) : curKindLifeStage.dessicatedBodyGraphicData.Graphic.GetColoredVersion(ShaderDatabase.Cutout, dessicatedColorInsect, dessicatedColorInsect));
                        }
                        if (pawn.IsMutant)
                        {
                            graphic2.ShadowGraphic = graphic.ShadowGraphic;
                        }
                        if (ag != null)
                        {
                            graphic2 = ag.GetDessicatedGraphic(graphic2);
                        }
                        return graphic2;
                    }
                    break;
            }
            return null;
        }
    }

}
