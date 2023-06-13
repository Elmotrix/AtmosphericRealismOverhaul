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
using Assets.Scripts.GridSystem;

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
            AroFlow.Equalize(__instance.InputNetwork.Atmosphere, __instance.OutputNetwork.Atmosphere, dp, 0.8f, Atmosphere.MatterState.All);
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
            float energy = AroFlow.CompressVolume(inputAtmosphere, outputAtmosphere, setting, Atmosphere.MatterState.All);
            energy = Mathf.Max(energy, setting) * AroEnergy.CompressEnergyPowerFactor;
            __instance.UsedPower = energy + 10f;
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
                    num = AroFlow.CompressVolume(__instance.InputNetwork.Atmosphere, __instance.InternalAtmosphere, __instance.OutputSetting2, Atmosphere.MatterState.Gas);
                    energy += Mathf.Max(num, __instance.OutputSetting2);
                }
                if (__instance.OutputNetwork != null)
                {
                    num = AroFlow.CompressVolume(__instance.InternalAtmosphere, __instance.OutputNetwork.Atmosphere, __instance.OutputSetting, Atmosphere.MatterState.Gas);
                    energy += Mathf.Max(num, __instance.OutputSetting);
                }
                if (__instance.OutputNetwork2 != null)
                {
                    num = AroFlow.CompressVolume(__instance.InternalAtmosphere, __instance.OutputNetwork2.Atmosphere, __instance.OutputSetting, Atmosphere.MatterState.Liquid);
                    energy += Mathf.Max(num, __instance.OutputSetting);
                }
            }
            __instance.UsedPower = energy * AroEnergy.CompressEnergyPowerFactor + 10f;
            return false;
        }
    }
    [HarmonyPatch(typeof(Atmosphere), nameof(Atmosphere.Mix), new Type[] { typeof(Atmosphere), typeof(Atmosphere), typeof(MatterState) })]
    public class AtmosphereMixPatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, MatterState matterState)
        {
            if (inputAtmos != null && outputAtmos != null)
            {
                AroFlow.BiDirectional(inputAtmos,outputAtmos, typeToMove: matterState);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(Atmosphere), nameof(Atmosphere.Mix), new Type[] { typeof(Atmosphere), typeof(Atmosphere) })]
    public class AtmosphereMix2Patch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos)
        {
            if (inputAtmos != null && outputAtmos != null)
            {
                AroFlow.BiDirectional(inputAtmos, outputAtmos); // , mixThreshold: 0.5f
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
                AroFlow.BiDirectional(inputAtmos, outputAtmos, eqRate: scale, mixRate: 0.01f);
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
            if (inputAtmos== null || outputAtmos == null)
            {
                return false;
            }
            AroFlow.CompressVolume(inputAtmos, outputAtmos, setting, matterStateToMove);
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
            AroFlow.Equalize(inputAtmos, outputAtmos, desiredPressureChange, 0.8f, typeToMove);
            return false;
        }
    }
    [HarmonyPatch(typeof(DeviceAtmospherics), nameof(DeviceAtmospherics.MoveToEqualizeBidirectional))]
    public class DeviceAtmosphericsBidirectionalPatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, float amountPressureToMove, MatterState typeToMove)
        {
            AroFlow.BiDirectional(inputAtmos, outputAtmos, amountPressureToMove, eqRate: 0.8f, mixThreshold: 0f, typeToMove: typeToMove);
            return false;
        }
    }
    [HarmonyPatch(typeof(ActiveVent), nameof(ActiveVent.OnAtmosphericTick))]
    public class ActiveVentPatch
    {
        [UsedImplicitly]
        public static bool Prefix(ActiveVent __instance)
        {
            
            if (!__instance.OnOff || !__instance.Powered || !AroFlow.IsOperable(__instance))
            {
                return false;
            }
            Atmosphere worldAtmosphere = __instance.GridController.AtmosphericsController.CloneGlobalAtmosphere(__instance.WorldGrid);
            Atmosphere pipeAtmosphere = __instance.ConnectedPipeNetwork.Atmosphere;
            switch (__instance.VentDirection)
            {
                case VentDirection.Inward:
                    //move gas from world to pipe (mode=1)
                    if (worldAtmosphere.PressureGassesAndLiquids > __instance.ExternalPressure && pipeAtmosphere.PressureGassesAndLiquids < __instance.InternalPressure)
                    {
                        float max = Mathf.Min(pipeAtmosphere.Volume, 1000);
                        AroFlow.ActiveEqualize(worldAtmosphere, pipeAtmosphere, max, AroFlow.atm * 2f, 0.8f, Atmosphere.MatterState.All);
                    }
                    break;
                case VentDirection.Outward:
                    //move gas from pipe to world (mode=0)
                    if (worldAtmosphere.PressureGassesAndLiquids < __instance.ExternalPressure && pipeAtmosphere.PressureGassesAndLiquids > __instance.InternalPressure)
                    {
                        AroFlow.ActiveEqualize(pipeAtmosphere, worldAtmosphere, 200f, AroFlow.atm * 2f, 0.6f, Atmosphere.MatterState.All);
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
            float rate = __instance.OutputSetting / 110f;
            AroFlow.BiDirectional(inputAtmos, outputAtmos, eqRate: rate, mixRate: Mathf.Pow(rate,2));
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
            float mixRate = Mathf.Max(Ratio1, Ratio2) * 0.2f;
            float n1 = AroFlow.GetEqualizeMole(inputAtmos1, outputAtmos, float.MaxValue, mixRate, Atmosphere.MatterState.All);
            float n2 = AroFlow.GetEqualizeMole(inputAtmos2, outputAtmos, float.MaxValue, mixRate, Atmosphere.MatterState.All);
            if (n1 * Ratio2 < n2 * Ratio1)
            {
                n2 = n1 * Ratio2 / Ratio1;
                AroFlow.MoveMassEnergy(inputAtmos1, outputAtmos, n1, Atmosphere.MatterState.All);
                AroFlow.MoveMassEnergy(inputAtmos2, outputAtmos, n2, Atmosphere.MatterState.All);
            }
            else
            {
                n1 = n2 * Ratio1 / Ratio2;
                AroFlow.MoveMassEnergy(inputAtmos2, outputAtmos, n2, Atmosphere.MatterState.All);
                AroFlow.MoveMassEnergy(inputAtmos1, outputAtmos, n1, Atmosphere.MatterState.All);
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
            if (!__instance.OnOff || !__instance.Powered || __instance.Mode == 0 || !AroFlow.IsOperable(__instance))
            {
                return false;
            }
            Atmosphere inputAtmos1 = __instance.InputNetwork.Atmosphere;
            Atmosphere outputAtmos1 = __instance.OutputNetwork.Atmosphere;
            Atmosphere outputAtmos2 = __instance.OutputNetwork2.Atmosphere;
            float n1 = AroFlow.GetEqualizeMole(inputAtmos1, outputAtmos1, float.MaxValue, 0.4f, Atmosphere.MatterState.All);
            float n2 = AroFlow.GetEqualizeMole(inputAtmos1, outputAtmos2, float.MaxValue, 0.4f, Atmosphere.MatterState.All);
            foreach (GasType type in AroFlow.gasTypes)
            {
                bool noFilter = true;
                float ratio = inputAtmos1.GasMixture.GetGasTypeRatio(type);
                foreach (GasFilter gasFilter in __instance.GasFilters)
                {
                    if (gasFilter.Quantity > 0f && gasFilter.FilterType == type)
                    {
                        noFilter = false;
                        Mole mole = AroFlow.MoveMassEnergy(inputAtmos1, outputAtmos1, n1 * ratio, type);
                        float degrade = 0;
                        switch (gasFilter.FilterLife)
                        {
                            case GasFilterLife.Normal:
                                degrade = 0.5f / 144;
                                break;
                            case GasFilterLife.Medium:
                                degrade = 0.5f / (144*5);
                                break;
                            case GasFilterLife.Large:
                                degrade = 0.5f / (144 * 20);
                                break;
                            case GasFilterLife.Infinite:
                            default:
                                break;
                        }
                        gasFilter.Quantity -= mole.Quantity * degrade;
                        break;
                    }
                }
                if (noFilter)
                {
                    AroFlow.MoveMassEnergy(inputAtmos1, outputAtmos2, n2 * ratio, type);
                }
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(PowerGeneratorPipe), nameof(PowerGeneratorPipe.OnAtmosphericTick))]
    public class PowerGeneratorPipePowerPatch
    {
        [UsedImplicitly]
        public static bool Prefix(PowerGeneratorPipe __instance, ref float ____energyAsPower, ref int ____ticksOver, ref float ____previousTotalEnergy)
        {

            __instance.WorldAtmosphere = __instance.GridController.AtmosphericsController.GetAtmosphereLocal(__instance.WorldGrid);

            Atmosphere inputAtmos = __instance.InputNetwork?.Atmosphere;
            Atmosphere outputAtmos = __instance.OutputNetwork?.Atmosphere;
            Atmosphere InternalAtmosphere = __instance.InternalAtmosphere;
            float outputCompressEnergy = 0;
            if (__instance.OutputNetwork != null)
            {
                float n = InternalAtmosphere.TotalMoles;
                outputCompressEnergy = AroEnergy.CalcEnergyGasCompression(InternalAtmosphere, outputAtmos, n);
                outputAtmos.Add(__instance.InternalAtmosphere.GasMixture);
                InternalAtmosphere.GasMixture.Reset();
                AroEnergy.AlterEnergy(outputAtmos, outputCompressEnergy);
                AroAtmosphereDataController.GetInstance().AddFlow(outputAtmos, n, outputCompressEnergy);
                AroAtmosphereDataController.GetInstance().AddFlow(InternalAtmosphere, -n, 0);
            }
            outputCompressEnergy = Mathf.Max(outputCompressEnergy * AroEnergy.CompressEnergyPowerFactor, 0f);
            if (____energyAsPower > 0f)
            {
                if (!__instance.Powered)
                {
                    OnServer.Interact(__instance.InteractPowered, 1);
                }
            }
            else if (__instance.Powered)
            {
                OnServer.Interact(__instance.InteractPowered, 0);
            }
            if (__instance.DoShutdown)
            {
                ____ticksOver++;
                if (____ticksOver > 4)
                {
                    OnServer.Interact(__instance.InteractOnOff, 0);
                    ____ticksOver = 0;
                    return false;
                }
            }
            else
            {
                ____ticksOver = 0;
            }
            if (__instance.InputNetwork == null || __instance.OutputNetwork == null || !AroFlow.IsOperable(__instance) || !__instance.OnOff)
            {
                ____ticksOver = 0;
                ____energyAsPower = 0f;
                if (__instance.Powered)
                {
                    OnServer.Interact(__instance.InteractPowered, 0);
                }
                return false;
            }
            if (inputAtmos.PressureGassesAndLiquids < 0.001f)
            {
                OnServer.Interact(__instance.InteractOnOff, 0);
                ____energyAsPower = 0f;
                ____previousTotalEnergy = 0f;
                return false;
            }

            AroFlow.CompressVolume(inputAtmos, InternalAtmosphere, InternalAtmosphere.Volume, Atmosphere.MatterState.Gas);

            InternalAtmosphere.Sparked = true;
            InternalAtmosphere.ManualCombust(0.9f);
            __instance.proceduralConvection = 0.01f + InternalAtmosphere.PressureGassesAndLiquids / 100f * 0.0066f;
            __instance.proceduralConvection *= 0.28f;
            ____previousTotalEnergy = InternalAtmosphere.CombustionEnergy - outputCompressEnergy;
            ____energyAsPower = ____previousTotalEnergy * PowerGeneratorPipe.Efficiency;
            if (__instance.OutputNetwork != null && (__instance.OnOff || InternalAtmosphere.CombustionEnergy > 0f))
            {
                InternalAtmosphere.GasMixture.RemoveEnergy(____energyAsPower);
            }
            float overPressure = InternalAtmosphere.PressureGasses - AroFlow.GFGmaxPressure;
            float damage = overPressure / 1000f;
            if (damage > 0f)
            {
                __instance.DamageState.Damage(ChangeDamageType.Increment, damage, DamageUpdateType.Brute);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(PowerGeneratorPipe), nameof(PowerGeneratorPipe.OnThreadUpdate))]
    public class PowerGeneratorPipeNeedlePatch
    {
        [UsedImplicitly]
        public static void Postfix(PowerGeneratorPipe __instance, ref float ____pressureRating, ref float ____needleRotation)
        {
            ____pressureRating = __instance.InternalAtmosphere.PressureGassesAndLiquids / AroFlow.GFGmaxPressure;
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
            AroFlow.Equalize(input, internalAtmos, float.MaxValue, 0.8f, Atmosphere.MatterState.All);
            float energy = AroEnergy.GetEnergyToTarget(internalAtmos, __instance.GoalTemperature);
            float pe = Mathf.Clamp01(Mathf.Min(internalAtmos.PressureGasses / 101.325f, waste.PressureGasses / 101.325f));
            float iwe = Mathf.Min(__instance.InputAndWasteEfficiency.Evaluate(internalAtmos.GasMixture.Temperature), __instance.InputAndWasteEfficiency.Evaluate(waste.GasMixture.Temperature));
            float tde = internalAtmos.Temperature / waste.Temperature;
            tde = (__instance.GoalTemperature > internalAtmos.Temperature) ? 1f / tde : tde; // heating?
            energy = Mathf.Clamp(energy, -14000f * pe * iwe * tde, 14000f * pe * iwe * tde);
            ____powerUsedDuringTick = Mathf.Abs(energy) * AroEnergy.CompressEnergyPowerFactor; // / Mathf.Max(pe * iwe * tde, 0.000001f);
            AroEnergy.AlterEnergy(waste, -energy);
            AroEnergy.AlterEnergy(internalAtmos, energy);
            AroAtmosphereDataController.GetInstance().AddFlow(waste, 0, -energy);
            AroAtmosphereDataController.GetInstance().AddFlow(internalAtmos, 0, energy);
            __instance.TemperatureDifferentialEfficiency = tde;
            __instance.OperationalTemperatureLimitor = iwe;
            __instance.OptimalPressureScalar = pe;
            AroFlow.Equalize(internalAtmos, output, float.MaxValue, 0.8f, Atmosphere.MatterState.All);
            return false;
        }
    }
    [HarmonyPatch(typeof(H2CombustorMachine), nameof(H2CombustorMachine.OnAtmosphericTick))]
    public class H2CombustorMachinePatch
    {
        [UsedImplicitly]
        public static bool Prefix(H2CombustorMachine __instance)
        {
            if (!__instance.OnOff || !__instance.Powered || !AroFlow.IsOperable(__instance) || __instance.Mode != 1)
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

            AroFlow.Equalize(input, internalAtmos, float.MaxValue, 0.6f, Atmosphere.MatterState.All);
            if (__instance.Activate == 1)
            {
                OnServer.Interact(__instance.InteractActivate, 0);
            }
            internalAtmos.CombustForWater(H2CombustorMachine.WaterRatio);
            internalAtmos.ManualCombust(0.9f);
            AroFlow.Equalize(internalAtmos, output, float.MaxValue, 0.2f, Atmosphere.MatterState.Liquid);
            if (__instance.OutputNetwork2 != null)
            {
                Atmosphere waste = __instance.OutputNetwork2.Atmosphere;
                AroFlow.Equalize(internalAtmos, waste, float.MaxValue, 0.6f, Atmosphere.MatterState.Gas);
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
                float energy = AroFlow.BiDirectional(atmosphere, atmosphere2,mixThreshold:0.001f,mixRate:0.1f);
                ____generatedPower = Mathf.Abs(energy) * AroEnergy.CompressEnergyPowerFactor;
            }
            return false;
        }
    }
    [HarmonyPatch]
    public class AtmosAnalyserClearPatch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(AtmosAnalyser), "Clear")]
        public static void AtmosAnalyserClear(object instance)
        {

        }
    }
    [HarmonyPatch]
    public class AtmosAnalyserSetHashPatch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(AtmosAnalyser), "SetHash")]
        public static void AtmosAnalyserSetHash(object instance, Mole mole, ref int tempHash, ref GasMixture parent)
        {

        }
    }
    [HarmonyPatch(typeof(AtmosAnalyser), nameof(AtmosAnalyser.OnPreScreenUpdate))]
    public class AtmosAnalyserOnPreScreenUpdate
    {
        [HarmonyPrefix]
        public static bool Prefix(out HeatExchangerData __state)
        {
            if (CursorManager.CursorThing is HeatExchanger exchanger)
            {
                HeatExchangerData exchangerData = AroAtmosphereDataController.GetInstance().GetHeatExchangerData(exchanger);
                __state = exchangerData;
                return false;
            }
            __state = null;
            return true;
        }
        [HarmonyPostfix]
        public static void Postfix(AtmosAnalyser __instance, HeatExchangerData __state, ref string ____temperatureValueText, ref string ____energyConvectedText, ref string ____energyRadiatedText, ref string ____pressureValueText, ref string ____selectedText, ref int ____gasHash)
        {
            //____energyConvectedText += "\n" + Math.Round(AroMath.debug, 3);
            //____energyRadiatedText += "\n" + Math.Round(AroMath.debug2, 3);
            IThermal thermal = CursorManager.CursorThing as IThermal;
            Atmosphere ScannedAtmosphere = __instance.ScannedAtmosphere;
            Atmosphere atmosphere = (((ScannedAtmosphere == null || ScannedAtmosphere.Mode == Atmosphere.AtmosphereMode.World) && thermal != null) ? thermal.ThermalAtmosphere : ScannedAtmosphere);
            if (thermal is PipeRadiator)
            {
                atmosphere = null;
            }
            if (__state != null)
            {
                HeatExchangerData exchangerData = __state;
                //AtmosAnalyserClearPatch.AtmosAnalyserClear(__instance);
                if (KeyManager.GetButton(KeyCode.LeftShift))
                {
                    atmosphere = exchangerData.Internal2;
                    ____selectedText = "Left: " + exchangerData.Exchanger.DisplayName.ToUpper();
                }
                else
                {
                    atmosphere = exchangerData.Internal3;
                    ____selectedText = "Right: " + exchangerData.Exchanger.DisplayName.ToUpper();
                }
                if (atmosphere.PressureGassesAndLiquids > float.Epsilon)
                {
                    ____pressureValueText = AroFlow.DisplayUnitCleaner(atmosphere.PressureGassesAndLiquids * 1000, "Pa");
                    //____temperatureValueText = Math.Round(atmosphere.Temperature,1) + "K";
                    ____temperatureValueText = AroFlow.DisplayUnitCleaner(atmosphere.Temperature - Chemistry.Temperature.ZeroDegrees, " °C");
                }
                try
                {
                    int tempHash = 0;
                    AtmosAnalyserSetHashPatch.AtmosAnalyserSetHash(__instance, atmosphere.GasMixture.Oxygen, ref tempHash, ref atmosphere.GasMixture);
                    AtmosAnalyserSetHashPatch.AtmosAnalyserSetHash(__instance, atmosphere.GasMixture.Nitrogen, ref tempHash, ref atmosphere.GasMixture);
                    AtmosAnalyserSetHashPatch.AtmosAnalyserSetHash(__instance, atmosphere.GasMixture.Volatiles, ref tempHash, ref atmosphere.GasMixture);
                    AtmosAnalyserSetHashPatch.AtmosAnalyserSetHash(__instance, atmosphere.GasMixture.Water, ref tempHash, ref atmosphere.GasMixture);
                    AtmosAnalyserSetHashPatch.AtmosAnalyserSetHash(__instance, atmosphere.GasMixture.Pollutant, ref tempHash, ref atmosphere.GasMixture);
                    AtmosAnalyserSetHashPatch.AtmosAnalyserSetHash(__instance, atmosphere.GasMixture.CarbonDioxide, ref tempHash, ref atmosphere.GasMixture);
                    AtmosAnalyserSetHashPatch.AtmosAnalyserSetHash(__instance, atmosphere.GasMixture.NitrousOxide, ref tempHash, ref atmosphere.GasMixture);
                    ____gasHash = tempHash;
                }
                catch (Exception)
                {
                }
            }
            if (atmosphere != null)
            {
                AroDataBase data = AroAtmosphereDataController.GetInstance().GetAroAtmosphereData(atmosphere);
                if (data != null)
                {
                    ____temperatureValueText += "\n" + AroFlow.DisplayUnitCleaner(data.EnergyFlowLastTick,"J");
                    ____pressureValueText += "\n" + AroFlow.DisplayUnitCleaner(data.MassFlowLastTick, "mol");
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
            AroAtmosphereDataController.GetInstance().MoveHistoricValues();
        }
    }
    [HarmonyPatch(typeof(AtmosphericsManager), nameof(AtmosphericsManager.StartManager))]
    public class AtmosphericsManagerStartManagerPatch
    {
        [UsedImplicitly]
        public static void Prefix()
        {
            //AroAtmosphereDataController.Instance = new AroAtmosphereDataController();
            AroAtmosphereDataController.GetInstance().ClearAtmosLists();
        }
    }
    [HarmonyPatch(typeof(Radiator), nameof(Radiator.OnPreAtmosphere))]
    public class RadiatorOnPreAtmospherePatch
    {
        [UsedImplicitly]
        public static bool Prefix(Radiator __instance)
        {
            if (__instance.InputNetwork != null && __instance.InputNetwork.IsNetworkValid())
            {
                AroFlow.BiDirectional(__instance.InputNetwork.Atmosphere, __instance.InternalAtmosphere, eqRate: 0.35f, mixRate: 0.0015f, mixThreshold: 0.0005f, typeToMove: __instance.MatterState);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(Radiator), nameof(Radiator.OnAtmosphericTick))]
    public class RadiatorOnAtmosphericTickPatch
    {
        [UsedImplicitly]
        public static bool Prefix(Radiator __instance)
        {
            if (__instance.OutputNetwork != null && __instance.OutputNetwork.IsNetworkValid())
            {
                AroFlow.BiDirectional(__instance.OutputNetwork.Atmosphere, __instance.InternalAtmosphere, eqRate:0.35f, mixRate: 0.0015f, mixThreshold: 0.0005f, typeToMove: __instance.MatterState);
            }
            return false;
        }
    }
    //[HarmonyPatch(typeof(PassiveVent), nameof(PassiveVent.OnAtmosphericTick))]
    //public class PassiveVentOnAtmosphericTickPatch
    //{
    //    [UsedImplicitly]
    //    public static bool Prefix(PassiveVent __instance, Atmosphere ____environment)
    //    {
    //        if (__instance.HasOpenGrid)
    //        {
    //            ____environment = __instance.GridController.AtmosphericsController.CloneGlobalAtmosphere(__instance.WorldGrid);
    //            AroFlow.BiDirectional(____environment, __instance.PipeNetwork.Atmosphere);
    //        }
    //        return false;
    //    }
    //}
    [HarmonyPatch(typeof(HeatExchanger), nameof(HeatExchanger.Start))]
    public class HeatExchangerStartPatch
    {
        [UsedImplicitly]
        public static void Postfix(HeatExchanger __instance, Atmosphere ___internalAtmosphere2, Atmosphere ___internalAtmosphere3)
        {
            AroAtmosphereDataController.GetInstance().SetHeatExchanger(__instance, ___internalAtmosphere2, ___internalAtmosphere3);
            ___internalAtmosphere2.Mode = Atmosphere.AtmosphereMode.Thing;
            ___internalAtmosphere3.Mode = Atmosphere.AtmosphereMode.Thing;
        }
    }
    [HarmonyPatch(typeof(HeatExchanger), nameof(HeatExchanger.OnAtmosphericTick))]
    public class HeatExchangerOnAtmosphericTickPatch
    {
        [UsedImplicitly]
        public static bool Prefix(HeatExchanger __instance, Atmosphere ___internalAtmosphere2, Atmosphere ___internalAtmosphere3)
        {
            if (__instance.InputNetwork != null && __instance.InputNetwork2 != null && __instance.OutputNetwork != null && __instance.OutputNetwork2 != null && __instance.InputNetwork.Atmosphere != null && __instance.InputNetwork2.Atmosphere != null)
            {
                AroFlow.BiDirectional(___internalAtmosphere2, __instance.InputNetwork.Atmosphere, eqRate: 0.35f, mixRate: 0.0015f, mixThreshold: 0.0005f);
                AroFlow.BiDirectional(___internalAtmosphere3, __instance.InputNetwork2.Atmosphere, eqRate: 0.35f, mixRate: 0.0015f, mixThreshold: 0.0005f);
                AroFlow.BiDirectional(___internalAtmosphere2, __instance.OutputNetwork.Atmosphere, eqRate: 0.35f, mixRate: 0.0015f, mixThreshold: 0.0005f);
                AroFlow.BiDirectional(___internalAtmosphere3, __instance.OutputNetwork2.Atmosphere, eqRate: 0.35f, mixRate: 0.0015f, mixThreshold: 0.0005f);
                float volume = __instance.Bounds.size.x * __instance.Bounds.size.y * __instance.Bounds.size.z * 20f;
                float convectionHeat = Atmosphere.GetConvectionHeat(___internalAtmosphere2, ___internalAtmosphere3, volume * ___internalAtmosphere2.RatioOneAtmosphereClamped * ___internalAtmosphere3.RatioOneAtmosphereClamped);
                ___internalAtmosphere2.GasMixture.TransferEnergyTo(ref ___internalAtmosphere3.GasMixture, convectionHeat * AtmosphericsManager.Instance.TickSpeedMs * 1f);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(Entity), nameof(Entity.OnLifeTick))]
    public class EntityOnLifeTickPatch
    {
        [UsedImplicitly]
        public static void Prefix(Entity __instance, ref float ____entityMolePerBreath)
        {
            ____entityMolePerBreath = 1.2e-4f;//2.0e-6f; 
            if (__instance.LungAtmosphere != null)
            {
                float volume = __instance.LungAtmosphere.Volume;
                if (volume > 6.5f)
                {
                    if (volume > 6.7f)
                    {
                        __instance.LungAtmosphere.Volume = 6.6f;
                    }
                    else
                    {
                        __instance.LungAtmosphere.Volume = 6f;
                    }
                }
                else
                {
                    if (volume < 6.3f)
                    {
                        __instance.LungAtmosphere.Volume = 6.4f;
                    }
                    else
                    {
                        __instance.LungAtmosphere.Volume = 7f;
                    }
                }
            }
        }
        [UsedImplicitly]
        public static void Postfix(Entity __instance)
        {
            if (__instance.LungAtmosphere != null && __instance.BreathingAtmosphere != null)
            {
                //AroFlow.Mix(__instance.LungAtmosphere, __instance.BreathingAtmosphere, 0.5f, MatterState.Gas);
            }
        }
    }

    [HarmonyPatch(typeof(Atmosphere), nameof(Atmosphere.CalculateThingEntropy))]
    public class AtmosphereCalculateThingEntropyPatch
    {
        [UsedImplicitly]
        public static void Prefix(Thing thing, ref float scale)
        {
            if (thing is PipeRadiator radiator)
            {
                scale *= AroEnergy.GetPipeRadiatorScale(radiator.NetworkAtmosphere);
            }
        }
    }
    [HarmonyPatch(typeof(Atmosphere), nameof(Atmosphere.CalculateThingConvection))]
    public class AtmosphereCalculateThingConvectionPatch
    {
        [UsedImplicitly]
        public static void Prefix(Thing thing, ref float scale)
        {
            if (thing is PipeRadiator radiator)
            {
                scale *= AroEnergy.GetPipeRadiatorScale(radiator.NetworkAtmosphere);
            }
        }
    }
    [HarmonyPatch(typeof(Atmosphere), "RatioOneAtmosphereClamped", MethodType.Getter)]
    public class AtmosphereRatioOneAtmosphereClampedPatch
    {
        [UsedImplicitly]
        public static bool Prefix(Atmosphere __instance, ref float __result)
        {
            __result = AroEnergy.PressureRatioClamped(__instance);
            return false;
        }
    }

    //[HarmonyPatch(typeof(PipeNetwork), nameof(PipeNetwork.Add), new Type[] { typeof(Pipe) })]
    //public class PipeNetworkAddPatch
    //{
    //    [UsedImplicitly]
    //    public static void Prefix(PipeNetwork __instance, Pipe pipe)
    //    {
    //        if (GameManager.GameState != GameState.Running && GameManager.RunSimulation)
    //        {
    //            return;
    //        }
    //        if (__instance.PipeList.Count > 1)
    //        {
    //            float energy = AroEnergy.CalcEnergyGasCompression(__instance.Atmosphere.Volume, __instance.Atmosphere.Volume + pipe.Volume, __instance.Atmosphere.TotalMoles);
    //            AroEnergy.AlterEnergy(__instance.Atmosphere, energy);
    //        }
    //    }
    //}
    //[HarmonyPatch(typeof(PipeNetwork), nameof(PipeNetwork.Remove))]
    //public class PipeNetworkRemovePatch
    //{
    //    [UsedImplicitly]
    //    public static void Prefix(PipeNetwork __instance, Pipe pipe)
    //    {
    //        if (GameManager.GameState != GameState.Running && GameManager.RunSimulation)
    //        {
    //            return;
    //        }
    //        if (__instance.PipeList.Count > 1)
    //        {
    //            float energy = AroEnergy.CalcEnergyGasCompression(__instance.Atmosphere.Volume, __instance.Atmosphere.Volume - pipe.Volume, __instance.Atmosphere.TotalMoles);
    //            AroEnergy.AlterEnergy(__instance.Atmosphere, energy);
    //        }
    //        else if (__instance.PipeList.Count == 1)
    //        {
    //            Atmosphere worldAtmosphere = pipe.GridController.AtmosphericsController.GetAtmosphereLocal(pipe.WorldGrid);
    //            if (worldAtmosphere != null)
    //            {
    //                AroFlow.MoveMassEnergy(__instance.Atmosphere, worldAtmosphere, __instance.Atmosphere.TotalMoles,MatterState.All);
    //            }
    //        }
    //    }
    //}
    //[HarmonyPatch(typeof(DeviceInternal), nameof(DeviceInternal.OnDestroy))]
    //public class DeviceInternalOnDestroyPatch
    //{
    //    [UsedImplicitly]
    //    public static void Prefix(DeviceInternal __instance)
    //    {
    //        if (GameManager.GameState != GameState.Running && GameManager.RunSimulation)
    //        {
    //            return;
    //        }
    //        if (__instance.ConnectedPipeNetwork == null)
    //        {
    //            Atmosphere worldAtmosphere = __instance.GridController.AtmosphericsController.GetAtmosphereLocal(__instance.WorldGrid);
    //            if (worldAtmosphere != null)
    //            {
    //                AroFlow.MoveMassEnergy(__instance.InternalAtmosphere, worldAtmosphere, __instance.InternalAtmosphere.TotalMoles, MatterState.All);
    //            }
    //        }
    //        else
    //        {
    //            if (__instance.ConnectedPipeNetwork.Atmosphere != null)
    //            {
    //                AroFlow.MoveMassEnergy(__instance.InternalAtmosphere, __instance.ConnectedPipeNetwork.Atmosphere, __instance.InternalAtmosphere.TotalMoles, MatterState.All);
    //            }
    //        }
    //    }
    //}
}
