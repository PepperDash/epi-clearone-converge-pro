﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Bridges;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Core.Bridges;

namespace ConvergeProDspPlugin
{
	/// <summary>
	/// DSP Device 
	/// </summary>
	/// <remarks>
	/// </remarks>
	public class ConvergeProDsp : EssentialsBridgeableDevice
	{
		/// <summary>
		/// Communication object
		/// </summary>
        private readonly IBasicCommunication _comm;

		/// <summary>
		/// Communication monitor object
		/// </summary>
        public readonly GenericCommunicationMonitor _commMonitor;        

        public CommunicationGather PortGather { get; private set; }
		public Dictionary<string, ConvergeProDspLevelControl> LevelControlPoints { get; private set; }
		public List<ConvergeProDspPreset> PresetList = new List<ConvergeProDspPreset>();
		public Dictionary<string, ConvergeProDspDialer> Dialers { get; set; }

		private readonly ConvergeProDspConfig _config;
		private uint HeartbeatTracker = 0;
		public bool ShowHexResponse { get; set; }
        public string DeviceId { get; set; }
        private bool postActivateComplete = false;
		
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key">String</param>
		/// <param name="name">String</param>
		/// <param name="comm">IBasicCommunication</param>
		/// <param name="dc">DeviceConfig</param>
		public ConvergeProDsp(string key, string name, IBasicCommunication comm, ConvergeProDspConfig config)
			: base(key, name)
		{
			Debug.Console(2, this, "Creating ClearOne Converge DSP Instance");
            _config = config;

            DeviceId = "30";
            if (!string.IsNullOrEmpty(_config.DeviceId))
            {
                DeviceId = _config.DeviceId;
            }

			_comm = comm;

			PortGather = new CommunicationGather(_comm, "\x0a");
			PortGather.LineReceived += this.ResponseReceived;

			_commMonitor = new GenericCommunicationMonitor(this, _comm, 30000, 121000, 301000, CheckComms);
            _commMonitor.StatusChange += new EventHandler<MonitorStatusChangeEventArgs>(ConnectionChange);

			LevelControlPoints = new Dictionary<string, ConvergeProDspLevelControl>();
			Dialers = new Dictionary<string, ConvergeProDspDialer>();
			CreateDspObjects();
            
            AddPostActivationAction(() =>
            {
                _comm.Connect();
                _commMonitor.Start();
                InitializeDspObjects();
                postActivateComplete = true;
            });
		}

		private void ConnectionChange(object sender, MonitorStatusChangeEventArgs e)
		{
            Debug.Console(2, this, "Communication monitor state: {0}", e.Status);
            if (e.Status == MonitorStatus.IsOk && postActivateComplete)
			{
				InitializeDspObjects();
			}
		}

        public void CreateDspObjects()
		{
			LevelControlPoints.Clear();
			PresetList.Clear();
			Dialers.Clear();

			if (_config.LevelControlBlocks != null)
			{
				foreach (KeyValuePair<string, ConvergeProDspLevelControlBlockConfig> block in _config.LevelControlBlocks)
				{
                    this.LevelControlPoints.Add(block.Key, new ConvergeProDspLevelControl(block.Key, block.Value, this));
                    Debug.Console(2, this, "Added LevelControlPoint {0} LevelTag: {1} MuteTag: {2}", block.Key, block.Value.Group, block.Value.Channel);
				}
			}
			if (_config.Presets != null)
			{
				foreach (KeyValuePair<string, ConvergeProDspPreset> preset in _config.Presets)
				{
                    this.addPreset(preset.Value);
                    Debug.Console(2, this, "Added Preset {0} {1}", preset.Value.Label, preset.Value.Preset);
				}
			}
			if (_config.Dialers != null)
			{
				foreach (KeyValuePair<string, ConvergeProDspDialerConfig> dialerConfig in _config.Dialers)
				{
					var value = dialerConfig.Value;
					var key = dialerConfig.Key;
					this.Dialers.Add(key, new ConvergeProDspDialer(value, this));
					Debug.Console(2, this, "Added Dialer {0}\n {1}", key, value);
				}
			}

		}

		/// <summary>
		/// Checks the comm health, should be called by comm monitor only. If no heartbeat has been detected recently, will clear the queue and log an error.
		/// </summary>
		private void CheckComms()
		{
			HeartbeatTracker++;
			SendLine("VER");
			CrestronEnvironment.Sleep(1000);

			if (HeartbeatTracker > 0)
			{
				Debug.Console(1, this, "Heartbeat missed, count {0}", HeartbeatTracker);

				if (HeartbeatTracker == 5)
					Debug.LogError(Debug.ErrorLogLevel.Warning, "Heartbeat missed 5 times");
			}
			else
			{
				Debug.Console(2, this, "Heartbeat okay");
			}
		}

		/// <summary>
		/// Initiates the subscription process to the DSP
		/// </summary>
		void InitializeDspObjects()
		{
            foreach (var channel in LevelControlPoints)
            {
                if (channel.Value.HasLevel)
                {
                    channel.Value.GetCurrentMinMax();
                    CrestronEnvironment.Sleep(250);
                }
            }
            foreach (var channel in LevelControlPoints)
            {
                if (channel.Value.HasLevel)
                {
                    channel.Value.GetCurrentGain();
                    CrestronEnvironment.Sleep(250);
                }
            }
            foreach (var channel in LevelControlPoints)
            {
                if (channel.Value.HasMute)
                {
                    channel.Value.GetCurrentMute();
                    CrestronEnvironment.Sleep(250);
             
                }
            }
            foreach (var line in Dialers)
            {
                line.Value.GetHookState();
            }
		}

		/// <summary>
		/// Handles a response message from the DSP
		/// </summary>
		/// <param name="dev"></param>
		/// <param name="args"></param>
		void ResponseReceived(object dev, GenericCommMethodReceiveTextArgs args)
		{
			Debug.Console(2, this, "RX: '{0}'", args.Text);
            HeartbeatTracker = 0;
			try
			{
                if (args.Text.Contains("#"))
                {
                    var startPoint = args.Text.IndexOf("#", 0) + 1;                             //example = #12 MUTE 5 M 0\x0D...
                    var endPoint = args.Text.IndexOf("\x0D", startPoint);
                    int length = endPoint - startPoint;

                    string[] data = args.Text.Substring(startPoint, length).Split(' ');         //example = [12, MUTE, 5, M, 0]

                    // data[0] = deviceId
                    // data[1] = response type
                    // data[2] = channel
                    // data[3] = group
                    // data[4]...data[n] = values
                    if((data.Length >= 5 && (data[1] == "MUTE" || data[1] == "GAIN")) || (data.Length >=6 && data[1] == "MINMAX"))
                    {
                        Debug.Console(1, this, "Found {0} response", data[1]);
                        foreach (KeyValuePair<string, ConvergeProDspLevelControl> controlPoint in LevelControlPoints)
					    {
                            if (data[0] == controlPoint.Value.DeviceId && data[2] == controlPoint.Value.Channel && data[3] == controlPoint.Value.Group)
						    {
                                controlPoint.Value.ParseResponse(data[1], data.Skip(4).ToArray());  //send command and any values after the group/channel info
							    return;
						    }
					    }
				    }
                    if(data.Length >=3 && data[1] == "TE")
                    {
                        foreach (KeyValuePair<string, ConvergeProDspDialer> dialer in Dialers)
                        {
                            if (data[0] == dialer.Value.DeviceId)
                            {
                                if (data[3] == "0")
                                {
                                    dialer.Value.OffHook = false;
                                    return;
                                } 
                                else if (data[3] == "1")
                                {
                                    dialer.Value.OffHook = true;
                                    return;
                                }
                                
                            }
                        }
                    }
                }
			}
			catch (Exception e)
			{
				if (Debug.Level == 2)
					Debug.Console(2, this, "Error parsing response: '{0}'\n{1}", args.Text, e);
			}

		}

		/// <summary>
		/// Sends a command to the DSP (with delimiter appended)
		/// </summary>
		/// <param name="s">Command to send</param>
		public void SendLine(string s)
		{
			Debug.Console(1, this, "TX: '{0}'", s);
			_comm.SendText(s + "\x0D");
		}

        /// <summary>
        /// Runs the preset with the number provided
        /// </summary>
        /// <param name="n">ushort</param>
        public void RunPreset(ushort preset)
        {
			Debug.Console(1, this, "RunPreset: '{0}'", preset);
			
            if (0 < preset && preset <= PresetList.Count && PresetList[preset-1] != null)
            {
                RunPreset(PresetList[preset-1]);
            }
        }

		/// <summary>
		/// Runs the preset object provided
		/// </summary>
		/// <param name="n">ConvergeProDspPreset</param>
		public void RunPreset(ConvergeProDspPreset preset)
		{
	        RunPresetByString(preset.Preset);
		}

		/// <summary>
		/// Adds a presst
		/// </summary>
		/// <param name="s">ConvergeProDspPresets</param>
		public void addPreset(ConvergeProDspPreset s)
		{
			PresetList.Add(s);
		}

		/// <summary>
		/// Sends a command to execute a preset
		/// </summary>
		/// <param name="name">Preset Name</param>
		public void RunPresetByString(string preset)
		{
			SendLine(string.Format("#{0} MACRO {1}", DeviceId,  preset));
		}

		/// <summary>
		/// Queues Commands
		/// </summary>
		public class QueuedCommand
		{
			public string Command { get; set; }
			public string AttributeCode { get; set; }
			public ConvergeProDspControlPoint ControlPoint { get; set; }
		}

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new ConvergeProDspJoinMap(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
            if (customJoins != null)
            {
                joinMap.SetCustomJoinData(customJoins);
            }

            Debug.Console(1, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));

            // from Plugin > to SiMPL
            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

            ushort x = 1;

            foreach (var channel in LevelControlPoints)
            {
                if (x > 200)
                {
                    break;
                }
                Debug.Console(2, this, "ConvergeProChannel {0} connect", x);

                var genericChannel = channel.Value as IBasicVolumeWithFeedback;
                if (channel.Value.Enabled)
                {
                    // from SiMPL > to Plugin
                    trilist.StringInput[joinMap.ChannelName.JoinNumber + x].StringValue = channel.Value.LevelCustomName;
                    trilist.UShortInput[joinMap.ChannelType.JoinNumber + x].UShortValue = (ushort)channel.Value.Type;
                    trilist.BooleanInput[joinMap.ChannelVisible.JoinNumber + x].BoolValue = true;
                    // from Plugin > to SiMPL
                    genericChannel.MuteFeedback.LinkInputSig(trilist.BooleanInput[joinMap.ChannelMuteToggle.JoinNumber + x]);
                    genericChannel.VolumeLevelFeedback.LinkInputSig(trilist.UShortInput[joinMap.ChannelVolume.JoinNumber + x]);
                    // from SiMPL > to Plugin
                    trilist.SetSigTrueAction(joinMap.ChannelMuteToggle.JoinNumber + x, () => genericChannel.MuteToggle());
                    trilist.SetSigTrueAction(joinMap.ChannelMuteOn.JoinNumber + x, () => genericChannel.MuteOn());
                    trilist.SetSigTrueAction(joinMap.ChannelMuteOff.JoinNumber + x, () => genericChannel.MuteOff());
                    // from SiMPL > to Plugin
                    trilist.SetBoolSigAction(joinMap.ChannelVolumeUp.JoinNumber + x, b => genericChannel.VolumeUp(b));
                    trilist.SetBoolSigAction(joinMap.ChannelVolumeDown.JoinNumber + x, b => genericChannel.VolumeDown(b));
                    // from SiMPL > to Plugin
                    trilist.SetUShortSigAction(joinMap.ChannelVolume.JoinNumber + x, u => genericChannel.SetVolume(u));
                }
                x++;
            }


            // Presets 
            x = 1;
            // from SiMPL > to Plugin
            foreach (var preset in PresetList)
            {

                var thisPreset = preset as ConvergeProDspPreset;
                if (x > 100)
                {
                    break;
                }
                // from SiMPL > to Plugin

                trilist.StringInput[joinMap.PresetName.JoinNumber + x].StringValue = preset.Label;
                trilist.SetStringSigAction(joinMap.PresetRecall.JoinNumber, s => RunPresetByString(s));
                trilist.SetUShortSigAction(joinMap.PresetRecall.JoinNumber, u => RunPreset(u));
                trilist.SetSigTrueAction(joinMap.PresetRecall.JoinNumber + x, () => RunPreset(thisPreset));
                x++;
            }
			// VoIP Dialer
			uint lineOffset = 0;
			foreach (var line in Dialers)
			{
				var dialer = line;

				var dialerLineOffset = lineOffset;
				Debug.Console(0, "AddingDialerBridge {0} {1} Offset", dialer.Key, dialerLineOffset);


				for (var i = 0; i < joinMap.KeypadNumeric.JoinSpan; i++)
				{
					var tempi = i;

					trilist.SetSigTrueAction((joinMap.KeypadNumeric.JoinNumber + (uint)i + dialerLineOffset), () => dialer.Value.SendKeypad((ConvergeProDspDialer.EKeypadKeys)(tempi)));
				}

				// from SiMPL > to Plugin
				trilist.SetSigTrueAction((joinMap.KeypadStar.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(ConvergeProDspDialer.EKeypadKeys.Star));
				trilist.SetSigTrueAction((joinMap.KeypadPound.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(ConvergeProDspDialer.EKeypadKeys.Pound));
				trilist.SetSigTrueAction((joinMap.KeypadClear.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(ConvergeProDspDialer.EKeypadKeys.Clear));
				trilist.SetSigTrueAction((joinMap.KeypadBackspace.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(ConvergeProDspDialer.EKeypadKeys.Backspace));
				// from SiMPL > to Plugin
				trilist.SetSigTrueAction(joinMap.KeypadDial.JoinNumber + dialerLineOffset, () => dialer.Value.Dial());
				trilist.SetStringSigAction(joinMap.DialString.JoinNumber + dialerLineOffset, dialer.Value.Dial);
				trilist.SetSigTrueAction(joinMap.DoNotDisturbToggle.JoinNumber + dialerLineOffset, () => dialer.Value.DoNotDisturbToggle());
				trilist.SetSigTrueAction(joinMap.DoNotDisturbOn.JoinNumber + dialerLineOffset, () => dialer.Value.DoNotDisturbOn());
				trilist.SetSigTrueAction(joinMap.DoNotDisturbOff.JoinNumber + dialerLineOffset, () => dialer.Value.DoNotDisturbOff());
				trilist.SetSigTrueAction(joinMap.AutoAnswerToggle.JoinNumber + dialerLineOffset, () => dialer.Value.AutoAnswerToggle());
				trilist.SetSigTrueAction(joinMap.AutoAnswerOn.JoinNumber + dialerLineOffset, () => dialer.Value.AutoAnswerOn());
				trilist.SetSigTrueAction(joinMap.AutoAnswerOff.JoinNumber + dialerLineOffset, () => dialer.Value.AutoAnswerOff());
				trilist.SetSigTrueAction(joinMap.EndCall.JoinNumber + dialerLineOffset, () => dialer.Value.EndAllCalls());
				//trilist.SetSigTrueAction(joinMap.IncomingCallAccept.JoinNumber + dialerLineOffset, () => dialer.Value.AcceptCall());
				//trilist.SetSigTrueAction(joinMap.IncomingCallReject.JoinNumber + dialerLineOffset, () => dialer.Value.RejectCall());

				// from SIMPL > to Plugin
				trilist.SetStringSigAction(joinMap.DialString.JoinNumber + dialerLineOffset, directDialString => dialer.Value.Dial(directDialString));

				// from Plugin > to SiMPL
				dialer.Value.DoNotDisturbFeedback.LinkInputSig(trilist.BooleanInput[joinMap.DoNotDisturbToggle.JoinNumber + dialerLineOffset]);
				dialer.Value.DoNotDisturbFeedback.LinkInputSig(trilist.BooleanInput[joinMap.DoNotDisturbOn.JoinNumber + dialerLineOffset]);
				dialer.Value.DoNotDisturbFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.DoNotDisturbOff.JoinNumber + dialerLineOffset]);

				// from Plugin > to SiMPL
				dialer.Value.AutoAnswerFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoAnswerToggle.JoinNumber + dialerLineOffset]);
				dialer.Value.AutoAnswerFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoAnswerOn.JoinNumber + dialerLineOffset]);
				dialer.Value.AutoAnswerFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.AutoAnswerOff.JoinNumber + dialerLineOffset]);
				dialer.Value.CallerIdNumberFeedback.LinkInputSig(trilist.StringInput[joinMap.CallerIdNumberFb.JoinNumber + dialerLineOffset]);

				// from Plugin > to SiMPL
				dialer.Value.OffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.KeypadDial.JoinNumber + dialerLineOffset]);
				dialer.Value.OffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.OffHook.JoinNumber + dialerLineOffset]);
				dialer.Value.OffHookFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.OnHook.JoinNumber + dialerLineOffset]);
				dialer.Value.DialStringFeedback.LinkInputSig(trilist.StringInput[joinMap.DialString.JoinNumber + dialerLineOffset]);

				// from Plugin > to SiMPL
				dialer.Value.IncomingCallFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IncomingCall.JoinNumber + dialerLineOffset]);

				lineOffset = lineOffset + 50;
			}
        }

	    public BoolFeedback IsOnline { get { return _commMonitor.IsOnlineFeedback; } }
	}
}