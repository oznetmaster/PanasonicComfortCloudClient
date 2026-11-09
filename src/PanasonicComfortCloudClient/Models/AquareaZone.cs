// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Describes a heating or cooling zone and its advertised ranges.</summary>
public sealed partial class AquareaZone : ExtensibleModel
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
	[JsonPropertyName ("operationStatus")]
	public PowerState? Power
		{
		get; set;
		}

	/// <summary>Gets or sets the measured zone temperature in degrees Celsius.</summary>
	/// <value>The measured zone temperature in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("temperatureNow")]
	public double? Temperature
		{
		get; set;
		}

	/// <summary>Gets or sets the heating setpoint; degrees Celsius or a compensation offset as configured by the device.</summary>
	/// <value>The heating setpoint; degrees Celsius or a compensation offset as configured by the device. Null when not reported or not requested.</value>
	[JsonPropertyName ("heatSet")]
	public double? HeatSetpoint
		{
		get; set;
		}

	/// <summary>Gets or sets the cooling setpoint; degrees Celsius or a compensation offset as configured by the device.</summary>
	/// <value>The cooling setpoint; degrees Celsius or a compensation offset as configured by the device. Null when not reported or not requested.</value>
	[JsonPropertyName ("coolSet")]
	public double? CoolSetpoint
		{
		get; set;
		}

	/// <summary>Gets or sets the minimum allowed heating setting in device units.</summary>
	/// <value>The minimum allowed heating setting in device units. Null when not reported or not requested.</value>
	[JsonPropertyName ("heatMin")]
	public double? HeatMinimum
		{
		get; set;
		}

	/// <summary>Gets or sets the maximum allowed heating setting in device units.</summary>
	/// <value>The maximum allowed heating setting in device units. Null when not reported or not requested.</value>
	[JsonPropertyName ("heatMax")]
	public double? HeatMaximum
		{
		get; set;
		}

	/// <summary>Gets or sets the minimum allowed cooling setting in device units.</summary>
	/// <value>The minimum allowed cooling setting in device units. Null when not reported or not requested.</value>
	[JsonPropertyName ("coolMin")]
	public double? CoolMinimum
		{
		get; set;
		}

	/// <summary>Gets or sets the maximum allowed cooling setting in device units.</summary>
	/// <value>The maximum allowed cooling setting in device units. Null when not reported or not requested.</value>
	[JsonPropertyName ("coolMax")]
	public double? CoolMaximum
		{
		get; set;
		}

	/// <summary>Gets or sets the Eco heating offset in device units.</summary>
	/// <value>The Eco heating offset in device units. Null when not reported or not requested.</value>
	[JsonPropertyName ("ecoHeat")]
	public double? EcoHeat
		{
		get; set;
		}

	/// <summary>Gets or sets the Eco cooling offset in device units.</summary>
	/// <value>The Eco cooling offset in device units. Null when not reported or not requested.</value>
	[JsonPropertyName ("ecoCool")]
	public double? EcoCool
		{
		get; set;
		}

	/// <summary>Gets or sets the Comfort heating offset in device units.</summary>
	/// <value>The Comfort heating offset in device units. Null when not reported or not requested.</value>
	[JsonPropertyName ("comfortHeat")]
	public double? ComfortHeat
		{
		get; set;
		}

	/// <summary>Gets or sets the Comfort cooling offset in device units.</summary>
	/// <value>The Comfort cooling offset in device units. Null when not reported or not requested.</value>
	[JsonPropertyName ("comfortCool")]
	public double? ComfortCool
		{
		get; set;
		}
	}