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
    }
}
