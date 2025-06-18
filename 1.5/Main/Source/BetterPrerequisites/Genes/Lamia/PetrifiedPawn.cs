//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using Verse;

//namespace BigAndSmall
//{

//    public class PetrifiedPawn : Building, IThingHolder
//    {
//        public ThingOwner innerContainer;
//        public PawnTextureAtlasFrameSet frameSet;
//        //public List<Material> cachedMaterials;

//        protected Pawn cachedPawn = null;
//        protected Material pawnMaterial;
//        protected Rot4? cachedRotation;

//        [Unsaved] protected float? cachedAngle;
//        [Unsaved] Texture2D greyTexture;
//        protected float? cachedZoom;

//        Texture2D greyScaledTex = null;
        

//        private string descriptionOverride = null;

//        public void GetChildHolders(List<IThingHolder> outChildren)
//        {
//            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
//        }

//        public ThingOwner GetDirectlyHeldThings()
//        {
//            return innerContainer;
//        }

//        public override void PostPostMake()
//        {
//            base.PostPostMake();
//            innerContainer = new ThingOwner<Thing>(this, oneStackOnly: true);
//            try
//            {
//                Pawn pawn = DEBUG_GenerateTestPawn();

//                if (holdingOwner != null)
//                {
//                    holdingOwner.TryTransferToContainer(pawn, innerContainer, stackCount);
//                }
//                else
//                {
//                    innerContainer.TryAdd(pawn);
//                }
//                //SnapshotPawnMaterial();
//            }
//            catch (Exception ex)
//            {
//                Log.Error($"Exception in PostMake: {ex}.\nStacktrace{ex.StackTrace}");
//            }
//        }

//        private Pawn DEBUG_GenerateTestPawn()
//        {
//            // Genereate a pawn and put it in the container.
//            var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.AncientSoldier, Faction.OfAncients);
//            //GenPlace.TryPlaceThing(pawn, Position, Find.AnyPlayerHomeMap, ThingPlaceMode.Near); // Doesn't seem to make a difference.
//            return pawn;
//        }

//        private void SnapshotPawnMaterial()
//        {
//            var pawn = GetPawn();
//            if (pawn != null)
//            {
//                GlobalTextureAtlasManager.TryGetPawnFrameSet(pawn, out frameSet, out var _);
//                pawnMaterial = MaterialPool.MatFrom(new MaterialRequest(frameSet.atlas, ShaderDatabase.Cutout));//ShaderDatabase.Transparent));
//            }
//        }

//        // Big thanks the Aelanna for helping out with a lot of code here! :)
//        private void DrawPawn()
//        {
//            Pawn pawn = GetPawn();

//            if (frameSet == null)
//            {
//                SnapshotPawnMaterial();
//            }

//            try
//            {
//                int index = frameSet.GetIndex(pawn.Rotation, PawnDrawMode.BodyAndHead);
//                float statueAngle = Rotation.AsAngle;

//                if (!pawn.Drawer.renderer.graphics.AllResolved)
//                {
//                    var atlas = frameSet.atlas;
//                    greyTexture = new Texture2D(atlas.width, atlas.height, TextureFormat.ARGB32, false);
//                    Graphics.CopyTexture(atlas, greyTexture);

//                    // Set all pixels to grey.
//                    var pixelList = greyTexture.GetPixels();
//                    for (int i = 0; i < pixelList.Length; i++)
//                    {
//                        var pixel = pixelList[i];
//                        // Average the pixel color. based on brightness.
//                        float brightness = (pixel.r + pixel.g + pixel.b) / 3f;
//                        pixel.r = brightness;
//                        pixel.g = brightness;
//                        pixel.b = brightness;
//                        pixelList[i] = pixel;
//                    }

//                    greyTexture.SetPixels(pixelList);

//                    // Write the greyTexture into the atlas.
//                    Graphics.CopyTexture(greyTexture, atlas);

//                    frameSet.atlas = atlas;

//                    pawn.Drawer.renderer.graphics.ResolveAllGraphics();
//                }
                
//                if (statueAngle != cachedAngle || greyTexture == null)
//                {
//                    cachedAngle = statueAngle;
//                    frameSet.isDirty[index] = true;
//                }


//                if (cachedZoom == null)
//                {
//                    cachedZoom = Find.CameraDriver.ZoomRootSize;
//                }

//                //if (cachedMaterials == null)
//                //{
//                //    cachedMaterials = pawn.Drawer.renderer.graphics.MatsBodyBaseAt(Rot4.South, false, RotDrawMode.Fresh, drawClothes: true);
//                //}



//                //Rot4 rotation = pawn.Rotation;

//                var pawnFacing = Pawn_RotationTracker.RotFromAngleBiased(statueAngle);


//                if (frameSet.isDirty[index])
//                {
//                    Find.PawnCacheCamera.rect = frameSet.uvRects[index];
//                    Find.PawnCacheRenderer.RenderPawn(pawn, frameSet.atlas, cameraOffset: Vector3.zero, cameraZoom: cachedZoom.Value, angle: 0, rotation: pawnFacing, renderHead: true, renderBody: true);
//                    Find.PawnCacheCamera.rect = new Rect(0f, 0f, 1f, 1f);
//                    frameSet.isDirty[index] = false;

//                    // Get pawn material.
                    
                    
//                    //float rotationOffset = 0f;
//                    //GlobalTextureAtlasManager.TryGetPawnFrameSet(pawn, )
//                }
//                //foreach (var material in cachedMaterials)
//                //{
//                //    material.color = Color.white;
//                //}
//                //pawnMaterial.color = Color.white;
//                //pawnMaterial.mainTexture = greyTexture;
//                //pawnMaterial.shader = myShaderHere
//                GenDraw.DrawMeshNowOrLater
//                (
//                    frameSet.meshes[index],
//                    DrawPos,
//                    Quaternion.AngleAxis(0, Vector3.up),
//                    pawnMaterial,
//                    drawNow: false
//                );
//            }
//            catch (Exception ex)
//            {
//                Log.Error("Exception drawing pawn: " + ex);
//            }
//        }

//        public override void DrawAt(Vector3 drawLoc, bool flip = false){}

//        public override void Draw()
//        {
//            //base.Draw();
//            if (def.drawerType == DrawerType.RealtimeOnly)
//            {
//                DrawPawn();

//            }
            
//        }

//        private Pawn GetPawn()
//        {
//            if (cachedPawn is Pawn) return cachedPawn;

//            var item = innerContainer.FirstOrDefault();
//            var pawn = item as Pawn;
//            if (pawn == null && item != null)
//            {
//                innerContainer.Remove(item); // Remove Item, it shouldn't be here.
//            }
//            cachedPawn = pawn;

//            return pawn;
//        }

//        public override string GetInspectString()
//        {
//            // Append the name of the pawn to the description.
//            var pawn = GetPawn();
//            string fullStr;
//            if (pawn != null)
//            {
//                fullStr = GetDescription(pawn);
//            }
//            else
//            {
//                fullStr = base.GetInspectString();
//            }

//            return fullStr;
//        }

//        private string GetDescription(Pawn pawn)
//        {
//            if (descriptionOverride == null)
//            {

//                List<string> randomSentenceEnd = new List<string>()
//                {
//                    "BS_StatueSurprised".Translate(),
//                    "BS_StatueFrightened".Translate(),
//                    "BS_StatueAngry".Translate(),
//                    "BS_StatuePain".Translate(),
//                    "BS_StatueDisoriented".Translate(),
//                    "BS_StatueDespair".Translate(),
//                    "BS_StatueScreaming".Translate(),
//                    "BS_StatueOblivious".Translate(),
//                    "BS_StatueStone".Translate()
//                };
//                descriptionOverride = $"{"BS_LifeSizeStatueOf".Translate()} {pawn.Name.ToStringShort}{randomSentenceEnd.RandomElement()}";
//            }
//            return descriptionOverride;
//        }

//        public override string DescriptionFlavor =>
//            GetPawn() is Pawn pawn
//            ? GetDescription(pawn)
//            : base.DescriptionFlavor;

//        public override void ExposeData()
//        {
//            base.ExposeData();
//            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
//            Scribe_Values.Look(ref descriptionOverride, "descriptionOverride");
//            Scribe_Values.Look(ref cachedRotation, "cachedRotation");
//        }
//    }
//}
