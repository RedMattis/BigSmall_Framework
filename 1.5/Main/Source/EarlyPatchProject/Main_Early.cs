
using Verse;
using HarmonyLib;

namespace BigAndSmall
{
    [StaticConstructorOnStartup]
    internal class BigAndSmall_Early : Mod
    {
        public static BigAndSmall_Early instance = null;
       // public static BSXenoSettings settings;

        public BigAndSmall_Early(ModContentPack content) : base(content)
        {
            instance = this;
            BigSmallMod.settings ??= GetSettings<BSSettings>();
            //settings = GetSettings<BSXenoSettings>();

            ApplyHarmonyPatches();
        }

        static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("RedMattis.BigAndSmall_Early");
            harmony.PatchAll();
        }
    }

    public class BSXenoSettings: ModSettings
    {
        
    }

    


}
