// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Describes a cloud policy document without accepting it.</summary>
public sealed partial class AgreementDocument : ExtensibleModel
	{
	/// <summary>Gets or sets the policy type identifier.</summary>
	/// <value>The policy type identifier. Null when not reported or not requested.</value>
	[JsonPropertyName ("type")]
	public int? Type
		{
		get; set;
		}

	/// <summary>Gets or sets the policy version.</summary>
	/// <value>The policy version. Null when not reported or not requested.</value>
	[JsonPropertyName ("version")]
	public string? Version
		{
		get; set;
		}

	/// <summary>Gets or sets the document content, if explicitly requested.</summary>
	/// <value>The document content, if explicitly requested. Null when not reported or not requested.</value>
	[JsonPropertyName ("content")]
	public string? Content
		{
		get; set;
		}

	/// <summary>Gets or sets the document title.</summary>
	/// <value>The document title. Null when not reported or not requested.</value>
	[JsonPropertyName ("title")]
	public string? Title
		{
		get; set;
		}

	/// <summary>Gets or sets the document URL, when returned.</summary>
	/// <value>The document URL, when returned. Null when not reported or not requested.</value>
	[JsonPropertyName ("url")]
	public string? Url
		{
		get; set;
		}
	}