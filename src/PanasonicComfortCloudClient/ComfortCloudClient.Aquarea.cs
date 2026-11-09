// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net.Http;

using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient;

public sealed partial class ComfortCloudClient
	{
	private const string TRANSFER_PATH = "remote/v1/app/common/transfer";

	/// <summary>Fetches groups and their device descriptions from Comfort Cloud.</summary>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>The reported groups; a missing group list is an error rather than an empty discovery result.</returns>
	public Task<IReadOnlyList<ComfortCloudGroup>> GetGroupsAsync (CancellationToken cancellationToken = default) => WithGateAsync<IReadOnlyList<ComfortCloudGroup>> (async token =>
	{
		GroupEnvelope response = await ApiCoreAsync<GroupEnvelope> (HttpMethod.Get, "device/group", null, token).ConfigureAwait (false);
		return response.Groups ?? throw new ComfortCloudException ("Discovery response omitted groupList.");
	}, cancellationToken);

	/// <summary>Fetches Aquarea status using the current transfer-proxy API.</summary>
	/// <param name="gatewayId">The discovered Aquarea device GUID.</param>
	/// <param name="source">Whether to request live or explicitly cached status; no fallback is performed.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>A complete typed response labelled with its requested source and receipt time.</returns>
	public Task<AquareaSnapshot> GetAquareaStatusAsync (string gatewayId, StatusSource source = StatusSource.Live, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (gatewayId);
		return !Enum.IsDefined (source)
			? throw new ArgumentOutOfRangeException (nameof (source))
			: WithGateAsync (token => GetAquareaCoreAsync (gatewayId, source, token), cancellationToken);
		}

	private async Task<AquareaSnapshot> GetAquareaCoreAsync (string gatewayId, StatusSource source, CancellationToken token)
		{
		var direct = source == StatusSource.Live ? "1" : "0";
		var path = "/remote/v1/api/devices?gwid=" + Uri.EscapeDataString (gatewayId) + "&deviceDirect=" + direct;
		AquareaSnapshot response = await ApiCoreAsync<AquareaSnapshot> (HttpMethod.Post, TRANSFER_PATH, new TransferRequest<object>
			{
			ApiName = path
			}, token).ConfigureAwait (false);
		if (response.Status is null)
			{
			throw new ComfortCloudException ("Aquarea response omitted status.");
			}

		response.Source = source;
		response.ObservedAt = DateTimeOffset.UtcNow;
		return response;
		}

	/// <summary>Applies explicit Aquarea changes while preserving other zones and tank operation.</summary>
	/// <param name="gatewayId">The discovered Aquarea device GUID.</param>
	/// <param name="update">Only the intended changes. Branch and Eco/Comfort changes first obtain a fresh status snapshot.</param>
	/// <param name="cancellationToken">Cancels waiting, the preliminary read or the control request.</param>
	/// <returns>A task completing on cloud acknowledgement, which does not itself confirm physical device state.</returns>
	/// <remarks>No device command is retried automatically. Concurrent changes made outside this client may race a read/modify/write operation.</remarks>
	public Task UpdateAquareaAsync (string gatewayId, AquareaUpdate update, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (gatewayId);
		ArgumentNullException.ThrowIfNull (update);

		AquareaUpdate copied = Clone (update);
		ValidateAquareaUpdate (copied);
		return WithGateAsync (async token =>
		{
			AquareaControlRequest request = ToControl (gatewayId, copied);
			if (copied.Power.HasValue || copied.Mode.HasValue || copied.Zones is not null || copied.Tank is not null || copied.Special.HasValue)
				{
				AquareaSnapshot current = await GetAquareaCoreAsync (gatewayId, StatusSource.Live, token).ConfigureAwait (false);
				PreserveAquareaBranches (request, copied, current.Status!);
				}

			_ = await ApiCoreAsync<ErrorEnvelope> (HttpMethod.Post, TRANSFER_PATH, new TransferRequest<AquareaControlRequest>
				{
				ApiName = "/remote/v1/api/devices",
				Method = "POST",
				Body = request
				}, token).ConfigureAwait (false);
			return true;
		}, cancellationToken);
		}

	private T Clone<T> (T value) => System.Text.Json.JsonSerializer.Deserialize<T> (System.Text.Json.JsonSerializer.Serialize (value, _json), _json)!;

	private static AquareaControlRequest ToControl (string id, AquareaUpdate update) => new ()
		{
		GatewayId = id,
		Power = update.Power,
		Mode = update.Mode,
		Quiet = update.Quiet,
		ForceHotWater = update.ForceHotWater,
		ForceHeater = update.ForceHeater,
		HolidayTimer = update.HolidayTimer,
		Powerful = update.Powerful,
		Special = update.Special,
		ForceDefrost = update.ForceDefrost,
		Tank = update.Tank,
		Zones = update.Zones
		};

	private static void ValidateAquareaUpdate (AquareaUpdate update)
		{
		if (update.Zones?.Any (z => z is null || z.Id is null) == true)
			{
			throw new ArgumentException ("Every changed zone needs its identifier.", nameof (update));
			}

		IEnumerable<PowerState?> switches = new[] { update.Power, update.Tank?.Power, update.ForceHotWater, update.ForceHeater, update.HolidayTimer
}.Concat (update.Zones?.Select (z => z.Power) ?? []);
		if (switches.Any (p => p.HasValue && p is not (PowerState.Off or PowerState.On)))
			{
			throw new ArgumentException ("Unknown operation status cannot be sent as a command.", nameof (update));
			}

		if (update.Mode.HasValue && !Enum.IsDefined (update.Mode.Value))
			{
			throw new ArgumentException ("Unknown control mode.", nameof (update));
			}

		if (update.Quiet.HasValue && !Enum.IsDefined (update.Quiet.Value))
			{
			throw new ArgumentException ("Unknown quiet level.", nameof (update));
			}

		if (update.Powerful.HasValue && !Enum.IsDefined (update.Powerful.Value))
			{
			throw new ArgumentException ("Unknown powerful duration.", nameof (update));
			}

		if (update.Special.HasValue && !Enum.IsDefined (update.Special.Value))
			{
			throw new ArgumentException ("Unknown special status.", nameof (update));
			}

		if (update.ForceDefrost is not (null or 1))
			{
			throw new ArgumentException ("Defrost requests use one.", nameof (update));
			}

		if (update.Zones is not null && update.Zones.Select (z => z.Id).Distinct ().Count () != update.Zones.Count)
			{
			throw new ArgumentException ("Duplicate changed zone.", nameof (update));
			}

		if (update.Power is null && update.Mode is null && update.Quiet is null && update.ForceHotWater is null && update.ForceHeater is null && update.HolidayTimer is null && update.Powerful is null && update.Special is null && update.ForceDefrost is null && update.Tank is null && update.Zones is null)
			{
			throw new ArgumentException ("No Aquarea changes were supplied.", nameof (update));
			}
		}

	private static void PreserveAquareaBranches (AquareaControlRequest request, AquareaUpdate changes, AquareaStatus current)
		{
		if (current.UncontrollableGateway == true)
			{
			throw new ComfortCloudException ("The gateway reports that control is unavailable.");
			}

		if (current.Zones is null)
			{
			throw new ComfortCloudException ("Zone status is unavailable; refusing to replace unknown branches.");
			}

		if (current.Zones.Any (z => z.Id is null || z.Power is not (PowerState.Off or PowerState.On)))
			{
			throw new ComfortCloudException ("A zone has unknown identity or operation status.");
			}

		var zones = current.Zones.Select (z => new AquareaZoneUpdate
			{
			Id = z.Id,
			Power = z.Power,
			HeatSetpoint = z.HeatSetpoint,
			CoolSetpoint = z.CoolSetpoint
			}).ToList ();
		foreach (AquareaZoneUpdate change in changes.Zones ?? [])
			{
			AquareaZoneUpdate target = zones.SingleOrDefault (z => z.Id == change.Id) ?? throw new ArgumentException ("The changed zone does not exist.", nameof (changes));
			AquareaZone limits = current.Zones.Single (z => z.Id == change.Id);
			if (change.Power.HasValue)
				{
				target.Power = change.Power;
				}

			if (change.HeatSetpoint.HasValue)
				{
				target.HeatSetpoint = InRange (change.HeatSetpoint.Value, limits.HeatMinimum, limits.HeatMaximum);
				}

			if (change.CoolSetpoint.HasValue)
				{
				target.CoolSetpoint = InRange (change.CoolSetpoint.Value, limits.CoolMinimum, limits.CoolMaximum);
				}
			}

		AquareaTankUpdate? tank = null;
		if (current.Tank is not null)
			{
			if (current.Tank.Power is not (PowerState.Off or PowerState.On))
				{
				throw new ComfortCloudException ("Tank operation status is unavailable.");
				}

			tank = new ()
				{
				Power = changes.Tank?.Power ?? current.Tank.Power,
				HeatSetpoint = changes.Tank?.HeatSetpoint ?? current.Tank.HeatSetpoint
				};
			if (changes.Tank?.HeatSetpoint is double target)
				{
				tank.HeatSetpoint = InRange (target, current.Tank.HeatMinimum, current.Tank.HeatMaximum);
				}
			}
		else if (changes.Tank is not null)
			{
			throw new ArgumentException ("No tank was reported by this device.", nameof (changes));
			}

		if (changes.Power == PowerState.Off || changes.Mode == AquareaMode.Off)
			{
			foreach (AquareaZoneUpdate? zone in zones)
				{
				zone.Power = PowerState.Off;
				}

			tank?.Power = PowerState.Off;
			}

		if (changes.Special.HasValue)
			{
			if (!current.Special.HasValue)
				{
				throw new ComfortCloudException ("Current special status is unknown; refusing an unverified offset change.");
				}

			foreach (AquareaZoneUpdate? zone in zones)
				{
				AquareaZone old = current.Zones.Single (z => z.Id == zone.Id);
				zone.HeatSetpoint = Offset (zone.HeatSetpoint, old, current.Special.Value, changes.Special.Value, true);
				zone.CoolSetpoint = Offset (zone.CoolSetpoint, old, current.Special.Value, changes.Special.Value, false);
				}
			}

		bool on = zones.Any (z => z.Power == PowerState.On) || tank?.Power == PowerState.On;
		if (changes.Power == PowerState.On && !on)
			{
			throw new ArgumentException ("Enable a zone or tank explicitly when every branch is off.", nameof (changes));
			}

		if (request.Mode is null && on && (changes.Power == PowerState.On || changes.Tank?.Power == PowerState.On || changes.Zones?.Any (z => z.Power == PowerState.On) == true))
			{
			request.Mode = current.Mode switch
				{
					AquareaReportedMode.Heat => AquareaMode.Heat,
					AquareaReportedMode.Cool => AquareaMode.Cool,
					AquareaReportedMode.AutoHeat or AquareaReportedMode.AutoCool => AquareaMode.Auto,
					_ => throw new ComfortCloudException ("An active operating mode is required when enabling a branch; supply it explicitly when the reported mode is unavailable.")
					};
			}

		request.Zones = zones;
		request.Tank = tank;
		request.Power = on ? PowerState.On : PowerState.Off;
		}

	private static double InRange (double value, double? minimum, double? maximum) => double.IsNaN (value) || double.IsInfinity (value) || (minimum.HasValue && value < minimum) || (maximum.HasValue && value > maximum)
			? throw new ArgumentOutOfRangeException (nameof (value), "Setting is outside the advertised device range.")
			: value;

	private static double? Offset (double? value, AquareaZone zone, SpecialStatus old, SpecialStatus next, bool heating)
		{
		if (value is null || old == next)
			{
			return value;
			}

		double Modifier (SpecialStatus state) => state switch
			{
				SpecialStatus.None => 0,
				SpecialStatus.Eco => (heating ? zone.EcoHeat : zone.EcoCool) ?? throw new ComfortCloudException ("Required Eco offset is unavailable."),
				SpecialStatus.Comfort => (heating ? zone.ComfortHeat : zone.ComfortCool) ?? throw new ComfortCloudException ("Required Comfort offset is unavailable."),
				_ => throw new ComfortCloudException ("Unknown current special status.")
				};
		var result = value.Value - Modifier (old) + Modifier (next);
		var minimum = heating ? zone.HeatMinimum : zone.CoolMinimum;
		var maximum = heating ? zone.HeatMaximum : zone.CoolMaximum;
		return Math.Max (minimum ?? double.MinValue, Math.Min (maximum ?? double.MaxValue, result));
		}

	/// <summary>Fetches Aquarea energy and cost history through its dedicated proxy endpoint.</summary>
	/// <param name="gatewayId">The discovered Aquarea device GUID.</param>
	/// <param name="period">Day, month or year; AC history uses a different enum.</param>
	/// <param name="date">The date selecting the requested period.</param>
	/// <param name="utcOffset">The UTC offset sent as osTimezone, in whole minutes between minus and plus fourteen hours.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>The reported entries; absent history is an error, while an explicit empty list is preserved.</returns>
	public Task<IReadOnlyList<EnergyHistoryEntry>> GetAquareaEnergyAsync (string gatewayId, EnergyPeriod period, DateTime date, TimeSpan utcOffset, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (gatewayId);
		var timeZone = FormatUtcOffset (utcOffset);
		return !Enum.IsDefined (period)
			? throw new ArgumentOutOfRangeException (nameof (period))
			: WithGateAsync<IReadOnlyList<EnergyHistoryEntry>> (async token =>
		{
			HistoryEnvelope response = await ApiCoreAsync<HistoryEnvelope> (HttpMethod.Post, TRANSFER_PATH,
					new TransferRequest<HistoryRequest<EnergyPeriod>>
						{
						ApiName = "/remote/v1/api/consumption",
						Method = "POST",
						Body = new ()
							{
							GatewayId = gatewayId,
							Period = period,
							Date = date.ToString ("yyyyMMdd", CultureInfo.InvariantCulture),
							TimeZone = timeZone
							}
						}, token).ConfigureAwait (false);
			return response.Entries ?? throw new ComfortCloudException ("Energy response omitted historyDataList.");
		}, cancellationToken);
		}
	}