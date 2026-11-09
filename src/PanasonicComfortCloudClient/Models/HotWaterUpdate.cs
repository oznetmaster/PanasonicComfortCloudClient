// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains the explicit standalone HWS controls supported by the reference protocol.</summary>
public sealed class HotWaterUpdate
	{
	/// <summary>Gets or sets the operating mode.</summary>
	/// <value>A schedule, photovoltaic or holiday mode, or null when unchanged.</value>
	[JsonPropertyName ("operationMode")]
	public HwsMode? Mode
		{
		get; set;
		}
	/// <summary>Gets or sets boost operation.</summary>
	/// <value>Off or on, or null when unchanged.</value>
	[JsonPropertyName ("boostMode")]
	public PowerState? Boost
		{
		get; set;
		}
	/// <summary>Gets or sets the complete weekly settings.</summary>
	/// <value>The seven daily settings to replace, or null when unchanged.</value>
	[JsonPropertyName ("weeklySettingInfo")]
	public List<HotWaterDay>? WeeklySettings
		{
		get; set;
		}
	}