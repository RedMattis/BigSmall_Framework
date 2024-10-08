﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_CompRemover : CompProperties
    {
        public List<string> compNameList = new List<string>();
        public List<string> compNamespaceList = new List<string>();
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
                    //foreach (var c in parent.AllComps)
                    //{
                    //    Log.Message($"Comparing {c.GetType().Name} to {compName}: Result {c.GetType().Name == compName}");
                    //}

                    if (parent.AllComps.FirstOrDefault(x => x.GetType().Name == compName) is ThingComp comp)
                    {
                        parent.AllComps.Remove(comp);
                    }
                }

                // Wildcard is same, but works on the namespace instead.
                foreach (var compName in compProps.compNamespaceList)
                {
                    //foreach (var c in parent.AllComps)
                    //{
                    //    Log.Message($"Comparing {c.GetType().Namespace} to {compName}: Result {c.GetType().Name == compName}");
                    //}

                    if (parent.AllComps.FirstOrDefault(x => x.GetType().Namespace == compName) is ThingComp comp)
                    {
                        parent.AllComps.Remove(comp);
                    }
                }
            }
        }
    }
}
