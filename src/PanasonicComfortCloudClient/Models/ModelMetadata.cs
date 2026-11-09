// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class DeviceInfo
	{
	/// <summary>Gets the API family indicated by the device description.</summary>
	/// <value>Aquarea for wire type two, HWS for eleven, or an AC/unknown classification.</value>
	[JsonIgnore]
	public DeviceFamily Family => DeviceType switch
		{
			"2" => DeviceFamily.Aquarea,
			"11" => DeviceFamily.HotWater,
			_ => Parameters is not null ? DeviceFamily.AirConditioner : AquareaTank is not null || AquareaZones is not null ? DeviceFamily.Aquarea : DeviceFamily.Unknown
			};
	}

public sealed partial class ComfortCloudGroup
	{
	/// <summary>Gets devices from the current or alternate group-list format.</summary>
	/// <value>The reported list, or an empty collection when no devices were reported.</value>
	[JsonIgnore]
	public IReadOnlyList<DeviceInfo> Devices => (IReadOnlyList<DeviceInfo>?)DeviceList ?? DeviceIdList ?? (IReadOnlyList<DeviceInfo>)[];
	}

public sealed partial class AquareaSnapshot
	{
	/// <summary>Gets the source explicitly requested by the caller.</summary>
	/// <value>Live or cached; cached data is never silently labelled live.</value>
	[JsonIgnore]
	public StatusSource Source
		{
		get; internal set;
		}

	/// <summary>Gets the time the complete response was observed.</summary>
	/// <value>The UTC receipt instant, which is not a device-generated measurement timestamp.</value>
	[JsonIgnore]
	public DateTimeOffset ObservedAt
		{
		get; internal set;
		}
	}

public partial class EnergyHistoryEntry
	{
	/// <summary>Gets the sum of reported Aquarea energy components.</summary>
	/// <value>The reported heating/cooling/tank sum in kWh, or null when no component is available.</value>
	[JsonIgnore]
	public double? ReportedTotal => HeatingEnergy.HasValue || CoolingEnergy.HasValue || HotWaterEnergy.HasValue
		 ? (HeatingEnergy ?? 0) + (CoolingEnergy ?? 0) + (HotWaterEnergy ?? 0) : Consumption;
	}