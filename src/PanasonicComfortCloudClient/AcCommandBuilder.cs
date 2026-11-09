// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

using PanasonicComfortCloudClient.Concurrency;
using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient;

/// <summary>Builds the upstream AC convenience commands, including explicit optional automatic power-on.</summary>
/// <remarks>This is a mutable application-owned builder. Synchronize concurrent edits externally, as with the typed request models.</remarks>
/// <param name="snapshot">A recent snapshot of the device to control.</param>
/// <param name="autoPowerOn">Whether preference setters should add power-on, as upstream defaults; mode and iAUTO-X always request operation.</param>
public sealed class AcCommandBuilder (AcSnapshot snapshot, bool autoPowerOn = true)
	{
	private static readonly JsonSerializerOptions _json = ComfortCloudJson.CreateOptions ();
	private readonly AcSnapshot _snapshot = snapshot ?? throw new ArgumentNullException (nameof (snapshot));
	private readonly AcUpdate _request = new ();
	private readonly bool _autoPowerOn = autoPowerOn;

	/// <summary>Gets whether any command has been requested.</summary>
	/// <value>True after at least one setter has been called.</value>
	public bool HasChanges
		{
		get; private set;
		}
	/// <summary>Creates an independent typed copy of the intended changes.</summary>
	/// <returns>The accumulated update; editing it does not change this builder.</returns>
	public AcUpdate Build () => Copy (_request);
	/// <summary>Sets explicit power operation.</summary>
	/// <param name="value">Off or on.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetPower (PowerState value)
		{
		_request.Power = value;
		HasChanges = true;
		return this;
		}
	/// <summary>Selects an operating mode and explicitly requests power-on.</summary>
	/// <param name="value">The operating mode.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetMode (AcMode value)
		{
		PowerOn (true);
		_request.Mode = value;
		HasChanges = true;
		return this;
		}
	/// <summary>Sets the target temperature, optionally requesting power-on.</summary>
	/// <param name="value">The temperature in the device's unit.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetTargetTemperature (double value)
		{
		PowerOn ();
		_request.TargetTemperature = value;
		HasChanges = true;
		return this;
		}
	/// <summary>Selects fan speed without implicitly adding power-on.</summary>
	/// <param name="value">The desired speed.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetFanSpeed (FanSpeed value)
		{
		_request.Fan = value;
		HasChanges = true;
		return this;
		}
	/// <summary>Selects economy/output mode, optionally requesting power-on.</summary>
	/// <param name="value">Normal, powerful or quiet.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetEcoMode (EcoMode value)
		{
		PowerOn ();
		_request.Eco = value;
		HasChanges = true;
		return this;
		}
	/// <summary>Selects horizontal swing while preserving the other axis.</summary>
	/// <param name="value">The upstream position value, or minus one for automatic.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetHorizontalSwing (int value)
		{
		Swing (value, true);
		return this;
		}
	/// <summary>Selects vertical swing while preserving the other axis.</summary>
	/// <param name="value">The upstream position value, or minus one for automatic.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetVerticalSwing (int value)
		{
		Swing (value, false);
		return this;
		}
	/// <summary>Sets nanoe without implicitly adding power-on.</summary>
	/// <param name="value">The upstream nanoe mode.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetNanoe (int value)
		{
		_request.Nanoe = value;
		HasChanges = true;
		return this;
		}
	/// <summary>Sets ECONAVI without implicitly adding power-on.</summary>
	/// <param name="value">The upstream ECONAVI mode.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetEcoNavi (int value)
		{
		_request.EcoNavi = value;
		HasChanges = true;
		return this;
		}
	/// <summary>Sets AI ECO without implicitly adding power-on.</summary>
	/// <param name="value">The upstream AI ECO mode.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetEcoFunction (int value)
		{
		_request.EcoFunction = value;
		HasChanges = true;
		return this;
		}
	/// <summary>Sets inside cleaning without implicitly adding power-on.</summary>
	/// <param name="value">The upstream inside-cleaning value.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetInsideCleaning (int value)
		{
		_request.InsideCleaning = value;
		HasChanges = true;
		return this;
		}
	/// <summary>Sets iAUTO-X, selecting Auto when enabled and Cool otherwise, and explicitly requests operation.</summary>
	/// <param name="value">The upstream iAUTO-X value, two for on and one for off.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetIAutoX (int value)
		{
		_ = SetMode (value == 2 ? AcMode.Auto : AcMode.Cool);
		_request.IAutoX = value;
		return this;
		}
	/// <summary>Sets one zone's operation while retaining other changes queued in this builder.</summary>
	/// <param name="zoneId">The reported zone identifier.</param>
	/// <param name="value">The requested zone operation.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetZonePower (int zoneId, PowerState value)
		{
		Zone (zoneId).Power = value;
		HasChanges = true;
		return this;
		}
	/// <summary>Sets one zone's damper level.</summary>
	/// <param name="zoneId">The reported zone identifier.</param>
	/// <param name="value">The requested damper level.</param>
	/// <returns>This builder.</returns>
	public AcCommandBuilder SetZoneDamper (int zoneId, int value)
		{
		Zone (zoneId).DamperLevel = value;
		HasChanges = true;
		return this;
		}
	private AcZone Zone (int id)
		{
		_request.Zones ??= [];
		AcZone? existing = _request.Zones.SingleOrDefault (z => z.Id == id);
		if (existing is not null)
			return existing;
		var zone = new AcZone { Id = id };
		_request.Zones.Add (zone);
		return zone;
		}
	private void PowerOn (bool force = false)
		{
		if ((force || _autoPowerOn) && _snapshot.Parameters?.Power != PowerState.On)
			_request.Power = PowerState.On;
		}
	private void Swing (int value, bool horizontal)
		{
		int? previous = _request.FanAutoMode ?? _snapshot.Parameters?.FanAutoMode;
		if (previous is not (0 or 1 or 2 or 3))
			throw new ArgumentException ("The current automatic swing setting must be known.");
		bool x = previous is 0 or 3, y = previous is 0 or 2;
		if (horizontal)
			{
			x = value == -1;
			_request.HorizontalSwing = x ? null : value;
			}
		else
			{
			y = value == -1;
			_request.VerticalSwing = y ? null : value;
			}

		_request.FanAutoMode = x ? y ? 0 : 3 : y ? 2 : 1;
		PowerOn ();
		HasChanges = true;
		}
	internal static AcUpdate Copy (AcUpdate value) => JsonSerializer.Deserialize<AcUpdate> (JsonSerializer.Serialize (value, _json), _json)!;
	internal static AcUpdate Merge (AcUpdate first, AcUpdate last)
		{
		AcUpdate merged = Copy (new AcUpdate
			{
			Power = last.Power ?? first.Power,
			Mode = last.Mode ?? first.Mode,
			TargetTemperature = last.TargetTemperature ?? first.TargetTemperature,
			Fan = last.Fan ?? first.Fan,
			Eco = last.Eco ?? first.Eco,
			HorizontalSwing = last.HorizontalSwing ?? first.HorizontalSwing,
			VerticalSwing = last.VerticalSwing ?? first.VerticalSwing,
			FanAutoMode = last.FanAutoMode ?? first.FanAutoMode,
			Nanoe = last.Nanoe ?? first.Nanoe,
			EcoNavi = last.EcoNavi ?? first.EcoNavi,
			EcoFunction = last.EcoFunction ?? first.EcoFunction,
			InsideCleaning = last.InsideCleaning ?? first.InsideCleaning,
			IAutoX = last.IAutoX ?? first.IAutoX,
			Zones = last.Zones ?? first.Zones
			});
		if (first.ExtensionData is not null)
			merged.ExtensionData = first.ExtensionData.ToDictionary (entry => entry.Key, entry => entry.Value.Clone (), StringComparer.Ordinal);
		if (last.ExtensionData is not null)
			{
			merged.ExtensionData ??= new (StringComparer.Ordinal);
			foreach (KeyValuePair<string, JsonElement> entry in last.ExtensionData)
				merged.ExtensionData[entry.Key] = entry.Value.Clone ();
			}

		return merged;
		}
	}

/// <summary>Owns upstream-style preference buffering for one AC when automatic power-on is disabled.</summary>
/// <param name="client">The application-owned cloud client.</param>
/// <param name="deviceGuid">The AC whose queued settings this controller owns.</param>
/// <param name="autoPowerOn">Whether to use immediate upstream-style command submission; false buffers preferences while off.</param>
public sealed class AcController (ComfortCloudClient client, string deviceGuid, bool autoPowerOn = true) : IDisposable
	{
	private readonly ComfortCloudClient _client = client ?? throw new ArgumentNullException (nameof (client));
	private readonly string _deviceGuid = !string.IsNullOrWhiteSpace (deviceGuid) ? deviceGuid : throw new ArgumentException ("A device GUID is required.", nameof (deviceGuid));
	private readonly bool _autoPowerOn = autoPowerOn;
	private readonly AsyncOperationGate _operations = new (typeof (AcController));
	private AcUpdate? _pending;
	/// <summary>Gets whether this controller retains preferences for the next explicit power-on.</summary>
	/// <value>True when preferences are buffered; no files or automatic writes are created.</value>
	public bool HasPendingChanges => Volatile.Read (ref _pending) is not null;
	/// <summary>Creates a builder with this controller's power policy.</summary>
	/// <param name="snapshot">The recent device snapshot used for swing and power decisions.</param>
	/// <returns>A new independent command builder.</returns>
	public AcCommandBuilder CreateChangeRequest (AcSnapshot snapshot) => new (snapshot, _autoPowerOn);
	/// <summary>Applies or buffers changes based on the explicitly supplied current device state.</summary>
	/// <param name="snapshot">The current snapshot of this controller's device.</param>
	/// <param name="changes">The intended settings, including explicit power-on when buffered settings should be submitted.</param>
	/// <param name="cancellationToken">Cancels waiting or submission.</param>
	/// <returns>True if a command was acknowledged; false if it was buffered.</returns>
	/// <remarks>Queued preferences are consumed before a write attempt, so a lost acknowledgement never causes them to be replayed automatically.</remarks>
	public async Task<bool> ApplyAsync (AcSnapshot snapshot, AcUpdate changes, CancellationToken cancellationToken = default)
		{
		ArgumentNullException.ThrowIfNull (snapshot);
		ArgumentNullException.ThrowIfNull (changes);
		using AsyncOperationGate.Operation operation = await _operations.EnterAsync (cancellationToken).ConfigureAwait (false);
		AcUpdate request = AcCommandBuilder.Copy (changes);
		bool buffered = _operations.Commit (() =>
			{
				AcUpdate? pending = _pending;
				if (request.Power == PowerState.On)
					{
					if (pending is not null)
						request = AcCommandBuilder.Merge (pending, request);
					Volatile.Write (ref _pending, null);
					}
				else if (!_autoPowerOn && snapshot.Parameters?.Power != PowerState.On)
					{
					Volatile.Write (ref _pending, pending is null ? request : AcCommandBuilder.Merge (pending, request));
					return true;
					}

				return false;
			});
		operation.Token.ThrowIfCancellationRequested ();
		if (buffered)
			return false;
		await _client.UpdateAcAsync (_deviceGuid, request, operation.Token).ConfigureAwait (false);
		operation.Token.ThrowIfCancellationRequested ();
		return true;
		}
	/// <summary>Discards queued preferences and cancels this controller's pending operations without disposing the shared client.</summary>
	public void Dispose () => _operations.Stop (() => Volatile.Write (ref _pending, null));
	}