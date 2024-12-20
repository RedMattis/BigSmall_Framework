﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class BSDrawData : DrawData
    {
        
    }
    public static class BSDrawDataExtentions
    {
        public static List<Vector3> GetCombinedOffsetsByRot(this List<BSDrawData> offsets, float multipler=1)
        {
            bool any = false;
            var result = new List<Vector3>() { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, };
            foreach (int i in Enumerable.Range(0, 4))
            {
                foreach (var offset in offsets)
                {
                    result[i] += offset.OffsetForRot(new Rot4(i)) * multipler;
                    any = true;
                }
            }
            return any ? result : null;
        }
    }
}
