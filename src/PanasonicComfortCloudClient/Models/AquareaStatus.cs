// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains one Aquarea status observation; omitted values remain null.</summary>
public sealed partial class AquareaStatus : ExtensibleModel
	{
	/// <summary>Gets or sets the whole-device operation status.</summary>
	/// <value>The whole-device operation status. Null when not reported or not requested.</value>
	[JsonPropertyName ("operationStatus")]
	public PowerState? Power
		{
		get; set;
		}

	/// <summary>Gets or sets the reported mode, whose encoding differs from writable modes.</summary>
	/// <value>The reported mode, whose encoding differs from writable modes. Null when not reported or not requested.</value>
	[JsonPropertyName ("operationMode")]
	public AquareaReportedMode? Mode
		{
		get; set;
		}

	/// <summary>Gets or sets the wire device status: zero normal, one defrost; the misspelled wire key is intentional.</summary>
	/// <value>The wire device status: zero normal, one defrost; the misspelled wire key is intentional. Null when not reported or not requested.</value>
	[JsonPropertyName ("deiceStatus")]
	public int? DeviceStatus
		{
		get; set;
		}

	/// <summary>Gets or sets the outdoor temperature in degrees Celsius.</summary>
	/// <value>The outdoor temperature in degrees Celsius. Null when not reported or not requested.</value>
	[JsonPropertyName ("outdoorNow")]
	public double? OutdoorTemperature
		{
		get; set;
		}

	/// <summary>Gets or sets the direction indicator: zero idle, one pump, two water.</summary>
	/// <value>The direction indicator: zero idle, one pump, two water. Null when not reported or not requested.</value>
	[JsonPropertyName ("direction")]
	public int? Direction
		{
		get; set;
		}

	/// <summary>Gets or sets the pump operation indication.</summary>
	/// <value>The pump operation indication. Null when not reported or not requested.</value>
	[JsonPropertyName ("pumpDuty")]
	public PowerState? PumpDuty
		{
		get; set;
		}

	/// <summary>Gets or sets the quiet-operation level.</summary>
	/// <value>The quiet-operation level. Null when not reported or not requested.</value>
	[JsonPropertyName ("quietMode")]
	public QuietMode? Quiet
		{
		get; set;
		}

	/// <summary>Gets or sets the forced domestic-hot-water flag.</summary>
	/// <value>The forced domestic-hot-water flag. Null when not reported or not requested.</value>
	[JsonPropertyName ("forceDHW")]
	public PowerState? ForceHotWater
		{
		get; set;
		}

	/// <summary>Gets or sets the forced backup-heater flag.</summary>
	/// <value>The forced backup-heater flag. Null when not reported or not requested.</value>
	[JsonPropertyName ("forceHeater")]
	public PowerState? ForceHeater
		{
		get; set;
		}

	/// <summary>Gets or sets the holiday-timer flag.</summary>
	/// <value>The holiday-timer flag. Null when not reported or not requested.</value>
	[JsonPropertyName ("holidayTimer")]
	public PowerState? HolidayTimer
		{
		get; set;
		}

	/// <summary>Gets or sets the powerful-mode duration.</summary>
	/// <value>The powerful-mode duration. Null when not reported or not requested.</value>
	[JsonPropertyName ("powerful")]
	public PowerfulDuration? Powerful
		{
		get; set;
		}

	/// <summary>Gets or sets the Eco or Comfort setpoint offset.</summary>
	/// <value>The Eco or Comfort setpoint offset. Null when not reported or not requested.</value>
	[JsonPropertyName ("specialStatus")]
	public SpecialStatus? Special
		{
		get; set;
		}

	/// <summary>Gets or sets the reported cooling capability flag.</summary>
	/// <value>The reported cooling capability flag. Null when not reported or not requested.</value>
	[JsonPropertyName ("coolMode")]
	public int? CoolingCapability
		{
		get; set;
		}

	/// <summary>Gets or sets the reported water pressure; the API does not guarantee a unit.</summary>
	/// <value>The reported water pressure; the API does not guarantee a unit. Null when not reported or not requested.</value>
	[JsonPropertyName ("waterPressure")]
	public double? WaterPressure
		{
		get; set;
		}

	/// <summary>Gets or sets the optional domestic-hot-water tank.</summary>
	/// <value>The optional domestic-hot-water tank. Null when not reported or not requested.</value>
	[JsonPropertyName ("tankStatus")]
	public AquareaTank? Tank
		{
		get; set;
		}

	/// <summary>Gets or sets the reported zones, or null when absent.</summary>
	/// <value>The reported zones, or null when absent.</value>
	[JsonPropertyName ("zoneStatus")]
	public List<AquareaZone>? Zones
		{
		get; set;
		}

	/// <summary>Gets or sets the returned faults, or null when absent.</summary>
	/// <value>The returned faults, or null when absent.</value>
	[JsonPropertyName ("faultStatus")]
	public List<ComfortCloudFault>? Faults
		{
		get; set;
		}

	/// <summary>Gets or sets the service type, or null when absent.</summary>
	/// <value>The service type, or null when absent.</value>
	[JsonPropertyName ("serviceType")]
	public string? ServiceType
		{
		get; set;
		}

	/// <summary>Gets or sets whether the gateway reports that control is unavailable.</summary>
	/// <value>Whether the gateway reports that control is unavailable. Null when not reported or not requested.</value>
	[JsonPropertyName ("uncontrollableTaw1Flg")]
	public bool? UncontrollableGateway
		{
		get; set;
		}
	}