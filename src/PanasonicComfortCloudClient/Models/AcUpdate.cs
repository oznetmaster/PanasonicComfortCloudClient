// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains explicit writable air-conditioner changes; null values are omitted.</summary>
public sealed partial class AcUpdate : ExtensibleModel
	{
	/// <summary>Gets or sets the operation state.</summary>
	/// <value>The operation state. Null when not reported or not requested.</value>
	[JsonPropertyName ("operate")]
	public PowerState? Power
		{
		get; set;
		}

	/// <summary>Gets or sets the operating mode.</summary>
	/// <value>The operating mode. Null when not reported or not requested.</value>
	[JsonPropertyName ("operationMode")]
	public AcMode? Mode
		{
		get; set;
		}

	/// <summary>Gets or sets the target temperature in the device temperature unit.</summary>
	/// <value>The target temperature in the device temperature unit. Null when not reported or not requested.</value>
	[JsonPropertyName ("temperatureSet")]
	public double? TargetTemperature
		{
		get; set;
		}

	/// <summary>Gets or sets the selected fan speed.</summary>
	/// <value>The selected fan speed. Null when not reported or not requested.</value>
	[JsonPropertyName ("fanSpeed")]
	public FanSpeed? Fan
		{
		get; set;
		}

	/// <summary>Gets or sets the economy or output mode.</summary>
	/// <value>The economy or output mode. Null when not reported or not requested.</value>
	[JsonPropertyName ("ecoMode")]
	public EcoMode? Eco
		{
		get; set;
		}

	/// <summary>Gets or sets the horizontal swing wire value; minus one denotes automatic.</summary>
	/// <value>The horizontal swing wire value; minus one denotes automatic. Null when not reported or not requested.</value>
	[JsonPropertyName ("airSwingLR")]
	public int? HorizontalSwing
		{
		get; set;
		}

	/// <summary>Gets or sets the vertical swing wire value; minus one denotes automatic.</summary>
	/// <value>The vertical swing wire value; minus one denotes automatic. Null when not reported or not requested.</value>
	[JsonPropertyName ("airSwingUD")]
	public int? VerticalSwing
		{
		get; set;
		}

	/// <summary>Gets or sets the automatic swing encoding: zero both, one disabled, two vertical, three horizontal.</summary>
	/// <value>The automatic swing encoding: zero both, one disabled, two vertical, three horizontal. Null when not reported or not requested.</value>
	[JsonPropertyName ("fanAutoMode")]
	public int? FanAutoMode
		{
		get; set;
		}

	/// <summary>Gets or sets the nanoe mode: zero unavailable, one off, two on, three G, four all.</summary>
	/// <value>The nanoe mode: zero unavailable, one off, two on, three G, four all. Null when not reported or not requested.</value>
	[JsonPropertyName ("nanoe")]
	public int? Nanoe
		{
		get; set;
		}

	/// <summary>Gets or sets the ECONAVI mode: zero unavailable, one off, two on.</summary>
	/// <value>The ECONAVI mode: zero unavailable, one off, two on. Null when not reported or not requested.</value>
	[JsonPropertyName ("ecoNavi")]
	public int? EcoNavi
		{
		get; set;
		}

	/// <summary>Gets or sets the AI ECO mode: zero unavailable, one off, two on.</summary>
	/// <value>The AI ECO mode: zero unavailable, one off, two on. Null when not reported or not requested.</value>
	[JsonPropertyName ("ecoFunctionData")]
	public int? EcoFunction
		{
		get; set;
		}

	/// <summary>Gets or sets the inside-cleaning flag.</summary>
	/// <value>The inside-cleaning flag. Null when not reported or not requested.</value>
	[JsonPropertyName ("insideCleaning")]
	public int? InsideCleaning
		{
		get; set;
		}

	/// <summary>Gets or sets the iAUTO-X flag.</summary>
	/// <value>The iAUTO-X flag. Null when not reported or not requested.</value>
	[JsonPropertyName ("iAuto")]
	public int? IAutoX
		{
		get; set;
		}

	/// <summary>Gets or sets the optional zoned-air-conditioner settings.</summary>
	/// <value>The optional zoned-air-conditioner settings. Null when not reported or not requested.</value>
	[JsonPropertyName ("zoneParameters")]
	public List<AcZone>? Zones
		{
		get; set;
		}
	}