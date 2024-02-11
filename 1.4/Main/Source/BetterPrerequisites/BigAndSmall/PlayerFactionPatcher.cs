//using HarmonyLib;
//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml;
//using Verse;

//namespace BigAndSmall
//{

//    public class FactionExtension : DefModExtension
//    {
//        public List<string> pawnKindsToSwapFaction = new List<string>();
//        public List<string> incidentToSwapPawnkind = new List<string>();
//        public PawnKindDef wandererPawnkind = null;
//        public bool forcePlayerXenotypes = true;
//    }

//    [HarmonyPatch]
//    public static class PlayerFactionPatcher
//    {
//        [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
//        [HarmonyPostfix]
//        public static void FinalizeInitPostFix()
//        {
//            RunDefsReplacements();
//            Log.Message($"Debug: Finalize Map Load Run...");
//        }

//        public static void RunDefsReplacements()
//        {
//            var allPawnKinds = DefDatabase<PawnKindDef>.AllDefsListForReading;

//            Log.Message($"Debug: Checking if we should be running defs replacements...");

//            if (BigSmallMod.settings.patchPlayerFactions && allPawnKinds.Where(x => x.defName == "BS_PlayerWanderer").FirstOrDefault() is PawnKindDef wanderer)
//            {
//                Log.Message($"Debug: Running defs replacements...");
//                var playerFaction = Faction.OfPlayer;
//                wanderer.defaultFactionType = playerFaction.def;
                
//                var pawnKindNamesToSwitchFaction = new List<string>()
//                {
//                    "VFET_Wildperson"
//                };
                
//                List<string> incidentNamesToSwitchFaction = new List<string>()
//                {
//                    "WandererJoin",
//                };

//                bool forceFactionXenotypes = false;

//                // Check for FactionExtension on the player faction
//                if (playerFaction.def.HasModExtension<FactionExtension>())
//                {
//                    var ext = playerFaction.def.GetModExtension<FactionExtension>();
//                    if (ext.pawnKindsToSwapFaction.Count > 0)
//                    {
//                        pawnKindNamesToSwitchFaction = ext.pawnKindsToSwapFaction;
//                    }
//                    if (ext.incidentToSwapPawnkind.Count > 0)
//                    {
//                        incidentNamesToSwitchFaction = ext.incidentToSwapPawnkind;
//                    }
//                    if (ext.wandererPawnkind != null)
//                    {
//                        // Replace the default wanderer with the forced pawnkind
//                        wanderer = ext.wandererPawnkind;
//                    }
//                    forceFactionXenotypes = ext.forcePlayerXenotypes;

//                    Log.Message($"Debug: did replacements from ModExt on Player Faction.");
//                }

//                // Switch faction of some pawnkinds
//                var matchingPawnKinds = allPawnKinds.Where(x => pawnKindNamesToSwitchFaction.Contains(x.defName)).ToList();
//                foreach(var kind in matchingPawnKinds)
//                {
//                    Log.Message($"DEBUG: Changing faction of {kind.defName} from {kind.defaultFactionType} to {playerFaction.def}.");
//                    kind.defaultFactionType = playerFaction.def;
//                }

//                // Switch Pawnkinds of some incidents
//                var matchingIncidents = DefDatabase<IncidentDef>.AllDefsListForReading.Where(x => incidentNamesToSwitchFaction.Contains(x.defName)).ToList();
//                foreach (var incident in matchingIncidents)
//                {
//                    Log.Message($"DEBUG: Changing pawnkind of {incident.defName} from {incident.pawnKind} to {wanderer.defName}.");
//                    incident.pawnKind = wanderer;
//                }

//                if (forceFactionXenotypes)
//                {
//                    // Force all pawns in the player faction to be xenotypes
//                   foreach (var kind in matchingPawnKinds)
//                    {
//                        Log.Message($"Debug: Setting xenotype for {kind.defName} from {kind.xenotypeSet} to {playerFaction.def.xenotypeSet}");
//                        kind.xenotypeSet = playerFaction.def.xenotypeSet;
//                    }

//                    Log.Message($"Debug: Forcing player faction xenotypes.");
//                }

//                Log.Message($"DEBUG: Count of matching pawnkinds: {matchingPawnKinds.Count}");
//                Log.Message($"DEBUG: Count of matching incidents: {matchingIncidents.Count}");
//            }

//        }
//    }
//}
