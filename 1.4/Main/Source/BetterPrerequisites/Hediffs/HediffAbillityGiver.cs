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
    public class CompProperties_GiveAbilities : HediffCompProperties
    {
        public List<AbilityDef> abilities;

        public CompProperties_GiveAbilities()
        {
            compClass = typeof(GiveAbilitiesComp);
        }
    }

    public class GiveAbilitiesComp : HediffComp
    {
        // Get properties
        public CompProperties_GiveAbilities Props => (CompProperties_GiveAbilities)props;

        // On comp add, apply the ablities to the pawn
        public override void CompPostMake()
        {
            base.CompPostMake();
            ApplyAbilities();
        }

        // Add abillity on load
        public override void CompExposeData()
        {
            base.CompExposeData();
            ApplyAbilities();
        }

        public void ApplyAbilities()
        {
            // Get the pawn
            Pawn pawn = parent.pawn;
            // Get the abilities
            List<AbilityDef> abilities = Props.abilities;
            // If the pawn is null, return
            if (pawn == null)
            {
                return;
            }
            // If the abilities are null, return
            if (abilities == null)
            {
                return;
            }
            // If the pawn already has the abilities, return
            if (pawn.abilities.abilities.Any(x => abilities.Contains(x.def)))
            {
                return;
            }
            // Add the abilities
            foreach (AbilityDef ability in abilities)
            {
                pawn.abilities.GainAbility(ability);
            }
        }

        // On comp removed, remove the abilities from the pawn
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            RemoveAbilities();
        }

        public void RemoveAbilities()
        {
            // Get the pawn
            Pawn pawn = parent.pawn;
            // Get the abilities
            List<AbilityDef> abilities = Props.abilities;
            // If the pawn is null, return
            if (pawn == null)
            {
                return;
            }
            // If the abilities are null, return
            if (abilities == null)
            {
                return;
            }
            // If the pawn doesn't have the abilities, return
            if (!pawn.abilities.abilities.Any(x => abilities.Contains(x.def)))
            {
                return;
            }

            // Remove the abilities
            foreach (AbilityDef ability in abilities)
            {
                pawn.abilities.RemoveAbility(ability);
            }
        }
    }
}
