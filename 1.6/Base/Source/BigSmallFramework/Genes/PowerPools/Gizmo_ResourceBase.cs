using BigAndSmall;
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
    public abstract class Gizmo_ResourceBase() : Gizmo_Slider()
    {
        
        public IResourcePool resource;
        protected override Color BarColor { get; }
        protected override Color BarHighlightColor { get; }
        protected override string BarLabel => $"{resource.ValueForDisplay} / {resource.MaxForDisplay}";
        protected override float ValuePercent => resource.ValuePercent;
        protected override int Increments => resource.Increments / 10;

        //protected override FloatRange DragRange => new FloatRange(0f, gene.Max);
        protected override bool DraggingBar { get { return field; } set { field = value; } }
        protected override float Target
        {
            get
            {
                return resource.TargetValue / resource.Max;
            }
            set
            {
                resource.SetTargetValuePct(value);
            }
        }

        protected override string Title
        {
            get
            {
                string text = resource.Label.CapitalizeFirst();
                if (Find.Selector.SelectedPawns.Count != 1)
                {
                    text = text + " (" + resource.Pawn.LabelShort + ")";
                }

                return text;
            }
        }
    }
}
