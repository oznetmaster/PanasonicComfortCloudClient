// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Describes a discovered device without synthesizing missing state.</summary>
public sealed partial class DeviceInfo : ExtensibleModel
	{
	/// <summary>Gets or sets the cloud device identifier.</summary>
	/// <value>The cloud device identifier. Null when not reported or not requested.</value>
	[JsonPropertyName ("deviceGuid")]
	public string? DeviceGuid
		{
		get; set;
		}

	/// <summary>Gets or sets the optional cloud hash identifier.</summary>
	/// <value>The optional cloud hash identifier. Null when not reported or not requested.</value>
	[JsonPropertyName ("deviceHashGuid")]
	public string? HashId
		{
		get; set;
		}

	/// <summary>Gets or sets the device display name.</summary>
	/// <value>The device display name. Null when not reported or not requested.</value>
	[JsonPropertyName ("deviceName")]
	public string? Name
		{
		get; set;
		}

	/// <summary>Gets or sets the reported hardware model.</summary>
	/// <value>The reported hardware model. Null when not reported or not requested.</value>
	[JsonPropertyName ("deviceModuleNumber")]
	public string? Model
		{
		get; set;
		}

	/// <summary>Gets or sets the wire device-type identifier.</summary>
	/// <value>The wire device-type identifier. Null when not reported or not requested.</value>
	[JsonPropertyName ("deviceType")]
	public string? DeviceType
		{
		get; set;
		}

	/// <summary>Gets or sets the account permission, or null when absent.</summary>
	/// <value>The account permission, or null when absent.</value>
	[JsonPropertyName ("permission")]
	public int? Permission
		{
		get; set;
		}

	/// <summary>Gets or sets the wire temperature unit, or null when absent.</summary>
	/// <value>The wire temperature unit, or null when absent.</value>
	[JsonPropertyName ("temperatureUnit")]
	public int? TemperatureUnit
		{
		get; set;
		}

	/// <summary>Gets or sets the group-list parameter summary, when supplied.</summary>
	/// <value>The group-list parameter summary, when supplied. Null when not reported or not requested.</value>
	[JsonPropertyName ("parameters")]
	public AcParameters? Parameters
		{
		get; set;
		}
	}