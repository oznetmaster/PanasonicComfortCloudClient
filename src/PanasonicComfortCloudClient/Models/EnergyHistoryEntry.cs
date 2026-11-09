// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains an energy-history sample without inventing missing totals.</summary>
public partial class EnergyHistoryEntry : ExtensibleModel
	{
	/// <summary>Gets or sets the date/time label supplied by the cloud.</summary>
	/// <value>The date/time label supplied by the cloud. Null when not reported or not requested.</value>
	[JsonPropertyName ("dataTime")]
	public string? DateTime
		{
		get; set;
		}

	/// <summary>Gets or sets heating energy consumption in kilowatt hours.</summary>
	/// <value>Heating energy consumption in kilowatt hours. Null when not reported or not requested.</value>
	[JsonPropertyName ("heatConsumption")]
	public double? HeatingEnergy
		{
		get; set;
		}

	/// <summary>Gets or sets cooling energy consumption in kilowatt hours.</summary>
	/// <value>Cooling energy consumption in kilowatt hours. Null when not reported or not requested.</value>
	[JsonPropertyName ("coolConsumption")]
	public double? CoolingEnergy
		{
		get; set;
		}

	/// <summary>Gets or sets domestic-hot-water energy in kilowatt hours.</summary>
	/// <value>Domestic-hot-water energy in kilowatt hours. Null when not reported or not requested.</value>
	[JsonPropertyName ("tankConsumption")]
	public double? HotWaterEnergy
		{
		get; set;
		}

	/// <summary>Gets or sets heating cost in the configured currency.</summary>
	/// <value>Heating cost in the configured currency. Null when not reported or not requested.</value>
	[JsonPropertyName ("heatCost")]
	public double? HeatingCost
		{
		get; set;
		}

	/// <summary>Gets or sets cooling cost in the configured currency.</summary>
	/// <value>Cooling cost in the configured currency. Null when not reported or not requested.</value>
	[JsonPropertyName ("coolCost")]
	public double? CoolingCost
		{
		get; set;
		}

	/// <summary>Gets or sets hot-water cost in the configured currency.</summary>
	/// <value>Hot-water cost in the configured currency. Null when not reported or not requested.</value>
	[JsonPropertyName ("tankCost")]
	public double? HotWaterCost
		{
		get; set;
		}

	/// <summary>Gets or sets air-conditioner or HWS energy consumption in the endpoint units.</summary>
	/// <value>Air-conditioner or HWS energy consumption in the endpoint units. Null when not reported or not requested.</value>
	[JsonPropertyName ("consumption")]
	public double? Consumption
		{
		get; set;
		}

	/// <summary>Gets or sets the outdoor temperature in degrees Celsius.</summary>
	/// <value>The outdoor temperature in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("outdoorTemp")]
	public virtual double? OutdoorTemperature
		{
		get; set;
		}
	}