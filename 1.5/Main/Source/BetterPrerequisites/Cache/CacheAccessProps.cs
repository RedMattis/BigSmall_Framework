using Verse;

namespace BigAndSmall
{
    public partial class BSCache
    {
        public Gender GetApparentGender() => apparentGender ?? pawn.gender;
    }
}
