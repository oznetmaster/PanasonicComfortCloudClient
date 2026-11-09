// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class AcParameters
	{
	/// <summary>Gets or sets the air-direction availability indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("airDirection")]
	public int? AirDirection
		{
		get; set;
		}

	/// <summary>Gets or sets the air-quality indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("airQuality")]
	public int? AirQuality
		{
		get; set;
		}

	/// <summary>Gets or sets the last-setting mode.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("lastSettingMode")]
	public int? LastSettingMode
		{
		get; set;
		}

	/// <summary>Gets or sets the fireplace mode.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("fireplace")]
	public int? Fireplace
		{
		get; set;
		}
	}