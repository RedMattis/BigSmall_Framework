using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class BSDrawData : DrawData
    {
        
    }
    public static class BSDrawDataExtentions
    {
        public static List<Vector3> GetCombinedOffsetsByRot(this List<BSDrawData> offsets)
        {
            bool any = false;
            var result = new List<Vector3>() { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, };
            foreach (int i in Enumerable.Range(0, 4))
            {
                foreach (var offset in offsets)
                {
                    result[i] += offset.OffsetForRot(new Rot4(i));
                    any = true;
                }
            }
            return any ? result : null;
        }
    }
}
