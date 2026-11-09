// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains the returned Aquarea device status and observation metadata.</summary>
public sealed partial class AquareaSnapshot : ExtensibleModel
	{
	/// <summary>Gets or sets the device name.</summary>
	/// <value>The device name. Null when not reported or not requested.</value>
	[JsonPropertyName ("a2wName")]
	public string? Name
		{
		get; set;
		}

	/// <summary>Gets or sets the operation description reported by the service.</summary>
	/// <value>The operation description reported by the service. Null when not reported or not requested.</value>
	[JsonPropertyName ("operation")]
	public string? Operation
		{
		get; set;
		}

	/// <summary>Gets or sets the account ownership flag, or null when absent.</summary>
	/// <value>The account ownership flag, or null when absent.</value>
	[JsonPropertyName ("ownerFlg")]
	public bool? IsOwner
		{
		get; set;
		}

	/// <summary>Gets or sets the device status, or null if the response did not contain it.</summary>
	/// <value>The device status, or null if the response did not contain it.</value>
	[JsonPropertyName ("status")]
	public AquareaStatus? Status
		{
		get; set;
		}
	}