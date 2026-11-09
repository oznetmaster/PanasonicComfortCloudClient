// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains explicit domestic-hot-water tank changes.</summary>
public sealed partial class AquareaTankUpdate : ExtensibleModel
	{
	/// <summary>Gets or sets the requested tank operation state.</summary>
	/// <value>The requested tank operation state. Null when not reported or not requested.</value>
	[JsonPropertyName ("operationStatus")]
	public PowerState? Power
		{
		get; set;
		}

	/// <summary>Gets or sets the requested tank temperature in degrees Celsius.</summary>
	/// <value>The requested tank temperature in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("heatSet")]
	public double? HeatSetpoint
		{
		get; set;
		}
	}