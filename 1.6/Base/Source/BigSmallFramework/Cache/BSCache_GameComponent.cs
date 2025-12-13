using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class BigAndSmallCache : GameComponent
    {
        public static BigAndSmallCache instance = null;
        public static Dictionary<Gene, bool?> frequentUpdateGenes = [];

        private HashSet<BSCache> scribedCache = [];
        public static bool regenerationAttempted = false;
        public Game game;
        /// <summary>
        /// Increments by one every time a new game is started.
        /// 
        /// Starts at 1 so thread caches will be invalid from the start.
        /// </summary>
        public static int gameInt = 1;

        public static Queue<Pawn> refreshQueue = new();

        public static Queue<Action> queuedJobs = new();

        public static Dictionary<int, HashSet<BSCache>> schedulePostUpdate = [];
        public static Dictionary<int, HashSet<BSCache>> scheduleFullUpdate = [];
        public static HashSet<BSCache> currentlyQueued = []; // Ensures we never queue the same cache twice.

        public static float globalRandNum = 1;

        public static HashSet<BSCache> ScribedCache { get => instance.scribedCache; set => instance.scribedCache = value; }

        public BigAndSmallCache(Game game)
        {
            this.game = game;
            instance = this;
        }
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            // Get all pawns registered in the game.
            var allPawns = PawnsFinder.All_AliveOrDead;

            RaceFuser.PostSaveLoadedSetup();
            foreach (var pawn in allPawns.Where(x => x != null && !x.Discarded && !x.Destroyed))
            {
                if (HumanoidPawnScaler.GetCache(pawn, scheduleForce: 1) is BSCache cache) { }
            }
        }

        public void QueueJobOrRunNowIfPaused(Action job)
        {
            if (Find.TickManager?.Paused == true)
            {
                job();
            }
            else
            {
                queuedJobs.Enqueue(job);
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look<BSCache>(ref scribedCache, saveDestroyedThings: false, "BS_scribedCache", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                gameInt++;
                queuedJobs.Clear();
                schedulePostUpdate.Clear();
                scheduleFullUpdate.Clear();
                currentlyQueued.Clear();
                HumanoidPawnScaler.Cache.Clear();
            }
        }

        public override void GameComponentTick()
        {
            BS.IncrementTick();
            int tick = BS.Tick;

            if (queuedJobs.Count > 0)
            {
                var job = queuedJobs.Dequeue();
                job();
            }

			if (schedulePostUpdate.Count > 0)
			{
				if (schedulePostUpdate.TryGetValue(tick, out HashSet<BSCache> values))
				{
					foreach (var cache in values)
					{
						cache?.DelayedUpdate();
					}
					schedulePostUpdate.Remove(tick);
				}
			}

			if (scheduleFullUpdate.Count > 0)
			{
				if (scheduleFullUpdate.TryGetValue(tick, out HashSet<BSCache> values))
				{
					foreach (var cache in values)
					{
						var cPawn = cache?.pawn;
						try
						{
							if (cPawn != null && !cPawn.Discarded)
							{
								HumanoidPawnScaler.GetCache(cPawn, forceRefresh: true);
							}
						}
						finally
						{
							currentlyQueued.Remove(cache);
						}
					}
					scheduleFullUpdate.Remove(tick);
				}
			}

            if (tick % 100 == 0)
            {
                SlowUpdate(tick/100);
            }
        }

        private static void SlowUpdate(int currentTick)
        {
            Rand.PushState(currentTick);
            globalRandNum = Rand.Value;
            Rand.PopState();

            // If the queue is empty, enqueue the HumanoidPawnScaler.Cache.
            if (refreshQueue.Count == 0)
            {
                foreach (BSCache cache in HumanoidPawnScaler.Cache.Values)
                {
                    if (cache.pawn != null)
                        refreshQueue.Enqueue(cache.pawn);
                }
            }
            else
            {
                if (currentTick % 5 == 0)
                {
                    // If the queue is not empty, dequeue the first cache and refresh it.
                    var cachedPawn = refreshQueue.Dequeue();
                    if (cachedPawn != null && (cachedPawn.Spawned || cachedPawn.Corpse?.Spawned == true))
                    {
                        HumanoidPawnScaler.GetCache(cachedPawn, forceRefresh: true);
                    }
                }
            }

            if (BigSmallMod.settings.jesusMode)
            {
                if (currentTick % 10 == 0)
                {
                    // Set all needs to full, and mood to max for testing purposes. Avoids mental breaks when testing, etc.
                    var allPawns = PawnsFinder.AllMapsAndWorld_Alive;
                    foreach (var pawn in allPawns.Where(x => x != null && !x.Discarded && !x.Destroyed))
                    {
                        pawn.needs?.AllNeeds?.ForEach(x => x.CurLevel = x.MaxLevel);
                    }
                }
            }

            bool verySlowUpdate = currentTick % 4 == 0;


			// Remove any entries from frequentUpdateGenes where the key or its pawn is null or discarded
			var toRemove = new List<Gene>();
			Gene[] genes = frequentUpdateGenes.Keys.ToArray();
			foreach (Gene gene in genes)
			{
				Pawn pawn = gene?.pawn;
				if (pawn == null || pawn.Discarded)
				{
					toRemove.Add(gene);
					continue;
				}

				if (pawn.Spawned || pawn.IsColonist)
				{
					var fug = frequentUpdateGenes[gene];
					bool? oldState = fug.Value;

					LockedNeed.UpdateLockedNeeds(gene);
					bool stateChange = gene.Active != oldState;
					if (verySlowUpdate)
					{
						if (stateChange && oldState != null)
						{
							HumanoidPawnScaler.GetInvalidateLater(gene.pawn);
						}
						// This triggers the Transpiler which will check if the gene should be active or not.
						gene.OverrideBy(gene.overriddenByGene);
						if (!stateChange) stateChange = gene.Active != stateChange;
					}
					if (stateChange)
					{
						frequentUpdateGenes[gene] = gene.Active;
						gene.pawn.genes.Notify_GenesChanged(gene.def);
					}
				}
			}

			for (int i = 0; i < toRemove.Count; i++)
				frequentUpdateGenes.Remove(toRemove[i]);
		}
    }
}
