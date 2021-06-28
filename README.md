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
| Volume Set                 | 201-400   | Volume Feedback     |
|                            | 401-600   | Volume Type         |

#### Serials
| serial-o (Input/Triggers)  | I/O       | serial-i (Feedback) |
|----------------------------|-----------|---------------------|
|                            | 101-200   | Preset Name         |
|                            | 201-400   | Channel Name        |
