// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains explicit changes to one zone.</summary>
public sealed partial class AquareaZoneUpdate : ExtensibleModel
	{
	/// <summary>Gets or sets the target zone identifier.</summary>
	/// <value>The target zone identifier. Null when not reported or not requested.</value>
	[JsonPropertyName ("zoneId")]
	public int? Id
		{
		get; set;
		}

	/// <summary>Gets or sets the requested operation state.</summary>
	/// <value>The requested operation state. Null when not reported or not requested.</value>
	[JsonPropertyName ("operationStatus")]
	public PowerState? Power
		{
		get; set;
		}

	/// <summary>Gets or sets the requested heating setting in the zone device units.</summary>
	/// <value>The requested heating setting in the zone device units. Null when not reported or not requested.</value>
	[JsonPropertyName ("heatSet")]
	public double? HeatSetpoint
		{
		get; set;
		}

	/// <summary>Gets or sets the requested cooling setting in the zone device units.</summary>
	/// <value>The requested cooling setting in the zone device units. Null when not reported or not requested.</value>
	[JsonPropertyName ("coolSet")]
	public double? CoolSetpoint
		{
		get; set;
		}
	}