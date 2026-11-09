// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using NUnit.Framework;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class ControlRestorationTests
	{
	[Test]
	public async Task SuccessfulQuietChangeIsConfirmedAndRestored ()
		{
		using var state = new Simulation ();
		using ComfortCloudClient client = await Fixture.Client (state._handler);
		await state.Run (client);
		Assert.That (state._writes, Is.EqualTo (2));
		Assert.That (state._quiet, Is.Zero);
		Assert.That (state.JournalRestored (), Is.True);
		}

	[TestCase ("lost-ack")]
	[TestCase ("failed-observation")]
	public async Task UncertainChangeIsNeverReplayedButOriginalIsRestored (string fault)
		{
		using var state = new Simulation (fault);
		using ComfortCloudClient client = await Fixture.Client (state._handler);
		_ = await Assert.ThrowsAsync<InvalidOperationException> (async () => await state.Run (client));
		Assert.That (state._writes, Is.EqualTo (2), "One change and one restoration; no replay of the uncertain change.");
		Assert.That (state._quiet, Is.Zero);
		Assert.That (state.JournalRestored (), Is.True);
		}

	[Test]
	public async Task FailedRestorationBlocksAllLaterControlTests ()
		{
		using var state = new Simulation ("restore-rejected");
		using ComfortCloudClient client = await Fixture.Client (state._handler);
		_ = await Assert.ThrowsAsync<AggregateException> (async () => await state.Run (client));
		Assert.That (state.JournalRestored (), Is.False);
		int count = state._handler._requests.Count;
		_ = await Assert.ThrowsAsync<InvalidOperationException> (async () => await state.Run (client));
		Assert.That (state._handler._requests.Count, Is.EqualTo (count), "Blocked before another HTTP operation.");
		Assert.That (state._writes, Is.EqualTo (2));
		}

	[Test]
	public async Task ThirdPartyTargetValueIsNotOverwrittenDuringRestoration ()
		{
		using var state = new Simulation ("external-value");
		using ComfortCloudClient client = await Fixture.Client (state._handler);
		_ = await Assert.ThrowsAsync<AggregateException> (async () => await state.Run (client));
		Assert.That (state._quiet, Is.EqualTo (2));
		Assert.That (state._writes, Is.EqualTo (1));
		Assert.That (state.JournalRestored (), Is.False);
		}

	private sealed class Simulation : IDisposable
		{
		internal readonly ScriptedHandler _handler = new ();
		private readonly string _fault, _directory;
		private bool _faulted;
		internal int _quiet, _writes;
		internal Simulation (string fault = "")
			{
			_fault = fault;
			_directory = Path.Combine (Path.GetTempPath (), "PanasonicRoundTripTests-" + Guid.NewGuid ().ToString ("N"));
			for (int i = 0; i < 60; i++)
				{
				_handler.Reply (Reply);
				}
			}
		private Task<HttpResponseMessage> Reply (CapturedRequest request, CancellationToken token)
			{
			using var body = JsonDocument.Parse (request.Body!);
			if (body.RootElement.GetProperty ("apiName").GetString ()!.Contains ("weeklyTimer"))
				{
				return Result ("{\"weeklyTimer\":{\"operation\":0,\"timer\":[]}}");
				}

			if (body.RootElement.TryGetProperty ("bodyParam", out JsonElement control))
				{
				int value = control.GetProperty ("quietMode").GetInt32 ();
				_writes++;
				if (value == 0 && _fault == "restore-rejected")
					{
					return Result ("{\"code\":5005}", HttpStatusCode.ServiceUnavailable);
					}

				_quiet = value;
				return value == 1 && _fault == "lost-ack"
					? throw new HttpRequestException ("Simulated acknowledgement loss after device applied the change.")
					: Result ("{\"result\":0}");
				}

			if (_writes == 1 && !_faulted && _fault == "failed-observation")
				{
				_faulted = true;
				throw new HttpRequestException ("Simulated read failure after acknowledgement.");
				}

			if (_writes == 1 && _fault == "external-value")
				{
				_quiet = 2;
				}

			return Result ("{\"ownerFlg\":true,\"status\":{\"uncontrollableTaw1Flg\":false,\"quietMode\":" + _quiet + ",\"operationMode\":1,\"operationStatus\":1,\"zoneStatus\":[{\"zoneId\":1,\"operationStatus\":1}],\"tankStatus\":{\"operationStatus\":1,\"heatSet\":59,\"heatMin\":40,\"heatMax\":65}}}");
			}
		private static Task<HttpResponseMessage> Result (string body, HttpStatusCode status = HttpStatusCode.OK) => Task.FromResult (new HttpResponseMessage (status)
			{
			Content = new StringContent (body, Encoding.UTF8, "application/json")
			});
		internal Task Run (ComfortCloudClient client) => AquareaControlRoundTrip.Run (client, "fake-gateway", "quiet", _directory, _ =>
		{
		}, TimeSpan.Zero, TimeSpan.FromSeconds (2));
		internal bool JournalRestored () => JsonSerializer.Deserialize<AquareaControlRoundTrip.Journal> (File.ReadAllText (Path.Combine (_directory, "control-journal.json")))!.Restored;
		public void Dispose ()
			{
			var root = Path.GetFullPath (Path.GetTempPath ()).TrimEnd (Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
			var path = Path.GetFullPath (_directory);
			if (!path.StartsWith (root, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName (path).StartsWith ("PanasonicRoundTripTests-", StringComparison.Ordinal))
				{
				throw new InvalidOperationException ("Unexpected temporary test directory.");
				}

			if (Directory.Exists (path))
				{
				Directory.Delete (path, true);
				}
			}
		}
	}