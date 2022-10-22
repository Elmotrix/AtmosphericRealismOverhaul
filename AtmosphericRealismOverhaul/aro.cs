using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using System.Reflection;
using Assets.Scripts.Objects.Items;
using static Assets.Scripts.Atmospherics.Chemistry;
using Objects.Pipes;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects;
using JetBrains.Annotations;
using Assets.Scripts;

namespace AtmosphericRealismOverhaul
{
    //[BepInPlugin("elmotrix.stationeers.aro", "Atmospheric Realism Overhaul", "1.0.0.0")]
    //public class aroMod : BaseUnityPlugin
    //{
    //    //const float R = 8.3144f;
    //    public const string pluginGuid = "elmotrix.stationeers.aro";
    //    public void Awake()
    //    {

    //    }
    //}
    [HarmonyPatch(typeof(PressureRegulator), nameof(PressureRegulator.OnAtmosphericTick))]
    public class RegulatorPatch
    {
        [UsedImplicitly]
        public static bool Prefix(PressureRegulator __instance)
        {
            __instance.UsedPower = 10;
            
            if (!__instance.OnOff || !__instance.Powered || __instance.InputNetwork == null || __instance.OutputNetwork == null)
            {
                return false;
            }
            float dp = 0;
            switch (__instance.RegulatorType)
            {
                case RegulatorType.Upstream:
                    //PR
                    dp = __instance.OutputSetting - __instance.OutputNetwork.Atmosphere.PressureGassesAndLiquids;
                    break;
                case RegulatorType.Downstream:
                    //BPR
                    dp = __instance.InputNetwork.Atmosphere.PressureGassesAndLiquids - __instance.OutputSetting;
                    break;
                default:
                    break;
            }
            AroMath.Equalize(__instance.InputNetwork.Atmosphere, __instance.OutputNetwork.Atmosphere, dp, 1f, 0f, Atmosphere.MatterState.All);
            return false;
        }
    }
    [HarmonyPatch(typeof(VolumePump), nameof(VolumePump.GetUsedPower))]
    public class VolumePumpPowerPatch
    {
        [UsedImplicitly]
        public static bool Prefix(DeviceAtmospherics __instance, CableNetwork cableNetwork, ref float __result)
        {
            if (__instance.PowerCable == null || __instance.PowerCable.CableNetwork != cableNetwork)
            {
                return true;
            }
            else if (!__instance.OnOff)
            {
                return true;
            }
            else if (__instance.OutputSetting <= 0f)
            {
                return true;
            }
            else
            {
                __result = __instance.UsedPower;
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(VolumePump), nameof(VolumePump.MoveAtmosphere))]
    public class VolumePumpAtmospherePatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmosphere, Atmosphere outputAtmosphere, VolumePump __instance)
        {
            float setting = __instance.OutputSetting;
            if (setting <= 0f)
            {
                __instance.UsedPower = 0f;
                return false;
            }
            __instance.UsedPower = Mathf.Max(AroMath.CompressVolume(inputAtmosphere, outputAtmosphere, setting, Atmosphere.MatterState.All), setting) * AroMath.CompressEnergyPowerFactor;
            return false;
        }
    }
    [HarmonyPatch(typeof(AdvancedFurnace), nameof(AdvancedFurnace.GetUsedPower))]
    public class AdvancedFurnacePowerPatch
    {
        [UsedImplicitly]
        public static bool Prefix(DeviceAtmospherics __instance, CableNetwork cableNetwork, ref float __result)
        {
            if (__instance.PowerCable == null || __instance.PowerCable.CableNetwork != cableNetwork)
            {
                __result = -1f;
            }
            else if (!__instance.OnOff)
            {
                __result = 0f;
            }
            else
            {
                __result = __instance.UsedPower;
            }
            return false;
        }

    }
    [HarmonyPatch(typeof(AdvancedFurnace), nameof(AdvancedFurnace.HandleGasInput))]
    public class AdvancedFurnaceAtmospherePatch
    {
        [UsedImplicitly]
        public static bool Prefix(AdvancedFurnace __instance)
        {
            float energy = 0;
            float num;
            if (__instance.OnOff && __instance.Powered && __instance.Error <= 0)
            {
                if (__instance.InputNetwork != null)
                {
                    num = AroMath.CompressVolume(__instance.InputNetwork.Atmosphere, __instance.InternalAtmosphere, __instance.OutputSetting2, Atmosphere.MatterState.Gas);
                    energy += Mathf.Max(num, __instance.OutputSetting2);
                }
                if (__instance.OutputNetwork != null)
                {
                    num = AroMath.CompressVolume(__instance.InternalAtmosphere, __instance.OutputNetwork.Atmosphere, __instance.OutputSetting, Atmosphere.MatterState.Gas);
                    energy += Mathf.Max(num, __instance.OutputSetting);
                }
                if (__instance.OutputNetwork2 != null)
                {
                    num = AroMath.CompressVolume(__instance.InternalAtmosphere, __instance.OutputNetwork2.Atmosphere, __instance.OutputSetting, Atmosphere.MatterState.Liquid);
                    energy += Mathf.Max(num, __instance.OutputSetting);
                }
            }
            __instance.UsedPower = energy * AroMath.CompressEnergyPowerFactor;
            return false;
        }
    }
    [HarmonyPatch(typeof(DeviceAtmospherics), nameof(DeviceAtmospherics.MoveVolume), new Type[] {typeof(Atmosphere), typeof(Atmosphere), typeof(float), typeof(Atmosphere.MatterState) })]
    public class DeviceAtmosphericsMoveVolumePatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, float setting, Atmosphere.MatterState matterStateToMove)
        {
            if (inputAtmos.PressureGassesAndLiquids < 0.001f && outputAtmos == null)
            {
                inputAtmos.GasMixture.Reset();
                return false;
            }
            AroMath.CompressVolume(inputAtmos, outputAtmos, setting, Atmosphere.MatterState.All);
            return false;
        }
    }
    [HarmonyPatch(typeof(DeviceAtmospherics), nameof(DeviceAtmospherics.MoveToEqualize), new Type[] {typeof(Atmosphere), typeof(Atmosphere), typeof(float), typeof(Atmosphere.MatterState)} )]
    public class DeviceAtmosphericsEqualizePatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, float desiredPressureChange, Atmosphere.MatterState typeToMove)
        {
            if (inputAtmos == null || outputAtmos == null)
            {
                return false;
            }
            AroMath.Equalize(inputAtmos, outputAtmos, desiredPressureChange, 1f, 0f, typeToMove);
            return false;
        }
    }
    [HarmonyPatch(typeof(DeviceAtmospherics), nameof(DeviceAtmospherics.MoveToEqualizeBidirectional))]
    public class DeviceAtmosphericsBidirectionalPatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, float amountPressureToMove, Atmosphere.MatterState typeToMove)
        {
            AroMath.BiDirectional(inputAtmos, outputAtmos, amountPressureToMove, 0.8f , typeToMove);
            return false;
        }
    }
    [HarmonyPatch(typeof(ActiveVent), nameof(ActiveVent.OnAtmosphericTick))]
    public class ActiveVentPatch
    {
        [UsedImplicitly]
        public static bool Prefix(ActiveVent __instance)
        {
            if (!__instance.OnOff || !__instance.Powered || !__instance.IsOperable)
            {
                return false;
            }
            //Atmosphere worldAtmosphere = __instance.AtmosphericsController.GetAtmosphereLocal(__instance.WorldGrid);
            Atmosphere worldAtmosphere = __instance.GridController.AtmosphericsController.CloneGlobalAtmosphere(__instance.WorldGrid);
            Atmosphere pipeAtmosphere = __instance.ConnectedPipeNetwork.Atmosphere;
            switch (__instance.VentDirection)
            {
                case VentDirection.Inward:
                    //move gas from world to pipe (mode=1)
                    if (worldAtmosphere.PressureGassesAndLiquids > __instance.ExternalPressure && pipeAtmosphere.PressureGassesAndLiquids < __instance.InternalPressure)
                    {
                        AroMath.ActiveEqualize(worldAtmosphere, pipeAtmosphere, 1000f, AroMath.atm * 2f,0.8f, Atmosphere.MatterState.All);
                    }
                    break;
                case VentDirection.Outward:
                    //move gas from pipe to world (mode=0)
                    if (worldAtmosphere.PressureGassesAndLiquids < __instance.ExternalPressure && pipeAtmosphere.PressureGassesAndLiquids > __instance.InternalPressure)
                    {
                        AroMath.ActiveEqualize(pipeAtmosphere, worldAtmosphere, 100f, AroMath.atm*2f, 0.6f, Atmosphere.MatterState.All);
                    }
                    break;
                default:
                    break;
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(Valve), nameof(Valve.OnAtmosphericTick))]
    public class ValvePatch
    {
        [UsedImplicitly]
        public static bool Prefix(Valve __instance)
        {

            if (!__instance.OnOff || __instance.Error == 1 || (__instance.HasPowerState && !__instance.Powered))
            {
                return false;
            }
            if (__instance.ConnectedPipeNetworks.Count != 2)
            {
                return true;
            }
            Atmosphere inputAtmos = __instance.ConnectedPipeNetworks[0].Atmosphere;
            Atmosphere outputAtmos = __instance.ConnectedPipeNetworks[1].Atmosphere;
            AroMath.BiDirectional(inputAtmos, outputAtmos, float.MaxValue, __instance.OutputSetting/110f, Atmosphere.MatterState.All);
            return false;
        }
    }
    [HarmonyPatch(typeof(Mixer), nameof(Mixer.OnAtmosphericTick))]
    public class MixerPatch
    {
        [UsedImplicitly]
        public static bool Prefix(Mixer __instance)
        {
            __instance.UsedPower = 10f;
            if (__instance.InputNetwork == null || __instance.InputNetwork2 == null || __instance.OutputNetwork == null)
            {
                return false;
            }
            if (!__instance.OnOff || !__instance.Powered || __instance.Error == 1)
            {
                return false;
            }
            Atmosphere inputAtmos1 = __instance.InputNetwork.Atmosphere;
            Atmosphere inputAtmos2 = __instance.InputNetwork2.Atmosphere;
            Atmosphere outputAtmos = __instance.OutputNetwork.Atmosphere;
            float Ratio1 = __instance.Ratio1 / 100f;
            float Ratio2 = __instance.Ratio2 / 100f;
            float mixRate = Mathf.Max(Ratio1, Ratio2);
            float n1 = AroMath.GetEqualizeMole(inputAtmos1, outputAtmos, float.MaxValue, 0f) * mixRate * 0.5f;
            float n2 = AroMath.GetEqualizeMole(inputAtmos2, outputAtmos, float.MaxValue, 0f) * mixRate * 0.5f;
            if (n1* Ratio2 < n2* Ratio1)
            {
                n2 = n1 * Ratio2 / Ratio1;
                AroMath.MoveMassEnergy(inputAtmos1, outputAtmos, n1, Atmosphere.MatterState.All);
                AroMath.MoveMassEnergy(inputAtmos2, outputAtmos, n2, Atmosphere.MatterState.All);
            }
            else
            {
                n1 = n2 * Ratio1 / Ratio2;
                AroMath.MoveMassEnergy(inputAtmos2, outputAtmos, n2, Atmosphere.MatterState.All);
                AroMath.MoveMassEnergy(inputAtmos1, outputAtmos, n1, Atmosphere.MatterState.All);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(FiltrationMachine), nameof(FiltrationMachine.OnAtmosphericTick))]
    public class FiltrationMachinePatch
    {
        [UsedImplicitly]
        public static bool Prefix(FiltrationMachine __instance)
        {
            if (!__instance.OnOff || !__instance.Powered || __instance.Mode == 0 || !__instance.IsOperable)
            {
                return false;
            }
            Atmosphere inputAtmos1 = __instance.InputNetwork.Atmosphere;
            Atmosphere outputAtmos1 = __instance.OutputNetwork.Atmosphere;
            Atmosphere outputAtmos2 = __instance.OutputNetwork2.Atmosphere;
            float n1 = AroMath.GetEqualizeMole(inputAtmos1, outputAtmos1, float.MaxValue, 0f)*0.9f;
            float n2 = AroMath.GetEqualizeMole(inputAtmos1, outputAtmos2, float.MaxValue, 0f)*0.9f;
            float energy;
            float n;
            float transferMoles = Mathf.Min(n1, n2);
            GasMixture fromMix = __instance.InputNetwork.Atmosphere.Remove(transferMoles);
            foreach (GasFilter gasFilter in __instance.GasFilters)
            {
                float g = gasFilter.Quantity;
                n = gasFilter.FilterGas(ref fromMix, ref outputAtmos1.GasMixture, inputAtmos1, AroMath.MinimumRatioToFilterAll);
                gasFilter.Quantity = g - 0.01f * n / (int)gasFilter.TicksBeforeDegrade;
                energy = AroMath.CalcEnergyGasCompression(inputAtmos1, outputAtmos1, n);
                AroMath.AlterEnergy(ref outputAtmos1.GasMixture, energy);
            }
            n = fromMix.TotalMolesGassesAndLiquids;
            __instance.OutputNetwork2.Atmosphere.Add(fromMix);
            energy = AroMath.CalcEnergyGasCompression(inputAtmos1, outputAtmos2, n);
            AroMath.AlterEnergy(ref outputAtmos2.GasMixture, energy);
            return false;
        }
    }
    [HarmonyPatch(typeof(PowerGeneratorPipe), nameof(PowerGeneratorPipe.OnAtmosphericTick))]
    public class PowerGeneratorPipePowerPatch
    {
        [UsedImplicitly]
        public static void Prefix(PowerGeneratorPipe __instance)
        {
            if (!__instance.OnOff || __instance.InputNetwork == null || __instance.OutputNetwork == null)
            {
                return;
            }
            __instance.PressurePerTick = __instance.InputNetwork.Atmosphere.PressureGasses;
        }
        [UsedImplicitly]
        public static void Postfix(PowerGeneratorPipe __instance, ref float ____energyAsPower)
        {
            if (!__instance.OnOff)
            {
                return;
            }
            float overPressure = __instance.InternalAtmosphere.PressureGasses - AroMath.GFGmaxPressure;
            float damage = overPressure / 1000f;
            if (damage > 0f)
            {
                __instance.DamageState.Damage(ChangeDamageType.Increment, damage, DamageUpdateType.Brute);
            }
            if (__instance.InputNetwork == null || __instance.OutputNetwork == null)
            {
                return;
            }
            float energy = AroMath.CalcEnergyGasCompression(__instance.InternalAtmosphere, __instance.OutputNetwork.Atmosphere, __instance.InternalAtmosphere.TotalMoles);
            AroMath.AlterEnergy(ref __instance.OutputNetwork.Atmosphere.GasMixture, energy);
            energy = Mathf.Max(energy * AroMath.CompressEnergyPowerFactor, 0f);
            AroMath.debug3 = energy;
            AroMath.debug = ____energyAsPower;
            if (____energyAsPower >= energy)
            {
                ____energyAsPower = ____energyAsPower - energy;
            }
            else
            {
                OnServer.Interact(__instance.InteractOnOff, 0);
                ____energyAsPower = 0f;
            }
            AroMath.debug2 = ____energyAsPower;
        }
    }
    [HarmonyPatch(typeof(PowerGeneratorPipe), nameof(PowerGeneratorPipe.OnThreadUpdate))]
    public class PowerGeneratorPipeNeedlePatch
    {
        [UsedImplicitly]
        public static void Postfix(PowerGeneratorPipe __instance, ref float ____pressureRating, ref float ____needleRotation)
        {
            ____pressureRating = __instance.InternalAtmosphere.PressureGassesAndLiquids / AroMath.GFGmaxPressure;
            ____needleRotation = Mathf.Lerp(__instance.NeedleMinimum, __instance.NeedleMaximum, ____pressureRating);
        }

    }
    [HarmonyPatch(typeof(AirConditioner), nameof(AirConditioner.OnAtmosphericTick))]
    public class AirConditionerPatch
    {
        [UsedImplicitly]
        public static bool Prefix(AirConditioner __instance, ref float ____powerUsedDuringTick)
        {
            __instance.ThermodynamicsScale = 0f;
            if (!__instance.OnOff || !__instance.Powered || __instance.Mode == 0 || !__instance.IsFullyConnected )
            {
                ____powerUsedDuringTick = 0f;
                return false;
            }
            Atmosphere input = __instance.InputNetwork.Atmosphere;
            Atmosphere output = __instance.OutputNetwork.Atmosphere;
            Atmosphere waste = __instance.OutputNetwork2.Atmosphere;
            Atmosphere internalAtmos = __instance.InternalAtmosphere;
            AroMath.Equalize(input, internalAtmos, float.MaxValue, 1f, 0f, Atmosphere.MatterState.All);
            float energy = AroMath.GetEnergyToTarget(internalAtmos, __instance.GoalTemperature);

            float pe = Mathf.Clamp01(Mathf.Min(internalAtmos.PressureGasses / 101.325f, waste.PressureGasses / 101.325f));
            float iwe = Mathf.Min(__instance.InputAndWasteEfficiency.Evaluate(internalAtmos.GasMixture.Temperature), __instance.InputAndWasteEfficiency.Evaluate(waste.GasMixture.Temperature));
            float tde = internalAtmos.Temperature / waste.Temperature;
            tde = (__instance.GoalTemperature > internalAtmos.Temperature) ? 1f / tde : tde; // heating?
            energy = energy * Mathf.Clamp01(pe * iwe * tde);
            ____powerUsedDuringTick = Mathf.Abs(energy) * AroMath.CompressEnergyPowerFactor / tde;
            AroMath.AlterEnergy(ref waste.GasMixture, -energy);
            AroMath.AlterEnergy(ref internalAtmos.GasMixture, energy);
            __instance.TemperatureDifferentialEfficiency = tde;
            __instance.OperationalTemperatureLimitor = iwe;
            __instance.OptimalPressureScalar = pe;
            AroMath.Equalize(internalAtmos, output, float.MaxValue, 1f, 0f, Atmosphere.MatterState.All);
            return false;
        }
    }
    [HarmonyPatch(typeof(H2CombustorMachine), nameof(H2CombustorMachine.OnAtmosphericTick))]
    public class H2CombustorMachinePatch
    {
        [UsedImplicitly]
        public static bool Prefix(H2CombustorMachine __instance)
        {
            if (!__instance.OnOff || !__instance.Powered || !__instance.IsOperable || __instance.Mode != 1)
            {
                if (__instance.Activate == 1)
                {
                    OnServer.Interact(__instance.InteractActivate, 0);
                }
                return false;
            }
            Atmosphere input = __instance.InputNetwork.Atmosphere;
            Atmosphere output = __instance.OutputNetwork.Atmosphere;
            Atmosphere internalAtmos = __instance.InternalAtmosphere;

            AroMath.Equalize(input, internalAtmos, float.MaxValue, 1f, 0f, Atmosphere.MatterState.All);
            if (__instance.Activate == 1)
            {
                OnServer.Interact(__instance.InteractActivate, 0);
            }
            internalAtmos.CombustForWater(H2CombustorMachine.WaterRatio);
            internalAtmos.ManualCombust(0.9f);
            AroMath.Equalize(internalAtmos, output, float.MaxValue, 1f, 0f, Atmosphere.MatterState.Liquid);
            if (__instance.OutputNetwork2 != null)
            {
                Atmosphere waste = __instance.OutputNetwork2.Atmosphere;
                AroMath.Equalize(internalAtmos, waste, float.MaxValue, 1f, 0f, Atmosphere.MatterState.Gas);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(AtmosAnalyser), nameof(AtmosAnalyser.OnPreScreenUpdate))]
    public class AtmosAnalyserOnPreScreenUpdate
    {
        [UsedImplicitly]
        public static void Postfix(AtmosAnalyser __instance, ref string ____energyConvectedText, ref string ____energyRadiatedText, ref string ____pressureValueText)
        {
            ____energyConvectedText += "\n" + Math.Round(AroMath.debug,3);
            ____energyRadiatedText += "\n" + Math.Round(AroMath.debug2, 3);
            ____pressureValueText += "\n " + Math.Round(AroMath.debug3, 3);

        }

    }
    public class AroMath
    {
        public const float CompressEnergyPowerFactor = 0.1f;
        public const float atm = 101.325f;
        public const float R = 8.3144f;
        public const float GFGmaxPressure = 5000f;
        public static readonly float MinimumRatioToFilterAll = 0.001f;
        public static float debug;
        public static float debug2;
        public static float debug3;
        public static float GetEnergyToTarget(Atmosphere inputAtmos , float targetTemperature)
        {
            return (targetTemperature - inputAtmos.Temperature) * inputAtmos.GasMixture.HeatCapacity; //  inputAtmos.TotalMoles *
        }
        public static float GetEnergyToEqualize(Atmosphere inputAtmos, Atmosphere outputAtmos)
        {
            float numerator = (inputAtmos.GasMixture.TotalEnergy * inputAtmos.GasMixture.HeatCapacity - outputAtmos.GasMixture.TotalEnergy * outputAtmos.GasMixture.HeatCapacity);
            float denominator = inputAtmos.GasMixture.HeatCapacity + outputAtmos.GasMixture.HeatCapacity;
            return numerator / denominator;
        }
        public static void BiDirectional(Atmosphere inputAtmos, Atmosphere outputAtmos, float amountPressureToMove, float mixRate, Atmosphere.MatterState typeToMove)
        {
            float outputPressure = outputAtmos.Pressure(typeToMove);
            float inputPressure = inputAtmos.Pressure(typeToMove);
            if (Mathf.Abs(outputPressure - inputPressure) < 5f)
            {
                Mix(inputAtmos, outputAtmos, mixRate, typeToMove);
            }
            else
            {
                if (outputPressure > inputPressure)
                {
                    Equalize(outputAtmos, inputAtmos, amountPressureToMove, mixRate, 0f, typeToMove);
                }
                else
                {
                    Equalize(inputAtmos, outputAtmos, amountPressureToMove, mixRate, 0f, typeToMove);
                }
            }
        }
        public static void Mix(Atmosphere inputAtmos, Atmosphere outputAtmos, float mixRate, Atmosphere.MatterState typeToMove)
        {
            if (inputAtmos != null && outputAtmos != null && mixRate > 0f)
            {
                GasMixture gasMixture = GasMixture.Create();
                float num = 0f;
                mixRate = Mathf.Clamp01(mixRate);
                gasMixture.Add(inputAtmos.Remove(inputAtmos.TotalMoles * mixRate));
                num += inputAtmos.Volume;
                gasMixture.Add(inputAtmos.Remove(outputAtmos.TotalMoles * mixRate));
                num += outputAtmos.Volume;
                inputAtmos.GasMixture.Add(gasMixture.Remove(gasMixture.TotalMoles(typeToMove) * inputAtmos.Volume / num, typeToMove));
                outputAtmos.GasMixture.Add(gasMixture.Remove(gasMixture.TotalMoles(typeToMove) * outputAtmos.Volume / num, typeToMove));
            }
        }
        public static float ActiveEqualize(Atmosphere inputAtmos, Atmosphere outputAtmos, float baseLiter, float maxDelta, float eqRate, Atmosphere.MatterState typeToMove)
        {
            float dp = Mathf.Abs(inputAtmos.PressureGassesAndLiquids - outputAtmos.PressureGassesAndLiquids);
            float liter = (1 - Mathf.Clamp01(dp / maxDelta)) * baseLiter;
            float literN = GetVolumeMole(inputAtmos, liter, Atmosphere.MatterState.All);
            float equalizN = GetEqualizeMole(inputAtmos, outputAtmos, float.MaxValue,0) * eqRate;
            float energy = MoveMassEnergy(inputAtmos, outputAtmos, Mathf.Max(equalizN, literN), typeToMove);
            return energy;
        }
        public static float Equalize(Atmosphere inputAtmos, Atmosphere outputAtmos, float desiredPressureChange, float mixRate, float ActivePressureDifference, Atmosphere.MatterState typeToMove)
        {
            mixRate = Mathf.Clamp01(mixRate);
            float n = GetEqualizeMole(inputAtmos, outputAtmos, desiredPressureChange, ActivePressureDifference) * mixRate;
            float energy = MoveMassEnergy(inputAtmos, outputAtmos, n, typeToMove);
            return energy;
        }
        public static float GetEqualizeMole(Atmosphere inputAtmos, Atmosphere outputAtmos, float desiredPressureChange, float ActivePressureDifference)
        {
            float dp = Mathf.Min(desiredPressureChange, inputAtmos.PressureGassesAndLiquids - outputAtmos.PressureGassesAndLiquids + ActivePressureDifference);
            float n = 0f;
            if (dp > 0f)
            {
                float outTemp = outputAtmos.Temperature;
                //outTemp = (outTemp == 0f) ? inputAtmos.Temperature : outTemp;
                outTemp = Mathf.Max(outTemp, inputAtmos.Temperature);
                float num2 = 8.3144f * inputAtmos.Temperature / inputAtmos.Volume;
                float num3 = 8.3144f * outTemp / outputAtmos.Volume;
                float num4 = num2 + num3;
                n = dp / num4;
            }
            n = Mathf.Min(n, inputAtmos.TotalMoles / 2f);
            return n; 
        }
        public static float GetVolumeMole(Atmosphere inputAtmos, float volume, Atmosphere.MatterState matterStateToMove)
        {
            float num = Mathf.Clamp(volume / (inputAtmos.Volume + volume), 0f, inputAtmos.Volume);
            float n = num * inputAtmos.GasMixture.TotalMoles(matterStateToMove);
            return n;
        }
        public static float CompressVolume(Atmosphere inputAtmos, Atmosphere outputAtmos, float volume, Atmosphere.MatterState matterStateToMove)
        {
            float n = GetVolumeMole(inputAtmos,volume,matterStateToMove);
            float energy = MoveMassEnergy(inputAtmos,outputAtmos,n,matterStateToMove);
            return energy;
        }
        public static float MoveMassEnergy(Atmosphere inputAtmos, Atmosphere outputAtmos, float n, Atmosphere.MatterState matterStateToMove)
        {
            if (n <= 0f)
            {
                return 0f;
            }
            GasMixture gasMixture = inputAtmos.Remove(n, matterStateToMove);
            outputAtmos.Add(gasMixture);
            float energy = CalcEnergyGasCompression(inputAtmos, outputAtmos, n);
            AlterEnergy(ref outputAtmos.GasMixture, energy);
            return energy;
        }
        public static void AlterEnergy(ref GasMixture gasMixture, float energy)
        {
            if (energy >= 0f)
            {
                gasMixture.AddEnergy(energy);
            }
            else
            {
                gasMixture.RemoveEnergy(-energy);
            }
        }
        public static float CalcEnergyGasCompression(Atmosphere inputAtmos, Atmosphere outputAtmos, float n)
        {
            if (n <= 0f)
            {
                return 0f;
            }
            float Ratio = Mathf.Clamp(Mathf.Max(3,outputAtmos.PressureGassesAndLiquids) / Mathf.Max(3, inputAtmos.PressureGassesAndLiquids),float.Epsilon,float.MaxValue);

            //float inTemp = inputAtmos.Temperature;
            //float outTemp = outputAtmos.Temperature;
            //inTemp = (inTemp == 0f) ? outTemp : inTemp;
            //outTemp = (outTemp == 0f) ? inTemp : outTemp;
            //float Ratio2 = Mathf.Clamp(Mathf.Max(1, inTemp) / Mathf.Max(1, outTemp), float.Epsilon, float.MaxValue);
            //Ratio = Ratio * Ratio2;

            float energy = n * 300f * Mathf.Log(Ratio);
            return energy;
        }
    }
}
