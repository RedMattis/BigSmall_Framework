using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public interface IScoreProvider
    {
        float? GetScore(object obj);
    }

    public interface IScoreHolder
    {
        public IEnumerable<IScoreProvider> Selectors { get; }
        public ScoreCalculator Calculator { get; }

        public float? GetScore(object obj);
        public float GetDefaultValue { get; }
    }
}
