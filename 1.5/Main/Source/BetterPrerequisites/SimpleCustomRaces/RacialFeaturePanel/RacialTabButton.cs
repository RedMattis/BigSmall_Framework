using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class MutationDebugging
    {
        [DebugAction("Big & Small", "View Mutations", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void EditHeraldicsForSelected()
        {
            var thing = Find.Selector.SelectedObjects.OfType<Pawn>().FirstOrDefault();
            if (thing == null) Find.Selector.SelectedObjects.OfType<Thing>().FirstOrDefault();
            if (thing == null) throw new Exception("No valid thing selected viewing mutations.");
            //Find.Selector.Select(thing);
            //InspectPaneUtility.OpenTab(typeof(ITab_Mutation));
            var window = new Dialog_ViewMutations(thing);
            Find.WindowStack.Add(window);
            //if (ThingSelectionUtility.SelectableByMapClick(thing))
            //{

            //}
        }
    }

    //public class CompProperties_ViewMutations : CompProperties
    //{
    //    public CompProperties_ViewMutations()
    //    {
    //        compClass = typeof(CompEditMutation);
    //    }
    //}
    //public class CompEditMutation : ThingComp
    //{
    //    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    //    {
    //        // Check if Heraldic Research is completed.
    //        var research = DefDatabase<ResearchProjectDef>.GetNamed("VFEM2_Heraldry");
    //        if (research != null && research.IsFinished)
    //        {
    //            yield return new Command_Action
    //            {
    //                icon = ContentFinder<Texture2D>.Get("BS_DefaultIcon"),
    //                defaultLabel = string.Format("BS_ViewGenetics".Translate(), parent.def.label),
    //                action = delegate
    //                {
    //                    if (ThingSelectionUtility.SelectableByMapClick(parent))
    //                    {
    //                        Find.Selector.Select(parent);
    //                        InspectPaneUtility.OpenTab(typeof(ITab_Mutation));
    //                    }

    //                    //var window = new Dialog_Heraldic(parent);
    //                    //Find.WindowStack.Add(window);
    //                }
    //            };
    //        }
    //    }
    //}

}
