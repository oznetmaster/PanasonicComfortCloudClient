// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class AcZone
	{
	/// <summary>Gets or sets the zone damper level.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("zoneLevel")]
	public int? DamperLevel
		{
		get; set;
		}

	/// <summary>Gets or sets the zone spill indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("zoneSpill")]
	public int? Spill
		{
		get; set;
		}

	/// <summary>Gets or sets the measured zone temperature; minus 255 is unavailable.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("zoneTemperature"), JsonConverter (typeof (PanasonicComfortCloudClient.Protocol.ZoneTemperatureConverter))]
	public double? Temperature
		{
		get; set;
		}
	}