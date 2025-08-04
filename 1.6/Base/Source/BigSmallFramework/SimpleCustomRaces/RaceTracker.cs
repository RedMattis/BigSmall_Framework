using System;
using System.Collections.Generic;
using Verse;

namespace BigAndSmall
{
    public class RaceTracker : HediffWithComps
    {
        public override bool Visible => true;
        private List<PawnExtension> pawnExtensions = null;
        public List<PawnExtension> PawnExtensions => pawnExtensions ??= def.ExtensionsOnDef<PawnExtension, HediffDef>();

        public override void PostAdd(DamageInfo? info)
        {
            // Ensure these are not set to true.
            def.isBad = false;
            def.everCurableByItem = false;

            base.PostAdd(info);
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
        }
        public override void PostTick() { }
        public override void Tick() { }
        public override void Tended(float quality, float maxQuality, int batchPosition = 0) { }

        public override float PainOffset { get { return 0; } }
    }

}
