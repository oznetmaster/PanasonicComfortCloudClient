// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains one weekday of typed scheduled changes.</summary>
/// <typeparam name="TPattern">The endpoint-specific scheduled pattern type.</typeparam>
public sealed partial class WeeklyTimerDay<TPattern> : ExtensibleModel where TPattern : TimerPattern
	{
	/// <summary>Gets or sets the weekday patterns.</summary>
	/// <value>Up to six slots in device-local time, or null when not reported.</value>
	[JsonPropertyName ("patternList")]
	public List<TPattern>? Patterns
		{
		get; set;
		}
	}

/// <summary>Defines the device-local start time shared by weekly timer patterns.</summary>
public abstract class TimerPattern : ExtensibleModel
	{
	/// <summary>Gets whether this is an unused schedule slot.</summary>
	/// <value>True when no start time is set.</value>
	[JsonIgnore]
	public bool IsEmpty => string.IsNullOrEmpty (StartTime);
	/// <summary>Gets or sets the start time.</summary>
	/// <value>A 24-hour HH:mm device-local time, or null for an unused slot.</value>
	[JsonPropertyName ("startTime")]
	public string? StartTime
		{
		get; set;
		}
	}

/// <summary>Contains one Aquarea weekly timer change.</summary>
public sealed partial class AquareaTimerPattern : TimerPattern
	{
	/// <summary>Gets or sets the requested control mode.</summary>
	/// <value>An Aquarea control mode, including unknown numeric values preserved on read, or null.</value>
	[JsonPropertyName ("mode")]
	public AquareaMode? Mode
		{
		get; set;
		}
	/// <summary>Gets or sets zone one's operation state.</summary>
	/// <value>The zone state, or null when not set.</value>
	[JsonPropertyName ("zone1OnOff")]
	public PowerState? Zone1Power
		{
		get; set;
		}
	/// <summary>Gets or sets zone two's operation state.</summary>
	/// <value>The zone state, or null when not set.</value>
	[JsonPropertyName ("zone2OnOff")]
	public PowerState? Zone2Power
		{
		get; set;
		}
	/// <summary>Gets or sets zone one's heating setting.</summary>
	/// <value>The setting in device units, or null when not set.</value>
	[JsonPropertyName ("zone1Heat")]
	public int? Zone1Heat
		{
		get; set;
		}
	/// <summary>Gets or sets zone one's cooling setting.</summary>
	/// <value>The setting in device units, or null when not set.</value>
	[JsonPropertyName ("zone1Cool")]
	public int? Zone1Cool
		{
		get; set;
		}
	/// <summary>Gets or sets zone two's heating setting.</summary>
	/// <value>The setting in device units, or null when not set.</value>
	[JsonPropertyName ("zone2Heat")]
	public int? Zone2Heat
		{
		get; set;
		}
	/// <summary>Gets or sets zone two's cooling setting.</summary>
	/// <value>The setting in device units, or null when not set.</value>
	[JsonPropertyName ("zone2Cool")]
	public int? Zone2Cool
		{
		get; set;
		}
	/// <summary>Gets or sets tank operation.</summary>
	/// <value>The tank state, or null when no tank setting is present.</value>
	[JsonPropertyName ("tankOnOff")]
	public PowerState? TankPower
		{
		get; set;
		}
	/// <summary>Gets or sets the tank target temperature.</summary>
	/// <value>The target in degrees Celsius, or null when not set.</value>
	[JsonPropertyName ("tankTemperature")]
	public int? TankHeat
		{
		get; set;
		}
	}

/// <summary>Contains the writable Aquarea weekly timer settings.</summary>
public sealed class AquareaWeeklySettings : ExtensibleModel
	{
	/// <summary>Gets or sets schedule activation.</summary>
	/// <value>One enabled, zero disabled, or null when activation was not reported.</value>
	[JsonPropertyName ("operation")]
	public int? Operation
		{
		get; set;
		}
	/// <summary>Gets or sets the seven weekdays.</summary>
	/// <value>Monday through Sunday in order; null means unavailable.</value>
	[JsonPropertyName ("timer")]
	public List<WeeklyTimerDay<AquareaTimerPattern>>? Days
		{
		get; set;
		}
	}

/// <summary>Contains the Aquarea weekly schedule and the accompanying device ranges.</summary>
public sealed partial class AquareaSchedule : ExtensibleModel
	{
	/// <summary>Gets or sets the writable timer settings.</summary>
	/// <value>The weekly settings, or null when omitted from the response.</value>
	[JsonPropertyName ("weeklyTimer")]
	public AquareaWeeklySettings? Timer
		{
		get; set;
		}
	/// <summary>Gets or sets the device ranges returned with the schedule.</summary>
	/// <value>The typed zone and tank limits, or null when unavailable.</value>
	[JsonPropertyName ("status")]
	public AquareaStatus? Limits
		{
		get; set;
		}
	}

/// <summary>Contains one air-conditioner weekly timer change.</summary>
public sealed partial class AcTimerPattern : TimerPattern
	{
	/// <summary>Gets or sets power.</summary>
	/// <value>The requested state, or null when unchanged.</value>
	[JsonPropertyName ("onOff")]
	public PowerState? Power
		{
		get; set;
		}
	/// <summary>Gets or sets mode.</summary>
	/// <value>The numeric mode, including unknown returned modes, or null when unchanged.</value>
	[JsonPropertyName ("mode")]
	public AcMode? Mode
		{
		get; set;
		}
	/// <summary>Gets or sets the temperature.</summary>
	/// <value>The target in degrees Celsius, or null when unchanged.</value>
	[JsonPropertyName ("temperature")]
	public double? Temperature
		{
		get; set;
		}
	/// <summary>Gets or sets fan speed.</summary>
	/// <value>The requested speed, or null when unchanged.</value>
	[JsonPropertyName ("fanSpeed")]
	public FanSpeed? Fan
		{
		get; set;
		}
	/// <summary>Gets or sets economy mode.</summary>
	/// <value>The requested economy mode, or null when unchanged.</value>
	[JsonPropertyName ("ecoMode")]
	public EcoMode? Eco
		{
		get; set;
		}
	/// <summary>Gets or sets the iAuto wire flag.</summary>
	/// <value>The returned value, or null when not set; the normal app default is one.</value>
	[JsonPropertyName ("iAuto")]
	public int? IAuto
		{
		get; set;
		}
	/// <summary>Gets or sets zoned AC operation.</summary>
	/// <value>The reported or requested zone settings, or null.</value>
	[JsonPropertyName ("zoneParameters")]
	public List<AcZone>? Zones
		{
		get; set;
		}
	}

/// <summary>Contains a complete air-conditioner weekly schedule.</summary>
public sealed partial class AcSchedule : ExtensibleModel
	{
	/// <summary>Gets or sets the target device.</summary>
	/// <value>The discovered device GUID.</value>
	[JsonPropertyName ("deviceGuid")]
	public string? DeviceGuid
		{
		get; set;
		}
	/// <summary>Gets or sets activation.</summary>
	/// <value>The active flag, or null if unavailable.</value>
	[JsonPropertyName ("avlFlg")]
	public bool? Enabled
		{
		get; set;
		}
	/// <summary>Gets or sets the seven weekdays.</summary>
	/// <value>Monday through Sunday in order; null means unavailable.</value>
	[JsonPropertyName ("weeklyTimerList")]
	public List<WeeklyTimerDay<AcTimerPattern>>? Days
		{
		get; set;
		}
	}