using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;


namespace ConvergeProDspPlugin
{
	/// <summary>
	/// Converge Pro DSP Properties config class
	/// </summary>
	public class ConvergeProDspConfig
	{
		public CommunicationMonitorConfig CommunicationMonitorProperties { get; set; }

		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }

		[JsonProperty("deviceId")]
		public string DeviceId { get; set; }

		[JsonProperty("levelControlBlocks")]
		public Dictionary<string, ConvergeProDspLevelControlBlockConfig> LevelControlBlocks { get; set; }

		[JsonProperty("presets")]
		public Dictionary<string, ConvergeProDspPreset> Presets { get; set; }
	}

	/// <summary>
	/// Converge Pro Presets Configurations
	/// </summary>
	public class ConvergeProDspPreset
	{
		[JsonProperty("label")]
		public string Label { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

		[JsonProperty("preset")]
		public string Preset { get; set; }
	}

	/// <summary>
	/// Converge Pro Level Control Block Configuration 
	/// </summary>
	public class ConvergeProDspLevelControlBlockConfig
	{
		[JsonProperty("label")]
		public string Label { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

		[JsonProperty("group")]
		public string Group { get; set; }

		[JsonProperty("channel")]
		public string Channel { get; set; }

		[JsonProperty("disabled")]
		public bool Disabled { get; set; }

		[JsonProperty("hasLevel")]
		public bool HasLevel { get; set; }

		[JsonProperty("hasMute")]
		public bool HasMute { get; set; }

		[JsonProperty("isMic")]
		public bool IsMic { get; set; }

		[JsonProperty("useAbsoluteValue")]
		public bool UseAbsoluteValue { get; set; }

		[JsonProperty("unmuteOnVolChange")]
		public bool UnmuteOnVolChange { get; set; }
	}
}