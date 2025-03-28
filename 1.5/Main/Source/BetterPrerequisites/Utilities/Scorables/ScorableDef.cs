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
    /// If is basically a def version of Scorable.
    /// </summary>
    public abstract class ScorableDef : Def, IScoreHolder
    {
        public Type scoreCalculatorType = typeof(ScoreCalculator);
        private ScoreCalculator _calculator = null;

        public abstract IEnumerable<IScoreProvider> Selectors { get; }
        public virtual ScoreCalculator Calculator => _calculator ??= (ScoreCalculator)Activator.CreateInstance(scoreCalculatorType, this);

        public virtual float? GetScore(object obj) => Calculator.GetScoreFor(obj);
        public virtual float GetDefaultValue => float.MinValue + 1;

        public static ScorableDef GetBestScoredDef<T>(object obj) where T : ScorableDef =>
            GetSortedScoredDefs<T>(obj)?.FirstOrDefault();

        public static List<T> GetSortedScoredDefs<T>(object obj) where T : ScorableDef
        {
            var allDefs = DefDatabase<T>.AllDefsListForReading.Cast<IScoreHolder>();
            if (allDefs.Any())
            {
                Log.Message($"ScorableList: {allDefs.Count()}");
                return [.. ScoreCalculator.GetSortedScores(obj, allDefs).Cast<T>()];
            }
            return null;
        }
    }
}
