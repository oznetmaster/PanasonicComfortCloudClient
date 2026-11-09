// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient;

/// <summary>Tracks typed observations explicitly for an application, replacing the upstream mutable-device load/change helpers.</summary>
/// <typeparam name="TSnapshot">The modeled state or energy type.</typeparam>
/// <param name="device">The device whose state this object owns.</param>
/// <remarks>Updates and individual property reads are synchronized. Current returns an independent coherent snapshot. No background work, disk storage or events are created. Use one tracker for one device and one response family; do not concurrently mutate caller-owned input models during an update.</remarks>
public sealed class DeviceObservation<TSnapshot> (DeviceInfo device) where TSnapshot : class
	{
	private static readonly JsonSerializerOptions _json = ComfortCloudJson.CreateOptions ();
#pragma warning disable IDE0330 // Monitor-based locking is shared with net472 and processor Mono.
	private readonly object _sync = new ();
#pragma warning restore IDE0330
	private byte[]? _previous;

	/// <summary>Gets the device description.</summary>
	/// <value>The explicitly supplied discovery model.</value>
	public DeviceInfo Device { get; } = device ?? throw new ArgumentNullException (nameof (device));
	/// <summary>Gets an independent copy of the latest accepted snapshot.</summary>
	/// <value>The typed state, or null before the first observation.</value>
	public TSnapshot? Current
		{
		get
			{
			lock (_sync)
				return field is null ? null : Copy (field);
			}
		private set;
		}
	/// <summary>Gets the latest observation time.</summary>
	/// <value>The supplied UTC receipt instant, or null before an observation.</value>
	public DateTimeOffset? ObservedAt
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}
	/// <summary>Gets when the modeled snapshot most recently changed.</summary>
	/// <value>The first observation or latest changed-state instant, independent of polling frequency.</value>
	public DateTimeOffset? LastChanged
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}
	/// <summary>Gets whether the most recent update changed the modeled state.</summary>
	/// <value>False before the first update, then the result of the latest update.</value>
	public bool HasChanged
		{
		get
			{
			lock (_sync)
				return field;
			}
		private set;
		}
	/// <summary>Loads a typed snapshot without aliasing the caller's mutable object.</summary>
	/// <param name="snapshot">The newly read state.</param>
	/// <param name="observedAt">Its receipt instant.</param>
	/// <returns>True on the first observation or a changed serialized state.</returns>
	public bool Update (TSnapshot snapshot, DateTimeOffset observedAt)
		{
		lock (_sync)
			return UpdateCore (snapshot, observedAt);
		}
	private bool UpdateCore (TSnapshot snapshot, DateTimeOffset observedAt)
		{
		ArgumentNullException.ThrowIfNull (snapshot);
		if (ObservedAt is DateTimeOffset previous && observedAt < previous)
			throw new ArgumentOutOfRangeException (nameof (observedAt), "Observations cannot move backward in time.");
		TSnapshot comparison = Copy (snapshot);
		if (comparison is AcSnapshot ac)
			ac.Timestamp = null;
		byte[] bytes = JsonSerializer.SerializeToUtf8Bytes (comparison, _json);
		bool changed = _previous is null || !_previous.AsSpan ().SequenceEqual (bytes);
		Current = Copy (snapshot);
		_previous = bytes;
		ObservedAt = observedAt;
		HasChanged = changed;
		if (changed)
			LastChanged = observedAt;
		return changed;
		}
	private static TSnapshot Copy (TSnapshot value)
		{
		TSnapshot copy = JsonSerializer.Deserialize<TSnapshot> (JsonSerializer.SerializeToUtf8Bytes (value, _json), _json)!;
		if (value is AquareaSnapshot original && copy is AquareaSnapshot snapshot)
			{
			snapshot.Source = original.Source;
			snapshot.ObservedAt = original.ObservedAt;
			}

		return copy;
		}
	}

public sealed partial class ComfortCloudClient
	{
	/// <summary>Refreshes an application-owned AC observation and reports whether its state changed.</summary>
	/// <param name="device">The observation associated with the intended device.</param>
	/// <param name="source">Live or explicitly cached data.</param>
	/// <param name="cancellationToken">Cancels the read.</param>
	/// <returns>True when the accepted snapshot changed.</returns>
	public async Task<bool> RefreshAsync (DeviceObservation<AcSnapshot> device, StatusSource source = StatusSource.Live, CancellationToken cancellationToken = default)
		{
		ArgumentNullException.ThrowIfNull (device);
		return !Enum.IsDefined (source)
			? throw new ArgumentOutOfRangeException (nameof (source))
			: await WithGateAsync (async token =>
			{
				AcSnapshot snapshot = await GetAcCoreAsync (ObservationId (device.Device), source, token).ConfigureAwait (false);
				return _operations.Commit (() => device.Update (snapshot, DateTimeOffset.UtcNow));
			}, cancellationToken).ConfigureAwait (false);
		}
	/// <summary>Refreshes an application-owned Aquarea observation.</summary>
	/// <param name="device">The observation of the intended gateway.</param>
	/// <param name="source">Live or explicitly cached data.</param>
	/// <param name="cancellationToken">Cancels the read.</param>
	/// <returns>True when the accepted snapshot changed.</returns>
	public async Task<bool> RefreshAsync (DeviceObservation<AquareaSnapshot> device, StatusSource source = StatusSource.Live, CancellationToken cancellationToken = default)
		{
		ArgumentNullException.ThrowIfNull (device);
		return !Enum.IsDefined (source)
			? throw new ArgumentOutOfRangeException (nameof (source))
			: await WithGateAsync (async token =>
			{
				AquareaSnapshot snapshot = await GetAquareaCoreAsync (ObservationId (device.Device), source, token).ConfigureAwait (false);
				return _operations.Commit (() => device.Update (snapshot, DateTimeOffset.UtcNow));
			}, cancellationToken).ConfigureAwait (false);
		}
	/// <summary>Refreshes an application-owned HWS observation.</summary>
	/// <param name="device">The observation of the intended tank.</param>
	/// <param name="cancellationToken">Cancels the live read; no cached HWS route is modeled upstream.</param>
	/// <returns>True when the accepted snapshot changed.</returns>
	public async Task<bool> RefreshAsync (DeviceObservation<HotWaterSnapshot> device, CancellationToken cancellationToken = default)
		{
		ArgumentNullException.ThrowIfNull (device);
		return await WithGateAsync (async token =>
			{
				HotWaterSnapshot snapshot = await GetHotWaterCoreAsync (ObservationId (device.Device), token).ConfigureAwait (false);
				return _operations.Commit (() => device.Update (snapshot, DateTimeOffset.UtcNow));
			}, cancellationToken).ConfigureAwait (false);
		}
	private static string ObservationId (DeviceInfo device) => device.DeviceGuid ?? throw new ArgumentException ("The observation's device has no GUID.", nameof (device));
	}