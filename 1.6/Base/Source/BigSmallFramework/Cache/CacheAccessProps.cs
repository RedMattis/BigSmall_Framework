using Verse;

namespace BigAndSmall
{
    public partial class BSCache
    {
        public static BSCache defaultCache = new() { isDefaultCache = true };

        /// <summary>
        /// For use by the Prepatcher.
        /// </summary>
        public static BSCache GetDefaultCache() => defaultCache;
        public Gender GetApparentGender() => apparentGender ?? pawn.gender;
    }
}
