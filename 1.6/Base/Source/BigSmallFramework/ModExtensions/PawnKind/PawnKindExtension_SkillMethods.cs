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
    public partial class PawnKindExtension : DefModExtension
    {
        public void ModifySkills(Pawn pawn)
        {
            if (pawn.skills?.skills == null)
            {
                if (skillRange != null || clampedSkills != null || forcedPassions != null || canHavePassions != null)
                {
                    Log.Warning($"PawnKindExtension for {pawn} tried to modify skills but they have no skills.");
                }
            }
            if (canHavePassions == false)
            {
                foreach (var skill in pawn.skills.skills)
                {
                    skill.passion = Passion.None;
                }
            }
            if (forcedPassions != null)
            {
                foreach (var fPassion in forcedPassions)
                {
                    foreach (var pawnSkill in pawn.skills.skills.Where(x => x.def == fPassion.skill))
                    {
                        if (fPassion.passion is Passion newPassion)
                        {
                            pawnSkill.passion = newPassion;

                            if (ModsConfig.BiotechActive)
                            {
                                foreach (var gene in GeneHelpers.GetAllActiveGenes(pawn))
                                {
                                    if (gene.def.passionMod is PassionMod passionMod && passionMod.skill == fPassion.skill)
                                    {
                                        gene.passionPreAdd = newPassion;
                                    }
                                }
                            }
                        }
                        for (int i = 0; i < Math.Abs(fPassion.incrementBy); i++)
                        {
                            if (fPassion.incrementBy > 0)
                            {
                                pawnSkill.passion = pawnSkill.passion.IncrementPassion();
                                //Log.Message($"Incremented passion of {pawn} for {pawnSkill.def} to {pawnSkill.passion}");
                            }
                            else if (fPassion.incrementBy < 0)
                            {
                                pawnSkill.passion = (Passion)Math.Max((int)Passion.None, (int)pawnSkill.passion - 1);
                            }
                        }
                    }
                }
            }

            if (skillRange != null
                && (skillRangeApplyToBabies || pawn.ageTracker?.CurLifeStage != LifeStageDefOf.HumanlikeBaby)
                )
            {
                foreach ((var skill, var range) in skillRange.Select(x => (x.Skill, x.Range)))
                {
                    foreach (var pawnSkill in pawn.skills.skills)
                    {
                        if (pawnSkill.def != skill) continue;
                        var randomLevel = Rand.RangeInclusive(range.min, range.max);
                        pawnSkill.Level = randomLevel;
                    }
                }
            }


            if (clampedSkills != null
                && (skillRangeApplyToBabies || pawn.ageTracker?.CurLifeStage != LifeStageDefOf.HumanlikeBaby)
                )
            {
                foreach ((var skill, var range) in clampedSkills.Select(x => (x.Skill, x.Range)))
                {
                    foreach (var pawnSkill in pawn.skills.skills)
                    {
                        if (pawnSkill.def != skill) continue;
                        var learnedLevel = pawnSkill.GetLevel(includeAptitudes: false);
                        if (learnedLevel < range.min)
                        {
                            pawnSkill.Level = range.min;
                        }
                        else if (learnedLevel > range.max)
                        {
                            pawnSkill.Level = range.max;
                        }
                    }
                }
            }
        }

    }
}
