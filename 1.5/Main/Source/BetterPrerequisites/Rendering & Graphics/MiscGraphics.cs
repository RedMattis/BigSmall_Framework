using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace BigAndSmall
{
    public class AdaptiveGraphicsData
    {
        public string tag;

        public string texturePath;

        private Gender? _gender = null;
        private BodyTypeDef _bodyType = null;
        private bool isDefault = false;

        public BodyTypeDef GetBodyType() { TrySetup(); return _bodyType; }
        public Gender? GetGender() { TrySetup(); return _gender; }
        public bool IsDefault() { TrySetup(); return isDefault; }

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
                }
                else
                {
                    isDefault = true;
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


    public class AdaptiveGraphicsCollection : List<AdaptiveGraphicsData>// <T> : List<T> where T : AdaptiveGraphicsData
    {
        //public static bool didWarn = false;

        public bool ValidFor(BSCache cache) => GetPaths(cache) != null;

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
            var result = this.Where(x => (x.GetBodyType() == null || x.GetBodyType() == pawn.story?.bodyType) && (x.GetGender() == null || x.GetGender() == targetGender)).Select(x => x.texturePath).ToList();

            if (!result.Any())
            {
                result = this.Where(x => x.IsDefault()).Select(x => x.texturePath).ToList();
            }
            return result.Any() ? result : null;
        }
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode xmlNode in xmlRoot.ChildNodes)
            {
                AdaptiveGraphicsData adaptiveGraphicsData = new AdaptiveGraphicsData();
                adaptiveGraphicsData.LoadDataFromXmlCustom(xmlNode);
                this.Add(adaptiveGraphicsData);
            }
        }
    }
}
