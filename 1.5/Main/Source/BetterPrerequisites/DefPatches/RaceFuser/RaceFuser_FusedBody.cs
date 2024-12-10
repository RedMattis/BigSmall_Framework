using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class FusedBody
    {
        public readonly static Dictionary<string, FusedBody> FusedBodies = [];
        public BodyDef generatedBody = null;
        public MergableBody[] mergableBodies = null;
        public ThingDef thing = null;
        public MergableBody fuseSetBody = null;
        public bool fake = false;
        public bool isMechanical = false;

        public FusedBody(BodyDef generatedBody, MergableBody fusetSetBody, bool mechanical, params MergableBody[] mergableBodies)
        {
            this.isMechanical = mechanical;
            this.generatedBody = generatedBody;
            this.mergableBodies = mergableBodies;
            this.fuseSetBody = fusetSetBody;
            FusedBodies[GetKey(mechanical, mergableBodies.Select(x => x.bodyDef).ToArray())] = this;
        }

        public MergableBody SourceBody => mergableBodies[0];

        private static string GetKey(bool mechanical, BodyDef[] bodyDefs)
        {
            var key = string.Join("|", bodyDefs.OrderBy(x => x.defName));
            //Log.Message($"{key} Generated from {mechanical} and {bodyDefs.Select(x => x.defName).ToCommaList()}");
            return string.Join("|", bodyDefs.OrderBy(x => x.defName));
        }

        public static FusedBody TryGetBody(bool mechanical, params BodyDef[] bodyDefs)
        {
            string mString = mechanical ? "mechanical" : "biological";
            if (false) Log.Message($"[Initial]: Fetching {mString} for and {string.Join(", ", bodyDefs.Select(x => x.defName))}");
            if (FusedBodies.TryGetValue(GetKey(mechanical, bodyDefs), out var body)) return body;
            if (bodyDefs.Count() > 1)
            {
                // Try substitute only first.
                //Log.Message($"[No_Match]: Trying substite of primary {bodyDefs[0].defName}");
                if (FusedBodies.TryGetValue(GetKey(mechanical, [GetSubstituted(bodyDefs).First(), .. bodyDefs.Skip(1)]), out var body2)) return body2;
                // Try substitute other.
                //Log.Message($"[No_Match]: Trying substite of secondaries {string.Join(", ", bodyDefs.Skip(1).Select(x => x.defName))}");
                if (FusedBodies.TryGetValue(GetKey(mechanical, [GetSubstituted(bodyDefs).First(), .. GetSubstituted([.. bodyDefs.Skip(1)])]), out var body3)) return body3;
                // Try substitute all.
            }
            //Log.Message($"[No_Match]: Trying substite of all {string.Join(", ", bodyDefs.Select(x => x.defName))}");
            return FusedBodies.TryGetValue(GetKey(mechanical, [.. GetSubstituted(bodyDefs)]), out var body4) ? body4 : null;
        }

        private static List<BodyDef> GetSubstituted(BodyDef[] bodyDefs)
        {
            var substitutedBodies = bodyDefs.ToList();
            var allSubs = BodyDefFusionsHelper.Substitutions;
            foreach (var inBody in bodyDefs)
            {
                var sub = allSubs.FirstOrDefault(x => x.bodyDefs.Contains(inBody));
                if (sub != null)
                {
                    substitutedBodies.Remove(inBody);
                    if (sub.target != null) substitutedBodies.Add(sub.target);
                }
            }
            return substitutedBodies;
        }

        public static BodyDef TryGetNonFused(params BodyDef[] bodyDefs)
        {
            if (GetSubstituted(bodyDefs).Count == 1)
            {
                return GetSubstituted(bodyDefs).First();
            }
            return null;
        }

        public static bool HasKey(bool mechanical, params BodyDef[] bodyDefs)
        {
            return FusedBodies.ContainsKey(GetKey(mechanical, bodyDefs));
        }
    }
}
