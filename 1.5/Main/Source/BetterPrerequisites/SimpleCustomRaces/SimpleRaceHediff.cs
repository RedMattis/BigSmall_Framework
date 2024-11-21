using BetterPrerequisites;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class RaceTracker : HediffWithComps
    {
        public override bool Visible => true;
        private List<PawnExtension> pawnExtensions = null;
        public List<PawnExtension> PawnExtensions => pawnExtensions ??= def.ExtensionsOnDef<PawnExtension, HediffDef>();

        public override void PostAdd(DamageInfo? info)
        {
            // Ensure these are not set to true.
            def.isBad = false;
            def.everCurableByItem = false;

            base.PostAdd(info);
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
        }
        public override void PostTick() { }
        public override void Tick() { }
        public override void Tended(float quality, float maxQuality, int batchPosition = 0) { }

        public override float PainOffset { get { return 0; } }
        /// <summary>
        /// Don't use stages on the RaceTracker, it causes it to start ticking.
        /// 
        /// Add a new Hediff instead if you want to use stages.
        /// </summary>
        //public override HediffStage CurStage { get { return null; } }

        public override string Description
        {
            get
            {
                var baseDesc = base.Description;

                try
                {
                    if (PawnExtensionExtension.TryGetDescription(PawnExtensions, out string pawnDesc))
                    {
                        baseDesc += $"\n\n{pawnDesc}";
                    }

                }
                catch (Exception e)
                {
                    Log.Error($"Error generating RaceTracker.Description: {e}");
                }
                finally
                {
                    
                }

                return baseDesc;
            }
        }
    }

}
