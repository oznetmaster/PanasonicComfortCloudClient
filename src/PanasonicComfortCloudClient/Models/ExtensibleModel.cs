// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Preserves unrecognized wire fields during typed read/modify/write operations.</summary>
public abstract class ExtensibleModel
	{
	[JsonInclude, JsonExtensionData]
	internal Dictionary<string, JsonElement>? ExtensionData
		{
		get; set;
		}
	}