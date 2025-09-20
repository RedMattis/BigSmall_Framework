using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class HasCustomizableGraphics : DefModExtension
    {
        // Required if put on nonDefs. Otherwise not needed.
        private FlagString tag = null;

        public bool colorA = false;
        public bool colorB = false;
        public bool colorC = false;
        public FlagStringList customFlags = [];

        public FlagString Flag { get => tag; set => tag = value; }

        public override string ToString() => $"[{nameof(HasCustomizableGraphics)}] - Tag: {Flag}, ColorA: {colorA}, ColorB: {colorB}, ColorC: {colorC}";

        public HasCustomizableGraphics TryMerge(HasCustomizableGraphics other)
        {
            if (Flag.TryFuseIdentical(other.Flag) is FlagString newTag)
            {
                return new HasCustomizableGraphics()
                {
                    Flag = newTag,
                    colorA = colorA || other.colorA,
                    colorB = colorB || other.colorB,
                    colorC = colorC || other.colorC,
                    customFlags = [.. customFlags.Union(other.customFlags)]
                };
            }
            return null;
        }
    }
}
