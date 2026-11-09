// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains standalone HWS status or explicit changes.</summary>
public sealed partial class HotWaterParameters : ExtensibleModel
	{
	/// <summary>Gets or sets the measured tank temperature in degrees Celsius.</summary>
	/// <value>The measured tank temperature in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("tankTemperature")]
	public double? TankTemperature
		{
		get; set;
		}

	/// <summary>Gets or sets the heat-pump-unit operation status.</summary>
	/// <value>The heat-pump-unit operation status. Null when not reported or not requested.</value>
	[JsonPropertyName ("hpuOperationStatus")]
	public PowerState? Power
		{
		get; set;
		}

	/// <summary>Gets or sets the selected operating mode.</summary>
	/// <value>The selected operating mode. Null when not reported or not requested.</value>
	[JsonPropertyName ("operationMode")]
	public HwsMode? Mode
		{
		get; set;
		}

	/// <summary>Gets or sets the boost mode flag.</summary>
	/// <value>The boost mode flag. Null when not reported or not requested.</value>
	[JsonPropertyName ("boostMode")]
	public PowerState? Boost
		{
		get; set;
		}

	/// <summary>Gets or sets the complete weekly schedule, when provided.</summary>
	/// <value>The complete weekly schedule, when provided. Null when not reported or not requested.</value>
	[JsonPropertyName ("weeklySettingInfo")]
	public List<HotWaterDay>? WeeklySettings
		{
		get; set;
		}
	}