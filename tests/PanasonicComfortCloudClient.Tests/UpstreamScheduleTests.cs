// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using NUnit.Framework;

using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class UpstreamScheduleTests
	{
	[Test]
	public void IndividualAquareaZoneEditsPreserveOtherPatternSettings ()
		{
		var pattern = new AquareaTimerPattern { StartTime = "06:00", TankHeat = 55 };
		pattern.SetZone (1, PowerState.On, 22, 18);
		pattern.SetZonePower (1, PowerState.Off);
		Assert.That (pattern.ZoneHeat (1), Is.EqualTo (22));
		pattern.SetZoneHeat (1, 23);
		pattern.SetZoneCool (1, 19);
		Assert.That (pattern.ZonePower (1), Is.EqualTo (PowerState.Off));
		Assert.That (pattern.ZoneHeat (1), Is.EqualTo (23));
		Assert.That (pattern.TankHeat, Is.EqualTo (55));
		_ = Assert.Throws<ArgumentOutOfRangeException> (() => pattern.SetZonePower (3, PowerState.On));
		}
	private static readonly JsonSerializerOptions _json = ComfortCloudJson.CreateOptions ();
	private static readonly string[] _createdTimes = ["03:00", "07:30"];
	[Test]
	public async Task AquareaCreateModifyDeleteRoundTripPreservesOtherEntriesMetadataAndDisabledState ()
		{
		AquareaSchedule state = ScheduleEditing.CreateAquarea ();
		_ = state.Day (DayOfWeek.Monday).AddPattern (new ()
			{
			StartTime = "03:00",
			Mode = AquareaMode.Heat,
			Zone1Power = PowerState.On,
			Zone1Heat = 21
			});
		state = JsonSerializer.Deserialize<AquareaSchedule> (JsonSerializer.Serialize (state, _json).Replace ("\"operation\":0", "\"operation\":0,\"futureTimer\":17"), _json)!;
		var handler = new ScriptedHandler ();
		for (int request = 0; request < 10; request++)
			{
			handler.Reply ((captured, _) =>
			{
				using JsonDocument wire = JsonDocument.Parse (captured.Body!);
				JsonElement root = wire.RootElement;
				if (root.TryGetProperty ("requestMethod", out JsonElement method) && method.GetString () == "POST")
					{
					JsonElement timer = root.GetProperty ("bodyParam").GetProperty ("weeklyTimer");
					Assert.That (timer.GetProperty ("operation").GetInt32 (), Is.Zero, "The test must never activate its schedule.");
					state.Timer = JsonSerializer.Deserialize<AquareaWeeklySettings> (timer.GetRawText (), _json);
					return Task.FromResult (Reply ("{}"));
					}

				return Task.FromResult (Reply (JsonSerializer.Serialize (state, _json)));
			});
			}

		using ComfortCloudClient client = await Fixture.Client (handler);
		AquareaSchedule read = await client.GetAquareaScheduleAsync ("fixture-gateway");
		_ = read.Day (DayOfWeek.Monday).AddPattern (new ()
			{
			StartTime = "7:30",
			TankHeat = 59,
			TankPower = PowerState.On
			});
		await client.SetAquareaScheduleAsync ("fixture-gateway", read);
		read = await client.GetAquareaScheduleAsync ("fixture-gateway");
		Assert.That (read.Day (DayOfWeek.Monday).Patterns!.Where (p => !string.IsNullOrEmpty (p.StartTime)).Select (p => p.StartTime), Is.EqualTo (_createdTimes));
		using (JsonDocument submitted = JsonDocument.Parse (handler._requests[1].Body!))
			{
			JsonElement created = submitted.RootElement.GetProperty ("bodyParam").GetProperty ("weeklyTimer").GetProperty ("timer")[0].GetProperty ("patternList")[1];
			Assert.That (created.GetProperty ("tankTemperature").GetInt32 (), Is.EqualTo (59));
			Assert.That (created.TryGetProperty ("tankHeat", out _), Is.False);
			}

		_ = read.Day (DayOfWeek.Monday).AddPattern (new ()
			{
			StartTime = "07:30",
			TankHeat = 58,
			TankPower = PowerState.On
			});
		await client.SetAquareaScheduleAsync ("fixture-gateway", read);
		read = await client.GetAquareaScheduleAsync ("fixture-gateway");
		Assert.That (read.Day (DayOfWeek.Monday).Patterns!.Single (p => p.StartTime == "07:30").TankHeat, Is.EqualTo (58));
		Assert.That (read.Day (DayOfWeek.Monday).RemovePattern ("7:30"), Is.True);
		await client.SetAquareaScheduleAsync ("fixture-gateway", read);
		read = await client.GetAquareaScheduleAsync ("fixture-gateway");
		Assert.That (read.Day (DayOfWeek.Monday).Patterns!.Count (p => !string.IsNullOrEmpty (p.StartTime)), Is.EqualTo (1));
		Assert.That (read.Day (DayOfWeek.Monday).Patterns![0].Zone1Heat, Is.EqualTo (21));
		Assert.That (read.Enabled, Is.False);
		Assert.That (JsonSerializer.Serialize (read, _json), Does.Contain ("\"futureTimer\":17"));
		}

	[Test]
	public async Task AcCreateModifyDeleteRoundTripPreservesOtherDaysAndOptionalPatternFields ()
		{
		AcSchedule state = ScheduleEditing.CreateAc ("fixture-ac");
		_ = state.Day (DayOfWeek.Tuesday).AddPattern (new ()
			{
			StartTime = "18:00",
			Power = PowerState.On,
			Temperature = 23
			});
		var handler = new ScriptedHandler ();
		for (int request = 0; request < 8; request++)
			{
			handler.Reply ((captured, _) =>
			{
				if (captured.Method == "POST")
					{
					state = JsonSerializer.Deserialize<AcSchedule> (captured.Body!, _json)!;
					Assert.That (state.Enabled, Is.False);
					return Task.FromResult (Reply ("{}"));
					}

				return Task.FromResult (Reply ("{\"devWeeklyTimerList\":[" + JsonSerializer.Serialize (state, _json) + "]}"));
			});
			}

		using ComfortCloudClient client = await Fixture.Client (handler);
		AcSchedule read = (await client.GetAcScheduleAsync ("fixture-ac"))!;
		_ = read.Day (DayOfWeek.Monday).AddPattern (new ()
			{
			StartTime = "06:00",
			Power = PowerState.On,
			FanAutoMode = 3,
			HorizontalSwing = 2,
			Nanoe = 4,
			EcoNavi = 2
			});
		await client.SetAcScheduleAsync (read);
		read = (await client.GetAcScheduleAsync ("fixture-ac"))!;
		Assert.That (read.Day (DayOfWeek.Monday).Patterns![0].IAuto, Is.EqualTo (1));
		Assert.That (read.Day (DayOfWeek.Monday).Patterns![0].Nanoe, Is.EqualTo (4));
		read.Day (DayOfWeek.Monday).Patterns![0].Temperature = 22;
		await client.SetAcScheduleAsync (read);
		read = (await client.GetAcScheduleAsync ("fixture-ac"))!;
		Assert.That (read.Day (DayOfWeek.Monday).Patterns![0].Temperature, Is.EqualTo (22));
		read.Day (DayOfWeek.Monday).Clear ();
		await client.SetAcScheduleAsync (read);
		read = (await client.GetAcScheduleAsync ("fixture-ac"))!;
		Assert.That (read.Day (DayOfWeek.Monday).Patterns!.All (p => string.IsNullOrEmpty (p.StartTime)), Is.True);
		Assert.That (read.Day (DayOfWeek.Tuesday).Patterns![0].Temperature, Is.EqualTo (23));
		}

	[Test]
	public void PatternReplacementSortingLimitsAndIdentityDeletionMatchUpstream ()
		{
		WeeklyTimerDay<AcTimerPattern> day = ScheduleEditing.CreateAc ("fixture").Day (DayOfWeek.Sunday);
		for (int hour = 6; hour > 0; hour--)
			{
			_ = day.AddPattern (new ()
				{
				StartTime = $"0{hour}:00",
				Temperature = hour
				});
			}

		Assert.That (day.Patterns![0].StartTime, Is.EqualTo ("01:00"));
		_ = day.AddPattern (new ()
			{
			StartTime = "01:00",
			Temperature = 22
			});
		Assert.That (day.Patterns!.Count, Is.EqualTo (6));
		Assert.That (day.Patterns[0].Temperature, Is.EqualTo (22));
		_ = Assert.Throws<ArgumentException> (() => day.AddPattern (new () { StartTime = "07:00" }));
		AcTimerPattern first = day.Patterns[0];
		Assert.That (day.RemovePattern (new AcTimerPattern { StartTime = first.StartTime }), Is.False);
		Assert.That (day.RemovePattern (first), Is.True);
		Assert.That (day.RemovePattern ("01:00"), Is.False);
		Assert.That (day.Weekday, Is.EqualTo (DayOfWeek.Sunday));
		}

	[Test]
	public async Task ReadNormalizesMissingDaysAndCrossGatewayWriteIsBlocked ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"weeklyTimer\":{\"operation\":0,\"timer\":[]}}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		AquareaSchedule schedule = await client.GetAquareaScheduleAsync ("original");
		Assert.That (schedule.Timer!.Days, Has.Count.EqualTo (7));
		Assert.That (schedule.Day (DayOfWeek.Friday).Patterns, Is.Empty);
		_ = await Assert.ThrowsAsync<ArgumentException> (async () => await client.SetAquareaScheduleAsync ("different", schedule));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[Test]
	public async Task HwsWeeklyControlsPreserveNumericHoursAndAvailabilityFlags ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		var days = Enumerable.Range (1, 7).Select (day => new HotWaterDay { Weekday = day, Mode = 0, FirstPeriod = new () { StartTime = 6, EndTime = 8 }, SecondPeriod = new () { SecondPeriodAvailable = 0, StartTime = 13, EndTime = 15, StartTemperature = 45, EndTemperature = 55 }, InhibitTime = new () { InhibitAvailable = 1, StartTime = 22, EndTime = 5 } }).ToList ();
		await client.UpdateHotWaterAsync ("fixture-hws", new ()
			{
			WeeklySettings = days
			});
		using JsonDocument wire = JsonDocument.Parse (handler._requests[0].Body!);
		JsonElement monday = wire.RootElement.GetProperty ("parameters").GetProperty ("weeklySettingInfo")[0];
		Assert.That (monday.GetProperty ("set1").GetProperty ("startTime").ValueKind, Is.EqualTo (JsonValueKind.Number));
		Assert.That (monday.GetProperty ("set2").GetProperty ("set2Available").GetInt32 (), Is.Zero);
		Assert.That (monday.GetProperty ("inhibitTime").GetProperty ("inhibitTimeAvailable").GetInt32 (), Is.EqualTo (1));
		}
	private static HttpResponseMessage Reply (string body) => new (HttpStatusCode.OK) { Content = new StringContent (body, Encoding.UTF8, "application/json") };
	}