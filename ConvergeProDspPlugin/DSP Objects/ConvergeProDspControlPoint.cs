using System;
using Crestron.SimplSharp;
using PepperDash.Essentials.Devices.Common.DSP;

namespace ConvergeProDspPlugin
{
    public class ConvergeProDspControlPoint : DspControlPoint
	{
        public string Key { get; protected set; }
        public string DeviceId { get; set; }
		public ConvergeProDsp Parent { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
        /// <param name="deviceId"> Optional device ID for this object, if not defined will use the global DSP device ID</param>
		/// <param name="parent">Parent DSP instance</param>
        protected ConvergeProDspControlPoint(ConvergeProDsp parent)
        {
            DeviceId = parent.DeviceId;
            Parent = parent;
        }
		protected ConvergeProDspControlPoint(string deviceId, ConvergeProDsp parent)
		{
            if (!string.IsNullOrEmpty(deviceId))
            {
                DeviceId = deviceId;
            }
            else
            {
                DeviceId = parent.DeviceId;
            }
			Parent = parent; 
		}

		/// <summary>
		/// Sends a command to the DSP
		/// </summary>
		/// <param name="cmd">command</param>
		/// <param name="instance">named control/instance tag</param>
		/// <param name="value">value (use "" if not applicable)</param>
		public virtual void SendFullCommand(string cmd, string[] values)
		{
            string cmdToSemd = string.Format("#{0} {1} {2}", DeviceId, cmd, string.Join(" ", values));

			Parent.SendLine(cmdToSemd);
		}
    }
}