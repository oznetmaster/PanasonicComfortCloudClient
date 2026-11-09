// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class DeviceInfo
	{
	/// <summary>Gets or sets the group display name.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("groupName")]
	public string? GroupName
		{
		get; set;
		}

	/// <summary>Gets or sets the summer-house mode capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("summerHouse")]
	public int? SummerHouse
		{
		get; set;
		}

	/// <summary>Gets or sets the standalone nanoe capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("nanoeStandAlone")]
	public bool? NanoeStandalone
		{
		get; set;
		}

	/// <summary>Gets or sets the automatic mode capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("autoMode")]
	public bool? SupportsAuto
		{
		get; set;
		}

	/// <summary>Gets or sets the energy-reporting capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("powerConsumptionFlg")]
	public bool? SupportsEnergy
		{
		get; set;
		}

	/// <summary>Gets or sets the available operating modes.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("modeAvlList")]
	public ModeAvailability? ModeAvailability
		{
		get; set;
		}

	/// <summary>Gets or sets the model revision.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("modelVersion")]
	public int? ModelVersion
		{
		get; set;
		}

	/// <summary>Gets or sets the device-coordination capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("coordinableFlg")]
	public bool? Coordinable
		{
		get; set;
		}
	}