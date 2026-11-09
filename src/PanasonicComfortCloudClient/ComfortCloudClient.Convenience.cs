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
	/// <summary>Gets the current explicitly selected official app version.</summary>
	/// <value>The protocol version advertised on subsequent requests.</value>
	public string AppVersion => Volatile.Read (ref _appVersion);
	/// <summary>Explicitly discovers and selects a current app version without replaying any cloud command.</summary>
	/// <param name="discovery">The application-owned version-discovery helper.</param>
	/// <param name="cancellationToken">Cancels discovery or waiting for active client requests.</param>
	/// <returns>The selected observation, which the application may persist.</returns>
	public async Task<AppVersionObservation> UpdateAppVersionAsync (AppVersionDiscovery discovery, CancellationToken cancellationToken = default)
		{
		ArgumentNullException.ThrowIfNull (discovery);
		return await WithGateAsync (async token =>
		{
			AppVersionObservation observation = await discovery.RefreshAsync (token).ConfigureAwait (false);
			_operations.Commit (() => Volatile.Write (ref _appVersion, observation.Version));
			return observation;
		}, cancellationToken).ConfigureAwait (false);
		}
	/// <summary>Discovers devices, optionally filtering by the modeled API family.</summary>
	/// <param name="family">The family to select, or null for all devices including unknown types.</param>
	/// <param name="cancellationToken">Cancels discovery.</param>
	/// <returns>The flattened current group listing.</returns>
	public async Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync (DeviceFamily? family = null, CancellationToken cancellationToken = default) =>
		[.. (await GetGroupsAsync (cancellationToken).ConfigureAwait (false)).SelectMany (g => g.Devices).Where (d => family is null || d.Family == family)];

	/// <summary>Fetches the requested date's AC daily total from monthly history.</summary>
	/// <param name="deviceGuid">The discovered device GUID.</param>
	/// <param name="date">The device-local calendar date; the caller selects today if wanted.</param>
	/// <param name="utcOffset">The UTC offset applicable to this device and period.</param>
	/// <param name="cancellationToken">Cancels the request.</param>
	/// <returns>The matching daily entry, or null when the cloud has no entry yet.</returns>
	public async Task<AcEnergyHistoryEntry?> GetAcDailyEnergyAsync (string deviceGuid, DateTime date, TimeSpan utcOffset, CancellationToken cancellationToken = default) =>
		(await GetAcEnergyAsync (deviceGuid, AcEnergyPeriod.Month, date, utcOffset, cancellationToken).ConfigureAwait (false)).FirstOrDefault (e => e.DateTime == date.ToString ("yyyyMMdd", CultureInfo.InvariantCulture));

	/// <summary>Fetches the requested date's HWS daily total from monthly history.</summary>
	/// <param name="deviceGuid">The discovered device GUID.</param>
	/// <param name="date">The device-local calendar date.</param>
	/// <param name="utcOffset">The UTC offset applicable to the device and period.</param>
	/// <param name="cancellationToken">Cancels the request.</param>
	/// <returns>The daily entry, or null when no matching entry is reported.</returns>
	public async Task<HotWaterEnergyHistoryEntry?> GetHotWaterDailyEnergyAsync (string deviceGuid, DateTime date, TimeSpan utcOffset, CancellationToken cancellationToken = default) =>
		(await GetHotWaterEnergyAsync (deviceGuid, EnergyPeriod.Month, date, utcOffset, cancellationToken).ConfigureAwait (false)).FirstOrDefault (e => e.DateTime == date.ToString ("yyyyMMdd", CultureInfo.InvariantCulture));

	/// <summary>Reads, edits only activation, and replaces an Aquarea weekly schedule.</summary>
	/// <param name="gatewayId">The gateway whose schedule is read and written.</param>
	/// <param name="enabled">The requested timer activation.</param>
	/// <param name="cancellationToken">Cancels reading or writing; uncertain writes are not retried.</param>
	/// <returns>The submitted schedule after cloud acknowledgement, without claiming a device readback.</returns>
	public async Task<AquareaSchedule> SetAquareaScheduleEnabledAsync (string gatewayId, bool enabled, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (gatewayId);
		return await WithGateAsync (async token =>
			{
				AquareaSchedule schedule = await GetAquareaScheduleCoreAsync (gatewayId, token).ConfigureAwait (false);
				schedule.Timer!.Operation = enabled ? 1 : 0;
				NormalizeDays (schedule.Timer.Days);
				_ = await SendAquareaScheduleCoreAsync (gatewayId, schedule.Timer, token).ConfigureAwait (false);
				return schedule;
			}, cancellationToken).ConfigureAwait (false);
		}

	/// <summary>Reads, edits only activation, and replaces an AC weekly schedule.</summary>
	/// <param name="deviceGuid">The device whose schedule is read and written.</param>
	/// <param name="enabled">The requested timer activation.</param>
	/// <param name="cancellationToken">Cancels reading or writing.</param>
	/// <returns>The submitted schedule after acknowledgement; an absent schedule is an error.</returns>
	public async Task<AcSchedule> SetAcScheduleEnabledAsync (string deviceGuid, bool enabled, CancellationToken cancellationToken = default)
		{
		_ = RequiredId (deviceGuid);
		return await WithGateAsync (async token =>
			{
				AcSchedule schedule = await GetAcScheduleCoreAsync (deviceGuid, token).ConfigureAwait (false) ?? throw new ComfortCloudException ("No AC schedule exists to activate.");
				schedule.Enabled = enabled;
				NormalizeDays (schedule.Days);
				foreach (AcTimerPattern pattern in schedule.Days!.SelectMany (day => day.Patterns!))
					{
					if (!string.IsNullOrEmpty (pattern.StartTime) && pattern.IAuto is null or -255)
						pattern.IAuto = 1;
					}

				_ = await SendAcScheduleCoreAsync (schedule, token).ConfigureAwait (false);
				return schedule;
			}, cancellationToken).ConfigureAwait (false);
		}

	/// <summary>Reads current agreement documents for one type.</summary>
	/// <param name="agreementType">The positive policy type to select.</param>
	/// <param name="includeContent">Whether to retrieve the document text.</param>
	/// <param name="language">The API language identifier.</param>
	/// <param name="cancellationToken">Cancels the read.</param>
	/// <returns>The filtered document list; nothing is accepted.</returns>
	public Task<IReadOnlyList<AgreementDocument>> GetAgreementDocumentsAsync (int agreementType, bool includeContent = false, int language = 0, CancellationToken cancellationToken = default)
		{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero (agreementType);
		return WithGateAsync<IReadOnlyList<AgreementDocument>> (async token =>
			{
				string path = "auth/v2/agreement/documents?language=" + language.ToString (CultureInfo.InvariantCulture) + "&type=" + agreementType.ToString (CultureInfo.InvariantCulture) + "&includeContent=" + (includeContent ? "1" : "0");
				AgreementDocumentEnvelope response = await ApiCoreAsync<AgreementDocumentEnvelope> (HttpMethod.Get, path, null, token).ConfigureAwait (false);
				return response.Agreements ?? throw new ComfortCloudException ("Document response omitted agreementList.");
			}, cancellationToken);
		}

	/// <summary>Reads accepted versions for one agreement type.</summary>
	/// <param name="agreementType">The positive policy type to select.</param>
	/// <param name="cancellationToken">Cancels the read.</param>
	/// <returns>The account's reported accepted versions.</returns>
	public Task<IReadOnlyList<AgreementAcceptance>> GetAgreementStatusAsync (int agreementType, CancellationToken cancellationToken = default)
		{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero (agreementType);
		return WithGateAsync<IReadOnlyList<AgreementAcceptance>> (async token =>
			{
				AgreementEnvelope response = await ApiCoreAsync<AgreementEnvelope> (HttpMethod.Get, "auth/v2/agreement/status?type=" + agreementType.ToString (CultureInfo.InvariantCulture), null, token).ConfigureAwait (false);
				return response.Agreements ?? throw new ComfortCloudException ("Agreement response omitted agreementList.");
			}, cancellationToken);
		}

	/// <summary>Finds outdated or unaccepted generally applicable policies.</summary>
	/// <param name="includeContent">Whether to include text for application presentation.</param>
	/// <param name="language">The policy language identifier.</param>
	/// <param name="cancellationToken">Cancels document/status reads.</param>
	/// <returns>Pending types one, two and four; the Turkey-only service policy is excluded as upstream specifies.</returns>
	public async Task<IReadOnlyList<AgreementDocument>> GetPendingAgreementsAsync (bool includeContent = false, int language = 0, CancellationToken cancellationToken = default) => await WithGateAsync (token => GetPendingAgreementsCoreAsync (includeContent, language, token), cancellationToken).ConfigureAwait (false);
	private async Task<IReadOnlyList<AgreementDocument>> GetPendingAgreementsCoreAsync (bool includeContent, int language, CancellationToken token)
		{
		IReadOnlyList<AgreementDocument> documents = await GetAgreementDocumentsCoreAsync (includeContent, language, token).ConfigureAwait (false);
		IReadOnlyList<AgreementAcceptance> accepted = await GetAgreementStatusCoreAsync (token).ConfigureAwait (false);
		return [.. documents.Where (d => d.Type is 1 or 2 or 4 && d.Version is not null && !accepted.Any (a => a.Type == d.Type && a.Version == d.Version))];
		}

	/// <summary>Explicitly accepts all currently pending generally applicable policies, matching the upstream convenience operation.</summary>
	/// <param name="language">The policy language identifier.</param>
	/// <param name="cancellationToken">Cancels the reads or acceptance request.</param>
	/// <returns>A task completing when no applicable policies remain to submit or the exact reported versions are acknowledged.</returns>
	/// <remarks>This is an account-changing operation. Call only after the account owner has authorized acceptance. Login never invokes it automatically.</remarks>
	public async Task EnsureAgreementsAcceptedAsync (int language = 0, CancellationToken cancellationToken = default) => _ = await WithGateAsync (async token =>
																																										{
																																											IReadOnlyList<AgreementDocument> pending = await GetPendingAgreementsCoreAsync (false, language, token).ConfigureAwait (false);
																																											if (pending.Count != 0)
																																												_ = await AcceptAgreementsCoreAsync ([.. pending.Select (d => new AgreementAcceptance { Type = d.Type, Version = d.Version })], token).ConfigureAwait (false);
																																											return true;
																																										}, cancellationToken).ConfigureAwait (false);

	/// <summary>Calls the retained legacy v1 agreement-status route.</summary>
	/// <param name="agreementType">The positive policy type.</param>
	/// <param name="cancellationToken">Cancels the request.</param>
	/// <returns>The reported status, or null when absent. Upstream reports that the retired route normally returns 403.</returns>
	public Task<int?> GetLegacyAgreementStatusAsync (int agreementType, CancellationToken cancellationToken = default)
		{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero (agreementType);
		return WithGateAsync<int?> (async token => (await ApiCoreAsync<LegacyAgreement> (HttpMethod.Get, "auth/agreement/status/" + agreementType.ToString (CultureInfo.InvariantCulture), null, token).ConfigureAwait (false)).Status, cancellationToken);
		}

	/// <summary>Explicitly calls the retired v1 agreement-acceptance route.</summary>
	/// <param name="agreementType">The positive policy type the account owner elected to accept.</param>
	/// <param name="cancellationToken">Cancels the request.</param>
	/// <returns>A task completing on acknowledgement; route errors are propagated without fallback to v2.</returns>
	public async Task AcceptLegacyAgreementAsync (int agreementType, CancellationToken cancellationToken = default)
		{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero (agreementType);
		_ = await WithGateAsync (token => ApiCoreAsync<ErrorEnvelope> (HttpMethod.Put, "auth/agreement/status/", new LegacyAgreement { Type = agreementType, Status = 1 }, token), cancellationToken).ConfigureAwait (false);
		}

	private sealed class LegacyAgreement
		{
		[JsonPropertyName ("type")]
		public int? Type
			{
			get; init;
			}
		[JsonPropertyName ("agreementStatus")]
		public int? Status
			{
			get; init;
			}
		}
	}