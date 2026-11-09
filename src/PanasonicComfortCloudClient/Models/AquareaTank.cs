// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Describes the optional domestic-hot-water tank.</summary>
public sealed partial class AquareaTank : ExtensibleModel
	{
	/// <summary>Gets or sets the tank operation status.</summary>
	/// <value>The tank operation status. Null when not reported or not requested.</value>
	[JsonPropertyName ("operationStatus")]
	public PowerState? Power
		{
		get; set;
		}

	/// <summary>Gets or sets the measured tank temperature in degrees Celsius.</summary>
	/// <value>The measured tank temperature in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("temperatureNow")]
	public double? Temperature
		{
		get; set;
		}

	/// <summary>Gets or sets the target tank temperature in degrees Celsius.</summary>
	/// <value>The target tank temperature in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("heatSet")]
	public double? HeatSetpoint
		{
		get; set;
		}

	/// <summary>Gets or sets the minimum target in degrees Celsius.</summary>
	/// <value>The minimum target in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("heatMin")]
	public double? HeatMinimum
		{
		get; set;
		}

	/// <summary>Gets or sets the maximum target in degrees Celsius.</summary>
	/// <value>The maximum target in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("heatMax")]
	public double? HeatMaximum
		{
		get; set;
		}
	}