// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Describes a group and both supported device-list wire formats.</summary>
public sealed partial class ComfortCloudGroup : ExtensibleModel
	{
	/// <summary>Gets or sets the cloud group identifier.</summary>
	/// <value>The cloud group identifier. Null when not reported or not requested.</value>
	[JsonPropertyName ("groupId")]
	public string? Id
		{
		get; set;
		}

	/// <summary>Gets or sets the group display name.</summary>
	/// <value>The group display name. Null when not reported or not requested.</value>
	[JsonPropertyName ("groupName")]
	public string? Name
		{
		get; set;
		}

	/// <summary>Gets or sets the current-format device list, or null when absent.</summary>
	/// <value>The current-format device list, or null when absent.</value>
	[JsonPropertyName ("deviceList")]
	public List<DeviceInfo>? DeviceList
		{
		get; set;
		}

	/// <summary>Gets or sets the alternate-format device list, or null when absent.</summary>
	/// <value>The alternate-format device list, or null when absent.</value>
	[JsonPropertyName ("deviceIdList")]
	public List<DeviceInfo>? DeviceIdList
		{
		get; set;
		}
	}