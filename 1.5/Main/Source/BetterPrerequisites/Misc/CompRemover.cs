using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_CompRemover : CompProperties
    {
        public List<string> compNameList = [];
        public List<string> compNamespaceList = [];
        public List<string> compFullNameList = [];
        public CompProperties_CompRemover()
        {
            compClass = typeof(CompRemover);
        }
    }

    public class CompRemover : ThingComp
    {
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            if (props is CompProperties_CompRemover compProps)
            {
                foreach (var compName in compProps.compNameList)
                {


                    if (parent.AllComps.FirstOrDefault(x => x.GetType().Name == compName) is ThingComp comp)
                    {
                        parent.AllComps.Remove(comp);
                    }
                }

                // Wildcard is same, but works on the namespace instead.
                foreach (var compName in compProps.compNamespaceList)
                {


                    if (parent.AllComps.FirstOrDefault(x => x.GetType().Namespace == compName) is ThingComp comp)
                    {
                        parent.AllComps.Remove(comp);
                    }
                }
                foreach (var compName in compProps.compFullNameList)
                {
                    //foreach (var c in parent.AllComps)
                    //{
                    //    Log.Message($"Comparing {c.GetType().FullName} to {compName}: Result {c.GetType().Name == compName}");
                    //}

                    if (parent.AllComps.FirstOrDefault(x => x.GetType().FullName == compName) is ThingComp comp)
                    {
                        parent.AllComps.Remove(comp);
                    }
                }
            }
        }
    }
}
