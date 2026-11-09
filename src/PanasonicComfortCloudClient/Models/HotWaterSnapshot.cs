// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains standalone heat-pump hot-water status.</summary>
public sealed partial class HotWaterSnapshot : ExtensibleModel
	{
	/// <summary>Gets or sets the live hot-water parameter set.</summary>
	/// <value>The live hot-water parameter set. Null when not reported or not requested.</value>
	[JsonPropertyName ("parameters")]
	public HotWaterParameters? Parameters
		{
		get; set;
		}

	/// <summary>Gets or sets the account control permission.</summary>
	/// <value>The account control permission. Null when not reported or not requested.</value>
	[JsonPropertyName ("permission")]
	public int? Permission
		{
		get; set;
		}

	/// <summary>Gets or sets whether the device advertises weekly mode.</summary>
	/// <value>Whether the device advertises weekly mode. Null when not reported or not requested.</value>
	[JsonPropertyName ("weeklyMode")]
	public bool? SupportsWeeklyMode
		{
		get; set;
		}

	/// <summary>Gets or sets whether the device advertises photovoltaic mode.</summary>
	/// <value>Whether the device advertises photovoltaic mode. Null when not reported or not requested.</value>
	[JsonPropertyName ("pvMode")]
	public bool? SupportsPhotovoltaicMode
		{
		get; set;
		}

	/// <summary>Gets or sets whether the device advertises holiday mode.</summary>
	/// <value>Whether the device advertises holiday mode. Null when not reported or not requested.</value>
	[JsonPropertyName ("holidayMode")]
	public bool? SupportsHolidayMode
		{
		get; set;
		}
	}