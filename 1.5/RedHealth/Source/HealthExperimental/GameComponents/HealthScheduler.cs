using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace RedHealth
{
    public class HealthScheduler : GameComponent
    {
        public static HealthScheduler instance = null;
        public Dictionary<int, List<HealthEvent>> schedule = [];
        private int? launchTick = null;

        public Game game;

        
        public HealthScheduler(Game game)
        {
            this.game = game;
            instance = this;
        }

        public override void GameComponentTick()
        {
            int currentTick = Find.TickManager.TicksGame;
            launchTick ??= currentTick;
            if (currentTick % 500 == 0)
            {
                if (Main.DoCheapLogging && Prefs.DevMode)
                {
                    int? nextEvent = schedule.Keys?.OrderBy(x => x).FirstOrDefault();
                    if (nextEvent != null && schedule.Keys.Count > 0)
                    {
                        Log.Message($"Simulated a total time of {(currentTick - launchTick) / Main.settings.devEventTimeAcceleration * Main.debugTickDivider:f1} days");
                        Log.Message($"HealthScheduler tick is {currentTick}. Next event is at {nextEvent}. {nextEvent - currentTick} ticks away.");
                    }
                }
            }
            if (schedule.ContainsKey(currentTick))
            {
                foreach (var healthEvent in schedule[currentTick])
                {
                    var pawn = healthEvent?.healthComp?.pawn;
                    var hp = healthEvent?.healthComp;
                    // Check if the pawn is invalid, removed from memory, etc. Check if the healthComp is invalid or removed.
                    if (pawn == null || hp == null || pawn.DestroyedOrNull() || pawn.Dead)
                    {
                        continue;
                    }
                    // Check so the pawn still has the healthComp
                    if (pawn.health.hediffSet.hediffs.FirstOrDefault(x => x == hp) == null)
                    {
                        continue;
                    }
                    healthEvent.healthComp.DoHealthEvent(healthEvent.name);
                }
                schedule.Remove(currentTick);
            }
            if (Main.settings.ActiveOnAllPawnsByDefault && currentTick % 60000 == 0)
            {
                AddTrackersNow();
            }
        }

        public static void AddTrackersNow()
        {
            var allPawns = PawnsFinder.AllMapsAndWorld_Alive;
            foreach (var pawn in allPawns)
            {
                if (pawn.health.hediffSet.hediffs.FirstOrDefault(x => x is HealthManager) == null)
                {
                    var hediff = HediffMaker.MakeHediff(HDefs.RED_SecretHealthTracker, pawn) as HealthManager;
                    pawn.health.AddHediff(hediff);
                }
            }
        }
        public static void RemoveAllTrackersNow()
        {
            var allPawns = PawnsFinder.AllMapsAndWorld_Alive;
            foreach (var pawn in allPawns)
            {
                try
                {
                    if (pawn.health.hediffSet.hediffs.FirstOrDefault(x => x is HealthManager) is HealthManager healthManager)
                    {
                        pawn.health.RemoveHediff(healthManager);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to remove HealthManager from {pawn}. {e}");
                }
            }
        }

        public override void LoadedGame()
        {
            if (Main.StandaloneModActive)
            {
                AddTrackersNow();
            }
        }

        public override void ExposeData()
        {
        }
    }
}
