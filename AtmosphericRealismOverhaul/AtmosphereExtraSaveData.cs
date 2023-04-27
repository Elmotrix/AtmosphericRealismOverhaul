using Assets.Scripts.Atmospherics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AtmosphericRealismOverhaul
{
    public class AtmosphereExtraSaveData: AtmosphereSaveData
    {
		[XmlElement]
		public float OxygenIce;
		[XmlElement]
		public float OxygenLiquid;

		[XmlElement]
		public float NitrogenIce;
		[XmlElement]
		public float NitrogenLiquid;

		[XmlElement]
		public float CarbonDioxideIce;
		[XmlElement]
		public float CarbonDioxideLiquid;

		[XmlElement]
		public float VolatilesIce;
		[XmlElement]
		public float VolatilesLiquid;

		[XmlElement]
		public float ChlorineIce;
		[XmlElement]
		public float ChlorineLiquid;

		[XmlElement]
		public float WaterIce;
		[XmlElement]
		public float WaterLiquid;

		[XmlElement]
		public float NitrousOxideIce;
		[XmlElement]
		public float NitrousOxideLiquid;
	}
}
