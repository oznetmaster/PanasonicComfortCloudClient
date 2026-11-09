// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains air-conditioner status and the reported freshness metadata.</summary>
public sealed partial class AcSnapshot : ExtensibleModel
	{
	/// <summary>Gets or sets the device parameters.</summary>
	/// <value>The device parameters. Null when not reported or not requested.</value>
	[JsonPropertyName ("parameters")]
	public AcParameters? Parameters
		{
		get; set;
		}

	/// <summary>Gets or sets the source timestamp in Unix milliseconds, when reported.</summary>
	/// <value>The source timestamp in Unix milliseconds, when reported. Null when not reported or not requested.</value>
	[JsonPropertyName ("timestamp")]
	public long? Timestamp
		{
		get; set;
		}

	/// <summary>Gets or sets the wire device status indicator.</summary>
	/// <value>The wire device status indicator. Null when not reported or not requested.</value>
	[JsonPropertyName ("deviceStatus")]
	public int? Status
		{
		get; set;
		}
	}