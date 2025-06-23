using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class BS
    {
        public static BSSettings Settings => BigSmallMod.settings;

        private static bool? prePatcherActive = null;
        private static int internalTick = 0;
        private static int internalTick10 = 0;
        private static int internalTick100 = 0;

        /// <summary>
        /// Used when you need to make sure ticks aren't randomly skipped. Thanks Ludeon or whatever mod causes this. Ó_ò
        /// </summary>
        public static int Tick { get => internalTick; }
        public static int Tick10 { get => internalTick10; }
        public static int Tick100 { get => internalTick100; }
        public static bool PrePatcherActive => prePatcherActive ??= ModsConfig.IsActive("zetrith.prepatcher");
        public static void IncrementTick()
        {
            if (internalTick == int.MaxValue)
            {
                internalTick = 0;
            }

            internalTick += 1;
            internalTick10 = internalTick /10;
            internalTick100 = internalTick / 100;
        }
        public static void SetTick(int tick) => internalTick = tick;
    }
}
