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

        public BodyTypeDef GetBodyType() { TrySetup(); return _bodyType; }
        public Gender? GetTGender() { TrySetup(); return _gender; }

        private List<string> SplitTags => tag.Split('_').ToList();

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
                bodyTypeDefs.FirstOrDefault(x => SplitTags.Contains(x.defName));
                _gender = texturePath.Contains("Male") ? Gender.Male : texturePath.Contains("Female") ? Gender.Female : Gender.None;
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
        public List<string> GetPaths(Pawn pawn, Gender? forceGender = null)
        {
            if (Count == 0) return [];
            
            var targetGender = forceGender == null ? pawn.gender : forceGender.Value;
            
            if (forceGender != null)
            {
                BodyTypeDef bt = pawn.story.bodyType;
                if (forceGender == Gender.Female && bt == BodyTypeDefOf.Male) pawn.story.bodyType = BodyTypeDefOf.Female;
                else if (forceGender == Gender.Male && bt == BodyTypeDefOf.Female) pawn.story.bodyType = BodyTypeDefOf.Male;
            }
            return this.Where(x => (x.GetBodyType() == null || x.GetBodyType() == pawn.story?.bodyType) && (x.GetTGender() == null || x.GetTGender() == targetGender)).Select(x => x.texturePath).ToList();
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
