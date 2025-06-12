using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class GeneStealDef : ScorableDef
    {
        public List<ScoreKey> selectors = [];
        public List<GeneDef> genes = [];
        public override IEnumerable<IScoreProvider> Selectors => selectors;
        public static ScorableDef GetBestGenesOnPawn(Pawn pawn) => GetBestScoredDef<GeneStealDef>(pawn);
    }
}
