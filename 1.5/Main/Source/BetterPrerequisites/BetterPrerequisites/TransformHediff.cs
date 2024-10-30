using BigAndSmall;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        
        public BodyTypeDef bodyDefOverride = null;
        public BodyTypeDef bodyDefOverride_Female = null;
        public HeadTypeDef headDefOverride = null;
        public HeadTypeDef headDefOverride_Female = null;
        public bool disableFacialAnims = false;
        public bool disableBeards = false;
        public bool disableHair = false;
        public bool hideHead = false;
        public bool hideBody = false;

        public BodyTypeDef BodyTypeDef(Pawn pawn) => (pawn.gender == Gender.Female && bodyDefOverride_Female != null) ? bodyDefOverride_Female : bodyDefOverride;
        public HeadTypeDef HeadTypeDef(Pawn pawn) => (pawn.gender == Gender.Female && headDefOverride_Female != null) ? headDefOverride_Female : headDefOverride;


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
                if (CRProps.BodyTypeDef(pawn) != null)
                {
                    pawn.story.bodyType = CRProps.BodyTypeDef(pawn);
                }
                if (CRProps.HeadTypeDef(pawn) != null)
                {
                    pawn.story.headType = CRProps.HeadTypeDef(pawn);
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
