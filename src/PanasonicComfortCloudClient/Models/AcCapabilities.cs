// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PanasonicComfortCloudClient.Models;

/// <summary>Evaluates the upstream room-unit and CAC capability rules against an explicit snapshot.</summary>
/// <param name="device">The discovered device description.</param>
/// <param name="state">The snapshot whose feature flags and parameters are evaluated.</param>
public sealed class AcCapabilities (DeviceInfo device, AcSnapshot state)
	{
	private readonly DeviceInfo _device = device ?? throw new ArgumentNullException (nameof (device));
	private readonly AcSnapshot _state = state ?? throw new ArgumentNullException (nameof (state));
	/// <summary>Gets whether this is a ducted/commercial unit.</summary>
	/// <value>True for the upstream CAC device-type set.</value>
	public bool IsCommercial => _device.DeviceType is "4" or "5" or "6" or "8" or "9" or "10";
	/// <summary>Gets whether ECONAVI is both advertised and available.</summary>
	/// <value>True when both feature and current setting permit it.</value>
	public bool HasEcoNavi => _state.SupportsEcoNavi == true && _state.Parameters?.EcoNavi is 1 or 2;
	/// <summary>Gets whether AI ECO is advertised and available.</summary>
	/// <value>True when both capability and current setting permit it.</value>
	public bool HasEcoFunction => _state.EcoFunctionCapability is > 0 && _state.Parameters?.EcoFunction is 1 or 2;
	/// <summary>Gets whether nanoe is advertised and available.</summary>
	/// <value>True when the feature and current setting permit it.</value>
	public bool HasNanoe => _state.SupportsNanoe == true && _state.Parameters?.Nanoe is >= 1 and <= 4;
	/// <summary>Gets whether current zone data exists.</summary>
	/// <value>True when at least one zone is reported.</value>
	public bool HasZones => _state.Parameters?.Zones?.Count > 0;
	/// <summary>Gets whether inside cleaning is advertised.</summary>
	/// <value>The advertised feature flag, with absent treated as unadvertised.</value>
	public bool HasInsideCleaning => _state.SupportsInsideCleaning == true;
	/// <summary>Gets whether horizontal swing is available for this device and direction mode.</summary>
	/// <value>The upstream type-seven exception and CAC/direction rules applied to the snapshot.</value>
	public bool HasHorizontalSwing => _state.SupportsHorizontalSwing == true && _state.Parameters is { HorizontalSwing: not null and not 6 } &&
		(_device.DeviceType == "7" || (!IsCommercial && _state.Parameters.AirDirection is not (1 or 3)));
	/// <summary>Gets whether automatic fan speed is currently available.</summary>
	/// <value>False for fixed-speed CAC units and CAC fan-only mode.</value>
	public bool SupportsFanAuto => !IsCommercial || (_state.FanSpeedSteps != 0 && _state.Parameters?.Mode != AcMode.Fan);
	/// <summary>Gets the fan speeds supported by the current snapshot.</summary>
	/// <value>Zero, three or five fixed steps, with automatic inserted when supported.</value>
	public IReadOnlyList<FanSpeed> SupportedFanSpeeds
		{
		get
			{
			if (IsCommercial && _state.FanSpeedSteps == 0)
				return [];
			List<FanSpeed> speeds = IsCommercial && _state.FanSpeedSteps == 3
				? [FanSpeed.Low, FanSpeed.Medium, FanSpeed.High]
				: [FanSpeed.Low, FanSpeed.LowMedium, FanSpeed.Medium, FanSpeed.HighMedium, FanSpeed.High];
			if (SupportsFanAuto)
				speeds.Insert (0, FanSpeed.Auto);
			return speeds;
			}
		}
	/// <summary>Gets whether quiet operation is advertised for a room unit.</summary>
	/// <value>False for CAC units or absent/false flags.</value>
	public bool SupportsQuiet => !IsCommercial && _state.SupportsQuiet == true;
	/// <summary>Gets whether powerful operation is advertised for a room unit.</summary>
	/// <value>False for CAC units or absent/false flags.</value>
	public bool SupportsPowerful => !IsCommercial && _state.SupportsPowerful == true;
	/// <summary>Gets the supported economy modes.</summary>
	/// <value>Normal plus the advertised powerful and quiet modes.</value>
	public IReadOnlyList<EcoMode> SupportedEcoModes => [EcoMode.Auto, .. SupportsPowerful ? new[] { EcoMode.Powerful } : [], .. SupportsQuiet ? new[] { EcoMode.Quiet } : []];
	/// <summary>Gets whether an indoor reading is available.</summary>
	/// <value>True only when the snapshot contains a non-sentinel temperature.</value>
	public bool HasIndoorTemperature => _state.Parameters?.IndoorTemperature is not null;
	/// <summary>Gets whether an outdoor reading is available.</summary>
	/// <value>True only when the snapshot contains a non-sentinel temperature.</value>
	public bool HasOutdoorTemperature => _state.Parameters?.OutdoorTemperature is not null;
	/// <summary>Gets whether iAUTO-X is advertised and available.</summary>
	/// <value>True when feature and current setting permit it.</value>
	public bool HasIAutoX => _state.SupportsIAutoX == true && _state.Parameters?.IAutoX is 1 or 2;
	/// <summary>Gets whether the current target is in the model's summer-house range.</summary>
	/// <value>True for advertised discrete 8/10-degree or continuous 8–15-degree ranges.</value>
	public bool InSummerHouseMode => _state.Parameters?.TargetTemperature is double target && _state.SummerHouse switch
		{
			1 or 3 => target is 8 or 10,
			2 => target is >= 8 and <= 15,
			_ => false
			};
	}