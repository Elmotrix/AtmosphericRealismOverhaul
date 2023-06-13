using System;
using UnityEngine;
using Assets.Scripts.Atmospherics;

using static Assets.Scripts.Atmospherics.Chemistry;
using static Assets.Scripts.Atmospherics.Atmosphere;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts;

namespace AtmosphericRealismOverhaul
{
    public class AroFlow
    {
        public const float atm = 101.325f;
        public const float R = 8.3144f;
        public const float GFGmaxPressure = 5000f;
        public static GasType[] gasTypes = new GasType[]
        { GasType.CarbonDioxide, GasType.Nitrogen, GasType.NitrousOxide, GasType.Oxygen, GasType.Pollutant, GasType.Volatiles, GasType.Water };
        public static float debug;
        public static float debug2;
        public static float debug3;
        public static string DisplayUnitCleaner(float baseValue, string unit)
        {
            float value = baseValue * 1000;
            string valueUnit = "m" + unit;
            if (Mathf.Abs(value) > 1000)
            {
                value = value / 1000;
                valueUnit = "" + unit;
            }
            if (Mathf.Abs(value) > 1000)
            {
                value = value / 1000;
                valueUnit = "k" + unit;
            }
            if (Mathf.Abs(value) > 1000)
            {
                value = value / 1000;
                valueUnit = "M" + unit;
            }
            int deci = 2;
            if (Mathf.Abs(value) > 10)
            {
                deci = 1;
            }
            if (Mathf.Abs(value) > 100)
            {
                deci = 0;
            }
            return Math.Round(value, deci) + valueUnit;
        }
        public static float BiDirectional(Atmosphere inputAtmos, Atmosphere outputAtmos, float amountPressureToMove=float.MaxValue, float eqRate=1f, float mixRate=0.07f, float mixThreshold = 0.01f, MatterState typeToMove = Atmosphere.MatterState.All)
        {
            if (inputAtmos == null || outputAtmos == null)
            {
                return 0;
            }
            float energy = 0;
            float outputPressure = outputAtmos.Pressure(typeToMove);
            float inputPressure = inputAtmos.Pressure(typeToMove);
            float ratio = 0;
            if (outputPressure > inputPressure)
            {
                ratio = inputPressure / outputPressure;
            }
            else
            {
                ratio = outputPressure / inputPressure;
            }
            ratio = Mathf.Abs(ratio - 1f);
            if (ratio < mixThreshold)
            {
                Mix(inputAtmos, outputAtmos, mixRate, typeToMove);
            }
            if (outputPressure > inputPressure)
            {
                energy += Equalize(outputAtmos, inputAtmos, amountPressureToMove, eqRate, typeToMove);
            }
            else
            {
                energy += Equalize(inputAtmos, outputAtmos, amountPressureToMove, eqRate, typeToMove);
            }
            return energy;
        }
        public static void Mix(Atmosphere inputAtmos, Atmosphere outputAtmos, float mixRate, MatterState typeToMove)
        {
            mixRate = Mathf.Clamp01(mixRate);
            if (inputAtmos != null && outputAtmos != null && mixRate > 0f)
            {
                GasMixture gasMixture = GasMixture.Create();
                float moleFlow = inputAtmos.TotalMoles;
                float volumeInn = inputAtmos.Volume;
                float volumeOut = outputAtmos.Volume;
                float totalVolume = volumeInn + volumeOut;
                mixRate = Mathf.Clamp01(mixRate);
                gasMixture.Add(inputAtmos.Remove(inputAtmos.TotalMoles * mixRate, typeToMove));
                gasMixture.Add(outputAtmos.Remove(outputAtmos.TotalMoles * mixRate, typeToMove));
                float totalN = gasMixture.TotalMoles(typeToMove);
                inputAtmos.GasMixture.Add(gasMixture.Remove(totalN * (volumeInn / totalVolume), typeToMove));
                outputAtmos.GasMixture.Add(gasMixture);
                gasMixture.Reset();
                moleFlow = moleFlow - inputAtmos.TotalMoles;
                AroAtmosphereDataController.GetInstance().AddFlow(inputAtmos, -moleFlow, 0f);
                AroAtmosphereDataController.GetInstance().AddFlow(outputAtmos, moleFlow, 0f);
            }
        }
        public static float ActiveEqualize(Atmosphere inputAtmos, Atmosphere outputAtmos, float baseLiter, float maxDelta, float eqRate, MatterState typeToMove)
        {
            float dp = Mathf.Abs(inputAtmos.PressureGassesAndLiquids - outputAtmos.PressureGassesAndLiquids);
            float liter = (1 - Mathf.Clamp01(dp / maxDelta)) * baseLiter;
            float literN = GetVolumeMole(inputAtmos, liter, typeToMove);
            float equalizeN = GetEqualizeMole(inputAtmos, outputAtmos, float.MaxValue, eqRate, typeToMove);
            float energy = MoveMassEnergy(inputAtmos, outputAtmos, Mathf.Max(equalizeN, literN), typeToMove);
            return energy;
        }
        public static float Equalize(Atmosphere inputAtmos, Atmosphere outputAtmos, float desiredPressureChange, float eqRate, MatterState typeToMove)
        {
            float n = GetEqualizeMole(inputAtmos, outputAtmos, desiredPressureChange, eqRate, typeToMove);
            float energy = MoveMassEnergy(inputAtmos, outputAtmos, n, typeToMove);
            return energy;
        }
        public static float GetEqualizeMole(Atmosphere inputAtmos, Atmosphere outputAtmos, float desiredPressureChange, float eqRate, MatterState matterStateToMove)
        {
            eqRate = Mathf.Clamp01(eqRate);
            float dp = Mathf.Min(desiredPressureChange, (inputAtmos.PressureGassesAndLiquids - outputAtmos.PressureGassesAndLiquids) * eqRate);
            float n = 0f;
            if (dp > 0f)
            {
                float outTemp = outputAtmos.Temperature;
                outTemp = Mathf.Max(outTemp, inputAtmos.Temperature);
                float num2 = 8.3144f * inputAtmos.Temperature / inputAtmos.Volume;
                float num3 = 8.3144f * outTemp / outputAtmos.Volume;
                float num4 = num2 + num3;
                n = dp / num4;
            }
            n = Mathf.Min(n, inputAtmos.GasMixture.TotalMoles(matterStateToMove) / 2f);
            return n;
        }
        public static float GetVolumeMole(Atmosphere inputAtmos, float volume, MatterState matterStateToMove)
        {
            float num = Mathf.Clamp01(volume / (inputAtmos.Volume + volume));
            float n = num * inputAtmos.GasMixture.TotalMoles(matterStateToMove);
            return n;
        }
        public static float CompressVolume(Atmosphere inputAtmos, Atmosphere outputAtmos, float volume, MatterState matterStateToMove)
        {
            float n = GetVolumeMole(inputAtmos, volume, matterStateToMove);
            float energy = MoveMassEnergy(inputAtmos, outputAtmos, n, matterStateToMove);
            return energy;
        }
        public static Mole MoveMassEnergy(Atmosphere inputAtmos, Atmosphere outputAtmos, float n, GasType gasType)
        {
            Mole mole = inputAtmos.GasMixture.Remove(gasType, n);
            if (mole.Quantity <= 0f)
            {
                return mole;
            }
            GasMixture gasMixture = GasMixture.Create();
            gasMixture.Add(mole);
            MoveMassEnergy(inputAtmos, outputAtmos,gasMixture);
            return mole;
        }
        public static float MoveMassEnergy(Atmosphere inputAtmos, Atmosphere outputAtmos, float n, MatterState matterStateToMove)
        {
            if (n <= 0f)
            {
                return 0f;
            }
            GasMixture gasMixture = inputAtmos.Remove(n, matterStateToMove);
            return MoveMassEnergy(inputAtmos, outputAtmos,gasMixture);
        }
        private static float MoveMassEnergy(Atmosphere inputAtmos, Atmosphere outputAtmos,GasMixture gasMixtureToAdd)
        {
            float n = gasMixtureToAdd.TotalMolesGassesAndLiquids;
            outputAtmos.Add(gasMixtureToAdd);
            float energy = AroEnergy.CalcEnergyGasCompression(inputAtmos, outputAtmos, n);
            AroEnergy.AlterEnergy(outputAtmos, energy);
            AroAtmosphereDataController.GetInstance().AddFlow(inputAtmos, -n, 0);
            AroAtmosphereDataController.GetInstance().AddFlow(outputAtmos, n, energy);
            return energy;
        }

        public static bool IsOperable(DeviceInputOutput device)
        {
            bool flag = device.InputNetwork != null && device.OutputNetwork != null;
            if (flag)
            {
                flag = flag && device.InputNetwork.IsNetworkValid() && device.OutputNetwork.IsNetworkValid();
            }
            if (GameManager.RunSimulation)
            {
                OnServer.Interact(device.InteractError, Convert.ToInt32(!flag));
            }
            return flag;
        }
        public static bool IsOperable(DeviceOutput device)
        {
            bool flag = device.HasPipeNetwork && device.GridController.CanContainAtmos(device.WorldGrid);
            if (GameManager.RunSimulation)
            {
                OnServer.Interact(device.InteractError, Convert.ToInt32(!flag));
            }
            return flag;
        }
    }
}
