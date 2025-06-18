//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Verse;


// Not worth the bother unless we patch random stuff like CertaintyChangeFactor. Easier/safer from compatibility perspective
// to just hax it by skipping childhood (set biological age), etc.

//namespace BigAndSmall
//{
//    public class LifeStageWorker_FullyFormedHumanlike : LifeStageWorker
//    {

//        private static readonly List<BackstoryCategoryFilter> VatgrowBackstoryFilter = new List<BackstoryCategoryFilter>
//        {
//            new BackstoryCategoryFilter
//            {
//                categories = new List<string> { "VatGrown" }
//            }
//        };

//        private static readonly List<BackstoryCategoryFilter> BackstoryFiltersTribal = new List<BackstoryCategoryFilter>
//        {
//            new BackstoryCategoryFilter
//            {
//                categories = new List<string> { "AdultTribal" }
//            }
//        };

//        private static readonly List<BackstoryCategoryFilter> BackstoryFiltersColonist = new List<BackstoryCategoryFilter>
//        {
//            new BackstoryCategoryFilter
//            {
//                categories = new List<string> { "AdultColonist" }
//            }
//        };

//        public override void Notify_LifeStageStarted(Pawn pawn, LifeStageDef previousLifeStage)
//        {
//            base.Notify_LifeStageStarted(pawn, previousLifeStage);
//            if (Current.ProgramState != ProgramState.Playing)
//            {
//                return;
//            }
//            if (pawn.Spawned && previousLifeStage != null && previousLifeStage.developmentalStage.Juvenile())
//            {
//                EffecterDefOf.Birthday.SpawnAttached(pawn, pawn.Map);
//            }
//            if (pawn.story.bodyType == BodyTypeDefOf.Child || pawn.story.bodyType == BodyTypeDefOf.Baby)
//            {
//                pawn.apparel?.DropAllOrMoveAllToInventory((Apparel apparel) => !apparel.def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult));
//                BodyTypeDef bodyTypeFor = PawnGenerator.GetBodyTypeFor(pawn);
//                pawn.story.bodyType = bodyTypeFor;
//                pawn.Drawer.renderer.SetAllGraphicsDirty();
//            }
//            if (!pawn.IsColonist)
//            {
//                return;
//            }
//        }
//    }

//}
