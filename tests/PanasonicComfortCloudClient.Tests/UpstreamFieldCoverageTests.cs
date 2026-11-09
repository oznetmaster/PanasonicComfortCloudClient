// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using NUnit.Framework;

using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class UpstreamFieldCoverageTests
	{
	private static readonly JsonSerializerOptions _json = ComfortCloudJson.CreateOptions ();
	private static readonly Dictionary<string, Type> _models = new (StringComparer.Ordinal)
		{
		["DeviceInfo"] = typeof (DeviceInfo),
		["AcParameters"] = typeof (AcParameters),
		["AcSnapshot"] = typeof (AcSnapshot),
		["AcZone"] = typeof (AcZone),
		["AcEnergyHistoryEntry"] = typeof (AcEnergyHistoryEntry),
		["AquareaSnapshot"] = typeof (AquareaSnapshot),
		["HotWaterSnapshot"] = typeof (HotWaterSnapshot),
		["AquareaZone"] = typeof (AquareaZone),
		["AquareaTank"] = typeof (AquareaTank),
		["AquareaStatus"] = typeof (AquareaStatus),
		["EnergyHistoryEntry"] = typeof (EnergyHistoryEntry),
		["HotWaterTemperatureLimits"] = typeof (HotWaterTemperatureLimits),
		["HotWaterInterval"] = typeof (HotWaterInterval),
		["HotWaterDay"] = typeof (HotWaterDay),
		["HotWaterParameters"] = typeof (HotWaterParameters),
		["HotWaterEnergyHistoryEntry"] = typeof (HotWaterEnergyHistoryEntry),
		["AcTimerPattern"] = typeof (AcTimerPattern),
		["WeeklyTimerDay`1"] = typeof (WeeklyTimerDay<AcTimerPattern>),
		["AcSchedule"] = typeof (AcSchedule),
		["AquareaTimerPattern"] = typeof (AquareaTimerPattern),
		["AquareaSchedule"] = typeof (AquareaSchedule)
		};
	private static Coverage ReadCoverage ()
		{
		using Stream stream = typeof (UpstreamFieldCoverageTests).Assembly.GetManifestResourceStream ("PanasonicComfortCloudClient.Tests.UpstreamCoverage.data") ?? throw new InvalidDataException ("The pinned upstream field inventory was not embedded.");
		return JsonSerializer.Deserialize<Coverage> (stream) ?? throw new InvalidDataException ("Missing upstream inventory.");
		}
	public static IEnumerable<TestCaseData> Fields () => ReadCoverage ().Fields.Select (f => new TestCaseData (f.Model, f.Key, f.Property, f.Kind).SetName ($"UpstreamField({f.SourceClass}.{f.Key})"));
	public static IEnumerable<TestCaseData> Models () => _models.Keys.Select (model => new TestCaseData (model).SetName ($"UnknownFields({model})"));
	[TestCaseSource (nameof (Models))]
	public void UnmodeledFieldsSurviveEveryTypedModelRoundTrip (string model)
		{
		Type type = _models[model];
		const string INPUT = "{\"futureUpstreamField\":{\"mode\":700}}";
		object value = JsonSerializer.Deserialize (INPUT, type, _json)!;
		using JsonDocument output = JsonDocument.Parse (JsonSerializer.Serialize (value, type, _json));
		Assert.That (output.RootElement.GetProperty ("futureUpstreamField").GetProperty ("mode").GetInt32 (), Is.EqualTo (700));
		}
	[Test]
	public void InventoryPinsEveryAuditedSourceFileAndHasNoUnmappedFields ()
		{
		Coverage coverage = ReadCoverage ();
		Assert.That (coverage.UpstreamCommit, Is.EqualTo ("80d7784fa6b35d6f71a7b1f842062e2929d2d653"));
		Assert.That (coverage.Fields, Has.Length.EqualTo (201));
		Assert.That (coverage.Missing, Is.Empty);
		Assert.That (coverage.SourceFiles, Is.Not.Empty);
		Assert.That (coverage.SourceFiles.All (f => f.Sha256.Length == 64), Is.True);
		Assert.That (coverage.Methods, Has.Length.EqualTo (166));
		Assert.That (coverage.Properties, Has.Length.EqualTo (300));
		Assert.That (coverage.Methods.Concat (coverage.Properties).All (m => !string.IsNullOrWhiteSpace (m.Target) && !m.Target.Contains ("UNMAPPED")), Is.True);
		}
	[TestCaseSource (nameof (Fields))]
	public void ModeledFieldHasAPublicTypedValueAndRoundTrips (string model, string key, string member, string kind)
		{
		Type type = _models[model];
		PropertyInfo property = type.GetProperty (member) ?? throw new InvalidOperationException ("The documented typed property is missing.");
		Assert.That (property.GetCustomAttribute<JsonPropertyNameAttribute> ()?.Name, Is.EqualTo (key));
		object value = kind switch
			{
				"string" => key == "startTime" ? "06:30" : "fixture-value",
				"number" => 2,
				"bool" => true,
				"array" => Array.Empty<object> (),
				"object" => new Dictionary<string, object> (),
				_ => throw new InvalidDataException ("Unexpected audited field kind.")
				};
		string input = JsonSerializer.Serialize (new Dictionary<string, object> { [key] = value }, _json);
		object instance = JsonSerializer.Deserialize (input, type, _json)!;
		Assert.That (property.GetValue (instance), Is.Not.Null, "Extension-data preservation alone does not provide typed coverage.");
		using JsonDocument output = JsonDocument.Parse (JsonSerializer.Serialize (instance, type, _json));
		using JsonDocument expected = JsonDocument.Parse (input);
		Assert.That (JsonElement.DeepEquals (output.RootElement.GetProperty (key), expected.RootElement.GetProperty (key)), Is.True);
		}
	private sealed class Coverage
		{
		public string UpstreamCommit { get; set; } = "";
		public Field[] Fields { get; set; } = [];
		public string[] Missing { get; set; } = [];
		public Source[] SourceFiles { get; set; } = [];
		public Mapping[] Methods { get; set; } = [];
		public Mapping[] Properties { get; set; } = [];
		}
	private sealed class Mapping
		{
		public string Target { get; set; } = "";
		}
	private sealed class Source
		{
		public string Sha256 { get; set; } = "";
		}
	private sealed class Field
		{
		public string SourceClass { get; set; } = "";
		public string Model { get; set; } = "";
		public string Key { get; set; } = "";
		public string Property { get; set; } = "";
		public string Kind { get; set; } = "";
		}
	}