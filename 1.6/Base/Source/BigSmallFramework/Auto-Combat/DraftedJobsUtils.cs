using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.AI;
using Verse;

namespace BigAndSmall
{
    // Based on my work for Insectoids 2 in Vanilla Expanded.
    public class Command_ToggleWithRClick : Command_Toggle
    {
        public Action rightClickAction;

        public override void ProcessInput(Event ev)
        {
            if (ev.button == 1)
            {
                rightClickAction?.Invoke();
            }
            else
            {
                base.ProcessInput(ev);
            }
        }
    }

    [HarmonyPatch]
    [StaticConstructorOnStartup]
    public static class DraftGizmos
    {
        public static readonly Texture2D AutoCastTex = ContentFinder<Texture2D>.Get("BS_UI/Auto_Tiny");
        public static readonly Texture2D HuntIcon = ContentFinder<Texture2D>.Get("BS_UI/Hunt");
        public static readonly Texture2D TakeCoverIcon = ContentFinder<Texture2D>.Get("BS_UI/TakeCover");
        public static readonly Texture2D MeleeCharge = ContentFinder<Texture2D>.Get("BS_UI/MeleeCharge");
        private static bool? autoCombatModEnabled = null;
        public static bool AutoCombatEnabled
        {
            get
            {
                autoCombatModEnabled ??= ModsConfig.IsActive("RedMattis.AutoCombat");
                return autoCombatModEnabled == true || BigSmallMod.settings.enableDraftedJobs;
            }
        }

        public static bool IsDraftedPlayerPawn(Pawn pawn)
        {
            return pawn?.Faction == Faction.OfPlayerSilentFail && pawn.Drafted;
        }

        [HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> GetGizmosPostfix(IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
        {
            static IEnumerable<Gizmo> UpdateEnumerable(IEnumerable<Gizmo> gizmos, List<Command_ToggleWithRClick> commands)
            {
                foreach (var gizmo in gizmos)
                {
                    if (gizmo is Command_Toggle draftCommand && draftCommand.icon == TexCommand.Draft)
                    {
                        yield return draftCommand;
                        foreach (var command in commands)
                        {
                            yield return command;
                        }
                    }
                    else
                    {
                        yield return gizmo;
                    }
                }
            }
            var pawn = __instance.pawn;
            if (AutoCombatEnabled && IsDraftedPlayerPawn(pawn))
            {
                // Add Hunt Toggle Gizmo
                Command_ToggleWithRClick huntCommand = AddHuntGizmo(pawn);
                if (DraftedActionHolder.GetData(pawn).hunt)
                {
                    Command_ToggleWithRClick takeCover = AddCoerGizmo(pawn);
                    Command_ToggleWithRClick meleeCharge = AddChargeGizmo(pawn);
                    return UpdateEnumerable(__result, [huntCommand, takeCover, meleeCharge]);
                }
                else
                {
                    return UpdateEnumerable(__result, [huntCommand]);
                }



            }
            return __result;
        }

        private static Command_ToggleWithRClick AddChargeGizmo(Pawn pawn)
        {
            return new Command_ToggleWithRClick
            {
                defaultLabel = "BS_MeleeChargeLabel".Translate(),
                defaultDesc = "BS_MeleeChargeDescription".Translate(),
                icon = MeleeCharge,
                isActive = () => DraftedActionHolder.GetData(pawn).meleeCharge,
                toggleAction = () => DraftedActionHolder.GetData(pawn).ToggleMeleeCharge(),
                rightClickAction = () =>
                {
                },
                activateSound = SoundDefOf.Click,
                groupKey = 6173615,
                hotKey = KeyBindingDefOf.Misc5
            };
        }

        private static Command_ToggleWithRClick AddCoerGizmo(Pawn pawn)
        {
            return new Command_ToggleWithRClick
            {
                defaultLabel = "BS_TakeCoverLabel".Translate(),
                defaultDesc = "BS_TakeCoverDescription".Translate(),
                icon = TakeCoverIcon,
                isActive = () => DraftedActionHolder.GetData(pawn).takeCover,
                toggleAction = () => DraftedActionHolder.GetData(pawn).ToggleCoverMode(),
                rightClickAction = () =>
                {
                },
                activateSound = SoundDefOf.Click,
                groupKey = 6173614,
                hotKey = KeyBindingDefOf.Misc4
            };
        }

        private static Command_ToggleWithRClick AddHuntGizmo(Pawn pawn)
        {
            return new Command_ToggleWithRClick
            {
                defaultLabel = "BS_DraftHuntLabel".Translate(),
                defaultDesc = "BS_HuntDescription".Translate(),
                icon = HuntIcon,
                isActive = () => DraftedActionHolder.GetData(pawn).hunt,
                toggleAction = () => DraftedActionHolder.GetData(pawn).ToggleHuntMode(),
                rightClickAction = () =>
                {
                    DraftedActionData data = DraftedActionHolder.GetData(pawn);
                    data.ToggleAutoForAll();
                },
                activateSound = SoundDefOf.Click,
                groupKey = 6173613,
                hotKey = KeyBindingDefOf.Misc3
            };
        }

        [HarmonyPatch(typeof(Command), "GizmoOnGUIInt")]
        [HarmonyPostfix]
        public static void GizmoOnGUIPostfix(Command __instance, GizmoResult __result, Rect butRect, GizmoRenderParms parms)
        {
            if (__instance.GetType() == typeof(Command_Ability))
            {
                var cmd = __instance as Command_Ability;
                var pawn = cmd.Pawn;
                if (IsDraftedPlayerPawn(pawn))
                {
                    var data = DraftedActionHolder.GetData(pawn);
                    if (data.AutoCastFor(cmd.Ability.def))
                    {
                        var size = parms.shrunk ? 12f : 24f;
                        Rect position = new(butRect.x + butRect.width - size, butRect.y, size, size);
                        GUI.DrawTexture(position, AutoCastTex);
                    }
                    if (__result.State == GizmoState.OpenedFloatMenu)
                    {
                        data.ToggleAutoCastFor(cmd.Ability.def);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PriorityWork), nameof(PriorityWork.ClearPrioritizedWorkAndJobQueue))]
        [HarmonyPostfix]
        public static void ClearPrioritizedWorkAndJobQueuePostfix(PriorityWork __instance)
        {
            if (BigSmallMod.settings.autoCombatResets)
            {
                if (!__instance.pawn.Drafted)
                {
                    var pawn = __instance.pawn;
                    if (DraftedActionHolder.GetData(pawn) is DraftedActionData data)
                    {
                        data.hunt = false;
                    }
                }
            }
        }
    }

    public class DraftedActionHolder : GameComponent
    {
        public static Dictionary<string, DraftedActionData> pawnDraftActionData = [];

        public static DraftedActionData GetData(Pawn pawn)
        {
            if (pawnDraftActionData.TryGetValue(pawn.ThingID, out DraftedActionData data))
            {
                return data;
            }
            pawnDraftActionData[pawn.ThingID] = new DraftedActionData(pawn);
            return pawnDraftActionData[pawn.ThingID];
        }

        public DraftedActionHolder(Game game) : base() { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pawnDraftActionData, "draftedActions", LookMode.Value, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!DraftGizmos.AutoCombatEnabled)
                {
                    pawnDraftActionData.Clear();
                }
            }
        }
    }

    public class DraftedActionData : IExposable
    {
        private Pawn pawn = null;
        public string pawnID;
        public bool hunt = false;
        public bool takeCover = false;
        public bool meleeCharge = true;
        public List<AbilityDef> autocastAbilities = new();

        public Pawn Pawn  // Always use this to get the pawn, not the field since it might be null.
        {
            get
            {
                if (pawn == null)
                {
                    // Get pawn using the pawnID
                    var allPawns = Find.Maps?.SelectMany(x => x.mapPawns.AllPawns);
                    // Get all pawns from the game and add them to the cache
                    foreach (var pawn in allPawns.Where(x => x.ThingID == pawnID))
                    {
                        this.pawn = pawn;
                        break;
                    }
                    if (pawn == null)
                    {
                        Log.Error($"DraftedActionData could not find pawn with ID {pawnID}.");
                    }
                }
                return pawn;
            }
        }

        public DraftedActionData(Pawn pawn)
        {
            this.pawn = pawn;
            this.pawnID = pawn.ThingID;
        }

        public DraftedActionData() { }   // For scribe

        private void RefreshDraft()
        {
            Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        public bool ToggleCoverMode()
        {
            takeCover = !takeCover;
            Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            return takeCover;
        }
        public bool ToggleMeleeCharge()
        {
            meleeCharge = !meleeCharge;
            RefreshDraft();
            return meleeCharge;
        }
        public bool ToggleHuntMode()
        {
            hunt = !hunt;
            RefreshDraft();
            return hunt;
        }

        public bool AutoCastFor(AbilityDef def)
        {
            return autocastAbilities.Contains(def);
        }

        public void ToggleAutoForAll()
        {
            if (autocastAbilities.Empty() && Pawn?.abilities?.abilities != null)
            {
                foreach (var ability in Pawn.abilities.abilities)
                {
                    if (ability.def.aiCanUse)
                    {
                        autocastAbilities.Add(ability.def);
                    }
                }
            }
            else autocastAbilities.Clear();
            RefreshDraft();
        }

        public void ToggleAutoCastFor(AbilityDef def)
        {
            if (autocastAbilities.Contains(def))
            {
                autocastAbilities.Remove(def);
            }
            else
            {
                autocastAbilities.Add(def);
            }
            RefreshDraft();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnID, "pawnID");
            Scribe_Values.Look(ref hunt, "huntMode", false);
            Scribe_Values.Look(ref takeCover, "takeCoverMode", false);
            Scribe_Values.Look(ref meleeCharge, "meleeChargeMode", true);
            Scribe_Collections.Look(ref autocastAbilities, "autocastAbilities", LookMode.Def);
        }
    }
}
