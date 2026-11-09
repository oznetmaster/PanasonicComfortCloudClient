// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains the upstream HotWaterTemperatureLimits fields; absent values remain null.</summary>
public sealed partial class HotWaterTemperatureLimits : ExtensibleModel
	{
	/// <summary>Gets or sets the fixed first-period ending temperature.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("set1EndTemperature")]
	public int? FirstEndTemperature
		{
		get; set;
		}

	/// <summary>Gets or sets the minimum second-period starting temperature.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("set2StartTemperatureMin")]
	public int? SecondStartMinimum
		{
		get; set;
		}

	/// <summary>Gets or sets the maximum second-period starting temperature.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("set2StartTemperatureMax")]
	public int? SecondStartMaximum
		{
		get; set;
		}

	/// <summary>Gets or sets the minimum second-period ending temperature.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("set2EndTemperatureMin")]
	public int? SecondEndMinimum
		{
		get; set;
		}

	/// <summary>Gets or sets the maximum second-period ending temperature.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("set2EndTemperatureMax")]
	public int? SecondEndMaximum
		{
		get; set;
		}
	}