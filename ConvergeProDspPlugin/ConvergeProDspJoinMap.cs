using System.Linq;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ConvergeProDspPlugin
{
	/// <summary>
	/// Converge Pro DSP Join Map
	/// </summary>
	public class ConvergeProDspJoinMap : JoinMapBaseAdvanced
	{
        #region Digital

        [JoinName("IsOnline")]
        public JoinDataComplete IsOnline = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "Is Online",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PresetRecall")]
        public JoinDataComplete PresetRecall = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 100,
                JoinSpan = 100
            },
            new JoinMetadata()
            {
                Description = "Preset Recall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ChannelVisible")]
        public JoinDataComplete ChannelVisible = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 200,
                JoinSpan = 200
            },
            new JoinMetadata()
            {
                Description = "Channel Visible",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ChannelMuteToggle")]
        public JoinDataComplete ChannelMuteToggle = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 400,
                JoinSpan = 200
            },
            new JoinMetadata()
            {
                Description = "Channel Mute Toggle",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ChannelMuteOn")]
        public JoinDataComplete ChannelMuteOn = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 600,
                JoinSpan = 200
            },
            new JoinMetadata()
            {
                Description = "Channel Mute On",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ChannelMuteOff")]
        public JoinDataComplete ChannelMuteOff = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 800,
                JoinSpan = 200
            },
            new JoinMetadata()
            {
                Description = "Channel Mute Off",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ChannelVolumeUp")]
        public JoinDataComplete ChannelVolumeUp = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 1000,
                JoinSpan = 200
            },
            new JoinMetadata()
            {
                Description = "Channel Volume Up",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ChannelVolumeDown")]
        public JoinDataComplete ChannelVolumeDown = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 1200,
                JoinSpan = 200
            },
            new JoinMetadata()
            {
                Description = "Channel Volume Down",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        #endregion

        #region Analog

        [JoinName("ChannelVolume")]
        public JoinDataComplete ChannelVolume = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 200,
                JoinSpan = 200
            },
            new JoinMetadata()
            {
                Description = "Channel Volume",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("ChannelType")]
        public JoinDataComplete ChannelType = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 400,
                JoinSpan = 200
            },
            new JoinMetadata()
            {
                Description = "Channel Type",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        #endregion

        #region Serial

        [JoinName("PresetName")]
        public JoinDataComplete PresetName = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 100,
                JoinSpan = 100
            },
            new JoinMetadata()
            {
                Description = "Preset Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ChannelName")]
        public JoinDataComplete ChannelName = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 200,
                JoinSpan = 200
            },
            new JoinMetadata()
            {
                Description = "Channel Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        #endregion

        public ConvergeProDspJoinMap(uint joinStart)
            : base(joinStart, typeof(ConvergeProDspJoinMap))
		{
		}
	}
}