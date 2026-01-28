# ClearOne Converge Pro DSP Essentials Plugin (c) 2021

## License

Provided under MIT license

## Device Specific Information

Supported models include 840, 880, 880T

### Communication Settings

| Setting           | Value       |
|-------------------|-------------|
| Delimiter         | "\n"        |
| Default Baud      | 57600       |
| Default Handshake | RTS/CTS     |


#### Plugin Valid Communication methods

```c#
Com
```

### Plugin Configuration Object

```json
{
	"devices": [
		{
			"key": "dsp-1",
			"name": "Converge Pro Essentials Plugin",
			"type": "convergeprodsp",
			"group": "pluginDevices",
			"properties": {
				"control": {
					"method": "com",
					"comParams": {
						"parity": "None",
						"protocol": "RS232",
						"baudRate": 57600,
						"dataBits": 8,
						"softwareHandshake": "None",
						"hardwareHandshake": "RTSCTS",
						"stopBits": 1
					},
					"controlPortNumber": 1,
					"controlPortDevKey": "processor",
				},
				"deviceID": "30",
				"levelControlBlocks": {},
				"presets": {}

			}
		}		
	]
}
```

### Plugin Level Control Configuration Object

```json
"properties": {
	"levelControlBlocks": {
		"fader-1": {
			"deviceID": "31",
			"label": "Room",
			"group": "P",
			"channel": "A",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false
		}
	}
}
```


### Plugin Preset Configuration Object
In ClearOne these are called "Macros". DeviceId is optional and will use global DeviceId if not defined

```json
"properties": {
	"presets": {
		"preset-1": {
			"deviceID": "31",
			"label": "System On",
			"preset": "1"
		},
		"preset-2": {
			"label": "System Off",
			"preset": "2"
		},
		"preset-3": {
			"label": "Default Levels",
			"preset": "3"
		}
	}
}
```

### Plugin Bridge Configuration Object

Update the bridge configuration object as needed for the plugin being developed.

```json
{
	"devices": [
		{
			"key": "dsp-1-bridge",
			"name": "Converge Pro Essentials Plugin Bridge",
			"group": "api",
			"type": "eiscApi",
			"properties": {
				"control": {
					"ipid": "1A",
					"tcpSshProperties": {
						"address": "127.0.0.2",
						"port": 0
					}
				},
				"devices": [
					{
						"deviceKey": "dsp-1",
						"joinStart": 1
					}
				]
			}
		}
	]
}
```

### SiMPL EISC Bridge Map

The selection below documents the digital, analog, and serial joins used by the SiMPL EISC. Update the bridge join maps as needed for the plugin being developed.

#### Digitals
| dig-o (Input/Triggers)     | I/O       | dig-i (Feedback)    |
|----------------------------|-----------|---------------------|
|                            | 1         | Is Online           |
| Preset Recall              | 101-200   |                     |
|                            | 201-400   | Channel Visible     |
| Mute Toggle                | 401-600   | Mute Feedback       |
| Mute On                    | 601-800   |                     |
| Mute Off                   | 801-1000  |                     |
| Volume Up                  | 1001-1200 |                     |
| Volume Down                | 1201-1400 |                     |

#### Analogs
| an_o (Input/Triggers)      | I/O       | an_i (Feedback)     |
|----------------------------|-----------|---------------------|
| Preset Recall              | 100       |                     |
| Volume Set                 | 201-400   | Volume Feedback     |
|                            | 401-600   | Volume Type         |

#### Serials
| serial-o (Input/Triggers)  | I/O       | serial-i (Feedback) |
|----------------------------|-----------|---------------------|
| Preset Recall              | 100       |                     |
|                            | 101-200   | Preset Name         |
|                            | 201-400   | Channel Name        |
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 1.8.4
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "convergeprodsp",
    "group": "Group",
    "properties": {
        "label": "SampleString",
        "deviceId": "SampleString",
        "group": "SampleString",
        "channel": "SampleString",
        "disabled": true,
        "hasLevel": true,
        "hasMute": true,
        "isMic": true,
        "useAbsoluteValue": true,
        "unmuteOnVolChange": true
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->
### Supported Types

- convergeprodsp
<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Is Online |
| 100 | R | Preset Recall |
| 200 | R | Channel Visible |
| 400 | R | Channel Mute Toggle |
| 600 | R | Channel Mute On |
| 800 | R | Channel Mute Off |
| 1000 | R | Channel Volume Up |
| 1200 | R | Channel Volume Down |
| 3100 | R | Call Incoming |
| 3106 | R | Answer Incoming Call |
| 3107 | R | End Call |
| 3110 | R | Keypad Digits 0-9 |
| 3120 | R | Keypad * |
| 3121 | R | Keypad # |
| 3122 | R | Keypad Clear |
| 3123 | R | Keypad Backspace |
| 3124 | R | Keypad Dial and Feedback |
| 3125 | R | Auto Answer On and Feedback |
| 3126 | R | Auto Answer Off and Feedback |
| 3127 | R | Auto Answer Toggle and On Feedback |
| 3129 | R | On Hook Set and Feedback |
| 3130 | R | Off Hook Set and Feedback |
| 3132 | R | Do Not Disturb Toggle and Feedback |
| 3133 | R | Do Not Disturb On Set and Feedback |
| 3134 | R | Do Not Disturb Of Set and Feedback |

#### Analogs

| Join | Type (RW) | Description |
| --- | --- | --- |
| 200 | R | Channel Volume |
| 400 | R | Channel Type |
| 3100 | R | Call State Feedback |

#### Serials

| Join | Type (RW) | Description |
| --- | --- | --- |
| 100 | R | Preset Name |
| 200 | R | Channel Name |
| 3100 | R | Dial String Send and Feedback |
| 3101 | R | Dialer Label |
| 3102 | R | Last Number Dialed Feedback |
| 3104 | R | Caller ID Number |
| 3105 | R | Caller ID Name |
| 3106 | R | This Line's Number |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IHasDialer
- IBasicVolumeWithFeedback
- IKeyed
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- JoinMapBaseAdvanced
- EssentialsBridgeableDevice
- ConvergeProDspControlPoint
- DspControlPoint
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void CreateDspObjects()
- public void SendLine(string s)
- public void RunPreset(ushort preset)
- public void RunPreset(ConvergeProDspPreset preset)
- public void addPreset(ConvergeProDspPreset s)
- public void RunPresetByString(string preset)
- public void OnCallStatusChange(CodecCallStatusItemChangeEventArgs args)
- public void Subscribe()
- public void DoNotDisturbToggle()
- public void DoNotDisturbOn()
- public void DoNotDisturbOff()
- public void AutoAnswerToggle()
- public void AutoAnswerOn()
- public void AutoAnswerOff()
- public void SendKeypad(EKeypadKeys button)
- public void Dial()
- public void Dial(string number)
- public void EndCall(CodecActiveCallItem item)
- public void GetHookState()
- public void EndAllCalls()
- public void AcceptCall()
- public void AcceptCall(CodecActiveCallItem item)
- public void RejectCall()
- public void RejectCall(CodecActiveCallItem item)
- public void SendDtmf(string digit)
- public void Initialize(string key, ConvergeProDspLevelControlBlockConfig config)
- public void ParseResponse(string command, string[] values)
- public void GetCurrentMinMax()
- public void GetCurrentGain()
- public void GetCurrentMute()
- public void MuteOff()
- public void MuteOn()
- public void SetVolume(ushort level)
- public void MuteToggle()
- public void VolumeDown(bool press)
- public void VolumeUp(bool press)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- IsOnline
- OffHookFeedback
- AutoAnswerFeedback
- DoNotDisturbFeedback
- IncomingCallFeedback
- MuteFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- VolumeLevelFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- DialStringFeedback
- CallerIdNumberFeedback
<!-- END String Feedbacks -->
