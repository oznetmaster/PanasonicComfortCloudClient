// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class AcSchedule
	{
	/// <summary>Gets or sets the schedule edit permission.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("permission")]
	public int? Permission
		{
		get; set;
		}

	/// <summary>Gets or sets the schedule temperature unit.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("temperatureUnit")]
	public int? TemperatureUnit
		{
		get; set;
		}

	/// <summary>Gets or sets the available schedule modes.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("modeAvlList")]
	public ModeAvailability? ModeAvailability
		{
		get; set;
		}
	}