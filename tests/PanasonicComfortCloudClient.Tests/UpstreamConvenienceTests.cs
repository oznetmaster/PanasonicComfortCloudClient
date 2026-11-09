// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using NUnit.Framework;

using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class UpstreamConvenienceTests
	{
	private static readonly JsonSerializerOptions _json = ComfortCloudJson.CreateOptions ();
	[Test]
	public void UnknownCapabilityValuesRemainUnavailable ()
		{
		var state = new AcSnapshot
			{
			SupportsEcoNavi = true,
			EcoFunctionCapability = 1,
			SupportsNanoe = true,
			SupportsIAutoX = true,
			Parameters = new () { EcoNavi = 99, EcoFunction = 99, Nanoe = 99, IAutoX = 99 }
			};
		var capabilities = new AcCapabilities (new (), state);
		Assert.Multiple (() =>
			{
				Assert.That (capabilities.HasEcoNavi, Is.False);
				Assert.That (capabilities.HasEcoFunction, Is.False);
				Assert.That (capabilities.HasNanoe, Is.False);
				Assert.That (capabilities.HasIAutoX, Is.False);
			});
		}
	[Test]
	public async Task BufferedPreferencesRetainUnknownFieldsWithLastValueWinning ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		using var controller = new AcController (client, "fixture", false);
		var snapshot = new AcSnapshot { Parameters = new () { Power = PowerState.Off } };
		AcUpdate first = JsonSerializer.Deserialize<AcUpdate> ("{\"futureSetting\":1,\"preservedSetting\":3,\"temperatureSet\":22}", _json)!;
		AcUpdate second = JsonSerializer.Deserialize<AcUpdate> ("{\"futureSetting\":2,\"operate\":1}", _json)!;
		Assert.That (await controller.ApplyAsync (snapshot, first), Is.False);
		Assert.That (await controller.ApplyAsync (snapshot, second), Is.True);
		using JsonDocument request = JsonDocument.Parse (handler._requests.Single ().Body!);
		JsonElement parameters = request.RootElement.GetProperty ("parameters");
		Assert.That (parameters.GetProperty ("futureSetting").GetInt32 (), Is.EqualTo (2));
		Assert.That (parameters.GetProperty ("preservedSetting").GetInt32 (), Is.EqualTo (3));
		Assert.That (parameters.GetProperty ("temperatureSet").GetInt32 (), Is.EqualTo (22));
		}
	[Test]
	public async Task DailyAcEnergySelectsRequestedDateAndRetainsRatios ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"historyDataList\":[{\"dataTime\":\"20261108\",\"consumption\":8},{\"dataTime\":\"20261109\",\"consumption\":10,\"heatConsumptionRate\":0.6,\"coolConsumptionRate\":0.4}]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		AcEnergyHistoryEntry? entry = await client.GetAcDailyEnergyAsync ("fixture", new DateTime (2026, 11, 9), TimeSpan.FromHours (1));
		Assert.That (entry!.Consumption, Is.EqualTo (10));
		Assert.That (entry.CalculatedHeatingConsumption, Is.EqualTo (6));
		Assert.That (entry.CalculatedCoolingConsumption, Is.EqualTo (4));
		using JsonDocument request = JsonDocument.Parse (handler._requests[0].Body!);
		Assert.That (request.RootElement.GetProperty ("dataMode").GetInt32 (), Is.EqualTo (2));
		Assert.That (request.RootElement.GetProperty ("osTimezone").GetString (), Is.EqualTo ("+01:00"));
		}
	[Test]
	public async Task HwsHistoryUsesDedicatedCostAndTemperatureNames ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"historyDataList\":[{\"dataTime\":\"20261109\",\"consumption\":2.5,\"cost\":0.75,\"tankTemperature\":55,\"outdoorTemperature\":0}]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		HotWaterEnergyHistoryEntry? entry = await client.GetHotWaterDailyEnergyAsync ("fixture", new DateTime (2026, 11, 9), TimeSpan.Zero);
		Assert.That (entry!.Cost, Is.EqualTo (0.75));
		Assert.That (entry.TankTemperature, Is.EqualTo (55));
		Assert.That (entry.OutdoorTemperature, Is.Zero);
		Assert.That (entry.ReportedTotal, Is.EqualTo (2.5));
		Assert.That (JsonSerializer.Serialize (entry, _json), Does.Not.Contain ("\"outdoorTemp\":"));
		Assert.That (handler._requests[0].Uri.AbsolutePath, Is.EqualTo ("/hphw/deviceHistoryData"));
		}
	[Test]
	public async Task MissingDailyEntryRemainsUnknown ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"historyDataList\":[]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		Assert.That (await client.GetHotWaterDailyEnergyAsync ("fixture", new DateTime (2026, 11, 9), TimeSpan.Zero), Is.Null);
		}
	[Test]
	public void EnergyInferenceHandlesIntervalsResetStaleValuesAndClamping ()
		{
		var tracker = new AcEnergyTracker ();
		var start = new DateTimeOffset (2026, 11, 9, 0, 0, 0, TimeSpan.Zero);
		Assert.That (tracker.Update (new ()
			{
			Consumption = 10,
			HeatingRatio = 0.6,
			CoolingRatio = 0.4
			}, start), Is.True);
		Assert.That (tracker.PowerWatts, Is.Null);
		_ = tracker.Update (new ()
			{
			Consumption = 11
			}, start.AddMinutes (30));
		Assert.That (tracker.PowerWatts, Is.EqualTo (2000));
		Assert.That (tracker.HeatingPowerWatts, Is.EqualTo (1200));
		Assert.That (tracker.CoolingPowerWatts, Is.EqualTo (800));
		_ = tracker.Update (new ()
			{
			Consumption = 11
			}, start.AddMinutes (55));
		Assert.That (tracker.PowerWatts, Is.Zero);
		Assert.That (tracker.HeatingPowerWatts, Is.Zero);
		_ = tracker.Update (new ()
			{
			Consumption = 0.5
			}, start.AddMinutes (60));
		Assert.That (tracker.PowerWatts, Is.EqualTo (1000));
		_ = tracker.Update (new ()
			{
			Consumption = 1000
			}, start.AddMinutes (61));
		Assert.That (tracker.PowerWatts, Is.EqualTo (5000));
		_ = tracker.Update (new ()
			{
			Consumption = double.NaN
			}, start.AddMinutes (62));
		Assert.That (tracker.Consumption, Is.EqualTo (1000));
		_ = Assert.Throws<ArgumentOutOfRangeException> (() => tracker.Update (new (), start));
		}
	[Test]
	public void EnergyInferenceDoesNotInventMissingChannels ()
		{
		var tracker = new AcEnergyTracker ();
		DateTimeOffset start = DateTimeOffset.UtcNow;
		_ = tracker.Update (new ()
			{
			Consumption = 1
			}, start);
		_ = tracker.Update (new ()
			{
			Consumption = 1.5
			}, start.AddHours (1));
		Assert.That (tracker.PowerWatts, Is.EqualTo (500));
		Assert.That (tracker.HeatingPowerWatts, Is.Null);
		Assert.That (tracker.CoolingConsumption, Is.Null);
		}
	[TestCase ("4", 0, AcMode.Cool, 0)]
	[TestCase ("4", 3, AcMode.Cool, 4)]
	[TestCase ("4", 3, AcMode.Fan, 3)]
	[TestCase ("1", 0, AcMode.Cool, 6)]
	public void CacFanAndEconomyCapabilitiesMatchUpstream (string type, int steps, AcMode mode, int expectedSpeeds)
		{
		var capabilities = new AcCapabilities (new ()
			{
			DeviceType = type
			}, new ()
				{
				FanSpeedSteps = steps,
				SupportsPowerful = true,
				SupportsQuiet = true,
				Parameters = new ()
					{
					Mode = mode
					}
				});
		Assert.That (capabilities.SupportedFanSpeeds.Count, Is.EqualTo (expectedSpeeds));
		Assert.That (capabilities.SupportsQuiet, Is.EqualTo (type == "1"));
		Assert.That (capabilities.SupportedEcoModes.Count, Is.EqualTo (type == "1" ? 3 : 1));
		}
	[TestCase ("7", 1, true)]
	[TestCase ("1", 1, false)]
	[TestCase ("4", 0, false)]
	[TestCase ("1", 0, true)]
	public void HorizontalSwingHonorsTypeSevenException (string type, int direction, bool expected)
		{
		var capability = new AcCapabilities (new ()
			{
			DeviceType = type
			}, new ()
				{
				SupportsHorizontalSwing = true,
				Parameters = new ()
					{
					HorizontalSwing = 2,
					AirDirection = direction
					}
				});
		Assert.That (capability.HasHorizontalSwing, Is.EqualTo (expected));
		}
	[Test]
	public void DiscoveryIdentityClassificationAndUnavailableTemperatureAreTyped ()
		{
		Assert.That (new DeviceInfo { DeviceGuid = "abc" }.StableId, Is.EqualTo ("900150983cd24fb0d6963f7d28e17f72"));
		Assert.That (new DeviceInfo { DeviceGuid = "abc", HashId = "server-id" }.StableId, Is.EqualTo ("server-id"));
		Assert.That (new DeviceInfo { AquareaTank = new () }.Family, Is.EqualTo (DeviceFamily.Aquarea));
		AcZone zone = JsonSerializer.Deserialize<AcZone> ("{\"zoneId\":1,\"zoneTemperature\":-255}", _json)!;
		Assert.That (zone.Temperature, Is.Null);
		AcParameters values = JsonSerializer.Deserialize<AcParameters> ("{\"temperatureSet\":126,\"insideTemperature\":0,\"fanAutoMode\":3,\"airSwingLR\":2}", _json)!;
		Assert.That (values.TargetTemperature, Is.Null);
		Assert.That (values.IndoorTemperature, Is.Zero);
		Assert.That (values.HorizontalSwingMode, Is.EqualTo (-1));
		}
	[Test]
	public void ObservationSeparatesTimestampPollsFromStateChangesAndOwnsCopies ()
		{
		var observation = new DeviceObservation<AcSnapshot> (new ()
			{
			DeviceGuid = "fixture"
			});
		var sample = new AcSnapshot { Timestamp = 1000, Parameters = new () { Power = PowerState.Off } };
		DateTimeOffset first = DateTimeOffset.UtcNow;
		Assert.That (observation.Update (sample, first), Is.True);
		sample.Timestamp = 2000;
		Assert.That (observation.Update (sample, first.AddSeconds (10)), Is.False);
		Assert.That (observation.LastChanged, Is.EqualTo (first));
		sample.Parameters.Power = PowerState.On;
		Assert.That (observation.Update (sample, first.AddSeconds (20)), Is.True);
		AcSnapshot returned = observation.Current!;
		returned.Parameters!.Power = PowerState.Off;
		Assert.That (observation.Current!.Parameters!.Power, Is.EqualTo (PowerState.On));
		Assert.That (observation.Current.Timestamp, Is.EqualTo (2000));
		}
	[Test]
	public void BuilderComposesSwingZonesAndExplicitPowerPolicyWithoutAliasing ()
		{
		var snapshot = new AcSnapshot { Parameters = new () { Power = PowerState.Off, FanAutoMode = 0 } };
		var builder = new AcCommandBuilder (snapshot, false);
		_ = builder.SetTargetTemperature (22).SetEcoMode (EcoMode.Quiet).SetHorizontalSwing (2).SetVerticalSwing (-1).SetZonePower (1, PowerState.On).SetZoneDamper (1, 45);
		AcUpdate changes = builder.Build ();
		Assert.That (changes.Power, Is.Null);
		Assert.That (changes.FanAutoMode, Is.EqualTo (2));
		Assert.That (changes.Zones, Has.Count.EqualTo (1));
		Assert.That (changes.Zones![0].DamperLevel, Is.EqualTo (45));
		changes.TargetTemperature = 26;
		Assert.That (builder.Build ().TargetTemperature, Is.EqualTo (22));
		Assert.That (builder.SetIAutoX (2).Build ().Power, Is.EqualTo (PowerState.On));
		Assert.That (builder.Build ().Mode, Is.EqualTo (AcMode.Auto));
		Assert.That (snapshot.Parameters.Power, Is.EqualTo (PowerState.Off));
		}
	[Test]
	public async Task BufferedSettingsMergeOnlyOnExplicitPowerOnAndAreNotReplayed ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		using var controller = new AcController (client, "fixture", false);
		var off = new AcSnapshot { Parameters = new () { Power = PowerState.Off } };
		Assert.That (await controller.ApplyAsync (off, new ()
			{
			TargetTemperature = 22
			}), Is.False);
		Assert.That (await controller.ApplyAsync (off, new ()
			{
			TargetTemperature = 23,
			Fan = FanSpeed.Medium
			}), Is.False);
		Assert.That (handler._requests, Is.Empty);
		Assert.That (controller.HasPendingChanges, Is.True);
		Assert.That (await controller.ApplyAsync (off, new ()
			{
			Power = PowerState.On,
			TargetTemperature = 24
			}), Is.True);
		using JsonDocument request = JsonDocument.Parse (handler._requests[0].Body!);
		Assert.That (request.RootElement.GetProperty ("parameters").GetProperty ("temperatureSet").GetDouble (), Is.EqualTo (24));
		Assert.That (request.RootElement.GetProperty ("parameters").GetProperty ("fanSpeed").GetInt32 (), Is.EqualTo (3));
		Assert.That (controller.HasPendingChanges, Is.False);
		handler.Reply ((_, _) => throw new HttpRequestException ("Fixture acknowledgement lost."));
		_ = await controller.ApplyAsync (off, new ()
			{
			TargetTemperature = 25
			});
		_ = await Assert.ThrowsAsync<HttpRequestException> (async () => await controller.ApplyAsync (off, new () { Power = PowerState.On }));
		Assert.That (controller.HasPendingChanges, Is.False);
		Assert.That (handler._requests.Count, Is.EqualTo (2));
		}
	[Test]
	public async Task PendingPoliciesExcludeTurkeyAndAlreadyAcceptedVersions ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"agreementList\":[{\"type\":\"1\",\"version\":\"a\"},{\"type\":2,\"version\":\"b\"},{\"type\":3,\"version\":\"c\"},{\"type\":4,\"version\":\"d\"}]}");
		handler.Reply ("{\"agreementList\":[{\"type\":1,\"version\":\"a\"},{\"type\":2,\"version\":\"old\"}]}");
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.EnsureAgreementsAcceptedAsync ();
		using JsonDocument request = JsonDocument.Parse (handler._requests[2].Body!);
		JsonElement accepted = request.RootElement.GetProperty ("agreementList");
		Assert.That (accepted.GetArrayLength (), Is.EqualTo (2));
		Assert.That (accepted[0].GetProperty ("type").GetInt32 (), Is.EqualTo (2));
		Assert.That (accepted[1].GetProperty ("type").GetInt32 (), Is.EqualTo (4));
		Assert.That (handler._requests[2].Method, Is.EqualTo ("PUT"));
		}
	[Test]
	public async Task FilteredAndLegacyAgreementRoutesUseTheirOwnWirePaths ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"agreementList\":[]}");
		handler.Reply ("{\"agreementList\":[]}");
		handler.Reply ("{\"agreementStatus\":1}");
		handler.Reply ("{}");
		handler.Reply ("{\"code\":4300}", HttpStatusCode.Forbidden);
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await client.GetAgreementDocumentsAsync (2, true, 3);
		_ = await client.GetAgreementStatusAsync (4);
		Assert.That (await client.GetLegacyAgreementStatusAsync (1), Is.EqualTo (1));
		await client.AcceptLegacyAgreementAsync (2);
		Assert.That (handler._requests[0].Uri.Query, Does.Contain ("type=2").And.Contain ("includeContent=1"));
		Assert.That (handler._requests[1].Uri.Query, Is.EqualTo ("?type=4"));
		Assert.That (handler._requests[2].Uri.AbsolutePath, Is.EqualTo ("/auth/agreement/status/1"));
		Assert.That (handler._requests[3].Uri.AbsolutePath, Is.EqualTo ("/auth/agreement/status/"));
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.GetLegacyAgreementStatusAsync (1));
		Assert.That (handler._requests.Count, Is.EqualTo (5), "A retired route must fail explicitly rather than changing another policy route.");
		}
	[Test]
	public async Task LegacyAquareaCookiesAreIsolatedAndCallerCookiesAreNotModified ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ((_, _) =>
			{
				var response = new HttpResponseMessage (HttpStatusCode.OK) { Content = new StringContent ("{\"value\":3}", Encoding.UTF8, "application/json") };
				_ = response.Headers.TryAddWithoutValidation ("Set-Cookie", "fixtureweb=retained; Path=/; Secure");
				return Task.FromResult (response);
			});
		handler.Reply ("{\"value\":4}");
		handler.Reply ("{\"groupList\":[]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		var cookies = new Dictionary<string, string> { ["language"] = "en" };
		Assert.That ((await client.GetLegacyAquareaAsync<LegacyValue> ("/read", cookies)).Value, Is.EqualTo (3));
		Assert.That ((await client.PostLegacyAquareaAsync<LegacyValue> ("/write")).Value, Is.EqualTo (4));
		_ = await client.GetGroupsAsync ();
		Assert.That (cookies.Count, Is.EqualTo (1));
		Assert.That (handler._requests[0].Headers["Cookie"], Does.Contain ("accessToken=fake-access-never-live"));
		Assert.That (handler._requests[1].Headers["Cookie"], Does.Contain ("fixtureweb=retained"));
		Assert.That (handler._requests[1].Body, Is.Empty);
		Assert.That (handler._requests[2].Headers.ContainsKey ("Cookie"), Is.False);
		}
	[TestCase ("https://untrusted.invalid/redirect")]
	[TestCase ("//untrusted.invalid/redirect")]
	public async Task LegacyAquareaCannotSendTokensToAnotherOrigin (string path)
		{
		var handler = new ScriptedHandler ();
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ArgumentException> (async () => await client.GetLegacyAquareaAsync<LegacyValue> (path));
		Assert.That (handler._requests, Is.Empty);
		}
	[Test]
	public async Task VersionDiscoveryCachesFallsBackExpiresAndKeepsCloudWritesExplicit ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("[\"5.1.2\"]");
		using var http = new HttpClient (handler);
		using var discovery = new AppVersionDiscovery (http);
		Assert.That ((await discovery.GetAsync ()).Version, Is.EqualTo ("5.1.2"));
		Assert.That ((await discovery.GetAsync ()).Version, Is.EqualTo ("5.1.2"));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		handler.Reply ("unavailable", HttpStatusCode.ServiceUnavailable);
		handler.Reply ("<meta itemprop='softwareVersion' content='5.1.3'/>");
		Assert.That ((await discovery.RefreshAsync ()).Source.Host, Is.EqualTo ("www.appbrain.com"));
		var stale = new AppVersionObservation { Version = "4.0.0", Source = new ("https://play.google.com/"), ObservedAt = DateTimeOffset.UtcNow.AddDays (-3) };
		using var restored = new AppVersionDiscovery (http, stale);
		handler.Reply ("[\"5.1.4\"]");
		Assert.That ((await restored.GetAsync ()).Version, Is.EqualTo ("5.1.4"));
		handler.Reply ("[\"5.1.5\"]");
		var cloud = new ScriptedHandler ();
		cloud.Reply ("{\"groupList\":[]}");
		using ComfortCloudClient client = await Fixture.Client (cloud);
		_ = await client.UpdateAppVersionAsync (discovery);
		Assert.That (cloud._requests, Is.Empty);
		_ = await client.GetGroupsAsync ();
		Assert.That (cloud._requests[0].Headers["x-app-version"], Is.EqualTo ("5.1.5"));
		}
	[Test]
	public async Task CommunityVersionIsExplicitAndFailedRefreshRetainsPriorObservation ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"files\":{\"comfort-cloud-version\":{\"content\":\"5.1.2\\n\"}}}");
		using var http = new HttpClient (handler);
		using var discovery = new AppVersionDiscovery (http);
		Assert.That ((await discovery.ReadCommunityGistAsync ()).Version, Is.EqualTo ("5.1.2"));
		handler.Reply ("not a version");
		handler.Reply ("not a version");
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await discovery.RefreshAsync ());
		Assert.That (discovery.Current!.Version, Is.EqualTo ("5.1.2"));
		}
	private sealed class LegacyValue
		{
		[JsonPropertyName ("value")]
		public int Value
			{
			get; set;
			}
		}
	}