using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RedHealth
{
    [DefOf]
    public class HDefs
    {
        public static HealthCurve RED_LinearCurve;
        public static HealthCurve RED_ExponentialCurve;

        public static HealthAspect RED_OverallHealth;

        public static HealthAspect RED_CardiHealth;

        public static HediffDef RED_SecretHealthTracker;

        public static HediffDef RED_FailingOrganDamage;


        // Vanilla
        public static ThingDef Filth_MachineBits;
    }
}
