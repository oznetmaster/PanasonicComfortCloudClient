// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains the upstream ModeAvailability fields; absent values remain null.</summary>
public sealed partial class ModeAvailability : ExtensibleModel
	{
	/// <summary>Gets or sets the automatic mode availability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("autoMode")]
	public bool? Auto
		{
		get; set;
		}

	/// <summary>Gets or sets the fan-only mode availability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("fanMode")]
	public bool? Fan
		{
		get; set;
		}
	}