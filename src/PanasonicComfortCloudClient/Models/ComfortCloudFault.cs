// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Contains a fault code returned as either a scalar or a structured fault entry.</summary>
[JsonConverter (typeof (ComfortCloudFaultConverter))]
public sealed class ComfortCloudFault
	{
	/// <summary>Gets the fault code.</summary>
	/// <value>The reported code, or null when not supplied.</value>
	public string? Code
		{
		get; init;
		}
	/// <summary>Gets the fault description.</summary>
	/// <value>The reported description, or null when not supplied.</value>
	public string? Message
		{
		get; init;
		}
	}

internal sealed class ComfortCloudFaultConverter : JsonConverter<ComfortCloudFault>
	{
	public override ComfortCloudFault? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
		if (reader.TokenType == JsonTokenType.Null)
			{
			return null;
			}

		if (reader.TokenType == JsonTokenType.String)
			{
			return new ()
				{
				Code = reader.GetString ()
				};
			}

		if (reader.TokenType == JsonTokenType.Number)
			{
			return new ()
				{
				Code = reader.GetInt64 ().ToString (System.Globalization.CultureInfo.InvariantCulture)
				};
			}

		FaultFields data = JsonSerializer.Deserialize<FaultFields> (ref reader, options) ?? throw new JsonException ("Invalid fault entry.");
		return new ()
			{
			Code = data.Code ?? data.ErrorCode,
			Message = data.Message
			};
		}
	public override void Write (Utf8JsonWriter writer, ComfortCloudFault value, JsonSerializerOptions options) =>
		 JsonSerializer.Serialize (writer, new FaultFields
			 {
			 Code = value.Code,
			 Message = value.Message
			 }, options);
	private sealed class FaultFields
		{
		[JsonPropertyName ("code")]
		public string? Code
			{
			get; set;
			}
		[JsonPropertyName ("errorCode")]
		public string? ErrorCode
			{
			get; set;
			}
		[JsonPropertyName ("message")]
		public string? Message
			{
			get; set;
			}
		}
	}