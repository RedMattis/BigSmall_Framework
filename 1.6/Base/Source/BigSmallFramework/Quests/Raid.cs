//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Verse;

//namespace BigAndSmall
//{
//    public class IncidentDefExtension : DefModExtension
//    {
//        private static readonly IncidentDefExtension DefaultValues = new IncidentDefExtension();

//        public float threatMultiplier = 1;
//        public FactionDef forcedFaction;
//        public RaidStrategyDef forcedStrategy;
//        public PawnsArrivalModeDef forcedArrivalMode;

//        public static IncidentDefExtension Get(Def def)
//        {
//            return def.GetModExtension<IncidentDefExtension>() ?? DefaultValues;
//        }
//    }


//    public class IncidentWorker_RaidCustom : IncidentWorker_RaidEnemy
//    {
//        private IncidentDefExtension IncidentDefExtension => IncidentDefExtension.Get(def);

//        protected override bool TryResolveRaidFaction(IncidentParms parms)
//        {
//            if (IncidentDefExtension.forcedFaction == null)
//            {
//                return base.TryResolveRaidFaction(parms);
//            }
//            parms.faction = Find.FactionManager.FirstFactionOfDef(IncidentDefExtension.forcedFaction);

//            return true;
//        }



//        public override void ResolveRaidArriveMode(IncidentParms parms)
//        {
//            base.ResolveRaidArriveMode(parms);
//            if (IncidentDefExtension.forcedArrivalMode != null)
//            {
//                parms.raidArrivalMode = IncidentDefExtension.forcedArrivalMode;
//            }
//        }

//        protected override void ResolveRaidPoints(IncidentParms parms)
//        {
//            parms.points = Find.Storyteller.difficulty.threatScale * IncidentDefExtension.threatMultiplier;
//        }

//        public override void ResolveRaidStrategy(IncidentParms parms, PawnGroupKindDef groupKind)
//        {
//            if (IncidentDefExtension.forcedStrategy == null)
//            {
//                base.ResolveRaidStrategy(parms, groupKind);
//            }
//            else
//            {
//                parms.raidStrategy = IncidentDefExtension.forcedStrategy;
//            }

//            //parms.pawnGroups
//        }
//    }

//}
