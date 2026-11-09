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
	/// <summary>Fetches air-conditioner state without silently falling back to cached data.</summary>
	/// <param name="deviceGuid">The discovered device GUID.</param>
	/// <param name="source">Live or explicitly cached status.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>The typed device status; missing parameters are an error.</returns>
	public Task<AcSnapshot> GetAcStatusAsync (string deviceGuid, StatusSource source = StatusSource.Live, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (deviceGuid);
		return !Enum.IsDefined (source)
			? throw new ArgumentOutOfRangeException (nameof (source))
			: WithGateAsync (token => GetAcCoreAsync (deviceGuid, source, token), cancellationToken);
		}

	private async Task<AcSnapshot> GetAcCoreAsync (string deviceGuid, StatusSource source, CancellationToken token)
		{
		var path = (source == StatusSource.Live ? "deviceStatus/" : "deviceStatus/now/") + RequestSigning.DevicePath (deviceGuid);
		AcSnapshot response = await ApiCoreAsync<AcSnapshot> (HttpMethod.Get, path, null, token).ConfigureAwait (false);
		return response.Parameters is null ? throw new ComfortCloudException ("AC response omitted parameters.") : response;
		}

	/// <summary>Applies only explicitly supplied AC settings without implicitly powering the device on.</summary>
	/// <param name="deviceGuid">The discovered device GUID.</param>
	/// <param name="update">The intended settings. Automatic swing requests preserve the other axis from a fresh read.</param>
	/// <param name="cancellationToken">Cancels waiting, any required read or the control request.</param>
	/// <returns>A task completing on acknowledgement; physical state must be read separately.</returns>
	public Task UpdateAcAsync (string deviceGuid, AcUpdate update, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (deviceGuid);
		ArgumentNullException.ThrowIfNull (update);

		AcUpdate copy = Clone (update);
		return copy.Power.HasValue && copy.Power is not (PowerState.On or PowerState.Off)
			? throw new ArgumentException ("Invalid requested power state.", nameof (update))
			: copy.Mode.HasValue && !Enum.IsDefined (copy.Mode.Value)
			? throw new ArgumentException ("Unknown requested AC mode.", nameof (update))
			: copy.Fan.HasValue && !Enum.IsDefined (copy.Fan.Value)
			? throw new ArgumentException ("Unknown requested fan speed.", nameof (update))
			: (Task)WithGateAsync (async token =>
		{
			if ((copy.HorizontalSwing.HasValue || copy.VerticalSwing.HasValue) && copy.FanAutoMode is null)
				{
				AcSnapshot current = await GetAcCoreAsync (deviceGuid, StatusSource.Live, token).ConfigureAwait (false);
				if (current.Parameters!.FanAutoMode is not (0 or 1 or 2 or 3))
					{
					throw new ComfortCloudException ("Current swing auto mode is unknown.");
					}

				bool horizontal = current.Parameters.FanAutoMode is 0 or 3;
				bool vertical = current.Parameters.FanAutoMode is 0 or 2;
				if (copy.HorizontalSwing.HasValue)
					{
					horizontal = copy.HorizontalSwing == -1;
					}

				if (copy.VerticalSwing.HasValue)
					{
					vertical = copy.VerticalSwing == -1;
					}

				if (horizontal)
					{
					copy.HorizontalSwing = null;
					}

				if (vertical)
					{
					copy.VerticalSwing = null;
					}

				copy.FanAutoMode = horizontal ? vertical ? 0 : 3 : vertical ? 2 : 1;
				}

			_ = await ApiCoreAsync<ErrorEnvelope> (HttpMethod.Post, "deviceStatus/control", new DeviceControlRequest<AcUpdate> { DeviceGuid = deviceGuid, Parameters = copy }, token).ConfigureAwait (false);
			return true;
		}, cancellationToken);
		}

	/// <summary>Fetches air-conditioner energy history using AC-specific period values.</summary>
	/// <param name="deviceGuid">The discovered device GUID.</param>
	/// <param name="period">The AC day/week/month/year period.</param>
	/// <param name="date">The date selecting the period.</param>
	/// <param name="utcOffset">The UTC offset sent as osTimezone, in whole minutes between minus and plus fourteen hours.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>The reported history entries.</returns>
	public Task<IReadOnlyList<AcEnergyHistoryEntry>> GetAcEnergyAsync (string deviceGuid, AcEnergyPeriod period, DateTime date, TimeSpan utcOffset, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (deviceGuid);
		var timeZone = FormatUtcOffset (utcOffset);
		return !Enum.IsDefined (period)
			? throw new ArgumentOutOfRangeException (nameof (period))
			: WithGateAsync<IReadOnlyList<AcEnergyHistoryEntry>> (async token =>
		{
			HistoryEnvelope<AcEnergyHistoryEntry> response = await ApiCoreAsync<HistoryEnvelope<AcEnergyHistoryEntry>> (HttpMethod.Post, "deviceHistoryData", new HistoryRequest<AcEnergyPeriod>
				{
				DeviceGuid = deviceGuid,
				Period = period,
				Date = date.ToString ("yyyyMMdd", CultureInfo.InvariantCulture),
				TimeZone = timeZone
				}, token).ConfigureAwait (false);
			return response.Entries ?? throw new ComfortCloudException ("History response omitted historyDataList.");
		}, cancellationToken);
		}

	/// <summary>Fetches standalone heat-pump hot-water status from its dedicated endpoint.</summary>
	/// <param name="deviceGuid">The discovered HWS device GUID.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>The typed HWS status; AC endpoints are not used for HWS devices.</returns>
	public Task<HotWaterSnapshot> GetHotWaterStatusAsync (string deviceGuid, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (deviceGuid);
		return WithGateAsync (token => GetHotWaterCoreAsync (deviceGuid, token), cancellationToken);
		}

	private async Task<HotWaterSnapshot> GetHotWaterCoreAsync (string deviceGuid, CancellationToken token)
		{
		HotWaterSnapshot response = await ApiCoreAsync<HotWaterSnapshot> (HttpMethod.Get, "hphw/deviceStatus?deviceGuid=" + RequestSigning.DevicePath (deviceGuid) + "&fromDevice=1", null, token).ConfigureAwait (false);
		return response.Parameters is null ? throw new ComfortCloudException ("HWS response omitted parameters.") : response;
		}

	/// <summary>Applies explicit standalone HWS changes through the hphw control endpoint.</summary>
	/// <param name="deviceGuid">The discovered HWS device GUID.</param>
	/// <param name="update">The explicit boost, operation mode or complete weekly schedule.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>A task completing on cloud acknowledgement; no command is retried.</returns>
	public Task UpdateHotWaterAsync (string deviceGuid, HotWaterUpdate update, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (deviceGuid);
		ArgumentNullException.ThrowIfNull (update);

		HotWaterUpdate copy = Clone (update);
		return (copy.Mode.HasValue && !Enum.IsDefined (copy.Mode.Value)) || (copy.Boost.HasValue && copy.Boost is not (PowerState.Off or PowerState.On))
			? throw new ArgumentException ("Invalid HWS control mode.", nameof (update))
			: copy.WeeklySettings is not null && (copy.WeeklySettings.Count != 7 || !copy.WeeklySettings.Select (d => d.Weekday).OrderBy (d => d).SequenceEqual (Enumerable.Range (1, 7).Select (i => (int?)i)) || copy.WeeklySettings.Any (d => d.Mode is not (0 or 1)))
			? throw new ArgumentException ("HWS weekly settings must include each weekday and an explicit daily mode.", nameof (update))
			: (Task)WithGateAsync (async token =>
		{
			_ = await ApiCoreAsync<ErrorEnvelope> (HttpMethod.Post, "hphw/deviceStatus/control", new DeviceControlRequest<HotWaterUpdate>
				{
				DeviceGuid = deviceGuid,
				Parameters = copy
				}, token).ConfigureAwait (false);
			return true;
		}, cancellationToken);
		}

	/// <summary>Fetches standalone HWS energy history.</summary>
	/// <param name="deviceGuid">The discovered HWS device GUID.</param>
	/// <param name="period">The HWS day/month/year period.</param>
	/// <param name="date">The date selecting the period.</param>
	/// <param name="utcOffset">The UTC offset sent as osTimezone, in whole minutes between minus and plus fourteen hours.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>The reported HWS history entries.</returns>
	public Task<IReadOnlyList<HotWaterEnergyHistoryEntry>> GetHotWaterEnergyAsync (string deviceGuid, EnergyPeriod period, DateTime date, TimeSpan utcOffset, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (deviceGuid);
		var timeZone = FormatUtcOffset (utcOffset);
		return !Enum.IsDefined (period)
			? throw new ArgumentOutOfRangeException (nameof (period))
			: WithGateAsync<IReadOnlyList<HotWaterEnergyHistoryEntry>> (async token =>
		{
			HistoryEnvelope<HotWaterEnergyHistoryEntry> response = await ApiCoreAsync<HistoryEnvelope<HotWaterEnergyHistoryEntry>> (HttpMethod.Post, "hphw/deviceHistoryData", new HistoryRequest<EnergyPeriod>
				{
				DeviceGuid = deviceGuid,
				Period = period,
				Date = date.ToString ("yyyyMMdd", CultureInfo.InvariantCulture),
				TimeZone = timeZone
				}, token).ConfigureAwait (false);
			return response.Entries ?? throw new ComfortCloudException ("History response omitted historyDataList.");
		}, cancellationToken);
		}

	/// <summary>Reads Panasonic policy documents without accepting them.</summary>
	/// <param name="includeContent">Whether to include the policy text.</param>
	/// <param name="language">The API language identifier; zero is the default language.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>The policy documents reported by the v2 agreement endpoint.</returns>
	public Task<IReadOnlyList<AgreementDocument>> GetAgreementDocumentsAsync (bool includeContent = false, int language = 0, CancellationToken cancellationToken = default) => WithGateAsync (token => GetAgreementDocumentsCoreAsync (includeContent, language, token), cancellationToken);
	private async Task<IReadOnlyList<AgreementDocument>> GetAgreementDocumentsCoreAsync (bool includeContent, int language, CancellationToken token)
		{
		AgreementDocumentEnvelope response = await ApiCoreAsync<AgreementDocumentEnvelope> (HttpMethod.Get, "auth/v2/agreement/documents?language=" + language.ToString (CultureInfo.InvariantCulture) + "&includeContent=" + (includeContent ? "1" : "0"), null, token).ConfigureAwait (false);
		return response.Agreements ?? throw new ComfortCloudException ("Document response omitted agreementList.");
		}

	/// <summary>Reads the policy versions already accepted by the account.</summary>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>The explicitly accepted type/version pairs.</returns>
	public Task<IReadOnlyList<AgreementAcceptance>> GetAgreementStatusAsync (CancellationToken cancellationToken = default) => WithGateAsync (GetAgreementStatusCoreAsync, cancellationToken);
	private async Task<IReadOnlyList<AgreementAcceptance>> GetAgreementStatusCoreAsync (CancellationToken token)
		{
		AgreementEnvelope response = await ApiCoreAsync<AgreementEnvelope> (HttpMethod.Get, "auth/v2/agreement/status", null, token).ConfigureAwait (false);
		return response.Agreements ?? throw new ComfortCloudException ("Agreement response omitted agreementList.");
		}

	/// <summary>Accepts only the exact policy versions explicitly supplied by the application.</summary>
	/// <param name="agreements">The type/version pairs reviewed and accepted by the account owner.</param>
	/// <param name="cancellationToken">Cancels waiting or the request.</param>
	/// <returns>A task completing when the cloud acknowledges acceptance.</returns>
	/// <remarks>No policy is accepted automatically during authentication or other API operations.</remarks>
	public Task AcceptAgreementsAsync (IReadOnlyList<AgreementAcceptance> agreements, CancellationToken cancellationToken = default)
		{
		if (agreements is null || agreements.Count == 0 || agreements.Any (a => a.Type is not > 0 || string.IsNullOrWhiteSpace (a.Version)))
			{
			throw new ArgumentException ("Explicit policy type/version pairs are required.", nameof (agreements));
			}

		AgreementAcceptance[] copy = [.. agreements.Select (Clone)];
		return WithGateAsync (token => AcceptAgreementsCoreAsync (copy, token), cancellationToken);
		}
	private async Task<bool> AcceptAgreementsCoreAsync (AgreementAcceptance[] copy, CancellationToken token)
		{
		_ = await ApiCoreAsync<ErrorEnvelope> (HttpMethod.Put, "auth/v2/agreement/status", new AgreementRequest
			{
			Agreements = copy
			}, token).ConfigureAwait (false);
		return true;
		}

	private sealed class AgreementDocumentEnvelope
		{
		[JsonPropertyName ("agreementList")]
		public List<AgreementDocument>? Agreements
			{
			get; set;
			}
		}
	}