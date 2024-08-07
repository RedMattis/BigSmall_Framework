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
        public List<Color> skinColorOverride = null;
        public List<Color> hairColorOverride = null;
        public FurDef furskinOverride = null;
        public bool skinIsHairColor = false;
        public BodyTypeDef bodyDefOverride = null;
        public HeadTypeDef headDefOverride = null;
        public bool disableFacialAnims = false;
        public bool disableBeards = false;

        public CompProperties_ColorAndFur()
        {
            compClass = typeof(HediffComp_ColorAndFur);
        }
    }

    public class HediffComp_ColorAndFur : HediffComp
    {
        public CompProperties_ColorAndFur Props => (CompProperties_ColorAndFur)props;
        

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
                if (Props.hairColorOverride != null)
                {
                    if (cache.randomPickHairColor == null || cache.randomPickHairColor >= Props.hairColorOverride.Count - 1)
                    {
                        cache.randomPickHairColor = Rand.Range(0, Props.hairColorOverride.Count - 1);
                    }
                    pawn.story.HairColor = Props.hairColorOverride[cache.randomPickHairColor.Value];
                }
                if (Props.skinIsHairColor)
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
                else if (Props.skinColorOverride != null)
                {
                    if (cache.randomPickSkinColor == null || cache.randomPickSkinColor >= Props.skinColorOverride.Count -1)
                    {
                        cache.randomPickSkinColor = Rand.Range(0, Props.skinColorOverride.Count - 1);
                    }
                    pawn.story.skinColorOverride = Props.skinColorOverride[cache.randomPickSkinColor.Value];
                }
                if (Props.bodyDefOverride != null)
                {
                    pawn.story.bodyType = Props.bodyDefOverride;
                }
                if (Props.headDefOverride != null)
                {
                    pawn.story.headType = Props.headDefOverride;
                }
                
                if (Props.furskinOverride != null)
                {
                    pawn.story.furDef = Props.furskinOverride;
                    //pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                if (Props.disableFacialAnims)
                {
                    cache.facialAnimationDisabled_Transform = true;
                }
                if (Props.disableBeards)
                {
                    pawn.style.beardDef = null;
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
                    if (Props.disableFacialAnims)
                    {
                        cache.facialAnimationDisabled_Transform = false;
                    }
                    if (Props.disableBeards)
                    {
                        if (cache.savedBeardDef != null && DefDatabase<BeardDef>.GetNamed(cache.savedBeardDef) is BeardDef beardDef)
                            pawn.style.beardDef = beardDef;
                        else
                        {
                            pawn.style.beardDef = null;
                        }
                    }
                }
            }
        }
    }
}
