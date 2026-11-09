// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains a standalone HWS daily schedule; unknown fields are preserved.</summary>
public sealed partial class HotWaterDay : ExtensibleModel
	{
	/// <summary>Gets or sets the weekday, with Monday one and Sunday seven.</summary>
	/// <value>The weekday, with Monday one and Sunday seven. Null when not reported or not requested.</value>
	[JsonPropertyName ("weekday")]
	public int? Weekday
		{
		get; set;
		}

	/// <summary>Gets or sets the daily mode: zero timer or one continuous.</summary>
	/// <value>The daily mode: zero timer or one continuous. Null when not reported or not requested.</value>
	[JsonPropertyName ("mode")]
	public int? Mode
		{
		get; set;
		}

	/// <summary>Gets or sets the period during which heating is inhibited.</summary>
	/// <value>The period during which heating is inhibited. Null when not reported or not requested.</value>
	[JsonPropertyName ("inhibitTime")]
	public HotWaterInterval? InhibitTime
		{
		get; set;
		}

	/// <summary>Gets or sets the first heating period.</summary>
	/// <value>The first heating period. Null when not reported or not requested.</value>
	[JsonPropertyName ("set1")]
	public HotWaterInterval? FirstPeriod
		{
		get; set;
		}

	/// <summary>Gets or sets the second heating period.</summary>
	/// <value>The second heating period. Null when not reported or not requested.</value>
	[JsonPropertyName ("set2")]
	public HotWaterInterval? SecondPeriod
		{
		get; set;
		}
	}