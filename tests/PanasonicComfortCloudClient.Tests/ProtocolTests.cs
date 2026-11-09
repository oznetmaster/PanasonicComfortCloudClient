// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;

using NUnit.Framework;

using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class ProtocolTests
	{
	[TestCase (0, "+00:00")]
	[TestCase (330, "+05:30")]
	[TestCase (-210, "-03:30")]
	[TestCase (840, "+14:00")]
	[TestCase (-840, "-14:00")]
	public async Task EnergyUsesNumericUtcOffset (int minutes, string expected)
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"historyDataList\":[]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await client.GetAquareaEnergyAsync ("gateway", EnergyPeriod.Day, new DateTime (2026, 11, 8), TimeSpan.FromMinutes (minutes));
		using var request = JsonDocument.Parse (handler._requests[0].Body!);
		Assert.That (request.RootElement.GetProperty ("bodyParam").GetProperty ("osTimezone").GetString (), Is.EqualTo (expected));
		}

	[TestCase (50460)]
	[TestCase (-50460)]
	[TestCase (1)]
	public async Task InvalidEnergyOffsetDoesNotSend (int seconds)
		{
		var handler = new ScriptedHandler ();
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException> (async () => await client.GetAquareaEnergyAsync ("gateway", EnergyPeriod.Day, DateTime.Today, TimeSpan.FromSeconds (seconds)));
		Assert.That (handler._requests, Is.Empty);
		}

	[Test]
	public async Task HwsIdentifierUsesReferenceSlashSubstitution ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"parameters\":{\"tankTemperature\":50}}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await client.GetHotWaterStatusAsync ("model/serial+id");
		Assert.That (handler._requests[0].Uri.Query, Is.EqualTo ("?deviceGuid=modelfserial%2Bid&fromDevice=1"));
		}

	[Test]
	public void PkceMatchesRfc7636Vector () => Assert.That (OAuthProtocol.Challenge ("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"), Is.EqualTo ("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"));

	[Test]
	public void DevicePathEscapesPlusAndReplacesSlash () => Assert.That (RequestSigning.DevicePath ("CS+AB/12"), Is.EqualTo ("CS%2BABf12"));

	[Test]
	public void SigningTimestampIsUtc () => Assert.That (RequestSigning.Timestamp (new DateTimeOffset (2026, 11, 8, 15, 30, 0, TimeSpan.FromHours (2))), Is.EqualTo ("2026-11-08 13:30:00"));

	[TestCase ("2", DeviceFamily.Aquarea)]
	[TestCase ("11", DeviceFamily.HotWater)]
	[TestCase ("99", DeviceFamily.Unknown)]
	public void DiscoveryKeepsFamilyAndNumericDeviceType (string type, DeviceFamily family)
		{
		DeviceInfo? device = JsonSerializer.Deserialize<DeviceInfo> ("{\"deviceGuid\":\"sample\",\"deviceType\":" + type + "}", ComfortCloudJson.CreateOptions ());
		Assert.That (device!.Family, Is.EqualTo (family));
		}

	[Test]
	public void MissingValuesRemainNullAndRealZeroIsPreserved ()
		{
		AquareaStatus status = JsonSerializer.Deserialize<AquareaStatus> ("{\"outdoorNow\":0,\"operationMode\":99}", ComfortCloudJson.CreateOptions ())!;
		Assert.That (status.OutdoorTemperature, Is.EqualTo (0));
		Assert.That (status.Power, Is.Null);
		Assert.That ((int)status.Mode!.Value, Is.EqualTo (99));
		Assert.That (JsonSerializer.Deserialize<AcParameters> ("{\"iAuto\":2}", ComfortCloudJson.CreateOptions ())!.IAutoX, Is.EqualTo (2));
		}

	[Test]
	public void UnknownScheduleFieldsRoundTripWithoutPublicRawJson ()
		{
		const string input = "{\"operation\":1,\"future\":{\"value\":7},\"timer\":[]}";
		JsonSerializerOptions options = ComfortCloudJson.CreateOptions ();
		AquareaWeeklySettings schedule = JsonSerializer.Deserialize<AquareaWeeklySettings> (input, options)!;
		using var output = JsonDocument.Parse (JsonSerializer.Serialize (schedule, options));
		Assert.That (output.RootElement.GetProperty ("future").GetProperty ("value").GetInt32 (), Is.EqualTo (7));
		}

	[TestCase ("126", null)]
	[TestCase ("0", 0d)]
	[TestCase ("-5.5", -5.5)]
	public void AcTemperatureSentinelIsNotARealReading (string number, double? expected)
		{
		AcParameters status = JsonSerializer.Deserialize<AcParameters> ("{\"insideTemperature\":" + number + "}", ComfortCloudJson.CreateOptions ())!;
		Assert.That (status.IndoorTemperature, Is.EqualTo (expected));
		}

	[Test]
	public async Task AquareaReadUsesProxyAndExactGuidWithoutAcSlashTransformation ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply (Fixture.AQUAREA_STATUS);
		using ComfortCloudClient client = await Fixture.Client (handler);
		AquareaSnapshot status = await client.GetAquareaStatusAsync ("gateway/with+space");
		Assert.That (status.Source, Is.EqualTo (StatusSource.Live));
		Assert.That (status.Status!.Mode, Is.EqualTo (AquareaReportedMode.Heat));
		Assert.That (handler._requests[0].Uri.AbsolutePath, Is.EqualTo ("/remote/v1/app/common/transfer"));
		using var body = JsonDocument.Parse (handler._requests[0].Body!);
		Assert.That (body.RootElement.GetProperty ("apiName").GetString (), Is.EqualTo ("/remote/v1/api/devices?gwid=gateway%2Fwith%2Bspace&deviceDirect=1"));
		Assert.That (body.RootElement.TryGetProperty ("bodyParam", out _), Is.False);
		Assert.That (handler._requests[0].Headers["X-CLIENT-ID"], Is.EqualTo ("fixture-client"));
		}

	[Test]
	public async Task AlternateDeviceListIsSupported ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"groupList\":[{\"groupId\":4,\"deviceIdList\":[{\"deviceGuid\":\"one\",\"deviceType\":\"2\"}]}]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		IReadOnlyList<ComfortCloudGroup> groups = await client.GetGroupsAsync ();
		Assert.That (groups[0].Id, Is.EqualTo ("4"));
		Assert.That (groups[0].Devices[0].Family, Is.EqualTo (DeviceFamily.Aquarea));
		Assert.That (handler._requests[0].Headers["Content-Type"], Is.EqualTo ("application/json"));
		Assert.That (handler._requests[0].Headers["Accept"], Is.EqualTo ("application/json; charset=utf-8"));
		Assert.That (handler._requests[0].Body, Is.Empty);
		Assert.That (handler._requests[0].Headers.Keys.Where (k => k.StartsWith ("x-", StringComparison.OrdinalIgnoreCase)).All (k => k.Equals (k.ToLowerInvariant (), StringComparison.Ordinal)), Is.True, "Aquarea requires the upstream lowercase custom header names.");
		}

	[Test]
	public async Task MissingDiscoveryListDoesNotBecomeEmptySuccess ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.GetGroupsAsync ());
		}

	[Test]
	public async Task RejectedReadDoesNotSilentlyFetchCachedStatus ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"code\":5005}", HttpStatusCode.InternalServerError);
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.GetAquareaStatusAsync ("gateway"));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[Test]
	public async Task ExplicitCachedReadIsLabelledCached ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply (Fixture.AQUAREA_STATUS);
		using ComfortCloudClient client = await Fixture.Client (handler);
		AquareaSnapshot status = await client.GetAquareaStatusAsync ("gateway", StatusSource.Cached);
		Assert.That (status.Source, Is.EqualTo (StatusSource.Cached));
		Assert.That (handler._requests[0].Body, Does.Contain ("deviceDirect=0"));
		}

	[TestCase (4100)]
	[TestCase (4103)]
	[TestCase (4106)]
	public async Task TypedProtocolErrorPreservesCodeAndDoesNotRetry (int code)
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"code\":" + code + ",\"message\":\"sensitive-body\"}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		ComfortCloudException? ex = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.GetGroupsAsync ());
		Assert.That (ex!.ErrorCode, Is.EqualTo (code.ToString (CultureInfo.InvariantCulture)));
		Assert.That (ex.Message, Does.Not.Contain ("sensitive-body"));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[Test]
	public async Task RetryAfterIsReportedWithoutAutomaticReplay ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ((_, _) =>
		{
			var response = new HttpResponseMessage ((HttpStatusCode)429)
				{
				Content = new StringContent ("{\"code\":42901}")
				};
			response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue (TimeSpan.FromSeconds (60));
			return Task.FromResult (response);
		});
		using ComfortCloudClient client = await Fixture.Client (handler);
		ComfortCloudException? ex = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.GetGroupsAsync ());
		Assert.That (ex!.RetryAfter, Is.EqualTo (TimeSpan.FromSeconds (60)));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[Test]
	public async Task AquareaYearIsTwoWhileAcYearIsFour ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"historyDataList\":[]}");
		handler.Reply ("{\"historyDataList\":[]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await client.GetAquareaEnergyAsync ("gateway", EnergyPeriod.Year, new DateTime (2026, 11, 8), TimeSpan.Zero);
		_ = await client.GetAcEnergyAsync ("ac", AcEnergyPeriod.Year, new DateTime (2026, 11, 8), TimeSpan.Zero);
		using var aq = JsonDocument.Parse (handler._requests[0].Body!);
		using var ac = JsonDocument.Parse (handler._requests[1].Body!);
		Assert.That (aq.RootElement.GetProperty ("bodyParam").GetProperty ("dataMode").GetInt32 (), Is.EqualTo (2));
		Assert.That (ac.RootElement.GetProperty ("dataMode").GetInt32 (), Is.EqualTo (4));
		}

	[Test]
	public async Task HwsStatusUsesDedicatedEndpoint ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"parameters\":{\"tankTemperature\":50,\"hpuOperationStatus\":0}}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		HotWaterSnapshot status = await client.GetHotWaterStatusAsync ("hot-water");
		Assert.That (handler._requests[0].Uri.AbsolutePath, Is.EqualTo ("/hphw/deviceStatus"));
		Assert.That (status.Parameters!.TankTemperature, Is.EqualTo (50));
		}
	}