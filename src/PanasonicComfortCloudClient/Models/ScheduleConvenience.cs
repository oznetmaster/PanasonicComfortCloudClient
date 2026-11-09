// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class WeeklyTimerDay<TPattern> where TPattern : TimerPattern
	{
	/// <summary>Gets the weekday assigned when the containing schedule is read or created.</summary>
	/// <value>The device-local weekday, or null when this day was constructed independently.</value>
	[JsonIgnore]
	public DayOfWeek? Weekday
		{
		get; internal set;
		}
	}

public sealed partial class AquareaSchedule
	{
	/// <summary>Gets the gateway associated with a read or explicitly created schedule.</summary>
	/// <value>The gateway ID, or null until associated by the application or client.</value>
	[JsonIgnore]
	public string? GatewayId
		{
		get; internal set;
		}
	/// <summary>Gets or sets explicit timer activation.</summary>
	/// <value>True for operation one, false for zero, or null for an unknown/missing activation.</value>
	[JsonIgnore]
	public bool? Enabled
		{
		get => Timer?.Operation switch { 0 => false, 1 => true, _ => null };
		set
			{
			Timer ??= new ();
			Timer.Operation = value is null ? null : value == true ? 1 : 0;
			}
		}
	/// <summary>Finds a zone's reported schedule limits.</summary>
	/// <param name="zoneId">The zone identifier.</param>
	/// <returns>The typed limits, or null when absent.</returns>
	public AquareaZone? ZoneLimits (int zoneId) => Limits?.GetZone (zoneId);
	/// <summary>Gets the reported tank schedule limits.</summary>
	/// <value>The typed tank limits, or null when absent.</value>
	[JsonIgnore]
	public AquareaTank? TankLimits => Limits?.Tank;
	}

public sealed partial class AquareaTimerPattern
	{
	/// <summary>Gets the unmodified numeric schedule mode.</summary>
	/// <value>The raw mode, including an unrecognized numeric value, or null.</value>
	[JsonIgnore]
	public int? RawMode => Mode is AquareaMode value ? (int)value : null;
	/// <summary>Reads one supported zone's scheduled operation.</summary>
	/// <param name="zoneId">One or two.</param>
	/// <returns>The requested state, or null when unspecified.</returns>
	public PowerState? ZonePower (int zoneId) => zoneId switch { 1 => Zone1Power, 2 => Zone2Power, _ => throw new ArgumentOutOfRangeException (nameof (zoneId)) };
	/// <summary>Reads one zone's heating target.</summary>
	/// <param name="zoneId">One or two.</param>
	/// <returns>The target, or null when unspecified.</returns>
	public int? ZoneHeat (int zoneId) => zoneId switch { 1 => Zone1Heat, 2 => Zone2Heat, _ => throw new ArgumentOutOfRangeException (nameof (zoneId)) };
	/// <summary>Reads one zone's cooling target.</summary>
	/// <param name="zoneId">One or two.</param>
	/// <returns>The target, or null when unspecified.</returns>
	public int? ZoneCool (int zoneId) => zoneId switch { 1 => Zone1Cool, 2 => Zone2Cool, _ => throw new ArgumentOutOfRangeException (nameof (zoneId)) };
	/// <summary>Changes only one zone's scheduled operation.</summary>
	/// <param name="zoneId">One or two.</param>
	/// <param name="power">The operation state, or null to omit it.</param>
	public void SetZonePower (int zoneId, PowerState? power) => SetZone (zoneId, power, ZoneHeat (zoneId), ZoneCool (zoneId));
	/// <summary>Changes only one zone's scheduled heating target.</summary>
	/// <param name="zoneId">One or two.</param>
	/// <param name="heat">The heating target, or null to omit it.</param>
	public void SetZoneHeat (int zoneId, int? heat) => SetZone (zoneId, ZonePower (zoneId), heat, ZoneCool (zoneId));
	/// <summary>Changes only one zone's scheduled cooling target.</summary>
	/// <param name="zoneId">One or two.</param>
	/// <param name="cool">The cooling target, or null to omit it.</param>
	public void SetZoneCool (int zoneId, int? cool) => SetZone (zoneId, ZonePower (zoneId), ZoneHeat (zoneId), cool);
	/// <summary>Sets a supported zone's scheduled operation and optional targets.</summary>
	/// <param name="zoneId">One or two.</param>
	/// <param name="power">The requested zone operation.</param>
	/// <param name="heat">The heating target, or null to omit it.</param>
	/// <param name="cool">The cooling target, or null to omit it.</param>
	public void SetZone (int zoneId, PowerState? power, int? heat = null, int? cool = null)
		{
		if (zoneId == 1)
			{
			Zone1Power = power;
			Zone1Heat = heat;
			Zone1Cool = cool;
			}
		else if (zoneId == 2)
			{
			Zone2Power = power;
			Zone2Heat = heat;
			Zone2Cool = cool;
			}
		else
			{
			throw new ArgumentOutOfRangeException (nameof (zoneId));
			}
		}
	}

public sealed partial class AcTimerPattern
	{
	/// <summary>Gets the unmodified numeric schedule mode.</summary>
	/// <value>The raw mode, including unrecognized values, or null.</value>
	[JsonIgnore]
	public int? RawMode => Mode is AcMode value ? (int)value : null;
	}