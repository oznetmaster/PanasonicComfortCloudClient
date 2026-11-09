// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains AC history and the reported heating/cooling consumption fractions.</summary>
public sealed class AcEnergyHistoryEntry : EnergyHistoryEntry
	{
	/// <summary>Gets or sets the reported heating fraction.</summary>
	/// <value>The fraction of total consumption assigned to heating, or null when absent.</value>
	[JsonPropertyName ("heatConsumptionRate")]
	public double? HeatingRatio
		{
		get; set;
		}

	/// <summary>Gets or sets the reported cooling fraction.</summary>
	/// <value>The fraction of total consumption assigned to cooling, or null when absent.</value>
	[JsonPropertyName ("coolConsumptionRate")]
	public double? CoolingRatio
		{
		get; set;
		}

	/// <summary>Gets the heating share calculated from reported consumption and fraction.</summary>
	/// <value>The heating consumption in kWh, or null if either input is absent.</value>
	[JsonIgnore]
	public double? CalculatedHeatingConsumption => Consumption * HeatingRatio;

	/// <summary>Gets the cooling share calculated from reported consumption and fraction.</summary>
	/// <value>The cooling consumption in kWh, or null if either input is absent.</value>
	[JsonIgnore]
	public double? CalculatedCoolingConsumption => Consumption * CoolingRatio;
	}