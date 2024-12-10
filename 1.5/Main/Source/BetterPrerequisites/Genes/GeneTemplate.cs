using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class GeneTemplate : Def
    {
        public GeneCategoryDef displayCategory;
        public float selectionWeight = 1f;
        public bool canGenerateInGeneSet = true;
        public string keyTag = "";
        public string backgroundPathEndogenes = null;
        public string backgroundPathXenogenes = null;
        public string backgroundPathArchite = null;
        public Color? iconColor = null;
        public List<string> customEffectDescriptions = [];
    }
}
