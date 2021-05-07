using System;
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
		/// Used for to identify level subscription values
		/// </summary>
		public string LevelCustomName { get; private set; }

		/// <summary>
		/// Used for to identify mute subscription values
		/// </summary>
		public string MuteCustomName { get; private set; }

		/// <summary>
		/// Minimum fader level
		/// </summary>
		double MinLevel;

		/// <summary>
		/// Maximum fader level
		/// </summary>
		double MaxLevel;

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

		    _parent._commMonitor.IsOnlineFeedback.OutputChange += (sender, args) =>
		        {
		            if (!args.BoolValue)
		                return;
		        };

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
		}

		/// <summary>
		/// Parses the response from the DspBase
		/// </summary>
		/// <param name="customName"></param>
		/// <param name="value"></param>
		/// <param name="absoluteValue"></param>
		public void ParseSubscriptionMessage(string customName, string value, string absoluteValue)
		{
			// Check for valid subscription response
			Debug.Console(1, this, "Level {0} Response: '{1}'", customName, value);
			if (
                !String.IsNullOrEmpty(Channel) 
                && customName.Equals(Channel, StringComparison.OrdinalIgnoreCase))
			{
			    switch (value)
			    {
			        case "true":
			        case "muted":
			            _isMuted = true;
			            break;
			        case "false":
			        case "unmuted":
			            _isMuted = false;
			            break;
			    }

			    MuteFeedback.FireUpdate();
			}
            else if (
                !String.IsNullOrEmpty(Group) 
                && customName.Equals(Group, StringComparison.OrdinalIgnoreCase) 
                && !UseAbsoluteValue)
			{
				var parsedValue = Double.Parse(value);

                _volumeLevel = (ushort)(parsedValue * 65535);
				Debug.Console(1, this, "Level {0} VolumeLevel: '{1}'", customName, _volumeLevel);

				VolumeLevelFeedback.FireUpdate();
			}
			else if (
                !String.IsNullOrEmpty(Group)
                && customName.Equals(Group, StringComparison.OrdinalIgnoreCase) 
                && UseAbsoluteValue)
			{

				_volumeLevel = ushort.Parse(absoluteValue);
				Debug.Console(1, this, "Level {0} VolumeLevel: '{1}'", customName, _volumeLevel);

				VolumeLevelFeedback.FireUpdate();
			}
		}

        private void simpleCommand(string command, string value)
        {
            SendFullCommand(command, new string[] { Channel, Group, value });
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
			Debug.Console(1, this, "volume: {0}", level);
			if (AutomaticUnmuteOnVolumeUp && _isMuted)
			{
				MuteOff();
			}
			if (!UseAbsoluteValue)
			{
				var newLevel = Scale(level);
				Debug.Console(1, this, "newVolume: {0}", newLevel);
                SendFullCommand("GAIN", new string[] { Channel, Group, newLevel.ToString(), "R" });
			}
			else
			{
                SendFullCommand("GAIN", new string[] { Channel, Group, level.ToString(), "A" });
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
		double Scale(double input)
		{
			Debug.Console(1, this, "Scaling (double) input '{0}'", input);

			var output = (input / 65535);

			Debug.Console(1, this, "Scaled output '{0}'", output);

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