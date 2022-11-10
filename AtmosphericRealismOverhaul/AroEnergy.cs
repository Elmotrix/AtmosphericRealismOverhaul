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
        public static void AlterEnergy(Atmosphere atmosphere, float energy)
        {
            if (energy >= 0f)
            {
                atmosphere.GasMixture.AddEnergy(energy);
            }
            else
            {
                atmosphere.GasMixture.RemoveEnergy(-energy);
            }
        }
        public static float GetEnergyToTarget(Atmosphere inputAtmos, float targetTemperature)
        {
            return (targetTemperature - inputAtmos.Temperature) * inputAtmos.GasMixture.HeatCapacity;
        }
        public static float GetEnergyToEqualize(Atmosphere inputAtmos, Atmosphere outputAtmos)
        {
            float numerator = (inputAtmos.GasMixture.TotalEnergy * inputAtmos.GasMixture.HeatCapacity - outputAtmos.GasMixture.TotalEnergy * outputAtmos.GasMixture.HeatCapacity);
            float denominator = inputAtmos.GasMixture.HeatCapacity + outputAtmos.GasMixture.HeatCapacity;
            return numerator / denominator;
        }
        public static float GetPipeRadiatorScale(Atmosphere pipeAtmosphere)
        {
            float scale = 0.3f;
            AroDataBase aroAtmosphereData = AroAtmosphereDataController.Instance.GetAroAtmosphereData(pipeAtmosphere);
            if (aroAtmosphereData != null)
            {
                scale += aroAtmosphereData.MassFlowLastTick * pipeAtmosphere.Volume / (pipeAtmosphere.TotalMoles * 100);
            }
            return Mathf.Clamp01(scale);
        }
        public static float PressureRatioClamped(Atmosphere atmosphere)
        {
            return PressureRatioClamped(atmosphere.PressureGassesAndLiquids);
        }
        private static float PressureRatioClamped(float pressure)
        {
            float result = pressure/((AroFlow.atm/2)+Mathf.Abs(pressure));
            return result;
        }
    }
}
