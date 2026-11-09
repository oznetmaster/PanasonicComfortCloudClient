// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Protocol;

/// <summary>Provides the same typed JSON settings used by the cloud transport.</summary>
public static class ComfortCloudJson
	{
	/// <summary>Creates independent serializer options for typed models.</summary>
	/// <param name="indented">Whether to indent output for interactive display.</param>
	/// <returns>Options preserving numeric enum values and omitting unspecified changes.</returns>
	public static JsonSerializerOptions CreateOptions (bool indented = false)
		{
		var options = new JsonSerializerOptions
			{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			NumberHandling = JsonNumberHandling.AllowReadingFromString,
			WriteIndented = indented,
			MaxDepth = 64
			};
		options.Converters.Add (new NumericEnumConverterFactory ());
		options.Converters.Add (new NumberOrStringConverter ());
		options.Converters.Add (new FlexibleBooleanConverter ());
		return options;
		}
	}

internal sealed class NumberOrStringConverter : JsonConverter<string>
	{
	public override string? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.String => reader.GetString (),
			JsonTokenType.Number when reader.TryGetInt64 (out var value) => value.ToString (CultureInfo.InvariantCulture),
			JsonTokenType.Number => reader.GetDecimal ().ToString (CultureInfo.InvariantCulture),
			_ => throw new JsonException ("Expected a string or numeric identifier.")
			};
	public override void Write (Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue (value);
	public override string ReadAsPropertyName (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.GetString () ?? throw new JsonException ("Expected a dictionary property name.");
	public override void WriteAsPropertyName (Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WritePropertyName (value);
	}

internal sealed class FlexibleBooleanConverter : JsonConverter<bool>
	{
	public override bool Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.True => true,
			JsonTokenType.False => false,
			JsonTokenType.Number when reader.TryGetInt32 (out var value) && value is 0 or 1 => value == 1,
			JsonTokenType.String when reader.GetString () is "0" or "false" => false,
			JsonTokenType.String when reader.GetString () is "1" or "true" => true,
			_ => throw new JsonException ("Expected a boolean or zero/one flag.")
			};
	public override void Write (Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue (value);
	}

internal sealed class NumericEnumConverterFactory : JsonConverterFactory
	{
	public override bool CanConvert (Type type) => type.IsEnum;
	public override JsonConverter CreateConverter (Type type, JsonSerializerOptions options) =>
		 (JsonConverter)Activator.CreateInstance (typeof (NumericEnumConverter<>).MakeGenericType (type))!;
	}

internal sealed class NumericEnumConverter<T> : JsonConverter<T> where T : struct, Enum
	{
	public override T Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
		var value = reader.TokenType == JsonTokenType.Number
			? reader.GetInt32 ()
			: reader.TokenType == JsonTokenType.String && int.TryParse (reader.GetString (), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
						? parsed
						: throw new JsonException ("Expected a numeric device enum value.");

		return (T)Enum.ToObject (typeof (T), value);
		}
	public override void Write (Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteNumberValue (Convert.ToInt32 (value, CultureInfo.InvariantCulture));
	}

internal sealed class TemperatureConverter : JsonConverter<double?>
	{
	public override double? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
		if (reader.TokenType == JsonTokenType.Null)
			{
			return null;
			}

		var temperature = reader.TokenType == JsonTokenType.Number ? reader.GetDouble ()
			 : double.Parse (reader.GetString ()!, NumberStyles.Float, CultureInfo.InvariantCulture);
		return temperature == 126 ? null : temperature;
		}
	public override void Write (Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
		{
		if (value.HasValue)
			{
			writer.WriteNumberValue (value.Value);
			}
		else
			{
			writer.WriteNullValue ();
			}
		}
	}
internal sealed class ZoneTemperatureConverter : JsonConverter<double?>
	{
	public override double? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
		double? value = JsonSerializer.Deserialize<double?> (ref reader, options);
		return value == -255 ? null : value;
		}
	public override void Write (Utf8JsonWriter writer, double? value, JsonSerializerOptions options) => JsonSerializer.Serialize (writer, value, options);
	}