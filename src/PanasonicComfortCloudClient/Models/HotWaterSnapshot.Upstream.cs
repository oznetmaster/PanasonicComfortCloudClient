// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class HotWaterSnapshot
	{
	/// <summary>Gets or sets the weekly temperature-setting limits.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("temperatureSettingLimits")]
	public HotWaterTemperatureLimits? TemperatureLimits
		{
		get; set;
		}
	}