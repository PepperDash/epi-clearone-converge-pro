using System;
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
		public List<ConvergeProDspPresets> PresetList = new List<ConvergeProDspPresets>();

		private readonly ConvergeProDspConfig _config;
		private CrestronQueue CommandQueue;
		bool CommandQueueInProgress = false;
		uint HeartbeatTracker = 0;
		public bool ShowHexResponse { get; set; }
        public readonly string DeviceId;
		
		
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

			CommandQueue = new CrestronQueue(100);
			_comm = comm;

			PortGather = new CommunicationGather(_comm, "\x0a");
			PortGather.LineReceived += this.Port_LineReceived;

			_commMonitor = new GenericCommunicationMonitor(this, _comm, 20000, 120000, 300000, CheckSubscriptions);

			LevelControlPoints = new Dictionary<string, ConvergeProDspLevelControl>();
			CreateDspObjects();
		}

		/// <summary>
		/// CustomActivate Override
		/// </summary>
		/// <returns></returns>
		public override bool CustomActivate()
		{
			_comm.Connect();
			_commMonitor.StatusChange += 
                (o, a) => Debug.Console(2, this, "Communication monitor state: {0}", _commMonitor.Status);

            return base.CustomActivate();
		}

		private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs e)
		{
			if (e.Client.IsConnected)
			{
				InitializeDspObjects();
			}
			else
			{
				// Cleanup items from this session
				CommandQueue.Clear();
				CommandQueueInProgress = false;
			}
		}

        public void CreateDspObjects()
		{
			LevelControlPoints.Clear();
			PresetList.Clear();

			if (_config.LevelControlBlocks != null)
			{
				foreach (KeyValuePair<string, ConvergeProDspLevelControlBlockConfig> block in _config.LevelControlBlocks)
				{
					var value = block.Value;
                    this.LevelControlPoints.Add(block.Key, new ConvergeProDspLevelControl(block.Key, value, this));
					Debug.Console(2, this, "Added LevelControlPoint {0} LevelTag: {1} MuteTag: {2}", block.Key, value.Group, value.Channel);
				}
			}
			if (_config.Presets != null)
			{
				foreach (KeyValuePair<string, ConvergeProDspPresets> preset in _config.Presets)
				{
					var value = preset.Value;
					this.addPreset(value);
					Debug.Console(2, this, "Added Preset {0} {1}", value.Label, value.Preset);
				}
			}

			InitializeDspObjects();
		}

		/// <summary>
		/// Checks the subscription health, should be called by comm monitor only. If no heartbeat has been detected recently, will resubscribe and log error.
		/// </summary>
		void CheckSubscriptions()
		{
			HeartbeatTracker++;
			SendLine("INFO");
			CrestronEnvironment.Sleep(1000);

			if (HeartbeatTracker > 0)
			{
				Debug.Console(1, this, "Heartbeat missed, count {0}", HeartbeatTracker);
				if (HeartbeatTracker % 5 == 0)
				{
					Debug.Console(1, this, "Heartbeat missed 5 times, attempting reinit");
					if (HeartbeatTracker == 5)
						Debug.LogError(Debug.ErrorLogLevel.Warning, "Heartbeat missed 5 times");
					InitializeDspObjects();
				}
			}
			else
			{
				Debug.Console(1, this, "Heartbeat okay");
			}
		}

		/// <summary>
		/// Initiates the subscription process to the DSP
		/// </summary>
		void InitializeDspObjects()
		{

			if (_commMonitor != null)
			{
				_commMonitor.Start();
			}

			if (!CommandQueueInProgress)
				SendNextQueuedCommand();
		}

		/// <summary>
		/// Handles a response message from the DSP
		/// </summary>
		/// <param name="dev"></param>
		/// <param name="args"></param>
		void Port_LineReceived(object dev, GenericCommMethodReceiveTextArgs args)
		{
			Debug.Console(2, this, "RX: '{0}'", args.Text);
			try
			{
				if (args.Text.EndsWith("cgpa\r"))
				{
					Debug.Console(1, this, "Found poll response");
					HeartbeatTracker = 0;
				}
				if (args.Text.IndexOf("sr ") > -1)
				{
				}
				else if (args.Text.IndexOf("cv") > -1)
				{
                    var changeMessage = Regex.Split(args.Text, " (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");   //Splits by space unless enclosed in double quotes using look ahead method: https://stackoverflow.com/questions/18893390/splitting-on-comma-outside-quotes

                    string changedInstance = changeMessage[1].Replace("\"", "");
					Debug.Console(1, this, "cv parse Instance: {0}", changedInstance);
					foreach (KeyValuePair<string, ConvergeProDspLevelControl> controlPoint in LevelControlPoints)
					{
                        if (changedInstance == controlPoint.Value.Group)
						{
							controlPoint.Value.ParseSubscriptionMessage(changedInstance, changeMessage[4], changeMessage[3]);
							return;
						}

                        else if (changedInstance == controlPoint.Value.Channel)
						{
							controlPoint.Value.ParseSubscriptionMessage(changedInstance, changeMessage[2].Replace("\"", ""), null);
							return;
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
			_comm.SendText(s + "\x0a");
		}

		/// <summary>
		/// Adds a command from a child module to the queue
		/// </summary>
		/// <param name="commandToEnqueue">Command object from child module</param>
		public void EnqueueCommand(QueuedCommand commandToEnqueue)
		{
			CommandQueue.Enqueue(commandToEnqueue);
			//Debug.Console(1, this, "Command (QueuedCommand) Enqueued '{0}'.  CommandQueue has '{1}' Elements.", commandToEnqueue.Command, CommandQueue.Count);

			if (!CommandQueueInProgress)
				SendNextQueuedCommand();
		}

		/// <summary>
		/// Adds a raw string command to the queue
		/// </summary>
		/// <param name="command"></param>
		public void EnqueueCommand(string command)
		{
			CommandQueue.Enqueue(command);
			//Debug.Console(1, this, "Command (string) Enqueued '{0}'.  CommandQueue has '{1}' Elements.", command, CommandQueue.Count);

			if (!CommandQueueInProgress)
				SendNextQueuedCommand();
		}

		/// <summary>
		/// Sends the next queued command to the DSP
		/// </summary>
		void SendNextQueuedCommand()
		{
			if (_comm.IsConnected && !CommandQueue.IsEmpty)
			{
				CommandQueueInProgress = true;

				if (CommandQueue.Peek() is QueuedCommand)
				{
					QueuedCommand nextCommand = new QueuedCommand();

					nextCommand = (QueuedCommand)CommandQueue.Peek();

					SendLine(nextCommand.Command);
				}
				else
				{
					string nextCommand = (string)CommandQueue.Peek();

					SendLine(nextCommand);
				}
			}

		}

		/// <summary>
		/// Runs the preset with the number provided
		/// </summary>
		/// <param name="n">ushort</param>
		public void RunPresetNumber(ushort n)
		{
			RunPreset(PresetList[n].Preset);
		}

		/// <summary>
		/// Adds a presst
		/// </summary>
		/// <param name="s">ConvergeProDspPresets</param>
		public void addPreset(ConvergeProDspPresets s)
		{
			PresetList.Add(s);
		}

		/// <summary>
		/// Sends a command to execute a preset
		/// </summary>
		/// <param name="name">Preset Name</param>
		public void RunPreset(string name)
		{
			SendLine(string.Format("ssl {0}", name));
			SendLine("cgp 1");
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
                    trilist.SetUShortSigAction(joinMap.ChannelVolume.JoinNumber + x, u => { if (u > 0) { genericChannel.SetVolume(u); } });
                }
                x++;
            }


            // Presets 
            x = 0;
            // from SiMPL > to Plugin
            foreach (var preset in PresetList)
            {
                if (x > 100)
                {
                    break;
                }
                // from SiMPL > to Plugin
                trilist.StringInput[joinMap.PresetName.JoinNumber + x + 1].StringValue = preset.Label;
                trilist.SetSigTrueAction(joinMap.PresetRecall.JoinNumber + x + 1, () => RunPresetNumber(x));
                x++;
            }
        }

	    public BoolFeedback IsOnline { get { return _commMonitor.IsOnlineFeedback; } }
	}
}