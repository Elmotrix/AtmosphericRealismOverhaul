using System;
using UnityEngine;
using Assets.Scripts.Atmospherics;

namespace AtmosphericRealismOverhaul
{
    public class AroEnergy
    {
        public const float CompressEnergyPowerFactor = 0.1f;
        public static float CalcEnergyGasCompression(Atmosphere inputAtmos, Atmosphere outputAtmos, float n)
        {
            if (n <= 0f)
            {
                return 0f;
            }
            float Ratio = Mathf.Clamp(Mathf.Max(5, outputAtmos.PressureGassesAndLiquids) / Mathf.Max(5, inputAtmos.PressureGassesAndLiquids), float.Epsilon, float.MaxValue);

            //float inTemp = inputAtmos.Temperature;
            //float outTemp = outputAtmos.Temperature;
            //inTemp = (inTemp == 0f) ? outTemp : inTemp;
            //outTemp = (outTemp == 0f) ? inTemp : outTemp;
            //float Ratio2 = Mathf.Clamp(Mathf.Max(1, inTemp) / Mathf.Max(1, outTemp), float.Epsilon, float.MaxValue);
            //Ratio = Ratio * Ratio2;

            float energy = n * 350f * Mathf.Log(Ratio);
            return energy;
        }
    }
}
