using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    /// <summary>
    /// Just copy pasting this from the "Femboy" mod for the simple reason that "Femboy" is a too controversial term to have as a dependency.
    /// </summary>
    [DefOf]
    public static class BSDefs
    {
        public static StatDef SM_BodySizeOffset;
        public static StatDef SM_Cosmetic_BodySizeOffset;
        public static StatDef SM_BodySizeMultiplier;
        public static StatDef SM_HeadSize_Cosmetic;
        public static StatDef SM_Minimum_Learning_Speed;
        public static StatDef SM_Food_Need_Capacity;
        public static StatDef SM_AttackSpeed;
        public static StatDef SM_UnarmedAttackSpeed;

        public static TraitDef BS_Gentle;
        public static GeneDef Body_Androgynous;
        public static GeneDef Body_MaleOnly;
        public static GeneDef Body_FemaleOnly;
        public static HediffDef VU_SuccubusBond;
        public static HediffDef VU_SuccubusBond_Victim;
        public static HediffDef VU_WhiteRoseBite;
        public static HediffDef VU_WhiteRoseThrall;
        public static HediffDef VU_Euphoria;
        public static HediffDef VU_DraculVampirism;
        public static HediffDef VU_DraculAge;
        public static HediffDef VU_AnimalReturned;
        public static HediffDef VU_DraculAnimalVampirism;
        public static WeaponClassDef BS_GiantWeapon;

        public static MentalStateDef BS_LostManhunter;
        public static MentalStateDef BS_LostManhunterPermanent;

        [MayRequire("RedMattis.BigSmall.Core")]
        public static DamageDef BS_BiteDevourDmg;

        // Organs
        public static ThingDef Heart;
        public static ThingDef Lung;
        public static ThingDef Kidney;
        public static ThingDef Liver;




        public static IncidentDef StrangerInBlackJoin; // Def of the existing incident for easy acccess.
        //public static IncidentDef WomanInBlueJoin;

        static BSDefs()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BSDefs));
        }

    }
}
