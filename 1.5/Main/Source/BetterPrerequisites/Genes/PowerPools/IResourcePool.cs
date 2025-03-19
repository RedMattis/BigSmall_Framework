using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public interface IResourcePool
    {
        public Pawn Pawn { get; }
        public string Label { get; }
        public float TargetValue { get; set; }
        public float Value { get; set; }
        public float Max { get; set; }
        public float ValueForDisplay { get; }
        public float MaxForDisplay { get; }
        public int Increments { get; }
        public float ValuePercent { get; }
        public void SetTargetValuePct(float value);
    }
}
