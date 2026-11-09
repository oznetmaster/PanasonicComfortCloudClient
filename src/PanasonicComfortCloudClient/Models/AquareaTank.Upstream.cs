// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class AquareaTank
	{
	/// <summary>Gets or sets the alternate schedule minimum tank temperature.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("minTemp")]
	public double? MinimumTemperature
		{
		get; set;
		}

	/// <summary>Gets or sets the alternate schedule maximum tank temperature.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("maxTemp")]
	public double? MaximumTemperature
		{
		get; set;
		}
	}