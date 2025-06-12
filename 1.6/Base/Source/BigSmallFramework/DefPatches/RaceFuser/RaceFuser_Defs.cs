using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class SimilarParts : Def
    {
        public string groupName;
        /// <summary>
        /// Avoid very low values unless you don't want them to merge.
        /// </summary>
        public float similarity = 1;
        protected List<string> parts = [];

        private List<BodyPartDef> _partsCache = null;
        public List<BodyPartDef> Parts => _partsCache ??= parts.Select(x => DefDatabase<BodyPartDef>.GetNamed(x, errorOnFail: false)).ToList();
    }

    public class MergableBody
    {
        public BodyDef bodyDef;
        public ThingDef thingDef;

        [NoTranslate]
        public string overrideDefNamer = null;
        public string prefixLabel = null;
        public string suffixLabel = null;
        private bool fuse = true;
        public bool fuseAll = false;
        public bool fuseSet = false;
        public bool isMechanical = false;
        public bool defaultMechanical = false;
        public bool canBeFusionOne = true;
        public bool canMakeRobotVersion = true;
        public List<string> exclusionTags = [];



        public List<SimilarParts> removesParts = [];
        /// <summary>
        /// Which order this will be merged in. Put weird stuff with a higher priority.
        /// 
        /// It is likely better that weird bodies are bodyOne so that a snake-hybrid starts with a snake body rather than trying to replace the legs.
        /// </summary>
        public float priority = 0;

        public bool Fuse { get => fuse && !fuseSet; }// && !fuseAll; }

        public bool ShouldRemovePart(BodyPartDef part)
        {
            foreach (var partSet in removesParts)
            {
                if (partSet.Parts.Contains(part))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class Substitutions
    {
        public List<BodyDef> bodyDefs = [];
        public BodyDef target = null;  // If this is null we simply remove the requirement.
    }
    public class RetainableTrackers
    {
        public List<HediffDef> raceTrackers = [];
        public BodyDef target = null;
    }

    public class BodyDefFusion : Def
    {
        public List<MergableBody> mergableBody = [];
        public List<Substitutions> substitutions = [];
        public List<RetainableTrackers> retainableTrackers = [];
        public List<SimilarParts> similarParts = [];
        public List<BodyPartDef> bodyPartToSkip = [];
    }
}
