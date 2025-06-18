using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    /// <summary>
    /// This is a class which contains one or more IScoreThing objects which acts as components for a score.
    /// 
    /// By default the highest single score is returned.
    /// 
    /// If is basically a non-def version of ScorableDef.
    /// </summary>
    public abstract class Scoreable : IScoreHolder, IScoreProvider
    {
        private ScoreCalculator _calculator = null;

        public abstract IEnumerable<IScoreProvider> Selectors { get; }
        public virtual ScoreCalculator Calculator => _calculator ??= new ScoreCalculator(this);

        public virtual float? GetScore(object obj) => Calculator.GetScoreFor(obj);

        public virtual float GetDefaultValue => float.MinValue+1;
    }
}
