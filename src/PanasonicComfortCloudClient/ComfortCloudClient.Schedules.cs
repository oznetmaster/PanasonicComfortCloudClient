// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net.Http;
using System.Text.Json.Serialization;

using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient;

public sealed partial class ComfortCloudClient
	{
	/// <summary>Reads an Aquarea weekly schedule and the accompanying limits.</summary>
	/// <param name="gatewayId">The Aquarea gateway GUID.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>The typed complete schedule; unrecognized fields are retained for round trips.</returns>
	public Task<AquareaSchedule> GetAquareaScheduleAsync (string gatewayId, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (gatewayId);
		return WithGateAsync (token => GetAquareaScheduleCoreAsync (gatewayId, token), cancellationToken);
		}

	private async Task<AquareaSchedule> GetAquareaScheduleCoreAsync (string gatewayId, CancellationToken token)
		{
		AquareaSchedule response = await ApiCoreAsync<AquareaSchedule> (HttpMethod.Post, TRANSFER_PATH,
				new TransferRequest<object>
					{
					ApiName = "/remote/v1/api/data/weeklyTimer?gwid=" + Uri.EscapeDataString (gatewayId)
					}, token).ConfigureAwait (false);
		if (response.Timer is null)
			throw new ComfortCloudException ("Aquarea schedule response omitted weeklyTimer.");
		response.GatewayId = gatewayId;
		response.Timer.Days = InitializeReadDays (response.Timer.Days);
		return response;
		}

	/// <summary>Replaces an Aquarea weekly schedule without changing its explicit activation value.</summary>
	/// <param name="gatewayId">The Aquarea gateway GUID.</param>
	/// <param name="schedule">A complete seven-day schedule; obtain it from the same device before editing.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>A task completing on cloud acknowledgement; no retry is performed.</returns>
	public Task SetAquareaScheduleAsync (string gatewayId, AquareaSchedule schedule, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (gatewayId);
		if (schedule?.GatewayId is string original && original != gatewayId)
			throw new ArgumentException ("The schedule belongs to another gateway.", nameof (schedule));
		if (schedule?.Timer is null)
			{
			throw new ArgumentException ("A complete Aquarea timer is required.", nameof (schedule));
			}

		AquareaWeeklySettings copy = Clone (schedule.Timer);
		if (copy.Operation is not (0 or 1))
			{
			throw new ArgumentException ("Explicit schedule activation is required.", nameof (schedule));
			}

		NormalizeDays (copy.Days);
		return WithGateAsync (token => SendAquareaScheduleCoreAsync (gatewayId, copy, token), cancellationToken);
		}

	private async Task<bool> SendAquareaScheduleCoreAsync (string gatewayId, AquareaWeeklySettings copy, CancellationToken token)
		{
		_ = await ApiCoreAsync<ErrorEnvelope> (HttpMethod.Post, TRANSFER_PATH, new TransferRequest<AquareaTimerRequest>
			{
			ApiName = "/remote/v1/api/data/weeklyTimer",
			Method = "POST",
			Body = new ()
				{
				GatewayId = gatewayId,
				Timer = copy
				}
			}, token).ConfigureAwait (false);
		return true;
		}

	/// <summary>Reads the complete weekly schedule of an air conditioner.</summary>
	/// <param name="deviceGuid">The discovered device GUID.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>The device schedule, or null when the service explicitly returns no schedule.</returns>
	public Task<AcSchedule?> GetAcScheduleAsync (string deviceGuid, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (deviceGuid);
		return WithGateAsync (token => GetAcScheduleCoreAsync (deviceGuid, token), cancellationToken);
		}

	private async Task<AcSchedule?> GetAcScheduleCoreAsync (string deviceGuid, CancellationToken token)
		{
		AcTimerEnvelope response = await ApiCoreAsync<AcTimerEnvelope> (HttpMethod.Get, "weeklyTimer/" + RequestSigning.DevicePath (deviceGuid), null, token).ConfigureAwait (false);
		if (response.Schedules is null)
			{
			throw new ComfortCloudException ("Schedule response omitted devWeeklyTimerList.");
			}

		if (response.Schedules.Count == 0)
			{
			return null;
			}

		if (response.Schedules.Count == 1 && string.IsNullOrEmpty (response.Schedules[0].DeviceGuid))
			{
			response.Schedules[0].DeviceGuid = deviceGuid;
			}

		AcSchedule[] matching = [.. response.Schedules.Where (s => s.DeviceGuid == deviceGuid)];
		AcSchedule selected = matching.Length > 1
			? throw new ComfortCloudException ("Duplicate schedules for the requested device.")
			: matching.SingleOrDefault () ?? throw new ComfortCloudException ("The returned schedule did not match the requested device.");
		selected.Days = InitializeReadDays (selected.Days);
		return selected;
		}

	/// <summary>Replaces an air-conditioner weekly schedule.</summary>
	/// <param name="schedule">The complete seven-day schedule with explicit device GUID and activation.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>A task completing on cloud acknowledgement; unknown wire fields remain preserved.</returns>
	public Task SetAcScheduleAsync (AcSchedule schedule, CancellationToken cancellationToken = default)
		{
		if (schedule is null || string.IsNullOrWhiteSpace (schedule.DeviceGuid) || schedule.Enabled is null)
			{
			throw new ArgumentException ("Device GUID and explicit activation are required.", nameof (schedule));
			}

		AcSchedule copy = Clone (schedule);
		NormalizeDays (copy.Days);
		foreach (WeeklyTimerDay<AcTimerPattern> day in copy.Days!)
			{
			foreach (AcTimerPattern pattern in day.Patterns!)
				{
				if (!string.IsNullOrEmpty (pattern.StartTime) && pattern.IAuto is null or -255)
					{
					pattern.IAuto = 1;
					}
				}
			}

		return WithGateAsync (token => SendAcScheduleCoreAsync (copy, token), cancellationToken);
		}

	private async Task<bool> SendAcScheduleCoreAsync (AcSchedule copy, CancellationToken token)
		{
		_ = await ApiCoreAsync<ErrorEnvelope> (HttpMethod.Post, "weeklyTimer", copy, token).ConfigureAwait (false);
		return true;
		}

	private static void NormalizeDays<T> (List<WeeklyTimerDay<T>>? days) where T : TimerPattern, new()
		{
		if (days is null || days.Count != 7)
			{
			throw new ArgumentException ("A weekly schedule must contain all seven weekdays.", nameof (days));
			}

		foreach (WeeklyTimerDay<T>? day in days)
			{
			if (day is null || day.Patterns is null)
				{
				throw new ArgumentException ("Each weekday needs an explicit pattern list.", nameof (days));
				}

			if (day.Patterns.Count > 6 || day.Patterns.Any (p => p is null))
				{
				throw new ArgumentException ("Each weekday supports at most six non-null slots.", nameof (days));
				}

			var active = day.Patterns.Where (p => !string.IsNullOrEmpty (p.StartTime)).ToList ();
			foreach (T? pattern in active)
				{
				if (!DateTime.TryParseExact (pattern.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
					{
					throw new ArgumentException ("Pattern times must use 24-hour HH:mm.", nameof (days));
					}
				}

			if (active.Select (p => p.StartTime).Distinct ().Count () != active.Count)
				{
				throw new ArgumentException ("Duplicate pattern start time.", nameof (days));
				}

			day.Patterns = [.. active.OrderBy (p => p.StartTime, StringComparer.Ordinal)];
			while (day.Patterns.Count < 6)
				{
				day.Patterns.Add (new T ());
				}
			}
		}
	private static List<WeeklyTimerDay<T>> InitializeReadDays<T> (List<WeeklyTimerDay<T>>? days) where T : TimerPattern
		{
		days ??= [];
		if (days.Count > 7)
			throw new ComfortCloudException ("A schedule returned more than seven weekdays.");
		while (days.Count < 7)
			{
			days.Add (new ()
				{
				Patterns = []
				});
			}

		for (int day = 0; day < 7; day++)
			{
			days[day] ??= new ();
			days[day].Patterns ??= [];
			days[day].Weekday = (DayOfWeek)((day + 1) % 7);
			}

		return days;
		}

	private sealed class AquareaTimerRequest
		{
		[JsonPropertyName ("gwid")]
		public string GatewayId
			{
			get; set;
			} = "";
		[JsonPropertyName ("weeklyTimer")]
		public AquareaWeeklySettings? Timer
			{
			get; set;
			}
		}
	private sealed class AcTimerEnvelope
		{
		[JsonPropertyName ("devWeeklyTimerList")]
		public List<AcSchedule>? Schedules
			{
			get; set;
			}
		}
	}