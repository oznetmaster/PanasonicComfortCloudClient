// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class AcTimerPattern
	{
	/// <summary>Gets or sets the automatic swing mode.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("fanAutoMode")]
	public int? FanAutoMode
		{
		get; set;
		}

	/// <summary>Gets or sets the horizontal swing position.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("airSwingLR")]
	public int? HorizontalSwing
		{
		get; set;
		}

	/// <summary>Gets or sets the vertical swing position.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("airSwingUD")]
	public int? VerticalSwing
		{
		get; set;
		}

	/// <summary>Gets or sets the ECONAVI setting.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("ecoNavi")]
	public int? EcoNavi
		{
		get; set;
		}

	/// <summary>Gets or sets the nanoe setting.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("nanoe")]
	public int? Nanoe
		{
		get; set;
		}
	}