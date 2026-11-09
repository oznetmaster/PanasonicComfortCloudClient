// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PanasonicComfortCloudClient.Models;

namespace PanasonicComfortCloudClient;

/// <summary>Derives AC power from cumulative daily consumption using the upstream reset, clamp and stale-value rules.</summary>
/// <remarks>This application-owned tracker performs no polling or storage. Updates and individual property reads are synchronized, including multiword nullable values on 32-bit runtimes. Give it chronological observations from one device. Multiple property reads are separate observations, not one aggregate transaction. Missing readings remain unknown.</remarks>
public sealed class AcEnergyTracker
	{
#pragma warning disable IDE0330 // Monitor-based locking is shared with net472 and processor Mono.
	private readonly object _sync = new ();
#pragma warning restore IDE0330
	private DateTimeOffset? _changedAt, _observedAt;
	/// <summary>Gets the maximum inferred power used by the Python reference.</summary>
	/// <value>Five thousand watts.</value>
	public const double MAXIMUM_POWER_WATTS = 5000;
	/// <summary>Gets the last accepted cumulative consumption.</summary>
	/// <value>Daily kWh, or null before a valid observation.</value>
	public double? Consumption
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}
	/// <summary>Gets the last accepted heating fraction.</summary>
	/// <value>The reported fraction, or null when unknown.</value>
	public double? HeatingRatio
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}
	/// <summary>Gets the last accepted cooling fraction.</summary>
	/// <value>The reported fraction, or null when unknown.</value>
	public double? CoolingRatio
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}
	/// <summary>Gets inferred heating consumption.</summary>
	/// <value>Cumulative daily heating kWh, or null when unknown.</value>
	public double? HeatingConsumption
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}
	/// <summary>Gets inferred cooling consumption.</summary>
	/// <value>Cumulative daily cooling kWh, or null when unknown.</value>
	public double? CoolingConsumption
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}
	/// <summary>Gets inferred total power.</summary>
	/// <value>Watts between consumption changes, null before a usable interval, or zero after 25 unchanged minutes.</value>
	public double? PowerWatts
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}
	/// <summary>Gets inferred heating power.</summary>
	/// <value>Heating watts with the same clamp and stale-value rules.</value>
	public double? HeatingPowerWatts
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}
	/// <summary>Gets inferred cooling power.</summary>
	/// <value>Cooling watts with the same clamp and stale-value rules.</value>
	public double? CoolingPowerWatts
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}

	/// <summary>Applies one chronological sample; resets in a daily counter count the new counter as the interval increment.</summary>
	/// <param name="sample">The sample from one AC's daily history entry.</param>
	/// <param name="observedAt">The observation instant, supplied by the application for reproducible timing.</param>
	/// <returns>True if an accepted reading or derived output changed.</returns>
	public bool Update (AcEnergyHistoryEntry sample, DateTimeOffset observedAt)
		{
		lock (_sync)
			return UpdateCore (sample, observedAt);
		}
	private bool UpdateCore (AcEnergyHistoryEntry sample, DateTimeOffset observedAt)
		{
		ArgumentNullException.ThrowIfNull (sample);
		if (_observedAt is DateTimeOffset prior && observedAt <= prior)
			throw new ArgumentOutOfRangeException (nameof (observedAt), "Observations must advance in time.");
		_observedAt = observedAt;
		(double? _consumption, double? _heatingRatio, double? _coolingRatio, double? PowerWatts, double? HeatingPowerWatts, double? CoolingPowerWatts) previous = (Consumption, HeatingRatio, CoolingRatio, PowerWatts, HeatingPowerWatts, CoolingPowerWatts);
		if (Valid (sample.HeatingRatio))
			HeatingRatio = sample.HeatingRatio;
		if (Valid (sample.CoolingRatio))
			CoolingRatio = sample.CoolingRatio;
		if (!Valid (sample.Consumption))
			return previous != (Consumption, HeatingRatio, CoolingRatio, PowerWatts, HeatingPowerWatts, CoolingPowerWatts);
		if (Consumption == sample.Consumption)
			{
			if (_changedAt is DateTimeOffset changed && observedAt - changed >= TimeSpan.FromMinutes (25))
				{
				PowerWatts = 0;
				HeatingPowerWatts = HeatingRatio.HasValue ? 0 : null;
				CoolingPowerWatts = CoolingRatio.HasValue ? 0 : null;
				}
			}
		else
			{
			double? heat = sample.Consumption * HeatingRatio, cool = sample.Consumption * CoolingRatio;
			if (_changedAt is DateTimeOffset changed)
				{
				double hours = (observedAt - changed).TotalHours;
				PowerWatts = Power (sample.Consumption, Consumption, hours);
				HeatingPowerWatts = Power (heat, HeatingConsumption, hours);
				CoolingPowerWatts = Power (cool, CoolingConsumption, hours);
				}

			Consumption = sample.Consumption;
			HeatingConsumption = heat;
			CoolingConsumption = cool;
			_changedAt = observedAt;
			}

		return previous != (Consumption, HeatingRatio, CoolingRatio, PowerWatts, HeatingPowerWatts, CoolingPowerWatts);
		}
	private static bool Valid (double? value) => value is double number && !double.IsNaN (number) && !double.IsInfinity (number) && number >= 0;
	private static double? Power (double? current, double? previous, double hours) => current is not double now || previous is not double old ? null
		: Math.Min (MAXIMUM_POWER_WATTS, Math.Max (0, Math.Round ((now < old ? now : now - old) * 1000 / hours)));
	}