using Verse;

namespace BigAndSmall
{
    public partial class BSCache
    {
        public static BSCache defaultCache = new() { isDefaultCache = true };
        public Gender GetApparentGender() => apparentGender ?? pawn.gender;
    }
}
