// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class DeviceInfo
	{
	/// <summary>Gets the upstream stable identifier, preserving a server hash when available.</summary>
	/// <value>The hash ID, otherwise the lowercase MD5 of the GUID for legacy identifier compatibility, or null without either input.</value>
	[JsonIgnore]
	public string? StableId => HashId ?? (DeviceGuid is null ? null : LegacyIdentifier (DeviceGuid));
	/// <summary>Gets whether this is one of the upstream ducted/commercial device types.</summary>
	/// <value>True for types four, five, six, eight, nine and ten.</value>
	[JsonIgnore]
	public bool IsCommercial => DeviceType is "4" or "5" or "6" or "8" or "9" or "10";
	/// <summary>Gets whether the listing supplies a usable AC identifier and parameter summary.</summary>
	/// <value>True for the air-conditioner family with both an identifier and parameters.</value>
	[JsonIgnore]
	public bool IsValidAirConditioner => Family == DeviceFamily.AirConditioner && StableId is not null && DeviceGuid is not null && Parameters is not null;
	/// <summary>Gets or sets the discovery tank summary used to identify older Aquarea entries without a type.</summary>
	/// <value>The reported tank summary, or null when absent.</value>
	[JsonPropertyName ("tankStatus")]
	public AquareaTank? AquareaTank
		{
		get; set;
		}
	/// <summary>Gets or sets the discovery zone summary used to identify older Aquarea entries.</summary>
	/// <value>The reported zone list, or null when absent.</value>
	[JsonPropertyName ("zoneStatus")]
	public List<AquareaZone>? AquareaZones
		{
		get; set;
		}
	[SuppressMessage ("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 reproduces the upstream device identifier only; it is not used for authentication, signing or security.")]
	private static string LegacyIdentifier (string value) => Convert.ToHexStringLower (MD5.HashData (Encoding.UTF8.GetBytes (value)));
	}

public sealed partial class AcSnapshot
	{
	/// <summary>Gets the source timestamp as an instant when it lies within the supported range.</summary>
	/// <value>The device timestamp, or null when absent or invalid; the original numeric value remains available.</value>
	[JsonIgnore]
	public DateTimeOffset? DeviceTimestamp
		{
		get
			{
			if (Timestamp is not long value)
				return null;
			try
				{
				return DateTimeOffset.FromUnixTimeMilliseconds (value);
				}
			catch (ArgumentOutOfRangeException)
				{
				return null;
				}
			}
		}
	}

public sealed partial class AcParameters
	{
	/// <summary>Gets the effective horizontal setting with fanAutoMode applied.</summary>
	/// <value>Minus one for automatic, the reported position otherwise, or null when absent.</value>
	[JsonIgnore]
	public int? HorizontalSwingMode => FanAutoMode is 0 or 3 ? -1 : HorizontalSwing;
	/// <summary>Gets the effective vertical setting with fanAutoMode applied.</summary>
	/// <value>Minus one for automatic, the reported position otherwise, or null when absent.</value>
	[JsonIgnore]
	public int? VerticalSwingMode => FanAutoMode is 0 or 2 ? -1 : VerticalSwing;
	/// <summary>Finds one reported zone.</summary>
	/// <param name="zoneId">The zone identifier.</param>
	/// <returns>The zone, or null when not reported.</returns>
	public AcZone? GetZone (int zoneId) => Zones?.FirstOrDefault (z => z.Id == zoneId);
	}

public sealed partial class AquareaStatus
	{
	/// <summary>Gets whether the snapshot advertises cooling.</summary>
	/// <value>True for a nonzero reported cooling capability.</value>
	[JsonIgnore]
	public bool SupportsCooling => CoolingCapability is not null and not 0;
	/// <summary>Gets whether the snapshot includes a tank.</summary>
	/// <value>True when a tank object is present.</value>
	[JsonIgnore]
	public bool HasTank => Tank is not null;
	/// <summary>Gets whether any fault is reported.</summary>
	/// <value>True when the fault list is nonempty.</value>
	[JsonIgnore]
	public bool IsOnError => Faults?.Count > 0;
	/// <summary>Finds one reported zone.</summary>
	/// <param name="zoneId">The zone identifier.</param>
	/// <returns>The zone, or null when absent.</returns>
	public AquareaZone? GetZone (int zoneId) => Zones?.FirstOrDefault (z => z.Id == zoneId);
	}

public sealed partial class AquareaZone
	{
	/// <summary>Gets whether this zone has a usable cooling range.</summary>
	/// <value>True when both range bounds are present, matching the upstream capability rule.</value>
	[JsonIgnore]
	public bool SupportsCooling => CoolMinimum.HasValue && CoolMaximum.HasValue;
	}

public sealed partial class AcSchedule
	{
	/// <summary>Gets whether the account has the upstream schedule-edit permission.</summary>
	/// <value>True for permission two or three.</value>
	[JsonIgnore]
	public bool CanEdit => Permission is 2 or 3;
	}