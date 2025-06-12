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
    public class Dialog_ViewMutations : Window
    {
        private const float WIDTH = 763f;
        private const float HEADER_HEIGHT = 30f;

        private Pawn target;
        public override Vector2 InitialSize => new Vector2(736f, UI.screenHeight * 0.7f);
        private Vector2 scrollPosition;
        
        public Dialog_ViewMutations(Pawn target)
        {
            this.target = target;
            forcePause = false;
            closeOnAccept = false;
            closeOnCancel = false;
            doCloseX = true;
            draggable = true;
            resizeable = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            DrawGeneSection.pCache = HumanoidPawnScaler.GetCache(target);
        }

        public override void WindowOnGUI()
        {
            if (resizer != null)
            {
                resizer.minWindowSize.x = WIDTH;
            }

            if (resizer?.isResizing == false && windowRect.width != WIDTH)
            {
                windowRect.width = WIDTH;
            }

            base.WindowOnGUI();
        }

        public override void ExtraOnGUI()
        {
            base.ExtraOnGUI();
        }

        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMax -= CloseButSize.y;
            Rect rect = inRect;
            rect.xMin += 34f;
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, "BS_ViewGenetics".Translate(target));
            Text.Font = GameFont.Small;
            GUI.color = XenotypeDef.IconColor;
            if (target.genes != null)
                GUI.DrawTexture(new Rect(inRect.x, inRect.y, HEADER_HEIGHT, HEADER_HEIGHT), target.genes.XenotypeIcon);
            GUI.color = Color.white;
            inRect.yMin += 34f;
            Vector2 size = windowRect.size;
            RaceViewUIManager.DrawRacialInfo(inRect, target, inRect.height, ref size, ref scrollPosition);
            if (Widgets.ButtonText(new Rect(inRect.xMax - CloseButSize.x, inRect.yMax, CloseButSize.x, CloseButSize.y), "Close".Translate()))
            {
                Close();
            }
        }
    }

}
