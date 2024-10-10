using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace BigAndSmall
{
    public class CompProperties_TempGenerator : HediffCompProperties
    {   
        public float targetTemperature = 9999;
        public float energyPerSecond = 21;
        public bool scaleToBodySize = true;

        public CompProperties_TempGenerator()
        {
            compClass = typeof(TemperatureGenerator);
        }
    }

    public class TemperatureGenerator : HediffComp
    {
        // Get properties
        public CompProperties_TempGenerator Props => (CompProperties_TempGenerator)props;

        // Every 250 ticks, add heat to the room comparable to four heaters
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // Just random code to make sure the pawn is spawned in and is on the map.
            if (Find.TickManager.TicksGame % 250 == 0 &&  // 250 is comparable to "Rare Tick".
                Pawn != null && Pawn.Spawned &&
                HumanoidPawnScaler.GetCache(Pawn) != null &&
                Pawn.Map != null)
            {
                // Check if pawn is spawned in on the map and not in a container.
                if (Pawn != null && Pawn.Spawned)
                {
                }

                float ambientTemperature = AmbientTemperature;
                float num = 0;

                // make the effect exponentially smaller the further it is from the AmbientTemperature.
                if (Props.energyPerSecond > 0)
                {
                    num = (ambientTemperature < 20f) ? 1f : Mathf.InverseLerp(200, 20f, ambientTemperature);
                }
                else
                {
                    num = (ambientTemperature > 20f) ? 1f : Mathf.InverseLerp(-100f, 20f, ambientTemperature);
                }

                if (num < 0.1) num = 0.1f;

                float finalEnegryPerSecond = Props.energyPerSecond;
                if (Props.scaleToBodySize)
                {
                    if (parent.pawn.BodySize < 1)
                    {
                        finalEnegryPerSecond *= parent.pawn.BodySize;
                    }
                    else
                    {
                        // Less aggressive scaling for larger pawns
                        finalEnegryPerSecond *= 1 + (parent.pawn.BodySize - 1) * 0.33f;
                    }
                }

                float energyLimit = finalEnegryPerSecond * 4.1666665f * num;
                try
                {
                    float temperatureAdded = GenTemperature.ControlTemperatureTempChange(parent.pawn.PositionHeld, parent.pawn.Map, energyLimit, Props.targetTemperature);
                    bool doChange = !Mathf.Approximately(temperatureAdded, 0f);
                    if (doChange)
                    {
                        parent.pawn.GetRoom().Temperature += temperatureAdded;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Error while controlling temperature: " + ex);
                }
            }
        }

        public float AmbientTemperature
        {
            get
            {
                if (parent.pawn.Spawned)
                {
                    return GenTemperature.GetTemperatureForCell(parent.pawn.Position, parent.pawn.Map);
                }

                if (parent.pawn.ParentHolder != null)
                {
                    for (IThingHolder parentHolder = parent.pawn.ParentHolder; parentHolder != null; parentHolder = parentHolder.ParentHolder)
                    {
                        if (ThingOwnerUtility.TryGetFixedTemperature(parentHolder, parent.pawn, out var temperature))
                        {
                            return temperature;
                        }
                    }
                }

                if (parent.pawn.SpawnedOrAnyParentSpawned)
                {
                    return GenTemperature.GetTemperatureForCell(parent.pawn.PositionHeld, parent.pawn.MapHeld);
                }

                if (parent.pawn.Tile >= 0)
                {
                    return GenTemperature.GetTemperatureAtTile(parent.pawn.Tile);
                }

                return 21f;
            }
        }
    }
}
