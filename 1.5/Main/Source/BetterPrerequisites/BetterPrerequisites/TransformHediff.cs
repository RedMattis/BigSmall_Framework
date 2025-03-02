using BigAndSmall;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BetterPrerequisites
{
    public class CompProperties_ColorAndFur : HediffCompProperties
    {
        private List<Color> skinColorOverride = null;
        private List<Color> hairColorOverride = null;
        public FurDef furskinOverride = null;
        public bool skinIsHairColor = false;
        
        protected BodyTypeDef bodyDefOverride = null;
        protected BodyTypeDef bodyDefOverride_Female = null;
        protected List<BodyTypeDef> bodyDefOverrideList = [];
        protected List<BodyTypeDef> bodyDefOverrideList_Female = [];
        
        protected HeadTypeDef headDefOverride = null;
        protected HeadTypeDef headDefOverride_Female = null;
        protected List<HeadTypeDef> headDefOverrideList = [];
        protected List<HeadTypeDef> headDefOverrideList_Female = [];

        public bool disableFacialAnims = false;
        public bool disableBeards = false;
        public bool disableHair = false;
        public bool hideHead = false;
        public bool hideBody = false;

        private List<BodyTypeDef> BodyDefs => bodyDefOverride == null ? bodyDefOverrideList : [.. bodyDefOverrideList, bodyDefOverride];
        private List<BodyTypeDef> BodyDefsFemale => bodyDefOverride_Female == null ? bodyDefOverrideList_Female : [.. bodyDefOverrideList_Female, bodyDefOverride_Female];
        private List<HeadTypeDef> HeadDefs => headDefOverride == null ? headDefOverrideList : [.. headDefOverrideList, headDefOverride];
        private List<HeadTypeDef> HeadDefsFemale => headDefOverride_Female == null ? headDefOverrideList_Female : [.. headDefOverrideList_Female, headDefOverride_Female];

        public List<BodyTypeDef> BodyTypeDefs(Gender targetGender) => (targetGender == Gender.Female && BodyDefsFemale.Any()) ? BodyDefsFemale : BodyDefs;
        public List<HeadTypeDef> HeadTypeDefs(Gender targetGender) => (targetGender == Gender.Female && HeadDefsFemale.Any()) ? HeadDefsFemale : HeadDefs;


        // If the body is hidden, we simply set the colors to fully transparent
        public List<Color> SkinColorOverride => skinColorOverride; //{ get => hideBody ? [new(0, 0, 0, 0)] : skinColorOverride; set => skinColorOverride = value;}
        public List<Color> HairColorOverride => hairColorOverride; //{ get => hideBody ? [new(0, 0, 0, 0)] : hairColorOverride; set => hairColorOverride = value; }
        
        public CompProperties_ColorAndFur()
        {
            compClass = typeof(HediffComp_ColorAndFur);
        }
    }

    public class HediffComp_ColorAndFur : HediffComp
    {
        public CompProperties_ColorAndFur CRProps => (CompProperties_ColorAndFur)props;

        
        public override void CompPostMake()
        {
            base.CompPostMake();
            Pawn pawn = parent?.pawn;
            
            if (FastAcccess.GetCache(pawn) is BSCache cache)
            {
                if (pawn.story != null)
                {
                    cache.savedSkinColor = pawn.story?.SkinColor;
                    cache.savedHairColor = pawn.story?.HairColor;
                    cache.savedFurSkin = pawn.story?.furDef?.defName;
                    cache.savedBodyDef = pawn.story?.bodyType?.defName;
                    cache.savedHeadDef = pawn.story?.headType?.defName;
                    cache.savedBeardDef = pawn.style?.beardDef?.defName;
                }
                var targetGender = cache.GetApparentGender();
                if (CRProps.HairColorOverride != null)
                {
                    if (cache.randomPickHairColor == null || cache.randomPickHairColor >= CRProps.HairColorOverride.Count - 1)
                    {
                        cache.randomPickHairColor = Rand.Range(0, CRProps.HairColorOverride.Count - 1);
                    }
                    pawn.story.HairColor = CRProps.HairColorOverride[cache.randomPickHairColor.Value];
                }
                if (CRProps.skinIsHairColor)
                {
                    if (pawn.story.HairColor.a < 0.05f)
                    {
                        // Just set it to brown
                        pawn.story.HairColor = new Color(0.3f, 0.2f, 0.1f, 1f);
                    }
                    var hairColor = pawn.story.HairColor;
                    hairColor.a = 1f;
                    pawn.story.skinColorOverride = pawn.story.HairColor;
                }
                else if (CRProps.SkinColorOverride != null)
                {
                    if (cache.randomPickSkinColor == null || cache.randomPickSkinColor >= CRProps.SkinColorOverride.Count -1)
                    {
                        cache.randomPickSkinColor = Rand.Range(0, CRProps.SkinColorOverride.Count - 1);
                    }
                    pawn.story.skinColorOverride = CRProps.SkinColorOverride[cache.randomPickSkinColor.Value];
                }
                var bodyTypes = CRProps.BodyTypeDefs(targetGender);
                if (pawn.story != null && bodyTypes.Any() && !bodyTypes.Contains(pawn.story.bodyType))
                {
                    using (new RandBlock(pawn.thingIDNumber))
                    {
                        pawn.story.bodyType = bodyTypes.RandomElement();
                    }
                }
                var headTypes = CRProps.HeadTypeDefs(targetGender);
                if (pawn.story != null && headTypes.Any() && !headTypes.Contains(pawn.story.headType))
                {
                    using (new RandBlock(pawn.thingIDNumber))
                    {
                        pawn.story.headType = headTypes.RandomElement();
                    }
                }
                
                if (CRProps.furskinOverride != null)
                {
                    pawn.story.furDef = CRProps.furskinOverride;
                    //pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                if (CRProps.disableFacialAnims)
                {
                    cache.facialAnimationDisabled_Transform = true;
                }
                if (CRProps.disableBeards)
                {
                    pawn.style.beardDef = null;
                }
                if (CRProps.disableHair)
                {
                    pawn.story.hairDef = HairDefOf.Bald;
                }
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Pawn pawn = parent?.pawn;
            if (FastAcccess.GetCache(pawn) is BSCache cache)
            {
                if (pawn.story != null)
                {
                    if (cache.savedSkinColor != null && cache.savedSkinColor.Value.a > 0.05)
                    {
                        pawn.story.skinColorOverride = cache.savedSkinColor.Value;
                    }
                    if (cache.savedHairColor != null && cache.savedHairColor.Value.a > 0.05)
                    {
                        pawn.story.HairColor = cache.savedHairColor.Value;
                    }
                    if (cache.savedFurSkin != null && DefDatabase<FurDef>.GetNamed(cache.savedFurSkin) is FurDef furDef)
                    {
                        pawn.story.furDef = furDef;
                        //pawn.Drawer.renderer.SetAllGraphicsDirty();
                    }
                    if (cache.savedBodyDef != null && DefDatabase<BodyTypeDef>.GetNamed(cache.savedBodyDef) is BodyTypeDef bodyDef)
                    {
                        pawn.story.bodyType = bodyDef;
                    }
                    if (cache.savedHeadDef != null && DefDatabase<HeadTypeDef>.GetNamed(cache.savedHeadDef) is HeadTypeDef headDef)
                    {
                        pawn.story.headType = headDef;
                    }
                    if (CRProps.disableFacialAnims)
                    {
                        cache.facialAnimationDisabled_Transform = false;
                    }
                    if (CRProps.disableBeards)
                    {
                        if (cache.savedBeardDef != null && DefDatabase<BeardDef>.GetNamed(cache.savedBeardDef) is BeardDef beardDef)
                            pawn.style.beardDef = beardDef;
                        else
                        {
                            pawn.style.beardDef = null;
                        }
                    }
                    if (CRProps.disableHair)
                    {
                        if (cache.savedHairDef != null && DefDatabase<HairDef>.GetNamed(cache.savedHairDef) is HairDef hairDef)
                            pawn.story.hairDef = hairDef;
                        else
                        {
                            pawn.story.hairDef = DefDatabase<HairDef>.AllDefs.RandomElement();
                        }
                    }
                }
            }
        }
    }
}
