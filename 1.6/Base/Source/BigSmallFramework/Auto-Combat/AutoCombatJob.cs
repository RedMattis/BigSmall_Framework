using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using Verse.AI.Group;
using System.Security.Cryptography;
using UnityEngine.Networking;

namespace BigAndSmall
{
    public class JobGiver_AutoCombat : JobGiver_AIFightEnemy
    {
        public DraftedActionData actionData = null;
        public bool Hunt => actionData.hunt;
        protected override bool OnlyUseAbilityVerbs => !actionData.hunt;
        protected override bool OnlyUseRangedSearch => false;

        public List<AbilityDef> blacklist = [];

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_AutoCombat obj = (JobGiver_AutoCombat)base.DeepCopy(resolve);
            return obj;
        }

        protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
        {
            return TryFindShootinPositionInner(pawn, out dest, verbToUse);
        }

        protected bool TryFindShootinPositionInner(Pawn pawn, out IntVec3 dest, Verb verbToUse, bool requestNewPos=false)
        {
            dest = pawn.Position;
            if (Hunt)
            {
                Thing enemyTarget = pawn.mindState.enemyTarget;
                CastPositionRequest newReq = default;
                newReq.caster = pawn;
                newReq.target = enemyTarget;
                newReq.wantCoverFromTarget = actionData.takeCover;
                newReq.preferredCastPosition = requestNewPos ? null : pawn.Position;
                if (verbToUse == null && CanTargetWithAbillities(pawn, enemyTarget, out Ability ability))
                {

                    newReq.verb = ability.verb;
                    newReq.maxRangeFromTarget = ability.verb.verbProps.range - 0.5f;
                    return CastPositionFinder.TryFindCastPosition(newReq, out dest);
                }
                else
                {
                    newReq.verb = verbToUse;
                    newReq.maxRangeFromTarget = verbToUse.verbProps.range - 0.5f;
                    return CastPositionFinder.TryFindCastPosition(newReq, out dest);
                }
            }
            return true;
        }

        protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
        {
            if (pawn?.Drafted != true) return false;

            if (base.ExtraTargetValidator(pawn, target))
            {
                return Hunt || CanTargetWithAbillities(pawn, target, out _);
            }
            return false;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn?.Drafted != true)
            {
                return null;
            }

            actionData = DraftedActionHolder.GetData(pawn);
            if (!Hunt)
            {
                if (actionData.autocastAbilities.Empty() || pawn.abilities?.abilities.NullOrEmpty() == false)
                {
                    // If we're not in hunt mode, and there are literally no valid abilities, just let it continue to the regular indefinite wait job.
                    return null;
                }
                if (pawn.abilities.abilities.All(ability => !ability.CanCast ||
                    blacklist.Contains(ability.def)))
                {
                    return GetWaitForTimeJob(pawn, 100);
                }
            }

            var hostility = pawn.playerSettings.hostilityResponse;
            pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;   // Faster than messing around with all the checks against this.

            var draftedJob = GiveDraftedHuntJob(pawn);

            pawn.playerSettings.hostilityResponse = hostility;
            if (draftedJob != null)
            {
                draftedJob.checkOverrideOnExpire = true;
                return draftedJob;
            }
            return GetWaitForTimeJob(pawn, 100);
        }

        protected static Job GetWaitForTimeJob(Pawn pawn, int ticks)
        {
            var waitForABit = JobMaker.MakeJob(JobDefOf.Wait_Combat, pawn.Position);
            waitForABit.expiryInterval = ticks;  // Set it so the job will start from the top after expiring
            waitForABit.checkOverrideOnExpire = true;
            return waitForABit;
        }

        protected override bool ShouldLoseTarget(Pawn pawn)
        {
            Thing enemyTarget = pawn.mindState.enemyTarget;
            float targetKeepRadius = actionData.fullAIControl ? 999 : this.targetKeepRadius;
            if (enemyTarget.Destroyed || Find.TickManager.TicksGame - pawn.mindState.lastEngageTargetTick > TicksSinceEngageToLoseTarget)
            {
                return true;
            }
            if ((enemyTarget as IAttackTarget)?.ThreatDisabled(pawn) == true || (enemyTarget is Pawn targetPawn && targetPawn.DeadOrDowned))
            {
                return true;
            }
            if (actionData.fullAIControl)
            {
                if (!pawn.CanReach(enemyTarget, PathEndMode.Touch, Danger.Deadly, canBashDoors: true))
                {
                    return true;
                }
            }
            else
            {
                if (!pawn.CanReach(enemyTarget, PathEndMode.Touch, Danger.Deadly)
                    || !((pawn.Position - enemyTarget.Position).LengthHorizontalSquared > targetKeepRadius * targetKeepRadius))
                {
                    return true;
                }
            }
            if (!Hunt || !CanTargetWithAbillities(pawn, pawn.mindState.enemyTarget, out _))
            {
                return true;
            }

            return false;
        }


        protected bool CanTargetWithAbillities(Pawn pawn, Thing target, out Ability pickedAbility)
        {
            pickedAbility = null;
            if (pawn.Drafted == false || pawn.abilities?.abilities == null)
            {
                return false;
            }
            foreach (var ability in pawn.abilities.abilities)
            {
                pickedAbility = CanTargetWithAbility(target, ability);
                if (pickedAbility != null)
                {
                    return true;
                }
            }
            return false;
        }

        private Ability CanTargetWithAbility(Thing target, Ability ability)
        {
            if (!ability.CanCast)
            {
                return null;
            }
            if (blacklist.Contains(ability.def))
            {
                return null;
            }
            if (!ability.def.verbProperties.targetParams.CanTarget(target))
            {
                return null;
            }
            if (!ability.CanApplyOn((LocalTargetInfo)target))
            {
                return null;
            }
            if (ability.def.aiCanUse == true)
            {
                if (!ability.AICanTargetNow(target))
                {
                    return null;
                }
            }

            bool aiCanUse = ability.def.aiCanUse;
            // Hack to get the AI to use abilities that the player selected even though not marked as AI usable.
            bool canUseNow = false;
            try
            {
                ability.def.aiCanUse = true;
                canUseNow = ability.AICanTargetNow(target);
            }
            finally
            {
                ability.def.aiCanUse = aiCanUse;
            }
            if (canUseNow)
            {
                return ability;
            }

            return null;
        }

        protected Job GiveDraftedHuntJob(Pawn pawn)
        {
            UpdateEnemyTarget(pawn);
            Thing enemyTarget = pawn.mindState.enemyTarget;
            if (enemyTarget == null)
            {
                return null;
            }

            if (enemyTarget is Pawn enemyPawn && (enemyPawn.IsPsychologicallyInvisible() || enemyPawn.DeadOrDowned))
            {
                return null;
            }

            Job abilityJob = GetAbilityJob(pawn, enemyTarget);
            if (abilityJob != null)
            {
                return abilityJob;
            }

            // If not on hunt mode we want to stop here.
            if (!Hunt) return null;


            // We could toggle this on, but we would probably need to use a whitelist or something to prevent triple rocket launchers and such from being used.
            Verb verb = TryGetAttackVerb(pawn, enemyTarget, allowManualCastWeapons: false);
            if (verb == null)
            {
                return null;
            }

            if (verb.verbProps.IsMeleeAttack)
            {
                if (!actionData.fullAIControl && !actionData.meleeCharge)
                {
                    // Check distance to foe, if we'd need to move more than 2 tiles, don't melee charge.
                    const int maxDist = 3 * 3;
                    if ((pawn.Position - enemyTarget.Position).LengthHorizontalSquared > maxDist) 
                    {
                        return JobMaker.MakeJob(JobDefOf.Wait_Combat, 100, checkOverrideOnExpiry: true);
                    }
                }
                var meleeJob = MeleeAttackJob(pawn, enemyTarget);  // Make sure they still re-check for abilities, and etc.
                meleeJob.checkOverrideOnExpire = true;
                meleeJob.expiryInterval = 100;
                meleeJob.canBashDoors = actionData.fullAIControl;
                return meleeJob;
            }
            bool takeCover = actionData.takeCover;
            float coverAmount = takeCover ? 0.24f : 0.0f;
            float coverAmountFound = CoverUtility.CalculateOverallBlockChance(pawn, enemyTarget.Position, pawn.Map);
            bool coverOkay = CoverUtility.CalculateOverallBlockChance(pawn, enemyTarget.Position, pawn.Map) >= coverAmount;
            bool standable = pawn.Position.Standable(pawn.Map) && pawn.Map.pawnDestinationReservationManager.CanReserve(pawn.Position, pawn, pawn.Drafted);
            bool canHitTarget = verb.CanHitTarget(enemyTarget);
            float verbRange = verb.verbProps.range;
            bool closeEnough = (pawn.Position - enemyTarget.Position).LengthHorizontalSquared < verbRange * verbRange;

           if (coverOkay && standable && canHitTarget && closeEnough)
            {
                return JobMaker.MakeJob(JobDefOf.Wait_Combat, ExpiryInterval_ShooterSucceeded.RandomInRange / 3, checkOverrideOnExpiry: true);
            }

            if (!TryFindShootingPosition(pawn, out var shootingPos, verbToUse: verb))
            {
                if (TryMeleeAttackJob(pawn, enemyTarget) is Job meleeJob)
                    return meleeJob;

                return JobMaker.MakeJob(JobDefOf.Wait_Combat, 100, checkOverrideOnExpiry: true);
            }

            if (shootingPos == pawn.Position)
            {
                if (takeCover && !coverOkay)
                {
                    if (TryFindShootinPositionInner(pawn, out var newShootingPos, verbToUse: verb, requestNewPos: true) && newShootingPos != pawn.Position)
                    {
                        return MakeGotoJob(newShootingPos);
                    }
                    else
                    {
                        if (TryMeleeAttackJob(pawn, enemyTarget) is Job meleeJob)
                            return meleeJob;
                    }
                }
                return JobMaker.MakeJob(JobDefOf.Wait_Combat, ExpiryInterval_ShooterSucceeded.RandomInRange / 3, checkOverrideOnExpiry: true);
            }
            return MakeGotoJob(shootingPos);
        }

        protected virtual Job TryMeleeAttackJob(Pawn pawn, Thing enemyTarget)
        {
            if (actionData.fullAIControl)
            {
                // If we can't find a shooting position, and we're allowed to bash doors, try meleeing.
                // Only one attack though to prevent actually moving into melee after doors are down.
                var meleeJob = MeleeAttackJob(pawn, enemyTarget);
                meleeJob.checkOverrideOnExpire = true;
                meleeJob.expiryInterval = 100;
                meleeJob.canBashDoors = true;
                meleeJob.maxNumMeleeAttacks = 1;
                return meleeJob;
            }
            return null;
        }

        protected static Job MakeGotoJob(IntVec3 shootingPos)
        {
            Job goToShootingPos = JobMaker.MakeJob(JobDefOf.Goto, shootingPos);
            goToShootingPos.expiryInterval = ExpiryInterval_ShooterSucceeded.RandomInRange/3;
            goToShootingPos.checkOverrideOnExpire = true;
            return goToShootingPos;
        }

        protected override void UpdateEnemyTarget(Pawn pawn)
        {
            Thing thing = pawn.mindState.enemyTarget;
            if (thing != null && ShouldLoseTarget(pawn))
            {
                thing = null;
            }

            if (thing == null)
            {
                thing = FindAttackTargetIfPossible(pawn);
                if (thing != null)
                {
                    Notify_EngagedTarget(pawn);
                    pawn.GetLord()?.Notify_PawnAcquiredTarget(pawn, thing);
                }
            }
            else
            {
                Thing thing2 = FindAttackTargetIfPossible(pawn);
                if (thing2 == null && !chaseTarget)
                {
                    thing = null;
                }
                else if (thing2 != null && thing2 != thing)
                {
                    Notify_EngagedTarget(pawn);
                    thing = thing2;
                }
            }

            pawn.mindState.enemyTarget = thing;
            Pawn enemy;
            if ((enemy = thing as Pawn) != null && thing.Faction == Faction.OfPlayer && pawn.Position.InHorDistOf(thing.Position, 40f) && !enemy.IsShambler && !pawn.IsPsychologicallyInvisible())
            {
                Find.TickManager.slower.SignalForceNormalSpeed();
            }
        }

        protected void Notify_EngagedTarget(Pawn pawn)  // This is marked internal for some reason...
        {
            pawn.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;
        }

        protected new Thing FindAttackTargetIfPossible(Pawn pawn)
        {
            if (pawn.TryGetAttackVerb(null, true) == null)
            {
                return null;
            }

            if (actionData.fullAIControl)
            {
                return FindAttackTargetAnywhere(pawn);
            }
            else
            {
                return FindAttackTarget(pawn);
            }
        }

        public virtual Thing FindAttackTargetAnywhere(Pawn pawn)
        {
            var flags = TargetScanFlags.NeedReachable | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
            return (Thing)AttackTargetFinder.BestAttackTarget(pawn, flags, IsGoodTarget, 0f, 900, default, float.MaxValue, canBashDoors: true);
        }

        public virtual bool IsGoodTarget(Thing thing)
        {
            if (!PlayerCanSeeThing(thing))
            {
                return false;
            }
            if (thing is Pawn pawn && !pawn.Downed)
            {
                return !pawn.IsPsychologicallyInvisible();
            }
            return false;
        }

        public static bool PlayerCanSeeThing(Thing thing)
        {
            if (!thing.Spawned)
            {
                return false;
            }
            if (thing.MapHeld != null && !thing.Fogged())
            {
                return true;
            }
            return false;
        }


        // Use our own method for more aggressive behaviour.
        public Verb TryGetAttackVerb(Pawn pawn, Thing target, bool allowManualCastWeapons = false, bool allowOnlyManualCastWeapons = false)
        {
            if (allowManualCastWeapons) // Unlike the vanilla method, we make this PREFER manual cast stuff if enabled. Let's blow those cooldowns and charges!
            {
                if (pawn.equipment?.Primary != null && pawn.equipment.PrimaryEq.PrimaryVerb.Available() && pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.onlyManualCast)
                {
                    return pawn.equipment.PrimaryEq.PrimaryVerb;
                }
                if (allowManualCastWeapons && pawn.apparel != null && pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.onlyManualCast)
                {
                    Verb firstApparelVerb = pawn.apparel.FirstApparelVerb;
                    if (firstApparelVerb != null && firstApparelVerb.Available())
                    {
                        return firstApparelVerb;
                    }
                }
            }
            if (allowOnlyManualCastWeapons) return null;

            if (pawn.equipment?.Primary != null && pawn.equipment.PrimaryEq.PrimaryVerb.Available() && (!pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.onlyManualCast))
            {
                return pawn.equipment.PrimaryEq.PrimaryVerb;
            }

            if (pawn.kindDef.canMeleeAttack)
            {
                return pawn.meleeVerbs.TryGetMeleeVerb(target);
            }

            return null;
        }

        private new Job GetAbilityJob(Pawn pawn, Thing enemyTarget)
        {
            if (pawn.abilities == null)
            {
                return null;
            }

            List<Ability> autoCastAbilities = [.. pawn.abilities.AllAbilitiesForReading.Where(x => actionData.autocastAbilities.Contains(x.def))];

            if (TrySelfBuff(pawn, autoCastAbilities) is Job selfBuff)
            {
                return selfBuff;
            }
            if (TryOffesnsiveAbility(pawn, enemyTarget, autoCastAbilities) is Job offensiveJob)
            {
                return offensiveJob;
            }

            return null;
        }

        private Job TrySelfBuff(Pawn pawn, List<Ability> abilities)
        {
            var selfTarget = new LocalTargetInfo(pawn);
            var selfBuffs = abilities.Where(ability => ability.verb.targetParams.canTargetSelf
                && ability.CanCast
                && (ability.def.aiCanUse == false || ability.AICanTargetNow(pawn))
            ).ToList();
            if (selfBuffs.Any())
            {
                var tempAbilityJob = selfBuffs.RandomElement().GetJob(selfTarget, selfTarget);

                return tempAbilityJob;
            }
            return null;
        }

        private Job TryOffesnsiveAbility(Pawn pawn, Thing enemyTarget, List<Ability> abilities)
        {
            List<Ability> list = pawn.abilities.AICastableAbilities(enemyTarget, offensive: true);
            list = [.. list.Where(abilities.Contains)];
            if (list.NullOrEmpty())
            {
                return null;
            }
            // Filter all abilites not on the whitelist.
            list = [.. list.Where(ability => CanTargetWithAbility(enemyTarget, ability) != null && (actionData.AutoCastFor(ability.def)))];
            if (!list.NullOrEmpty())
            {
                if (pawn.Position.Standable(pawn.Map) && pawn.Map.pawnDestinationReservationManager.CanReserve(pawn.Position, pawn, pawn.Drafted))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].verb.CanHitTarget(enemyTarget))
                        {
                            return list[i].GetJob(enemyTarget, enemyTarget);
                        }
                    }

                    for (int j = 0; j < list.Count; j++)
                    {
                        LocalTargetInfo localTargetInfo = list[j].AIGetAOETarget();
                        if (localTargetInfo.IsValid)
                        {
                            return list[j].GetJob(localTargetInfo, localTargetInfo);
                        }
                    }

                    for (int k = 0; k < list.Count; k++)
                    {
                        if (list[k].verb.targetParams.canTargetSelf)
                        {
                            return list[k].GetJob(pawn, pawn);
                        }
                    }
                }
            }
            return null;
        }
    }
}
