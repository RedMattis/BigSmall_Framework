using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace BigAndSmall
{
    public class AdaptivePawnPath
    {
        public string tag;

        public string texturePath;

        private Gender? _gender = null;
        private BodyTypeDef _bodyType = null;
        private int priority = -1;

        public BodyTypeDef GetBodyType() { TrySetup(); return _bodyType; }
        public Gender? GetGender() { TrySetup(); return _gender; }
        public int GetPriority() { TrySetup(); return priority; }

        private bool initialized = false;
        private static bool defsLoaded = false;
        private static List<BodyTypeDef> bodyTypeDefs = [];
        private void TrySetup()
        {
            if (!defsLoaded)
            {
                bodyTypeDefs = DefDatabase<BodyTypeDef>.AllDefsListForReading;
                defsLoaded = true;
            }
            if (!initialized)
            {
                // Style 1: <Male>Path</Male>
                _gender = tag == "Male" ? Gender.Male :
                    tag == "Female" ? Gender.Female :
                    tag == "None" ? Gender.None :
                    null;

                if (_gender == null)
                {
                    // Style 2: <Body_Male>Path</Body_Male> or <Female_Body_Hulk>Path</Female_Body_Hulk>
                    _bodyType = bodyTypeDefs.FirstOrDefault(x => tag.Contains("Body_" + x.defName));
                    _gender =
                        tag.StartsWith("Female") == true ? Gender.Female :
                        tag.StartsWith("Male") == true ? Gender.Male :
                        tag.StartsWith("None") == true ? Gender.None :
                        null;
                    priority = _gender == null ? 10 : 100;
                }
                else
                {
                    priority = -10;
                }
               
                initialized = true;
            }
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            tag = xmlRoot.Name;
            texturePath = xmlRoot.FirstChild.Value;
        }
    }

    public class AdaptivePawnPathDef : Def
    {
        public AdaptivePathPathList texturePaths = null;
    }


    public class AdaptivePathPathList : List<AdaptivePawnPath>// <T> : List<T> where T : AdaptiveGraphicsData
    {
        public bool ValidFor(BSCache cache, Gender? apparentGender) => GetPaths(cache, apparentGender) != null;

        public bool TryGetPath(BSCache cache, ref string path)
        {
            var rng = cache.pawn.GetPawnRNGSeed();
            Gender? gender = cache.GetApparentGender();
            var pathList = GetPaths(cache, gender);
            if (!pathList.NullOrEmpty()) using (new RandBlock(rng))
                {
                    path = pathList?.RandomElement();
                    return true;
                }
            return false;
        }

        public List<string> GetPaths(BSCache cache, Gender? forceGender = null)
        {
            if (Count == 0) return null;
            var pawn = cache.pawn;
            
            var targetGender = forceGender == null ? pawn.gender : forceGender.Value;
            
            if (forceGender != null)
            {
                BodyTypeDef bt = pawn.story.bodyType;
                if (forceGender == Gender.Female && bt == BodyTypeDefOf.Male) pawn.story.bodyType = BodyTypeDefOf.Female;
                else if (forceGender == Gender.Male && bt == BodyTypeDefOf.Female) pawn.story.bodyType = BodyTypeDefOf.Male;
            }
            var paths = this.Where(x => (x.GetBodyType() == null || x.GetBodyType() == pawn.story?.bodyType) && (x.GetGender() == null || x.GetGender() == targetGender));
            if (!paths.Any()) return null;

            var bestPriority = paths.Select(x => x.GetPriority()).DefaultIfEmpty(0).Max();
            var result = paths.Where(x => x.GetPriority() == bestPriority).Select(x => x.texturePath).ToList();

            return result.Any() ? result : null;
        }
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode xmlNode in xmlRoot.ChildNodes)
            {
                AdaptivePawnPath adaptiveGraphicsData = new AdaptivePawnPath();
                adaptiveGraphicsData.LoadDataFromXmlCustom(xmlNode);
                this.Add(adaptiveGraphicsData);
            }
        }
    }
}
