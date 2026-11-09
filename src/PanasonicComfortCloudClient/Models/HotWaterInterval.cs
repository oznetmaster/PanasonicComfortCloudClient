// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains one HWS schedule period and optional temperature settings.</summary>
public sealed partial class HotWaterInterval : ExtensibleModel
	{
	/// <summary>Gets or sets the device-local start hour.</summary>
	/// <value>The device-local start hour. Null when not reported or not requested.</value>
	[JsonPropertyName ("startTime")]
	public int? StartTime
		{
		get; set;
		}

	/// <summary>Gets or sets the device-local end hour.</summary>
	/// <value>The device-local end hour. Null when not reported or not requested.</value>
	[JsonPropertyName ("endTime")]
	public int? EndTime
		{
		get; set;
		}

	/// <summary>Gets or sets the starting temperature in degrees Celsius.</summary>
	/// <value>The starting temperature in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("startTemperature")]
	public double? StartTemperature
		{
		get; set;
		}

	/// <summary>Gets or sets the ending temperature in degrees Celsius.</summary>
	/// <value>The ending temperature in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("endTemperature")]
	public double? EndTemperature
		{
		get; set;
		}
	}