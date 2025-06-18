using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    // Patch Beauty and Ugly thoughts
    [HarmonyPatch(typeof(ThoughtWorker_Pretty), "CurrentSocialStateInternal", new Type[]
    {
        typeof(Pawn), typeof(Pawn)
    })]
    public static class CurrentSocialStateInternal_Patch
    {
        public static void Postfix(ref ThoughtState __result, Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                __result = false; return;
            }
            if (RelationsUtility.IsDisfigured(other, pawn))
            {
                __result = false; return;
            }
            if (PawnUtility.IsBiologicallyOrArtificiallyBlind(pawn))
            {
                __result = false; return;
            }
            try
            {
                float statValue = other.GetStatValue(StatDefOf.PawnBeauty);
                if (statValue >= 4f)
                {
                    __result = ThoughtState.ActiveAtStage(3);
                }
                else if (statValue >= 3f)
                {
                    __result = ThoughtState.ActiveAtStage(2);
                }
                else if (statValue >= 2f)
                {
                    __result = ThoughtState.ActiveAtStage(1);
                }
                else if (statValue >= 1f)
                {
                    __result = ThoughtState.ActiveAtStage(0);
                }
            }
            catch
            {
                return;
            }
        }
    }

    [HarmonyPatch(typeof(ThoughtWorker_Ugly), "CurrentSocialStateInternal", new Type[]
    {
        typeof(Pawn), typeof(Pawn)
    })]
    public static class ThoughtWorker_Ugly_Patch
    {
        public static void Postfix(ref ThoughtState __result, Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                __result = false; return;
            }
            if (PawnUtility.IsBiologicallyOrArtificiallyBlind(pawn))
            {
                __result = false; return;
            }
            if (pawn.story.traits.HasTrait(TraitDefOf.Kind))
            {
                __result = false; return;
            }
            try
            {
                float statValue = other.GetStatValue(StatDefOf.PawnBeauty);
                if (statValue <= -4f)
                {
                    __result = ThoughtState.ActiveAtStage(3);
                }
                else if (statValue <= -3f)
                {
                    __result = ThoughtState.ActiveAtStage(2);
                }
                else if (statValue <= -2f)
                {
                    __result = ThoughtState.ActiveAtStage(1);
                }
                else if (statValue <= -1f)
                {
                    __result = ThoughtState.ActiveAtStage(0);
                }
            }
            catch
            {
                return;
            }
        }
    }

    [HarmonyPatch(typeof(RelationsUtility))]
    [HarmonyPatch("IsDisfigured")]
    public static class IsDisfigured_Patch
    {
        [HarmonyPostfix]
        public static void RemoveDisfigurement(ref bool __result, Pawn pawn)
        {
            if (HumanoidPawnScaler.GetBSDict(pawn) is BSCache cache)
            {
                if (cache.preventDisfigurement)
                {
                    __result = false;
                }
            }
        }
    }



    // Move this elsewhere later...
    [HarmonyPatch(typeof(FiringIncident), MethodType.Constructor, new Type[]
    {
        typeof(IncidentDef),
        typeof(StorytellerComp),
        typeof(IncidentParms),
    })]
    public static class FiringIncident_Patch
    {
        public static void Postfix(FiringIncident __instance)
        {
            // Finalise stranger in black pawn kind
            if (__instance.def == BSDefs.StrangerInBlackJoin)
            {
                // Check the player wealth
                float playerWealth = Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal;

                // get a value between 50000 and 150000
                int randomWealth = Rand.Range(20000, 100000);

                Log.Message($"Player wealth is {playerWealth}. Required (random) wealth to trigger Woman in blue is {randomWealth}.");
                if (playerWealth > randomWealth)
                {
                    IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed("BS_WomanInBlueJoin");
                    if (incidentDef != null)
                        Find.Storyteller.incidentQueue.Add(incidentDef, 4500, __instance.parms);
                }
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_WandererJoin), "GeneratePawn")]
    public static class TryExecuteWorker
    {
        public static void Postfix(Pawn __result, IncidentWorker_WandererJoin __instance)
        {
            var incidentDef = __instance.def;
            bool isWomanInBlue = incidentDef == DefDatabase<IncidentDef>.GetNamed("BS_WomanInBlueJoin");

            if (isWomanInBlue)
            {
                var pawnKind = incidentDef.pawnKind;
                // Get the pawn
                Pawn pawn = __result;

                // Remove all apparel
                pawn.apparel.DestroyAll();

                // Remove Tattoos
                pawn.style.BodyTattoo = TattooDefOf.NoTattoo_Body;
                pawn.style.FaceTattoo = TattooDefOf.NoTattoo_Face;

                // Edit Nickname
                //pawn.Name.Named("Skadi");
                //var oldName = (NameTriple)pawn.Name;

                var validNameList = new List<NameTriple>
                {
                    new NameTriple("Skadi", "Skadi", "Huntress"),
                    new NameTriple("Angrboda ", "Angra ", "Jarnvid"),
                    new NameTriple("Gerd ", "Gerd ", "Evergreen")
                };

                while (validNameList.Count > 0)
                {
                    var name = validNameList.RandomElement();
                    if (name.UsedThisGame) { validNameList.Remove(name); }
                    else
                    {
                        pawn.Name = name;
                        if (name.First == "Skadi")
                        {
                            pawn.story.traits.GainTrait(new Trait(TraitDefOf.SpeedOffset, 2));
                            pawn.skills.GetSkill(SkillDefOf.Melee).Level = Rand.Range(9, 18);
                            pawn.skills.GetSkill(SkillDefOf.Shooting).Level = Rand.Range(10, 16);
                            pawn.skills.GetSkill(SkillDefOf.Shooting).passion = Passion.Major;
                            pawn.skills.GetSkill(SkillDefOf.Plants).Level = Rand.Range(4, 16);
                        }
                        else if (name.First == "Angrboda")
                        {
                            pawn.Name = name;
                            pawn.skills.GetSkill(SkillDefOf.Melee).Level = Rand.Range(10, 16);
                            pawn.skills.GetSkill(SkillDefOf.Melee).passion = Passion.Major;
                            pawn.story.traits.GainTrait(new Trait(TraitDefOf.Tough));
                            var gTrait = pawn.story.traits.GetTrait(DefDatabase<TraitDef>.GetNamed("BS_Gentle"));
                            pawn.story.traits.RemoveTrait(gTrait);
                            pawn.genes.AddGene(DefDatabase<GeneDef>.GetNamed("Fertile"), false);
                        }
                        else if (name.First == "Gerd")
                        {
                            pawn.Name = name;
                            pawn.story.traits.GainTrait(new Trait(TraitDefOf.Beauty, 2));
                            pawn.skills.GetSkill(SkillDefOf.Plants).Level = 20;
                            pawn.skills.GetSkill(SkillDefOf.Plants).passion = Passion.Major;
                            pawn.skills.GetSkill(SkillDefOf.Social).Level = Rand.Range(12, 20);
                            pawn.skills.GetSkill(SkillDefOf.Social).passion = Passion.Minor;
                            pawn.skills.GetSkill(SkillDefOf.Medicine).Level = Rand.Range(10, 20);
                            pawn.skills.GetSkill(SkillDefOf.Medicine).passion = Passion.Minor;
                        }
                        break;
                    }
                }

                if (pawn.needs?.food?.CurLevelPercentage != null)
                {
                    pawn.needs.food.CurLevelPercentage = 0.75f;
                }

                // Add the equipment from the pawnkind
                foreach (var equipment in pawnKind.apparelRequired)
                {
                    Apparel apparel;
                    if (equipment.defName != "Apparel_Pants")
                    {
                        apparel = (Apparel)ThingMaker.MakeThing(equipment, ThingDefOf.Leather_Plain);
                    }
                    else
                    {
                        apparel = (Apparel)ThingMaker.MakeThing(equipment, ThingDefOf.Steel);
                    }
                    if (apparel.TryGetQuality(out QualityCategory quality))
                    {
                        apparel.TryGetComp<CompQuality>().SetQuality(QualityCategory.Good, ArtGenerationContext.Outsider);
                    }
                    // Check color
                    if (apparel.def.colorGenerator != null)
                    {
                        apparel.SetColor(__instance.def.pawnKind.favoriteColor.Value, false);
                    }
                    pawn.apparel.Wear(apparel);
                }
                
            }
        }
    }
}