// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;

using NUnit.Framework;

using PanasonicComfortCloudClient.Models;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class AquareaControlTests
	{
	[TestCase (1, 2)]
	[TestCase (2, 3)]
	[TestCase (3, 8)]
	[TestCase (4, 8)]
	public async Task EnablingTankResendsCurrentModeWithControlEncoding (int reportedMode, int controlMode)
		{
		var handler = new ScriptedHandler ();
		handler.Reply (Fixture.AQUAREA_STATUS.Replace ("\"operationMode\":1", "\"operationMode\":" + reportedMode));
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Tank = new ()
				{
				Power = PowerState.On
				}
			});
		using var request = JsonDocument.Parse (handler._requests[1].Body!);
		JsonElement body = request.RootElement.GetProperty ("bodyParam");
		Assert.That (body.GetProperty ("operationMode").GetInt32 (), Is.EqualTo (controlMode));
		Assert.That (body.GetProperty ("zoneStatus")[0].GetProperty ("operationStatus").GetInt32 (), Is.EqualTo (1));
		Assert.That (body.GetProperty ("zoneStatus")[1].GetProperty ("operationStatus").GetInt32 (), Is.EqualTo (0));
		}

	[Test]
	public async Task EnablingBranchWithUnknownModeDoesNotSendControl ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply (Fixture.AQUAREA_STATUS.Replace ("\"operationMode\":1", "\"operationMode\":99"));
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Zones = [new () { Id = 2, Power = PowerState.On
}]
			}));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[TestCase (2)]
	[TestCase (99)]
	public async Task InvalidBooleanControlNeverSends (int value)
		{
		var handler = new ScriptedHandler ();
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ArgumentException> (async () => await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			ForceHotWater = (PowerState)value
			}));
		Assert.That (handler._requests, Is.Empty);
		}

	[Test]
	public async Task ChangingOneZonePreservesOtherZoneAndTank ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply (Fixture.AQUAREA_STATUS);
		handler.Reply ("{\"result\":0}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Zones = [new () { Id = 1, HeatSetpoint = 23
}]
			});
		Assert.That (handler._requests.Count, Is.EqualTo (2));
		using var request = JsonDocument.Parse (handler._requests[1].Body!);
		JsonElement body = request.RootElement.GetProperty ("bodyParam");
		Assert.That (body.GetProperty ("zoneStatus")[0].GetProperty ("heatSet").GetDouble (), Is.EqualTo (23));
		Assert.That (body.GetProperty ("zoneStatus")[1].GetProperty ("operationStatus").GetInt32 (), Is.EqualTo (0));
		Assert.That (body.GetProperty ("tankStatus").GetProperty ("operationStatus").GetInt32 (), Is.EqualTo (0));
		Assert.That (body.GetProperty ("operationStatus").GetInt32 (), Is.EqualTo (1));
		}

	[Test]
	public async Task DisablingLastActiveZoneTurnsWholeDeviceOff ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply (Fixture.AQUAREA_STATUS);
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Zones = [new () { Id = 1, Power = PowerState.Off
}]
			});
		using var request = JsonDocument.Parse (handler._requests[1].Body!);
		Assert.That (request.RootElement.GetProperty ("bodyParam").GetProperty ("operationStatus").GetInt32 (), Is.EqualTo (0));
		}

	[Test]
	public async Task ExplicitWholeDeviceOffDisablesEveryBranch ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply (Fixture.AQUAREA_STATUS);
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Power = PowerState.Off
			});
		using var request = JsonDocument.Parse (handler._requests[1].Body!);
		JsonElement body = request.RootElement.GetProperty ("bodyParam");
		Assert.That (body.GetProperty ("operationStatus").GetInt32 (), Is.EqualTo (0));
		Assert.That (body.GetProperty ("zoneStatus")[0].GetProperty ("operationStatus").GetInt32 (), Is.EqualTo (0));
		Assert.That (body.GetProperty ("tankStatus").GetProperty ("operationStatus").GetInt32 (), Is.EqualTo (0));
		}

	[Test]
	public async Task ControlModeUsesWriteEncoding ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply (Fixture.AQUAREA_STATUS);
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Mode = AquareaMode.Heat
			});
		using var request = JsonDocument.Parse (handler._requests[1].Body!);
		Assert.That (request.RootElement.GetProperty ("bodyParam").GetProperty ("operationMode").GetInt32 (), Is.EqualTo (2));
		}

	[Test]
	public async Task UnknownBranchStatePreventsAnyWrite ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"status\":{\"zoneStatus\":[{\"zoneId\":1}],\"tankStatus\":{\"operationStatus\":0}}}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Mode = AquareaMode.Heat
			}));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[TestCase (9)]
	[TestCase (31)]
	[TestCase (double.NaN)]
	public async Task InvalidSetpointDoesNotReachControlEndpoint (double value)
		{
		var handler = new ScriptedHandler ();
		handler.Reply (Fixture.AQUAREA_STATUS);
		using ComfortCloudClient client = await Fixture.Client (handler);
		Exception? exception = await Assert.CatchAsync (async () => await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Zones = [new () { Id = 1, HeatSetpoint = value
}]
			}));
		Assert.That (exception, Is.Not.Null);
		Assert.That (handler._requests.Count, Is.LessThanOrEqualTo (1));
		}

	[Test]
	public async Task QuietOnlyDoesNotReadOrChangeBranches ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Quiet = QuietMode.Level2
			});
		using var request = JsonDocument.Parse (handler._requests[0].Body!);
		JsonElement body = request.RootElement.GetProperty ("bodyParam");
		Assert.That (body.GetProperty ("quietMode").GetInt32 (), Is.EqualTo (2));
		Assert.That (body.TryGetProperty ("operationStatus", out _), Is.False);
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[Test]
	public async Task FailedWriteIsNotRetried ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"code\":5005}", HttpStatusCode.ServiceUnavailable);
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Quiet = QuietMode.Level1
			}));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[Test]
	public async Task FaultAfterCancelledReadDoesNotSendControl ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply (async (_, token) =>
		{
			await Task.Delay (Timeout.Infinite, token);
			return null!;
		});
		using ComfortCloudClient client = await Fixture.Client (handler);
		using var timeout = new CancellationTokenSource (100);
		_ = await Assert.CatchAsync<OperationCanceledException> (async () => await client.UpdateAquareaAsync ("gateway", new AquareaUpdate
			{
			Mode = AquareaMode.Heat
			}, timeout.Token));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[Test]
	public async Task SettingAcTemperatureDoesNotImplicitlyTurnItOn ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.UpdateAcAsync ("ac", new AcUpdate
			{
			TargetTemperature = 23,
			IAutoX = 2
			});
		using var request = JsonDocument.Parse (handler._requests[0].Body!);
		Assert.That (request.RootElement.GetProperty ("parameters").TryGetProperty ("operate", out _), Is.False);
		Assert.That (request.RootElement.GetProperty ("parameters").GetProperty ("iAuto").GetInt32 (), Is.EqualTo (2));
		Assert.That (request.RootElement.GetProperty ("parameters").TryGetProperty ("iAutoX", out _), Is.False);
		}

	[Test]
	public async Task SwingChangePreservesAutomaticOtherAxis ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"parameters\":{\"fanAutoMode\":2}}");
		handler.Reply ("{}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.UpdateAcAsync ("ac", new AcUpdate
			{
			HorizontalSwing = -1
			});
		using var request = JsonDocument.Parse (handler._requests[1].Body!);
		Assert.That (request.RootElement.GetProperty ("parameters").GetProperty ("fanAutoMode").GetInt32 (), Is.EqualTo (0));
		Assert.That (request.RootElement.GetProperty ("parameters").TryGetProperty ("airSwingLR", out _), Is.False);
		}
	}