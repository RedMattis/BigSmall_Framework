using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class GeneGizmo_ResourceSlime(BS_GeneSlimePower spGene, List<IGeneResourceDrain> drainGenes, Color barColor, Color barhighlightColor)
        : GeneGizmo_Resource(spGene, drainGenes, barColor, barhighlightColor)
    {
        private List<Pair<IGeneResourceDrain, float>> tmpDrainGenes = []; // Unused.

        protected override string GetTooltip()
        {
            tmpDrainGenes.Clear();
            string text = $"{spGene.ResourceLabel.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor)}: {gene.ValueForDisplay} / {gene.MaxForDisplay}\n";
            if (spGene.pawn.IsColonistPlayerControlled || spGene.pawn.IsPrisonerOfColony)
            {
                text = text + (string)("BS_AccumulateSlimeUntil".Translate() + ": ") + spGene.PostProcessValue(gene.targetValue);
            }
            if (!spGene.def.resourceDescription.NullOrEmpty())
            {
                text = text + "\n\n" + spGene.def.resourceDescription.Formatted(gene.pawn.Named("PAWN")).Resolve();
            }
            return text;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            return base.GizmoOnGUI(topLeft, maxWidth, parms);
        }
    }
}
