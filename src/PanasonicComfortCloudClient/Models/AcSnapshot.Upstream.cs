// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class AcSnapshot
	{
	/// <summary>Gets or sets the account control permission.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("permission")]
	public int? Permission
		{
		get; set;
		}

	/// <summary>Gets or sets the summer-house capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("summerHouse")]
	public int? SummerHouse
		{
		get; set;
		}

	/// <summary>Gets or sets the iAUTO-X capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("iAutoX")]
	public bool? SupportsIAutoX
		{
		get; set;
		}

	/// <summary>Gets or sets the nanoe capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("nanoe")]
	public bool? SupportsNanoe
		{
		get; set;
		}

	/// <summary>Gets or sets the standalone nanoe capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("nanoeStandAlone")]
	public bool? SupportsNanoeStandalone
		{
		get; set;
		}

	/// <summary>Gets or sets the automatic mode capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("autoMode")]
	public bool? SupportsAuto
		{
		get; set;
		}

	/// <summary>Gets or sets the heating capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("heatMode")]
	public bool? SupportsHeat
		{
		get; set;
		}

	/// <summary>Gets or sets the fan-only capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("fanMode")]
	public bool? SupportsFan
		{
		get; set;
		}

	/// <summary>Gets or sets the drying capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("dryMode")]
	public bool? SupportsDry
		{
		get; set;
		}

	/// <summary>Gets or sets the cooling capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("coolMode")]
	public bool? SupportsCool
		{
		get; set;
		}

	/// <summary>Gets or sets the ECONAVI capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("ecoNavi")]
	public bool? SupportsEcoNavi
		{
		get; set;
		}

	/// <summary>Gets or sets the powerful-mode capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("powerfulMode")]
	public bool? SupportsPowerful
		{
		get; set;
		}

	/// <summary>Gets or sets the quiet-mode capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("quietMode")]
	public bool? SupportsQuiet
		{
		get; set;
		}

	/// <summary>Gets or sets the horizontal-swing capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("airSwingLR")]
	public bool? SupportsHorizontalSwing
		{
		get; set;
		}

	/// <summary>Gets or sets the automatic vertical-swing capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("autoSwingUD")]
	public bool? SupportsAutoVerticalSwing
		{
		get; set;
		}

	/// <summary>Gets or sets the AI ECO capability indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("ecoFunction")]
	public int? EcoFunctionCapability
		{
		get; set;
		}

	/// <summary>Gets or sets the temperature-unit indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("temperatureUnit")]
	public int? TemperatureUnit
		{
		get; set;
		}

	/// <summary>Gets or sets the clothes-drying capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("clothesDrying")]
	public bool? SupportsClothesDrying
		{
		get; set;
		}

	/// <summary>Gets or sets the inside-cleaning capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("insideCleaning")]
	public bool? SupportsInsideCleaning
		{
		get; set;
		}

	/// <summary>Gets or sets the fireplace capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("fireplace")]
	public bool? SupportsFireplace
		{
		get; set;
		}

	/// <summary>Gets or sets the off-timer capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("offTimer")]
	public bool? SupportsOffTimer
		{
		get; set;
		}

	/// <summary>Gets or sets the energy-reporting capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("powerConsumptionFlg")]
	public bool? SupportsEnergy
		{
		get; set;
		}

	/// <summary>Gets or sets the device pairing indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("pairedFlg")]
	public bool? Paired
		{
		get; set;
		}

	/// <summary>Gets or sets the device nanoe indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("deviceNanoe")]
	public int? DeviceNanoe
		{
		get; set;
		}

	/// <summary>Gets or sets the available modes.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("modeAvlList")]
	public ModeAvailability? ModeAvailability
		{
		get; set;
		}

	/// <summary>Gets or sets the CAC fan-speed steps (zero fixed, three or five adjustable).</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("fanSpeedMode")]
	public int? FanSpeedSteps
		{
		get; set;
		}

	/// <summary>Gets or sets the CAC direction capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("fanDirectionMode")]
	public int? FanDirectionMode
		{
		get; set;
		}
	}