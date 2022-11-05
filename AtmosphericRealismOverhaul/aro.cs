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
using static Assets.Scripts.Atmospherics.Atmosphere;

namespace AtmosphericRealismOverhaul
{
    [HarmonyPatch(typeof(PressureRegulator), nameof(PressureRegulator.OnAtmosphericTick))]
    public class RegulatorPatch
    {
        [UsedImplicitly]
        public static bool Prefix(PressureRegulator __instance)
        {
            __instance.UsedPower = 10;
            __instance.MaxSetting = 60000;
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
            AroMath.Equalize(__instance.InputNetwork.Atmosphere, __instance.OutputNetwork.Atmosphere, dp, 1f, MatterState.All);
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
                __result = -1f;
            }
            else if (!__instance.OnOff)
            {
                __result = 0f;
            }
            else if (__instance.OutputSetting <= 0f)
            {
                __result = __instance.UsedPower;
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
                __instance.UsedPower = 10f;
                return false;
            }
            float num = Mathf.Max(AroMath.CompressVolume(inputAtmosphere, outputAtmosphere, setting, MatterState.All), setting) * AroMath.CompressEnergyPowerFactor;
            __instance.UsedPower = num + 10f;
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
                    num = AroMath.CompressVolume(__instance.InputNetwork.Atmosphere, __instance.InternalAtmosphere, __instance.OutputSetting2, MatterState.Gas);
                    energy += Mathf.Max(num, __instance.OutputSetting2);
                }
                if (__instance.OutputNetwork != null)
                {
                    num = AroMath.CompressVolume(__instance.InternalAtmosphere, __instance.OutputNetwork.Atmosphere, __instance.OutputSetting, MatterState.Gas);
                    energy += Mathf.Max(num, __instance.OutputSetting);
                }
                if (__instance.OutputNetwork2 != null)
                {
                    num = AroMath.CompressVolume(__instance.InternalAtmosphere, __instance.OutputNetwork2.Atmosphere, __instance.OutputSetting, MatterState.Liquid);
                    energy += Mathf.Max(num, __instance.OutputSetting);
                }
            }
            __instance.UsedPower = energy * AroMath.CompressEnergyPowerFactor + 10f;
            return false;
        }
    }
    //[HarmonyPatch(typeof(Atmosphere), nameof(Atmosphere.Mix), new Type[] { typeof(Atmosphere), typeof(Atmosphere), typeof(MatterState) })]
    public class AtmosphereMixPatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, MatterState matterState)
        {
            if (inputAtmos != null && outputAtmos != null)
            {
            }
            return false;
        }
    }
    //[HarmonyPatch(typeof(Atmosphere), nameof(Atmosphere.Mix), new Type[] { typeof(Atmosphere), typeof(Atmosphere) })]
    public class AtmosphereMix2Patch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos)
        {
            if (inputAtmos != null && outputAtmos != null)
            {
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(Atmosphere), nameof(Atmosphere.EqualizeBothWays))]
    public class AtmosphereEqualizeBothWaysatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, float scale)
        {
            if (inputAtmos != null && outputAtmos != null)
            {
                AroMath.BiDirectional(inputAtmos, outputAtmos, float.MaxValue, scale, MatterState.All);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(DeviceAtmospherics), nameof(DeviceAtmospherics.MoveVolume), new Type[] { typeof(Atmosphere), typeof(Atmosphere), typeof(float), typeof(MatterState) })]
    public class DeviceAtmosphericsMoveVolumePatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, float setting, MatterState matterStateToMove)
        {
            if (inputAtmos.PressureGassesAndLiquids < 0.001f && outputAtmos == null)
            {
                inputAtmos.GasMixture.Reset();
                return false;
            }
            AroMath.CompressVolume(inputAtmos, outputAtmos, setting, matterStateToMove);
            return false;
        }
    }
    [HarmonyPatch(typeof(DeviceAtmospherics), nameof(DeviceAtmospherics.MoveToEqualize), new Type[] { typeof(Atmosphere), typeof(Atmosphere), typeof(float), typeof(MatterState) })]
    public class DeviceAtmosphericsEqualizePatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, float desiredPressureChange, MatterState typeToMove)
        {
            if (inputAtmos == null || outputAtmos == null)
            {
                return false;
            }
            AroMath.Equalize(inputAtmos, outputAtmos, desiredPressureChange, 1f, typeToMove);
            return false;
        }
    }
    [HarmonyPatch(typeof(DeviceAtmospherics), nameof(DeviceAtmospherics.MoveToEqualizeBidirectional))]
    public class DeviceAtmosphericsBidirectionalPatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, float amountPressureToMove, MatterState typeToMove)
        {
            AroMath.BiDirectional(inputAtmos, outputAtmos, amountPressureToMove, 0.8f, typeToMove);
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
                        AroMath.ActiveEqualize(worldAtmosphere, pipeAtmosphere, 1000f, AroMath.atm * 2f, 0.8f, MatterState.All);
                    }
                    break;
                case VentDirection.Outward:
                    //move gas from pipe to world (mode=0)
                    if (worldAtmosphere.PressureGassesAndLiquids < __instance.ExternalPressure && pipeAtmosphere.PressureGassesAndLiquids > __instance.InternalPressure)
                    {
                        AroMath.ActiveEqualize(pipeAtmosphere, worldAtmosphere, 100f, AroMath.atm * 2f, 0.6f, MatterState.All);
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
            AroMath.BiDirectional(inputAtmos, outputAtmos, float.MaxValue, __instance.OutputSetting / 110f, MatterState.All);
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
            float n1 = AroMath.GetEqualizeMole(inputAtmos1, outputAtmos, float.MaxValue) * mixRate * 0.5f;
            float n2 = AroMath.GetEqualizeMole(inputAtmos2, outputAtmos, float.MaxValue) * mixRate * 0.5f;
            if (n1 * Ratio2 < n2 * Ratio1)
            {
                n2 = n1 * Ratio2 / Ratio1;
                AroMath.MoveMassEnergy(inputAtmos1, outputAtmos, n1, MatterState.All);
                AroMath.MoveMassEnergy(inputAtmos2, outputAtmos, n2, MatterState.All);
            }
            else
            {
                n1 = n2 * Ratio1 / Ratio2;
                AroMath.MoveMassEnergy(inputAtmos2, outputAtmos, n2, MatterState.All);
                AroMath.MoveMassEnergy(inputAtmos1, outputAtmos, n1, MatterState.All);
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
            float n1 = AroMath.GetEqualizeMole(inputAtmos1, outputAtmos1, float.MaxValue);
            float n2 = AroMath.GetEqualizeMole(inputAtmos1, outputAtmos2, float.MaxValue);
            float n1o = 0;
            float n2o = 0;
            foreach (GasType type in AroMath.gasTypes)
            {
                bool noFilter = true;
                float ratio = inputAtmos1.GasMixture.GetGasTypeRatio(type);
                foreach (GasFilter gasFilter in __instance.GasFilters)
                {
                    if (gasFilter.Quantity > 0f && gasFilter.FilterType == type)
                    {
                        noFilter = false;
                        Mole mole = inputAtmos1.GasMixture.Remove(type, n1 * ratio);
                        n1o += mole.Quantity;
                        outputAtmos1.GasMixture.Add(mole);
                        gasFilter.Quantity -= 0.5f * mole.Quantity / (int)gasFilter.TicksBeforeDegrade;
                        break;
                    }
                }
                if (noFilter)
                {
                    Mole mole = inputAtmos1.GasMixture.Remove(type, n2 * ratio);
                    n2o += mole.Quantity;
                    outputAtmos2.GasMixture.Add(mole);
                }
            }
            float energy = AroMath.CalcEnergyGasCompression(inputAtmos1, outputAtmos1, n1o);
            AroMath.AlterEnergy(outputAtmos1, energy);
            energy = AroMath.CalcEnergyGasCompression(inputAtmos1, outputAtmos2, n2o);
            AroMath.AlterEnergy(outputAtmos2, energy);
            AroAtmosphereDataController.Instance.AddFlow(inputAtmos1, -n1o - n2o, 0);
            AroAtmosphereDataController.Instance.AddFlow(outputAtmos1, n1o, energy);
            AroAtmosphereDataController.Instance.AddFlow(outputAtmos2, n2o, energy);
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
            AroMath.AlterEnergy(__instance.OutputNetwork.Atmosphere, energy);
            energy = Mathf.Max(energy * AroMath.CompressEnergyPowerFactor, 0f);
            if (____energyAsPower >= energy)
            {
                ____energyAsPower = ____energyAsPower - energy;
            }
            else
            {
                OnServer.Interact(__instance.InteractOnOff, 0);
                ____energyAsPower = 0f;
            }
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
    [HarmonyPatch(typeof(AirConditioner), nameof(AirConditioner.ReceivePower))]
    public class AirConditionerReceivePowerPatch
    {
        [UsedImplicitly]
        public static bool Prefix(float powerAdded, ref float ____powerUsedDuringTick)
        {
            ____powerUsedDuringTick = Mathf.Max(____powerUsedDuringTick - powerAdded, 0f);
            return false;
        }
    }
    [HarmonyPatch(typeof(AirConditioner), nameof(AirConditioner.OnAtmosphericTick))]
    public class AirConditionerOnAtmosphericTickPatch
    {
        [UsedImplicitly]
        public static bool Prefix(AirConditioner __instance, ref float ____powerUsedDuringTick)
        {
            __instance.ThermodynamicsScale = 0f;
            if (!__instance.OnOff || !__instance.Powered || __instance.Mode == 0 || !__instance.IsFullyConnected || ____powerUsedDuringTick > 0f)
            {
                return false;
            }
            Atmosphere input = __instance.InputNetwork.Atmosphere;
            Atmosphere output = __instance.OutputNetwork.Atmosphere;
            Atmosphere waste = __instance.OutputNetwork2.Atmosphere;
            Atmosphere internalAtmos = __instance.InternalAtmosphere;
            AroMath.Equalize(input, internalAtmos, float.MaxValue, 1f, MatterState.All);
            float energy = AroMath.GetEnergyToTarget(internalAtmos, __instance.GoalTemperature);
            energy = Mathf.Clamp(energy, -14000f, 14000f);
            float pe = Mathf.Clamp01(Mathf.Min(internalAtmos.PressureGasses / 101.325f, waste.PressureGasses / 101.325f));
            float iwe = Mathf.Min(__instance.InputAndWasteEfficiency.Evaluate(internalAtmos.GasMixture.Temperature), __instance.InputAndWasteEfficiency.Evaluate(waste.GasMixture.Temperature));
            float tde = internalAtmos.Temperature / waste.Temperature;
            tde = (__instance.GoalTemperature > internalAtmos.Temperature) ? 1f / tde : tde; // heating?
            ____powerUsedDuringTick = Mathf.Abs(energy) * AroMath.CompressEnergyPowerFactor / Mathf.Max(pe * iwe * tde, 0.000001f);
            AroMath.AlterEnergy(waste, -energy);
            AroMath.AlterEnergy(internalAtmos, energy);
            AroAtmosphereDataController.Instance.AddFlow(waste, 0, -energy);
            AroAtmosphereDataController.Instance.AddFlow(internalAtmos, 0, energy);
            __instance.TemperatureDifferentialEfficiency = tde;
            __instance.OperationalTemperatureLimitor = iwe;
            __instance.OptimalPressureScalar = pe;
            AroMath.Equalize(internalAtmos, output, float.MaxValue, 1f, MatterState.All);
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

            AroMath.Equalize(input, internalAtmos, float.MaxValue, 1f, MatterState.All);
            if (__instance.Activate == 1)
            {
                OnServer.Interact(__instance.InteractActivate, 0);
            }
            internalAtmos.CombustForWater(H2CombustorMachine.WaterRatio);
            internalAtmos.ManualCombust(0.9f);
            AroMath.Equalize(internalAtmos, output, float.MaxValue, 1f, MatterState.Liquid);
            if (__instance.OutputNetwork2 != null)
            {
                Atmosphere waste = __instance.OutputNetwork2.Atmosphere;
                AroMath.Equalize(internalAtmos, waste, float.MaxValue, 1f, MatterState.Gas);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(TurbineGenerator), nameof(TurbineGenerator.OnAtmosphericTick))]
    public class TurbineGeneratorPatch
    {
        [UsedImplicitly]
        public static bool Prefix(TurbineGenerator __instance, ref float ____generatedPower)
        {
            ____generatedPower = 0f;
            if (__instance.GridController.CanContainAtmos(__instance.ForwardGrid) && __instance.GridController.CanContainAtmos(__instance.BackwardGrid))
            {
                Atmosphere atmosphere = __instance.GridController.AtmosphericsController.CloneGlobalAtmosphere(__instance.ForwardGrid, 0L);
                Atmosphere atmosphere2 = __instance.GridController.AtmosphericsController.CloneGlobalAtmosphere(__instance.BackwardGrid, 0L);
                float energy = AroMath.BiDirectional(atmosphere, atmosphere2, float.MaxValue, 1f, MatterState.All);
                ____generatedPower = Mathf.Abs(energy) * AroMath.CompressEnergyPowerFactor;
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(AtmosAnalyser), nameof(AtmosAnalyser.OnPreScreenUpdate))]
    public class AtmosAnalyserOnPreScreenUpdate
    {
        [UsedImplicitly]
        public static void Postfix(AtmosAnalyser __instance, ref string ____temperatureValueText, ref string ____energyConvectedText, ref string ____energyRadiatedText, ref string ____pressureValueText)
        {
            //____energyConvectedText += "\n" + Math.Round(AroMath.debug, 3);
            //____energyRadiatedText += "\n" + Math.Round(AroMath.debug2, 3);
            Atmosphere atmosphere = __instance.ScannedAtmosphere;
            if (atmosphere != null && (atmosphere.Mode == Atmosphere.AtmosphereMode.Network || atmosphere.Mode == Atmosphere.AtmosphereMode.Thing))
            {
                AroAtmosphereData data = AroAtmosphereDataController.Instance.GetAroAtmosphereData(atmosphere);
                if (data != null)
                {
                    ____temperatureValueText += "\n" + AroMath.DisplayUnitCleaner(data.EnergyFlowLastTick,"J");
                    ____pressureValueText += "\n" + AroMath.DisplayUnitCleaner(data.MassFlowLastTick, "mol");
                }
            }
        }

    }
    [HarmonyPatch(typeof(AtmosphericsManager), nameof(AtmosphericsManager.ThingAtmosphereTick))]
    public class AtmosphericsManagerThingAtmosphereTickPatch
    {
        [UsedImplicitly]
        public static void Prefix()
        {
            AroAtmosphereDataController.Instance.MoveHistoricValues();
        }
    }
    [HarmonyPatch(typeof(AtmosphericsManager), nameof(AtmosphericsManager.StartManager))]
    public class AtmosphericsManagerStartManagerPatch
    {
        [UsedImplicitly]
        public static void Postfix()
        {
            AroAtmosphereDataController.Instance = new AroAtmosphereDataController();
        }
    }
    public class AroMath
    {
        public const float CompressEnergyPowerFactor = 0.1f;
        public const float atm = 101.325f;
        public const float R = 8.3144f;
        public const float GFGmaxPressure = 5000f;
        public static readonly float MinimumRatioToFilterAll = 0.001f;
        public static GasType[] gasTypes = new GasType[]
        { GasType.CarbonDioxide, GasType.Nitrogen, GasType.NitrousOxide, GasType.Oxygen, GasType.Pollutant, GasType.Volatiles, GasType.Water };
        public static float debug;
        public static float debug2;
        public static float debug3;
        public static string DisplayUnitCleaner(float baseValue, string unit)
        {
            float value = baseValue*1000;
            string valueUnit = "m" + unit;
            if (Mathf.Abs(value) > 1000)
            {
                value = value / 1000;
                valueUnit = ""+ unit;
            }
            if (Mathf.Abs(value) > 1000)
            {
                value = value / 1000;
                valueUnit = "k" + unit;
            }
            if (Mathf.Abs(value) > 1000)
            {
                value = value / 1000;
                valueUnit = "M"+ unit;
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
        public static float BiDirectional(Atmosphere inputAtmos, Atmosphere outputAtmos, float amountPressureToMove, float mixRate, MatterState typeToMove)
        {
            float outputPressure = outputAtmos.Pressure(typeToMove);
            float inputPressure = inputAtmos.Pressure(typeToMove);
            float ratio = outputPressure / inputPressure;
            float energy = 0;
            if (ratio > 0.90f && ratio < 1.10f)
            {
                Mix(inputAtmos, outputAtmos, mixRate, typeToMove);
            }
            else
            {
                if (outputPressure > inputPressure)
                {
                    energy += Equalize(outputAtmos, inputAtmos, amountPressureToMove, mixRate, typeToMove);
                }
                else
                {
                    energy += Equalize(inputAtmos, outputAtmos, amountPressureToMove, mixRate, typeToMove);
                }
            }
            return energy;
        }
        private static void Mix(Atmosphere inputAtmos, Atmosphere outputAtmos, float mixRate, MatterState typeToMove)
        {
            if (inputAtmos != null && outputAtmos != null && mixRate > 0f)
            {
                GasMixture gasMixture = GasMixture.Create();
                float volumeInn = inputAtmos.Volume;
                float volumeOut = outputAtmos.Volume;
                float totalVolume = volumeInn + volumeOut;
                mixRate = Mathf.Clamp01(mixRate);
                gasMixture.Add(inputAtmos.Remove(inputAtmos.TotalMoles * mixRate, typeToMove));
                gasMixture.Add(inputAtmos.Remove(outputAtmos.TotalMoles * mixRate, typeToMove));
                float totalN = gasMixture.TotalMoles(typeToMove);
                inputAtmos.GasMixture.Add(gasMixture.Remove(totalN * (volumeInn / totalVolume), typeToMove));
                outputAtmos.GasMixture.Add(gasMixture);
                gasMixture.Reset();
            }
        }
        public static float ActiveEqualize(Atmosphere inputAtmos, Atmosphere outputAtmos, float baseLiter, float maxDelta, float eqRate, MatterState typeToMove)
        {
            float dp = Mathf.Abs(inputAtmos.PressureGassesAndLiquids - outputAtmos.PressureGassesAndLiquids);
            float liter = (1 - Mathf.Clamp01(dp / maxDelta)) * baseLiter;
            float literN = GetVolumeMole(inputAtmos, liter, typeToMove);
            float equalizN = GetEqualizeMole(inputAtmos, outputAtmos, float.MaxValue) * eqRate;
            float energy = MoveMassEnergy(inputAtmos, outputAtmos, Mathf.Max(equalizN, literN), typeToMove);
            return energy;
        }
        public static float Equalize(Atmosphere inputAtmos, Atmosphere outputAtmos, float desiredPressureChange, float mixRate, MatterState typeToMove)
        {
            mixRate = Mathf.Clamp01(mixRate);
            float n = GetEqualizeMole(inputAtmos, outputAtmos, desiredPressureChange) * mixRate;
            float energy = MoveMassEnergy(inputAtmos, outputAtmos, n, typeToMove);
            return energy;
        }
        public static float GetEqualizeMole(Atmosphere inputAtmos, Atmosphere outputAtmos, float desiredPressureChange)
        {
            float dp = Mathf.Min(desiredPressureChange, inputAtmos.PressureGassesAndLiquids - outputAtmos.PressureGassesAndLiquids);
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
            n = Mathf.Min(n, inputAtmos.TotalMoles / 2f);
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
        public static float MoveMassEnergy(Atmosphere inputAtmos, Atmosphere outputAtmos, float n, MatterState matterStateToMove)
        {
            if (n <= 0f)
            {
                return 0f;
            }
            GasMixture gasMixture = inputAtmos.Remove(n, matterStateToMove);
            outputAtmos.Add(gasMixture);
            float energy = CalcEnergyGasCompression(inputAtmos, outputAtmos, n);
            AlterEnergy(outputAtmos, energy);
            AroAtmosphereDataController.Instance.AddFlow(inputAtmos, -n,0);
            AroAtmosphereDataController.Instance.AddFlow(outputAtmos, n, energy);
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
    public class AroAtmosphereDataController
    {
        public static AroAtmosphereDataController Instance;
        public AroAtmosphereDataController()
        {
            AroAtmosphereDataList = new List<AroAtmosphereData>();
        }
        private List<AroAtmosphereData> AroAtmosphereDataList;
        public void MoveHistoricValues()
        {
            foreach (AroAtmosphereData item in AroAtmosphereDataList)
            {
                item.EnergyFlowLastTick = item.EnergyThisTick;
                item.EnergyThisTick = 0;
                item.MassFlowLastTick = item.MassThisTick;
                item.MassThisTick = 0;
            }
        }

        public void AddFlow(Atmosphere atmosphere,float mass, float energy)
        {
            if (atmosphere.Mode == Atmosphere.AtmosphereMode.World || atmosphere.Mode == Atmosphere.AtmosphereMode.Global)
            {
                return;
            }
            foreach (AroAtmosphereData item in AroAtmosphereDataList)
            {
                if (item.Atmosphere == atmosphere)
                {
                    AddFlow(item, mass, energy);
                    return;
                }
            }
            AroAtmosphereData newAroData = new AroAtmosphereData(atmosphere);
            AroAtmosphereDataList.Add(newAroData);
            AddFlow(newAroData, mass, energy);
        }

        private void AddFlow(AroAtmosphereData atmosphereData, float mass, float energy)
        {
            atmosphereData.MassThisTick += Mathf.Abs(mass);
            atmosphereData.EnergyThisTick += energy;
        }

        public AroAtmosphereData GetAroAtmosphereData(Atmosphere atmosphere)
        {
            foreach (AroAtmosphereData item in AroAtmosphereDataList)
            {
                if (item.Atmosphere == atmosphere)
                {
                    return item;
                }
            }
            return null;
        }

    }

    public class AroAtmosphereData
    {
        public AroAtmosphereData(Atmosphere atmosphere)
        {
            Atmosphere = atmosphere;
        }
        public Atmosphere Atmosphere;
        public float MassFlowLastTick { get; set; }
        public float MassThisTick { get; set; }
        public float EnergyFlowLastTick { get; set; }
        public float EnergyThisTick { get; set; }
    }
}
