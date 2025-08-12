using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace BigAndSmall
{
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

			// All the code below is mostly copy-pasta from PawnRenderNode_AnimalPart.
			Graphic graphic = null;
			AlternateGraphic ag = null;
			if (pawn.overrideGraphicIndex != null && animalKind.alternateGraphics?.Count > pawn.overrideGraphicIndex + 1)
			{
				ag = animalKind.alternateGraphics[pawn.overrideGraphicIndex.Value];
				graphic = ag.GetGraphic(curKindLifeStage.bodyGraphicData.Graphic);
			}

			// Try to fetch alternate graphic if available otherwise fetch default.
			if (graphic == null)
			{
				if (pawn.gender == Gender.Female && curKindLifeStage.femaleGraphicData != null)
					graphic = curKindLifeStage.femaleGraphicData.Graphic;
				else
					graphic = curKindLifeStage.bodyGraphicData.Graphic;
			}
						
			if ((pawn.Dead || (pawn.IsMutant && pawn.mutant.Def.useCorpseGraphics)) && curKindLifeStage.corpseGraphicData != null)
            {
				if (pawn.gender == Gender.Female && curKindLifeStage.femaleCorpseGraphicData != null)
					graphic = curKindLifeStage.femaleCorpseGraphicData.Graphic.GetColoredVersion(curKindLifeStage.femaleCorpseGraphicData.Graphic.Shader, graphic.Color, graphic.ColorTwo);
				else
					graphic = curKindLifeStage.corpseGraphicData.Graphic.GetColoredVersion(curKindLifeStage.corpseGraphicData.Graphic.Shader, graphic.Color, graphic.ColorTwo);
            }


			ColorSetting colorA = BSDefs.BS_DefaultSapientAnimalColorA.color;
			ColorSetting colorB = BSDefs.BS_DefaultSapientAnimalColorB.color;

			var material = HumanoidPawnScaler.GetCache(pawn).bodyMaterial;
			if (material != null)
			{
				if (material.colorA != null)
					colorA = material.colorA;

				if (material.colorB != null)
					colorB = material.colorB;
			}

			Color color1 = colorA.GetColor(this, graphic.color, ColorSetting.clrOneKey);
			Color color2 = colorB.GetColor(this, graphic.colorTwo, ColorSetting.clrTwoKey);
			graphic = graphic.GetColoredVersion(graphic.Shader, color1, color2);

			switch (pawn.Drawer.renderer.CurRotDrawMode)
            {
                case RotDrawMode.Fresh:
                    if (ModsConfig.AnomalyActive && pawn.IsMutant && pawn.mutant.HasTurned)
                    {
                        return graphic.GetColoredVersion(ShaderDatabase.Cutout, MutantUtility.GetMutantSkinColor(pawn, color1), MutantUtility.GetMutantSkinColor(pawn, color2));
                    }
                    return graphic;
                case RotDrawMode.Rotting:
                    return graphic.GetColoredVersion(ShaderDatabase.Cutout, PawnRenderUtility.GetRottenColor(color1), PawnRenderUtility.GetRottenColor(color2));
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
