using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    public class EngulfHediff : HediffWithComps, IThingHolder
    {
        public bool canEject = true;
        public float internalBaseDamage = 10f;
        public float selfDamageMultiplier = 0.2f;
        public Hediff enchumberanceHediff = null;
        public float baseCapacity = 1f; // Multiplied by bodySize
        public DamageDef damageDef = null;
        public const float globalDamageMultiplier = 0.70f;
        public float bodyPartsRegeneratedPerDay = 0;

        public bool alliesAttackBack = true;
        public bool dealsDamage = true;
        public float healPerDay = -1; // hp per day. -1 for no healing
        public float regularHealingMultiplier = -1; // hp per day. -1 for no healing
        public bool healsScars = false;
        public bool canHealBrain = false;
        public const int tickRate = 530;

        // For the "Lamia?" special behavior.
        //public List<string> tags = new List<string>();

        public static float PowScale(float bodySize) => Mathf.Pow(bodySize, 1.4f);

        public float MaxCapacity { get => baseCapacity * PowScale(pawn.BodySize); }
        public float Fullness => TotalMass / MaxCapacity;

        public bool HealsInner => healPerDay > -0.5 || regularHealingMultiplier > -0.5f;

        public float TotalMass =>
            innerContainer.Where(x => x is Pawn).Sum(x => PowScale(((Pawn)x).BodySize)) +
            innerContainer.Where(x => x is Corpse).Sum(x => PowScale(((Corpse)x).InnerPawn.BodySize*0.5f));

        public Hediff EnchumberanceHediff
        {
            get
            {
                if (enchumberanceHediff == null)
                {
                    enchumberanceHediff = GetEnchumberedHediff();
                    return enchumberanceHediff;
                }
                return enchumberanceHediff;
            }
            set => enchumberanceHediff = value;
        }

        public IThingHolder ParentHolder => pawn;

        public ThingOwner innerContainer;

        public bool HasAnyContents => innerContainer.Count > 0;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public bool Engulf(Thing thing)
        {
            if (thing.Spawned == false)
            {
                return false;
            }

            thing.DeSpawnOrDeselect();
            bool flag;

            if (thing is Pawn engulfedPawn && engulfedPawn.IsColonist)
            {
                Messages.Message("BS_EngulfedColonist".Translate(pawn.LabelShort, engulfedPawn.LabelShort), pawn, MessageTypeDefOf.NegativeHealthEvent);
            }
            if (thing.holdingOwner != null)
            {
                thing.holdingOwner.TryTransferToContainer(thing, innerContainer, thing.stackCount);
                flag = true;
            }
            else
            {
                flag = innerContainer.TryAdd(thing);
            }
            EnchumberanceHediff.Severity = Fullness;
            return flag;
        }

        public Hediff GetEnchumberedHediff()
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BS_EngulfedEnchumberance");
            if (hediffDef == null)
            {
                Log.Error("Could not find hediff with name " + "BS_EngulfedEnchumberance");
                return null;
            }
            // Check if we already have the hediff
            if (pawn.health.hediffSet.HasHediff(hediffDef))
            {
                enchumberanceHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            }
            else
            {
                pawn.health.AddHediff(hediffDef);
                enchumberanceHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            }
            return enchumberanceHediff;
        }

        // Append the contents of the container to the description
        public override string GetTooltip(Pawn pawn, bool showHediffsDebugInfo)
        {
            var baseTooltip = base.GetTooltip(pawn, showHediffsDebugInfo);
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(baseTooltip);
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("BS_EngulfedContents".Translate());
            foreach (var thing in innerContainer)
            {
                if (thing is Pawn innerPawn)
                {
                    // get inner pawn overal health percentage
                    float healthPercent = innerPawn.health.summaryHealth.SummaryHealthPercent;
                    stringBuilder.AppendLine(thing.LabelCap + $" ({healthPercent * 100}%" + (innerPawn.Downed ? ", downed" : "") + ")");
                }
                else
                {
                    stringBuilder.AppendLine(thing.LabelCap + $"{thing.HitPoints / thing.MaxHitPoints * 100}%");
                }
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        // On death, eject the contents of the container
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            IList<Thing> contents = innerContainer;
            for (int i = contents.Count - 1; i >= 0; i--)
            {
                Thing thing = contents[i];
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.MapHeld, ThingPlaceMode.Near);
            }
        }

        // On removal, eject the contents of the container
        public override void PostRemoved()
        {
            base.PostRemoved();
            IList<Thing> content = innerContainer;

            for (int i = content.Count - 1; i >= 0; i--)
            {
                Thing thing = content[i];
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.MapHeld, ThingPlaceMode.Near);
            }
            // Remove the hediff that tracks the total mass of the contents
            try { pawn.health.RemoveHediff(EnchumberanceHediff); }
            catch { }
        }

        public override string LabelInBrackets
        {
            get
            {
                // Return pawn BodySize / BodySize of contents to percent.
                try
                {
                    return Fullness.ToStringPercent();
                }
                catch
                {
                    return "FULLNESS CALCULATION FAILED";
                }
            }
        }
        // Deal damage to the contents of the container every X ticks
        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % tickRate == 0)
            {
                if (innerContainer.Count() == 0)
                {
                    return;
                }
                if (damageDef == null)
                {
                    damageDef = DefDatabase<DamageDef>.GetNamed("BS_AcidDmgDirectQuiet");
                    if (damageDef == null)
                    {
                        damageDef = DefDatabase<DamageDef>.GetNamed("AcidBurn");
                    }
                }

                float digestionEffiency = 1;

                // Check if the pawn has Metabolism Capacity. If not it is probably a mechanoid or something, so just use default.
                bool hasDigestionCap = pawn.health.capacities.CapableOf(BSDefs.Metabolism);
                if (hasDigestionCap)
                {
                    // Get pawn metabolism capacity
                    digestionEffiency = pawn.health.capacities.GetLevel(BSDefs.Metabolism);
                    if (digestionEffiency > 1f)
                    {
                        digestionEffiency += (digestionEffiency - 1) * 3; // If digestion is above 100%, increase damage further, because the pawn likely has a nuclear furnace for a stomach or something.
                    }
                }
                bool ejectDownedPrisoner = pawn.health.Downed && pawn.IsPrisonerOfColony;
                bool inSeriousPain = pawn?.health?.hediffSet?.PainTotal > 0.5f;
                bool stomachIntact = true;
                bool torsoIntact = true;

                // Get stomach part, if it exists or is destroyed, get the torso.
                var stomach = pawn.RaceProps.body.AllParts.FirstOrDefault(x => x.def.defName == "Stomach");
                var torso = pawn.RaceProps.body.AllParts.FirstOrDefault(x => x.def.defName == "Torso");
                if (stomach == null)
                {
                    stomach = torso;
                }
                else
                {
                    // Check the health on the stomach part of the pawn and transfer damage to the torso if it is below 60%.
                    var stomachHealth = pawn.health.hediffSet.GetPartHealth(stomach);
                    if (stomachHealth < stomach.def.GetMaxHealth(pawn) * 0.3f)
                    {
                        stomach = torso;
                        stomachIntact = false;
                    }
                    if (torso != null)
                    {
                        // Check torso
                        var torsoHealth = pawn.health.hediffSet.GetPartHealth(torso);
                        if (torsoHealth < torso.def.GetMaxHealth(pawn) * 0.3f)
                        {
                            torsoIntact = false;
                        }
                    }
                }

                // If a pawn can't vomit they will take damage to the torso instead. If the torso is destroyed, the contents will spill out.
                if (ejectDownedPrisoner || (inSeriousPain || digestionEffiency < 0.51f || stomachIntact == false)
                    && canEject || !torsoIntact || Fullness > 1.4f)
                {
                    // Remove this Hediff
                    pawn.health.RemoveHediff(this);
                    pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Vomit), JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);

                    // Send message
                    Messages.Message("BS_EngulfedVomit".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeHealthEvent);
                    return;
                }


                bool containsCorpse = innerContainer.Any(x => x is Corpse);
                bool containsPawn = innerContainer.Any(x => x is Pawn);
                if ((containsPawn || containsCorpse) && !HealsInner)
                {
                    // Add tiny amount of nutrition to the pawn to prevent starvation and get people to quit complaining that they only get hunger
                    // when the corpse is destroyed.
                    pawn.needs.food.CurLevel += pawn.needs.food.FoodFallPerTick * tickRate * 1.20f;
                }
                else if (containsPawn && HealsInner)
                {
                    // Drain nutrition faster if the pawn is healing an inner pawn.
                    pawn.needs.food.CurLevel += -pawn.needs.food.FoodFallPerTick * tickRate * -0.5f;
                }

                List<Thing> toRemove = [];
                foreach (var thing in innerContainer)
                {
                    if (thing is Pawn innerPawn)
                    {
                        if (SanguophageUtility.ShouldBeDeathrestingOrInComaInsteadOfDead(innerPawn))
                        {
                            innerPawn.Kill(new DamageInfo(damageDef, 999 * digestionEffiency, armorPenetration: 100, instigator: pawn, intendedTarget: thing, spawnFilth: false));
                        }
                        else if (!innerPawn.IsColonist && innerPawn.health.summaryHealth.SummaryHealthPercent < 0.1f)
                        {
                            innerPawn.Kill(new DamageInfo(damageDef, 999 * digestionEffiency, armorPenetration: 100, instigator: pawn, intendedTarget: thing, spawnFilth: false));
                        }

                        if (dealsDamage)
                        {
                            AttackInnerThing(digestionEffiency, stomachIntact, thing);
                        }
                        else if (HealsInner)
                        {
                            HealInner(innerPawn);
                        }

                        // If innerPawn is not downed.
                        if (!innerPawn.health.Downed && !innerPawn.health.ShouldBeDead() && !innerPawn.health.ShouldBeDowned())
                        {
                            try
                            {
                                // Check if pawn is an enemy and isn't a prisoner or slave. If the faction is null we'll assume it's an enemy.
                                bool isEnemy = innerPawn.Faction == null || pawn.Faction == null ||
                                    (pawn.Faction != null && pawn.Faction.HostileTo(innerPawn.Faction) && !innerPawn.IsSlaveOfColony && !innerPawn.IsPrisonerOfColony);

                                if (alliesAttackBack || isEnemy)
                                {
                                    AttackPossessor(stomach, innerPawn);
                                }

                                // If the pawn is dead, delay the removal of this hediff so it goes into the corpse-processing step.
                                if (innerPawn.Dead)
                                {
                                    Severity += 0.2f;
                                }
                            }
                            catch (Exception e)
                            {
                                // Whatever if this fails, it's not critical.
                                //Log.Error("Error in BS_HediffComp_Engulfed.CompPostTick(): " + e);
                            }
                        }
                    }
                    // if Thing is indestructible expell it
                    else if (thing.def.destroyable == false || thing.def.useHitPoints == false)
                    {
                        toRemove.Add(thing);
                    }
                    else
                    {
                        // Deal much less damage to items unless they are corpses
                        if (thing is Corpse)
                        {
                            thing.TakeDamage(new DamageInfo(damageDef, 5 * digestionEffiency, armorPenetration: 100, instigator: pawn, intendedTarget: thing, spawnFilth: false));
                        }
                        else
                        {
                            thing.TakeDamage(new DamageInfo(damageDef, 1 * digestionEffiency, armorPenetration: 100, instigator: pawn, intendedTarget: thing, spawnFilth: false));
                        }
                    }
                    // It is is a corpse, destroy it
                    if (thing.HitPoints < 20 && thing is Corpse corpse)
                    {
                        DestroyCorpse(corpse);
                    }
                }
                // Remove indestructible things using reverse for loop and place them on the ground.
                for (int i = toRemove.Count - 1; i >= 0; i--)
                {
                    innerContainer.Remove(toRemove[i]);
                    GenPlace.TryPlaceThing(toRemove[i], pawn.Position, pawn.Map, ThingPlaceMode.Near);
                }

            }
            EnchumberanceHediff.Severity = Fullness;
        }

        private int countDownToRegenerate = 0;
        private void HealInner(Pawn innerPawn)
        {
            const int ticksPerDay = 60000;
            // Heal amount is the healingRate * tickrate / ticks per day "1" amount means healing 1 hp of injuryies per day.
            float healAmount = healPerDay * tickRate / ticksPerDay;
            healAmount = (innerPawn.BodySize > 1 ? innerPawn.HealthScale : 1) * pawn.BodySize;

            // Add the Pawns' regular healing amount to the heal amount.
            var hFactor = innerPawn.GetStatValue(StatDefOf.InjuryHealingFactor);
            if (regularHealingMultiplier > 1) { hFactor *= regularHealingMultiplier; }

            healAmount += innerPawn.HealthScale * 0.01f * hFactor * regularHealingMultiplier;

            var hediffSet = innerPawn.health.hediffSet;

            // Tend all the wounds on the pawn
            foreach (var tendableInjury in hediffSet.hediffs.Where(x => (x is Hediff_Injury || x is Hediff_MissingPart) && x.TendableNow(ignoreTimer: true)))
            {
                tendableInjury.Tended(0.15f, 1, 1);
            }

            // Get a random injury (if any)
            var injuries = hediffSet.hediffs.Where(x => x is Hediff_Injury && (!x.IsPermanent() || healsScars)).ToList();

            // Removes Permanent injuries from the list if they are on the brain.
            if (!canHealBrain)
            {
                var brain = hediffSet.GetBrain();
                injuries = injuries.Where(x => !(x?.Part?.def.defName == brain?.def.defName && x.IsPermanent())).ToList();
            }
            if (injuries.Count > 0)
            {
                var injury = injuries.RandomElement();
                injury.Heal(healAmount);
            }

            // Get missing body parts and heal them
            var missingParts = hediffSet.hediffs.Where(x => x is Hediff_MissingPart).ToList();
            if (missingParts.Count > 0)
            {
                countDownToRegenerate++;

                if (bodyPartsRegeneratedPerDay > 0.001)
                {
                    if (countDownToRegenerate > ticksPerDay / tickRate / bodyPartsRegeneratedPerDay) // 24 is a placeholder
                    {
                        var missingPart = missingParts.RandomElement();
                        innerPawn.health.RestorePart(missingPart.Part);
                        countDownToRegenerate = 0;
                        // Message that the part has regenerated
                        //Messages.Message("BS_EngulfedPartRegenerated".Translate(innerPawn.LabelShort, missingPart.Part.def.label), innerPawn, MessageTypeDefOf.PositiveEvent);
                    }
                }
                return;
            }
        }

        public void DestroyCorpse(Corpse corpse)
        {
            ProcessCorpseDestruction(pawn, corpse.InnerPawn);
            // Add all items carried by the corpse to the container
            if (corpse?.InnerPawn?.inventory?.innerContainer != null)
            {
                List<Thing> inventoryList = corpse.InnerPawn.inventory.innerContainer.ToList();
                for (int i = inventoryList.Count - 1; i >= 0; i--)
                {
                    Thing item = inventoryList[i];
                    Engulf(item);
                }
                corpse.InnerPawn.inventory.innerContainer.Clear();
            }
            if (corpse?.InnerPawn?.apparel?.WornApparel != null)
            {
                // Add all apparel worn by the corpse to the container
                List<Apparel> worn = corpse.InnerPawn.apparel.WornApparel;
                for (int i = worn.Count - 1; i >= 0; i--)
                {
                    Apparel apparel = worn[i];
                    Engulf(apparel);
                }
                //corpse.InnerPawn.apparel.WornApparel.Clear();
            }
            if (corpse?.InnerPawn?.equipment?.AllEquipmentListForReading != null)
            {
                // Add all equipment carried by the corpse to the container
                List<ThingWithComps> equipmentList = corpse.InnerPawn.equipment.AllEquipmentListForReading.ToList();
                for (int i = equipmentList.Count - 1; i >= 0; i--)
                {
                    ThingWithComps equipment = equipmentList[i];
                    Engulf(equipment);
                }
                //corpse.InnerPawn.equipment = null;
            }

            // Destroy the corpse
            corpse.Destroy();
        }

        private void AttackInnerThing(float digestionEffiency, bool stomachIntact, Thing thing)
        {
            var dmgType = damageDef;
            // 20% chance of crushing just so we don't end up with something acid-proof in there forever.
            if (Rand.Chance(0.20f)) { dmgType = DamageDefOf.Crush; }

            bool guilty = true;

            float damageAmount = internalBaseDamage * digestionEffiency * globalDamageMultiplier;

            if (thing is Pawn innerPawn)
            {
                if (innerPawn.ageTracker.CurLifeStageIndex == 0) { guilty = false; }

                float sizeRatio = pawn.BodySize / innerPawn.BodySize;

                if (sizeRatio > 2)
                {
                    float mult = (sizeRatio - 1) / 2 + 1;
                    damageAmount *= mult;
                }
            }

            if (!stomachIntact)
            {
                // Only relevant for True Lamia. Further reduce digestion dramatically if the stomach is destroyed.
                // They are unable to vomit, so they still need to deal some damage to the target.
                thing.TakeDamage(new DamageInfo(dmgType, damageAmount * 0.66f, instigatorGuilty: guilty,
                    armorPenetration: 100, instigator: pawn, intendedTarget: thing, spawnFilth: false));
            }
            else
            {
                thing.TakeDamage(new DamageInfo(dmgType, damageAmount, instigatorGuilty: guilty,
                    armorPenetration: 100, instigator: pawn, intendedTarget: thing, spawnFilth: false));
            }
        }

        private void AttackPossessor(BodyPartRecord targetPart, Pawn innerPawn)
        {
            // Get a random attack from the inner pawns' melee verbs
            var targetAttack = innerPawn.meleeVerbs.GetUpdatedAvailableVerbsList(false).MaxBy(x => x.verb.verbProps.AdjustedMeleeDamageAmount(x.verb, innerPawn));
            var randomAttack = innerPawn.meleeVerbs.GetUpdatedAvailableVerbsList(false).RandomElement();

            // Check if the innerPawn has melee skill, if so, use that as a multiplier for the damage.
            float damageFromSkills = 1;
            if (innerPawn?.skills?.GetSkill(SkillDefOf.Melee)?.Level > 0)
            {
                int skill = innerPawn.skills.GetSkill(SkillDefOf.Melee).Level;
                if (skill <= 4)
                {
                    damageFromSkills = 0.75f;
                }
                else if (skill <= 7)
                {
                    damageFromSkills = 0.85f;
                }
                else if (skill <= 10)
                {
                    damageFromSkills = 1.0f;
                }
                else if (skill <= 14)
                {
                    damageFromSkills = 1.15f;
                }
                else if (skill <= 17)
                {
                    damageFromSkills = 1.30f;
                }
                else if (skill <= 20)
                {
                    damageFromSkills = 1.50f;
                }
            }

            // Randomly pick one of the two attacks
            if (Rand.Chance(0.6f))
            {
                targetAttack = randomAttack;
            }

            // Get damage and damage type of the attack
            var damage = targetAttack.verb.verbProps.AdjustedMeleeDamageAmount(targetAttack.verb, innerPawn);
            var damageType = targetAttack.verb.verbProps.meleeDamageDef;
            bool canInterruptJobs = damageType.canInterruptJobs;
            bool makesBlood = damageType.makesBlood;

            // If damage is less than 1/16 of the part health when adjusted for incomming damage multiplier, abort.
            if (damage < targetPart.def.GetMaxHealth(pawn) / pawn.GetStatValue(StatDefOf.IncomingDamageFactor) / 16)
            {
                return;
            }

            float idd = HumanoidPawnScaler.GetCacheUltraSpeed(pawn, canRegenerate: false)?.internalDamageDivisor ?? 1;

            damageType.canInterruptJobs = false;
            damageType.makesBlood = false;
            pawn.TakeDamage(new DamageInfo(damageType, damage * selfDamageMultiplier * damageFromSkills * globalDamageMultiplier / idd, intendedTarget: pawn, hitPart: targetPart, armorPenetration: 500,
                    instigatorGuilty: false, instigator: innerPawn, spawnFilth: false));

            damageType.canInterruptJobs = canInterruptJobs;
            damageType.makesBlood = makesBlood;
        }

        protected virtual void ProcessCorpseDestruction(Pawn attacker, Pawn innerPawn)
        {
            if (attacker.needs?.food != null && innerPawn?.BodySize != null)
            {
                attacker.needs.food.CurLevel += 6 * innerPawn.BodySize;
            }
            try
            {
                GetEatenCorpseMeatThoughts(attacker, innerPawn);
            }
            catch (Exception e)
            {
                Log.Warning("Error adding through after destroying corpse: " + e);
            }

            if (attacker.needs?.TryGetNeed<Need_KillThirst>() is Need_KillThirst killThirst)
            {
                killThirst.CurLevelPercentage = 1;
            }
        }

        public static void GetEatenCorpseMeatThoughts(Pawn attacker, Pawn target)
        {
            if (target.RaceProps.Humanlike)
            {
                Thought_Memory thought_Memory;
                if (attacker?.story?.traits?.HasTrait(BSDefs.Cannibal) ?? false)
                {
                    thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(BSDefs.AteHumanlikeMeatDirectCannibal);
                    attacker.mindState.lastHumanMeatIngestedTick = Find.TickManager.TicksGame;
                    attacker?.needs?.mood?.thoughts?.memories?.TryGainMemory(thought_Memory);
                }
                else if (attacker?.ideo?.Ideo is Ideo ideo)
                {
                    if (ideo.HasPrecept(PreceptDefOf.Cannibalism_Preferred) ||
                        ideo.HasPrecept(PreceptDefOf.Cannibalism_RequiredRavenous) ||
                        ideo.HasPrecept(PreceptDefOf.Cannibalism_RequiredStrong) ||
                        ideo.HasPrecept(BSDefs.Cannibalism_Acceptable))
                    {
                        attacker.mindState.lastHumanMeatIngestedTick = Find.TickManager.TicksGame;
                    }
                }
                else
                {
                    thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(BSDefs.AteHumanlikeMeatDirect);
                    attacker?.needs?.mood?.thoughts?.memories?.TryGainMemory(thought_Memory);
                }
                foreach(var gene in GeneHelpers.GetAllActiveGenes(attacker))
                {
                    gene.Notify_IngestedThing(target, 1);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look(ref selfDamageMultiplier, "selfDamageMultiplier", 1f);
            Scribe_Values.Look(ref internalBaseDamage, "internalBaseDamage", 1f);
            Scribe_Values.Look(ref canEject, "canEject", true);
            Scribe_Values.Look(ref baseCapacity, "maxCapacity", 1f);
            Scribe_Values.Look(ref alliesAttackBack, "alliesAttackBack", true);
            Scribe_Values.Look(ref dealsDamage, "dealsDamage", true);
            Scribe_Values.Look(ref healPerDay, "healPerDay", -1);
            Scribe_Values.Look(ref regularHealingMultiplier, "regularHealingMultiplier", -1);
            Scribe_Values.Look(ref healsScars, "healsScars", false);
            Scribe_Values.Look(ref canHealBrain, "canHealBrain", false);
            Scribe_Values.Look(ref bodyPartsRegeneratedPerDay, "bodyPartsRegeneratedPerDay", 0);
            Scribe_Defs.Look(ref damageDef, "damageDef");
        }
    }
}
