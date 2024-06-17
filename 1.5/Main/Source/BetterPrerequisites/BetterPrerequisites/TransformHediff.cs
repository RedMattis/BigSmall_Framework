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
                
                if (Props.furskinOverride != null)
                {
                    pawn.story.furDef = Props.furskinOverride;
                    //pawn.Drawer.renderer.SetAllGraphicsDirty();
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
                    if (cache.savedSkinColor != null)
                    {
                        pawn.story.skinColorOverride = cache.savedSkinColor.Value;
                    }
                    if (cache.savedHairColor != null)
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
                }
            }
        }
    }
}
