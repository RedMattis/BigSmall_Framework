using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace BigAndSmall
{
    /// <summary>
    /// This class is essentially a greatly simplified version of UltimateRenderNode, etc, for some common "switch my thing's graphic out if" rules.
    /// 
    /// Complex Rendernode supports these properties. The Ultimate one does not, it has its own more powerful system.
    /// 
    /// The main purpose of this node is to make it easier to just copy-paste add/patch compatibility for wings, tails, etc.
    /// 
    /// If the pawn doesn't have the part at all it won't disable the node, so this only works on pawns that have the part. The
    /// reason for this is to avoid hiding wings from mods like "Sarg's Alpha Genes" that render wings but don't actually add them to the pawn.
    /// </summary>
    public class PawnRenderNode_SimpleSwitchesProps : PawnRenderNodeProperties
    {
        public class DisableIfWing
        {
            public bool bionic = false;
            public bool bionicLeft = false; // Mirrored
            public bool bionicRight = false;
            public bool missingTwo = false;
            public bool missingLeft = false; // Mirrored
            public bool missingRight = false;
            public bool ShouldDisable(Pawn pawn) =>
                (missingTwo && GraphicsHelper.GetPartsWithHediff(pawn, 1, BSDefs.BS_Wing, HediffDefOf.MissingBodyPart) > 1) ||
                (missingLeft && GraphicsHelper.GetPartsWithHediff(pawn, 1, BSDefs.BS_Wing, HediffDefOf.MissingBodyPart, mirrored:true) > 0) ||
                (missingRight && GraphicsHelper.GetPartsWithHediff(pawn, 1, BSDefs.BS_Wing, HediffDefOf.MissingBodyPart, mirrored:false) > 0) ||
                (bionic && GraphicsHelper.GetPartsReplaced(pawn, 1, BSDefs.BS_Wing) > 0) ||
                (bionicLeft && GraphicsHelper.GetPartsReplaced(pawn, 1, BSDefs.BS_Wing, mirrored: true) > 0) ||
                (bionicRight && GraphicsHelper.GetPartsReplaced(pawn, 1, BSDefs.BS_Wing, mirrored: false) > 0);
        }
        public class DisableIfTail
        {
            public bool missing = false;
            public bool bionic = false;
            public bool ShouldDisable(Pawn pawn) =>
                (missing && GraphicsHelper.GetPartsWithHediff(pawn, 1, BSDefs.Tail, HediffDefOf.MissingBodyPart) > 0) ||
                (bionic && GraphicsHelper.GetPartsReplaced(pawn, 1, BSDefs.Tail) > 0);
        }

        public DisableIfWing disableIfWing;
        public DisableIfTail disableIfTail;

        public bool ShouldDisable(Pawn pawn) =>
            disableIfWing?.ShouldDisable(pawn) == true ||
            disableIfTail?.ShouldDisable(pawn) == true;
    }

    public class PawnRenderNode_SimpleSwitches : PawnRenderNode
    {
        readonly string noImage = "BS_Blank";
        PawnRenderNode_SimpleSwitchesProps ComplexProps => (PawnRenderNode_SimpleSwitchesProps)props;
        public PawnRenderNode_SimpleSwitches(Pawn pawn, PawnRenderNode_SimpleSwitchesProps props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        protected override string TexPathFor(Pawn pawn)
        {
            if (ComplexProps.ShouldDisable(pawn))
            {
                return noImage;
            }
            return base.TexPathFor(pawn);
        }
    }
}
