// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class HotWaterParameters
	{
	/// <summary>Gets or sets the outdoor temperature in degrees Celsius.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("outdoorTemperature")]
	public double? OutdoorTemperature
		{
		get; set;
		}
	}