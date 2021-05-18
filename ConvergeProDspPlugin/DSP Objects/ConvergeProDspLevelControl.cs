using System;
using System.Globalization;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ConvergeProDspPlugin
{
	public class ConvergeProDspLevelControl : ConvergeProDspControlPoint, IBasicVolumeWithFeedback, IKeyed
	{
		bool _isMuted;
		ushort _volumeLevel;
		public BoolFeedback MuteFeedback { get; private set; }
        public IntFeedback VolumeLevelFeedback { get; private set; }
        public string Group { get; set; }
        public string Channel { get; set; }
		public bool Enabled { get; set; }
		public bool UseAbsoluteValue { get; set; }
		public ePdtLevelTypes Type;
	    private readonly ConvergeProDsp _parent;

		/// <summary>
		/// Used for debug
		/// </summary>
		public string LevelCustomName { get; private set; }

		/// <summary>
		/// Minimum fader level
		/// </summary>
		private float minLevel;

		/// <summary>
		/// Maximum fader level
		/// </summary>
		private float maxLevel;

		public bool AutomaticUnmuteOnVolumeUp { get; private set; }

		public bool HasMute { get; private set; }
		public bool HasLevel { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key">instance key</param>
		/// <param name="config">level control block configuration object</param>
		/// <param name="parent">dsp parent isntance</param>
		public ConvergeProDspLevelControl(string key, ConvergeProDspLevelControlBlockConfig config, ConvergeProDsp parent)
			: base(config.DeviceId, parent)
		{
		    _parent = parent;
		    if (config.Disabled) 
                return;

            Initialize(key, config);
		}

		/// <summary>
		/// Initializes this attribute based on config values and adds commands to the parent's queue.
		/// </summary>
		/// <param name="key">instance key</param>
		/// <param name="config">level control block configuration object</param>
		public void Initialize(string key, ConvergeProDspLevelControlBlockConfig config)
		{
			Key = string.Format("{0}-{1}", Parent.Key, key);
			Enabled = true;
			DeviceManager.AddDevice(this);
			Type = config.IsMic ? ePdtLevelTypes.Microphone : ePdtLevelTypes.Speaker;
			Debug.Console(2, this, "Adding LevelControl '{0}'", Key);

			MuteFeedback = new BoolFeedback(() => _isMuted);
			VolumeLevelFeedback = new IntFeedback(() => _volumeLevel);
			LevelCustomName = config.Label;
			HasMute = config.HasMute;
			HasLevel = config.HasLevel;
			UseAbsoluteValue = config.UseAbsoluteValue;
            Group = config.Group;
            Channel = config.Channel;
            minLevel = -65;
            maxLevel = 20;
		}

		/// <summary>
		/// Parses the response from the DSP. Command is "MUTE, GAIN, MINMAX, erc. Values[] is the returned values after the channel and group.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="values"></param>
		public void ParseResponse(string command, string[] values)
		{
            Debug.Console(1, this, "Parsing response {0} values: '{1}'", command, string.Join(", ", values));
			if(command == "MUTE")
			{
			    if(values[0] == "1")
			    {
			        _isMuted = true;
			    }
                else if(values[0] == "0")
                {
                    _isMuted = false;
                }
			    MuteFeedback.FireUpdate();
                return;
			}
            else if(command == "GAIN")
			{
				float parsedValue = float.Parse(values[0], CultureInfo.InvariantCulture);

                if(UseAbsoluteValue)
                {
                    _volumeLevel = (ushort)parsedValue;
                    Debug.Console(1, this, "Level {0} VolumeLevel: '{1}'", LevelCustomName, _volumeLevel);
                }
                else if (maxLevel > minLevel)
                {
                    if (parsedValue >= maxLevel)
                        _volumeLevel = (ushort)(((parsedValue - minLevel) * 65535) / (maxLevel - minLevel));
                    else if (parsedValue <= minLevel)
                        _volumeLevel = (ushort)minLevel;
                    else      
                        _volumeLevel = (ushort)(((parsedValue - minLevel) * 65535) / (maxLevel - minLevel));
                    Debug.Console(1, this, "Level {0} VolumeLevel: '{1}'", LevelCustomName, _volumeLevel);
                }
                else
                {
                    Debug.Console(1, this, "Min and Max levels not valid for level {0}", LevelCustomName);
                    return;
                }	

				VolumeLevelFeedback.FireUpdate();
                return;
			}
			else if(command == "MINMAX")
			{
                minLevel = float.Parse(values[0], CultureInfo.InvariantCulture);
                maxLevel = float.Parse(values[1], CultureInfo.InvariantCulture);
				Debug.Console(1, this, "Level {0} new min: {1}, new max: {2}", LevelCustomName, minLevel, maxLevel);
			}
		}

        private void simpleCommand(string command, string value)
        {
            SendFullCommand(command, new string[] { Channel, Group, value });
        }

        /// <summary>
        /// Polls the DSP for the min and max levels for this object
        /// </summary>
        public void GetMinMax()
        {
            SendFullCommand("MINMAX", new string[] { Channel, Group });
        }

		/// <summary>
		/// Turns the mute off
		/// </summary>
		public void MuteOff()
		{
            simpleCommand("MUTE", "0");
		}

		/// <summary>
		/// Turns the mute on
		/// </summary>
		public void MuteOn()
		{
            simpleCommand("MUTE", "1");
		}

		/// <summary>
		/// Sets the volume to a specified level
		/// </summary>
		/// <param name="level"></param>
		public void SetVolume(ushort level)
		{
			Debug.Console(1, this, "Set Volume: {0}", level);
			if (AutomaticUnmuteOnVolumeUp && _isMuted)
			{
				MuteOff();
			}
			if (UseAbsoluteValue)
			{
                SendFullCommand("GAIN", new string[] { Channel, Group, level.ToString("N2"), "A" });
			}
			else
			{
                double tempLevel = Scale(level);
                Debug.Console(1, this, "Set Scaled Volume: {0}", tempLevel);
                SendFullCommand("GAIN", new string[] { Channel, Group, tempLevel.ToString("N2"), "A" });
			}
		}

		/// <summary>
		/// Toggles mute status
		/// </summary>
		public void MuteToggle()
		{
            simpleCommand("MUTE", "2");
		}

		/// <summary>
		/// Decrements volume level
		/// </summary>
		/// <param name="press"></param>
		public void VolumeDown(bool press)
		{
			if (press)
			{
                simpleCommand("RAMP", "-15"); 
			}
			else
			{
                simpleCommand("RAMP", "0"); 
			}
		}

		/// <summary>
		/// Increments volume level
		/// </summary>
		/// <param name="press"></param>
		public void VolumeUp(bool press)
		{
            if (AutomaticUnmuteOnVolumeUp && _isMuted)
            {
                MuteOff();
            }
            if (press)
            {
                simpleCommand("RAMP", "15"); 
            }
            else
            {
                simpleCommand("RAMP", "0"); 
            }
		}
		
		/// <summary>
		/// Scales the input provided
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		double Scale(ushort input)
		{
			double scaled = (ushort)(input * (maxLevel - minLevel) / 65535) + minLevel;
            double output = Math.Round(scaled, 2);
            Debug.Console(1, this, "Scaled output: {0}", output);
			return output;
		}
	}

	/// <summary>
	/// Level type enum
	/// </summary>
	public enum ePdtLevelTypes
	{
		Speaker = 0,
		Microphone = 1
	}
}