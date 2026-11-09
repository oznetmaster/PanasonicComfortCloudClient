// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

using NUnit.Framework;

using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class ScheduleTests
	{
	[Test]
	public async Task DeviceScopedScheduleMayOmitRepeatedGuid ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"devWeeklyTimerList\":[{\"avlFlg\":false,\"weeklyTimerList\":[]}]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		AcSchedule? schedule = await client.GetAcScheduleAsync ("requested");
		Assert.That (schedule!.DeviceGuid, Is.EqualTo ("requested"));
		}

	private static AquareaSchedule Schedule () => new ()
		{
		Timer = new ()
			{
			Operation = 0,
			Days = [.. Enumerable.Range (0, 7).Select (_ => new WeeklyTimerDay<AquareaTimerPattern> { Patterns = []
})]
			}
		};

	[Test]
	public async Task AquareaScheduleUsesProxyRetainsActivationAndPadsDays ()
		{
		AquareaSchedule schedule = Schedule ();
		schedule.Timer!.Days![0].Patterns!.Add (new ()
			{
			StartTime = "06:30",
			Mode = AquareaMode.Heat,
			Zone1Power = PowerState.On
			});
		var handler = new ScriptedHandler ();
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.SetAquareaScheduleAsync ("gateway", schedule);
		using var json = JsonDocument.Parse (handler._requests[0].Body!);
		JsonElement body = json.RootElement.GetProperty ("bodyParam");
		Assert.That (json.RootElement.GetProperty ("apiName").GetString (), Is.EqualTo ("/remote/v1/api/data/weeklyTimer"));
		Assert.That (body.GetProperty ("weeklyTimer").GetProperty ("operation").GetInt32 (), Is.EqualTo (0));
		Assert.That (body.GetProperty ("weeklyTimer").GetProperty ("timer").GetArrayLength (), Is.EqualTo (7));
		Assert.That (body.GetProperty ("weeklyTimer").GetProperty ("timer")[0].GetProperty ("patternList").GetArrayLength (), Is.EqualTo (6));
		Assert.That (schedule.Timer.Days[0].Patterns!.Count, Is.EqualTo (1), "Normalization must not mutate caller input.");
		}

	[TestCase (0)]
	[TestCase (6)]
	[TestCase (8)]
	public async Task IncompleteOrOversizeWeekNeverSends (int count)
		{
		AquareaSchedule schedule = Schedule ();
		schedule.Timer!.Days = [.. Enumerable.Range (0, count).Select (_ => new WeeklyTimerDay<AquareaTimerPattern> { Patterns = []
})];
		var handler = new ScriptedHandler ();
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ArgumentException> (async () => await client.SetAquareaScheduleAsync ("gateway", schedule));
		Assert.That (handler._requests, Is.Empty);
		}

	[TestCase ("24:00")]
	[TestCase ("7:30")]
	[TestCase ("12:99")]
	public async Task InvalidScheduleTimeNeverSends (string time)
		{
		AquareaSchedule schedule = Schedule ();
		schedule.Timer!.Days![0].Patterns!.Add (new ()
			{
			StartTime = time
			});
		var handler = new ScriptedHandler ();
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ArgumentException> (async () => await client.SetAquareaScheduleAsync ("gateway", schedule));
		Assert.That (handler._requests, Is.Empty);
		}

	[Test]
	public async Task DuplicateScheduleTimeNeverSends ()
		{
		AquareaSchedule schedule = Schedule ();
		schedule.Timer!.Days![0].Patterns = [new () { StartTime = "07:00"
}, new () { StartTime = "07:00"
}];
		var handler = new ScriptedHandler ();
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ArgumentException> (async () => await client.SetAquareaScheduleAsync ("gateway", schedule));
		Assert.That (handler._requests, Is.Empty);
		}

	[Test]
	public async Task UnknownTimerFieldsAndModesSurviveReadWrite ()
		{
		AquareaSchedule schedule = Schedule ();
		schedule.Timer!.Days![0].Patterns = [new () { StartTime = "07:00", Mode = (AquareaMode)99
}];
		var text = JsonSerializer.Serialize (schedule, ComfortCloudJson.CreateOptions ()).Replace ("\"operation\":0", "\"operation\":0,\"futureField\":17");
		var handler = new ScriptedHandler ();
		handler.Reply (text);
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		AquareaSchedule read = await client.GetAquareaScheduleAsync ("gateway");
		await client.SetAquareaScheduleAsync ("gateway", read);
		using var json = JsonDocument.Parse (handler._requests[1].Body!);
		JsonElement timer = json.RootElement.GetProperty ("bodyParam").GetProperty ("weeklyTimer");
		Assert.That (timer.GetProperty ("futureField").GetInt32 (), Is.EqualTo (17));
		Assert.That (timer.GetProperty ("timer")[0].GetProperty ("patternList")[0].GetProperty ("mode").GetInt32 (), Is.EqualTo (99));
		}

	[Test]
	public async Task AcScheduleSelectsRequestedDeviceInsteadOfFirstEntry ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"devWeeklyTimerList\":[{\"deviceGuid\":\"other\"},{\"deviceGuid\":\"wanted\",\"avlFlg\":false,\"weeklyTimerList\":[]}]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		AcSchedule? schedule = await client.GetAcScheduleAsync ("wanted");
		Assert.That (schedule!.DeviceGuid, Is.EqualTo ("wanted"));
		Assert.That (schedule.Enabled, Is.False);
		}

	[Test]
	public async Task AgreementDocumentsUseV2AndDoNotAutoAccept ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"agreementList\":[{\"type\":\"1\",\"version\":\"2026\",\"content\":\"Review this policy\"}]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		IReadOnlyList<AgreementDocument> documents = await client.GetAgreementDocumentsAsync (true);
		Assert.That (documents[0].Type, Is.EqualTo (1));
		Assert.That (documents[0].Content, Is.EqualTo ("Review this policy"));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		Assert.That (handler._requests[0].Method, Is.EqualTo ("GET"));
		Assert.That (handler._requests[0].Uri.AbsolutePath, Is.EqualTo ("/auth/v2/agreement/documents"));
		}
	}