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
            float Ratio = Mathf.Max(5, outputAtmos.PressureGassesAndLiquids) / Mathf.Max(5, inputAtmos.PressureGassesAndLiquids);
            float energy = n * 350f * Mathf.Log(Ratio);
            return energy;
        }
        public static float CalcEnergyGasCompression(float oldVolume, float newVolume, float n)
        {
            if (n <= 0f || newVolume <= 0 || oldVolume <= 0)
            {
                return 0f;
            }
            float energy = n * 350f * Mathf.Log(oldVolume/ newVolume);
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
            AroDataBase aroAtmosphereData = AroAtmosphereDataController.GetInstance().GetAroAtmosphereData(pipeAtmosphere);
            if (aroAtmosphereData != null)
            {
                scale += 10f * aroAtmosphereData.MassFlowLastTick * pipeAtmosphere.Volume / (pipeAtmosphere.TotalMoles * Chemistry.PipeVolume);
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
