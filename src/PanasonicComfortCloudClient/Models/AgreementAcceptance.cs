// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Identifies the exact policy version the application explicitly elects to accept.</summary>
public sealed partial class AgreementAcceptance : ExtensibleModel
	{
	/// <summary>Gets or sets the policy type identifier.</summary>
	/// <value>The policy type identifier. Null when not reported or not requested.</value>
	[JsonPropertyName ("type")]
	public int? Type
		{
		get; set;
		}

	/// <summary>Gets or sets the exact policy version being accepted.</summary>
	/// <value>The exact policy version being accepted. Null when not reported or not requested.</value>
	[JsonPropertyName ("version")]
	public string? Version
		{
		get; set;
		}
	}