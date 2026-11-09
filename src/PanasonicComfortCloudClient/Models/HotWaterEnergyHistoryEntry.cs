// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains HWS history with the dedicated endpoint's temperature and cost field names.</summary>
public sealed class HotWaterEnergyHistoryEntry : ExtensibleModel
	{
	/// <summary>Gets or sets the cloud's date/time label.</summary>
	/// <value>The reported period label, or null when absent.</value>
	[JsonPropertyName ("dataTime")]
	public string? DateTime
		{
		get; set;
		}
	/// <summary>Gets or sets the reported consumption.</summary>
	/// <value>The HWS energy consumption in endpoint units, or null when absent.</value>
	[JsonPropertyName ("consumption")]
	public double? Consumption
		{
		get; set;
		}
	/// <summary>Gets the reported total without synthesizing a missing value.</summary>
	/// <value>The reported HWS consumption, or null when absent.</value>
	[JsonIgnore]
	public double? ReportedTotal => Consumption;
	/// <summary>Gets or sets the reported energy cost.</summary>
	/// <value>The cost in the account currency, or null when absent.</value>
	[JsonPropertyName ("cost")]
	public double? Cost
		{
		get; set;
		}

	/// <summary>Gets or sets the reported tank temperature.</summary>
	/// <value>The tank temperature in degrees Celsius, or null when absent.</value>
	[JsonPropertyName ("tankTemperature")]
	public double? TankTemperature
		{
		get; set;
		}

	/// <summary>Gets or sets the reported outdoor temperature.</summary>
	/// <value>The outdoor temperature in degrees Celsius, or null when absent.</value>
	[JsonPropertyName ("outdoorTemperature")]
	public double? OutdoorTemperature
		{
		get; set;
		}
	}