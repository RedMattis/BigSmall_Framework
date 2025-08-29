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
        public FlagString tag = null;

        public bool colorA = false;
        public bool colorB = false;
        public bool colorC = false;

        public override string ToString() => $"[{nameof(HasCustomizableGraphics)}] - Tag: {tag}, ColorA: {colorA}, ColorB: {colorB}, ColorC: {colorC}";

        public HasCustomizableGraphics TryMerge(HasCustomizableGraphics other)
        {
            if (tag.TryFuseIdentical(other.tag) is FlagString newTag)
            {
                return new HasCustomizableGraphics()
                {
                    tag = newTag,
                    colorA = colorA || other.colorA,
                    colorB = colorB || other.colorB,
                    colorC = colorC || other.colorC
                };
            }
            return null;
        }
    }
}
