// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains one ducted-air-conditioner zone setting.</summary>
public sealed partial class AcZone : ExtensibleModel
	{
	/// <summary>Gets or sets the zone identifier.</summary>
	/// <value>The zone identifier. Null when not reported or not requested.</value>
	[JsonPropertyName ("zoneId")]
	public int? Id
		{
		get; set;
		}

	/// <summary>Gets or sets the zone name.</summary>
	/// <value>The zone name. Null when not reported or not requested.</value>
	[JsonPropertyName ("zoneName")]
	public string? Name
		{
		get; set;
		}

	/// <summary>Gets or sets the zone operation status.</summary>
	/// <value>The zone operation status. Null when not reported or not requested.</value>
	[JsonPropertyName ("zoneOnOff")]
	public PowerState? Power
		{
		get; set;
		}
	}