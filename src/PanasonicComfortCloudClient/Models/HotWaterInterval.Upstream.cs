// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class HotWaterInterval
	{
	/// <summary>Gets or sets the inhibit-period availability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("inhibitTimeAvailable")]
	public int? InhibitAvailable
		{
		get; set;
		}

	/// <summary>Gets or sets the second-period availability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("set2Available")]
	public int? SecondPeriodAvailable
		{
		get; set;
		}
	}