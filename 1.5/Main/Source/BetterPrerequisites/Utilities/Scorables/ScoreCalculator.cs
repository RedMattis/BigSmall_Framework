using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class ScoreCalculator(IScoreHolder scorableList)
    {
        protected IScoreHolder parent = scorableList;

        /// <summary>
        /// This is the default implementation of the score calculator.
        /// </summary>
        /// <param name="obj">Object to calculate the score for.</param>
        /// <returns>Highest value, if any. Else null</returns>
        public virtual float? GetScoreFor(object obj)
        {
            if (parent.Selectors.EnumerableNullOrEmpty())
            {
                return parent.GetDefaultValue;
            }
            var scores = parent.Selectors.Select(item => item.GetScore(obj)).Where(filterScore => filterScore != null);
            return scores.Any() ? scores.Max() : null;
        }

        public static IScoreHolder GetBestScored<T>(object obj, List<IScoreHolder> list) where T : IScoreHolder
        {
            return GetSortedScores(obj, list)?.FirstOrDefault();
        }

        public static List<IScoreHolder> GetSortedScores(object obj, IEnumerable<IScoreHolder> list)
        {
            List<(float score, IScoreHolder item)> elements = [];
            foreach (var item in list)
            {
                Log.Message($"Item: {item}");
                var score = item.Calculator.GetScoreFor(obj);
                if (score != null)
                {
                    Log.Message($"Score: {score}");
                    elements.Add((score.Value, item));
                }
            }
            if (elements.Count != 0)
            {
                Log.Message($"Elements: {elements.Count}");
                return [.. elements.OrderByDescending(element => element.score).Select(element => element.item)];
            }
            return null;
        }
    }
}
