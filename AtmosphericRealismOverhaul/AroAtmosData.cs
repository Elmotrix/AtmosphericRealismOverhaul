using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Atmospherics;

using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Pipes;

namespace AtmosphericRealismOverhaul
{
    public class AroAtmosphereDataController
    {
        public static AroAtmosphereDataController Instance;
        public static AroAtmosphereDataController GetInstance()
        {
            if (Instance == null)
            {
                Instance = new AroAtmosphereDataController();
            }
            return Instance;
        }
        public AroAtmosphereDataController()
        {
            AroAtmosphereDataList = new List<AroAtmosphereData>();
            AroRoomDataList = new List<AroRoomData>();
            heatExchangerDataList = new List<HeatExchangerData>();
        }
        private List<AroAtmosphereData> AroAtmosphereDataList;
        private List<AroRoomData> AroRoomDataList;
        private List<HeatExchangerData> heatExchangerDataList;
        public void SetHeatExchanger(HeatExchanger heatExchanger, Atmosphere atmos2, Atmosphere atmos3)
        {
            heatExchangerDataList.Add(new HeatExchangerData() { Exchanger = heatExchanger,Internal2 = atmos2 , Internal3=atmos3});
        }
        public void ClearAtmosLists()
        {
            AroAtmosphereDataList.Clear();
            AroRoomDataList.Clear();
        }
        public HeatExchangerData GetHeatExchangerData(HeatExchanger heatExchanger)
        {
            foreach (HeatExchangerData item in heatExchangerDataList)
            {
                if (item.Exchanger == heatExchanger)
                {
                    return item;
                }
            }
            return null;
        }
        public void MoveHistoricValues()
        {
            foreach (AroAtmosphereData item in AroAtmosphereDataList)
            {
                MoveHistoricValues(item);
            }
            foreach (AroRoomData item in AroRoomDataList)
            {
                MoveHistoricValues(item);
            }
        }
        private void MoveHistoricValues(AroDataBase aroDataBase)
        {
            aroDataBase.EnergyFlowLastTick = aroDataBase.EnergyThisTick;
            aroDataBase.EnergyThisTick = 0;
            aroDataBase.MassFlowLastTick = Mathf.Min(aroDataBase.MassInnThisTick, aroDataBase.MassOutThisTick);
            aroDataBase.MassInnThisTick = 0;
            aroDataBase.MassOutThisTick = 0;
        }

        public void AddFlow(Atmosphere atmosphere, float mass, float energy)
        {
            if (atmosphere.Mode == Atmosphere.AtmosphereMode.Global)
            {
                return;
            }
            if (atmosphere.Mode == Atmosphere.AtmosphereMode.World)
            {
                if (atmosphere.Room != null)
                {
                    foreach (AroRoomData item in AroRoomDataList)
                    {
                        if (item.Room == atmosphere.Room)
                        {
                            AddFlow(item, mass, energy);
                            return;
                        }
                    }
                    AroRoomData newAroData = new AroRoomData(atmosphere.Room);
                    AroRoomDataList.Add(newAroData);
                    AddFlow(newAroData, mass, energy);
                }
            }
            else
            {
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
        }

        private void AddFlow(AroDataBase atmosphereData, float mass, float energy)
        {
            if (mass > 0)
            {
                atmosphereData.MassInnThisTick += Mathf.Abs(mass);
            }
            else
            {
                atmosphereData.MassOutThisTick += Mathf.Abs(mass);
            }
            atmosphereData.EnergyThisTick += energy;
        }

        public AroDataBase GetAroAtmosphereData(Atmosphere atmosphere)
        {
            switch (atmosphere.Mode)
            {
                case Atmosphere.AtmosphereMode.World:
                    if (atmosphere.Room == null)
                    {
                        return null;
                    }
                    foreach (AroRoomData item in AroRoomDataList)
                    {
                        if (item.Room == atmosphere.Room)
                        {
                            return item;
                        }
                    }
                    break;
                case Atmosphere.AtmosphereMode.Network:
                case Atmosphere.AtmosphereMode.Thing:
                    foreach (AroAtmosphereData item in AroAtmosphereDataList)
                    {
                        if (item.Atmosphere == atmosphere)
                        {
                            return item;
                        }
                    }
                    break;
                case Atmosphere.AtmosphereMode.Global:
                default:
                    break;
            }
            return null;
        }

    }

    public class AroDataBase
    {
        public float MassFlowLastTick { get; set; }
        public float MassOutThisTick { get; set; }
        public float MassInnThisTick { get; set; }
        public float EnergyFlowLastTick { get; set; }
        public float EnergyThisTick { get; set; }
    }
    public class AroRoomData : AroDataBase
    {
        public AroRoomData(Room room)
        {
            Room = room;
        }
        public Room Room;
    }
    public class AroAtmosphereData: AroDataBase
    {
        public AroAtmosphereData(Atmosphere atmosphere)
        {
            Atmosphere = atmosphere;
        }
        public Atmosphere Atmosphere;
    }
    public class HeatExchangerData
    {
        public HeatExchanger Exchanger { get; set; }
        public Atmosphere Internal2 { get; set; }
        public Atmosphere Internal3 { get; set; }
    }
}
