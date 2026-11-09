// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Describes an explicit partial Aquarea control request; null values are omitted.</summary>
public sealed partial class AquareaUpdate : ExtensibleModel
	{
	/// <summary>Gets or sets the requested whole-device operation status.</summary>
	/// <value>The requested whole-device operation status. Null when not reported or not requested.</value>
	[JsonPropertyName ("operationStatus")]
	public PowerState? Power
		{
		get; set;
		}

	/// <summary>Gets or sets the requested control mode, distinct from reported modes.</summary>
	/// <value>The requested control mode, distinct from reported modes. Null when not reported or not requested.</value>
	[JsonPropertyName ("operationMode")]
	public AquareaMode? Mode
		{
		get; set;
		}

	/// <summary>Gets or sets the requested quiet level.</summary>
	/// <value>The requested quiet level. Null when not reported or not requested.</value>
	[JsonPropertyName ("quietMode")]
	public QuietMode? Quiet
		{
		get; set;
		}

	/// <summary>Gets or sets the requested forced domestic-hot-water state.</summary>
	/// <value>The requested forced domestic-hot-water state. Null when not reported or not requested.</value>
	[JsonPropertyName ("forceDHW")]
	public PowerState? ForceHotWater
		{
		get; set;
		}

	/// <summary>Gets or sets the requested forced backup-heater state.</summary>
	/// <value>The requested forced backup-heater state. Null when not reported or not requested.</value>
	[JsonPropertyName ("forceHeater")]
	public PowerState? ForceHeater
		{
		get; set;
		}

	/// <summary>Gets or sets the requested holiday-timer state.</summary>
	/// <value>The requested holiday-timer state. Null when not reported or not requested.</value>
	[JsonPropertyName ("holidayTimer")]
	public PowerState? HolidayTimer
		{
		get; set;
		}

	/// <summary>Gets or sets the requested powerful-mode duration.</summary>
	/// <value>The requested powerful-mode duration. Null when not reported or not requested.</value>
	[JsonPropertyName ("powerful")]
	public PowerfulDuration? Powerful
		{
		get; set;
		}

	/// <summary>Gets or sets the requested special offset status.</summary>
	/// <value>The requested special offset status. Null when not reported or not requested.</value>
	[JsonPropertyName ("specialStatus")]
	public SpecialStatus? Special
		{
		get; set;
		}

	/// <summary>Gets or sets one to request a defrost cycle; null sends no defrost request.</summary>
	/// <value>One to request a defrost cycle; null sends no defrost request.</value>
	[JsonPropertyName ("forcedefrost")]
	public int? ForceDefrost
		{
		get; set;
		}

	/// <summary>Gets or sets the requested tank changes.</summary>
	/// <value>The requested tank changes. Null when not reported or not requested.</value>
	[JsonPropertyName ("tankStatus")]
	public AquareaTankUpdate? Tank
		{
		get; set;
		}

	/// <summary>Gets or sets the requested zone changes.</summary>
	/// <value>The requested zone changes. Null when not reported or not requested.</value>
	[JsonPropertyName ("zoneStatus")]
	public List<AquareaZoneUpdate>? Zones
		{
		get; set;
		}
	}