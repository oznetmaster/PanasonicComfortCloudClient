// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;

using NUnit.Framework;

using PanasonicComfortCloudClient.Authentication;
using PanasonicComfortCloudClient.Concurrency;
using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture, NonParallelizable]
public sealed class ConcurrencyTests
	{
	private static readonly JsonSerializerOptions _json = ComfortCloudJson.CreateOptions ();
	private static TaskCompletionSource<bool> Signal () => new (TaskCreationOptions.RunContinuationsAsynchronously);
	private static async Task<Exception?> FailureOf (Task task)
		{
		try
			{
			await task.WaitAsync (TimeSpan.FromSeconds (10));
			return null;
			}
		catch (Exception error)
			{
			return error;
			}
		}
	private static void CanceledOrDisposed (Exception? error) => Assert.That (error,
		Is.InstanceOf<OperationCanceledException> ().Or.InstanceOf<ObjectDisposedException> ());
	[Test]
	public void RuntimeAndAtomicPublicationContract ()
		{
		Type? mono = Type.GetType ("Mono.Runtime");
		string? display = mono?.GetMethod ("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke (null, null) as string;
		TestContext.WriteLine ($"Concurrency runtime: {(mono is null ? ".NET" : "Mono")} {display}; CLR={Environment.Version}; pointer bytes={IntPtr.Size}");
		object first = new (), second = new ();
		object published = first;
		Volatile.Write (ref published, second);
		Assert.That (Volatile.Read (ref published), Is.SameAs (second));
		Assert.That (Interlocked.Exchange (ref published, first), Is.SameAs (second));
		Assert.That (Volatile.Read (ref published), Is.SameAs (first));
		}
	[Test]
	public async Task DisposingClientCancelsActiveAndQueuedReadsWithoutDisposingInjectedHttp ()
		{
		TaskCompletionSource<bool> started = Signal ();
		TaskCompletionSource<bool> release = Signal ();
		using var handler = new ConcurrentHandler (async (ignoredRequest, token) =>
			{
				_ = started.TrySetResult (true);
				_ = await release.Task;
				return Reply ("{\"parameters\":{\"operate\":1}}");
			});
		using var http = new HttpClient (handler);
		using ComfortCloudClient client = await Client (http);
		Task active = client.GetAcStatusAsync ("fixture");
		try
			{
			_ = await started.Task.WaitAsync (TimeSpan.FromSeconds (10));
			Task queued = client.GetAcStatusAsync ("fixture");
			client.Dispose ();
			_ = release.TrySetResult (true);
			CanceledOrDisposed (await FailureOf (active));
			CanceledOrDisposed (await FailureOf (queued));
			Assert.That (handler.Count, Is.EqualTo (1));
			Assert.That (handler.Disposals, Is.Zero);
			CanceledOrDisposed (await FailureOf (client.GetAcStatusAsync ("fixture")));
			}
		finally
			{
			_ = release.TrySetResult (true);
			}
		}
	[Test]
	public async Task LateRefreshBodyCannotPublishCredentialsAfterClientDisposal ()
		{
		using var stream = new GatedStream ("{\"access_token\":\"fake-replacement\",\"refresh_token\":\"fake-rotation\",\"expires_in\":3600}");
		using var handler = new ConcurrentHandler ((_, _) => Task.FromResult (new HttpResponseMessage (HttpStatusCode.OK) { Content = new StreamContent (stream) }));
		using var http = new HttpClient (handler);
		using ComfortCloudClient client = await Client (http);
		ComfortCloudSession? before = client.CurrentSession;
		Task refresh = client.RefreshSessionAsync ();
		try
			{
			_ = await stream.Started.Task.WaitAsync (TimeSpan.FromSeconds (10));
			client.Dispose ();
			_ = stream.Release.TrySetResult (true);
			CanceledOrDisposed (await FailureOf (refresh));
			Assert.That (client.CurrentSession, Is.SameAs (before));
			}
		finally
			{
			_ = stream.Release.TrySetResult (true);
			}
		}
	[Test]
	public async Task DisposingVersionDiscoveryCancelsWaitersAndRejectsLateCachePublication ()
		{
		var initial = new AppVersionObservation { Version = "1.0.0", Source = new ("https://fixture.invalid/"), ObservedAt = DateTimeOffset.UtcNow.AddDays (-3) };
		using var stream = new GatedStream ("[[\"9.8.7\"]]");
		using var handler = new ConcurrentHandler ((_, _) => Task.FromResult (new HttpResponseMessage (HttpStatusCode.OK) { Content = new StreamContent (stream) }));
		using var http = new HttpClient (handler);
		using var discovery = new AppVersionDiscovery (http, initial);
		Task first = discovery.RefreshAsync ();
		try
			{
			_ = await stream.Started.Task.WaitAsync (TimeSpan.FromSeconds (10));
			Task queued = discovery.RefreshAsync ();
			discovery.Dispose ();
			_ = stream.Release.TrySetResult (true);
			CanceledOrDisposed (await FailureOf (first));
			CanceledOrDisposed (await FailureOf (queued));
			Assert.That (discovery.Current, Is.SameAs (initial));
			Assert.That (handler.Count, Is.EqualTo (1), "Disposal must not start a fallback source or queued lookup.");
			Assert.That (handler.Disposals, Is.Zero);
			}
		finally
			{
			_ = stream.Release.TrySetResult (true);
			}
		}
	[Test]
	public async Task DisposedCloudClientDoesNotStartIndependentVersionLookup ()
		{
		using var handler = new ConcurrentHandler ((_, _) => Task.FromResult (Reply ("[[\"9.8.7\"]]")));
		using var http = new HttpClient (handler);
		using var discovery = new AppVersionDiscovery (http);
		using ComfortCloudClient client = await Client (http);
		client.Dispose ();
		CanceledOrDisposed (await FailureOf (client.UpdateAppVersionAsync (discovery)));
		Assert.That (handler.Count, Is.Zero);
		}
	[Test]
	public async Task ConcurrentPreferenceUpdatesAreMergedBeforeOneExplicitPowerOn ()
		{
		using var handler = new ConcurrentHandler ((_, _) => Task.FromResult (Reply ("{}")));
		using var http = new HttpClient (handler);
		using ComfortCloudClient client = await Client (http);
		for (int iteration = 0; iteration < 16; iteration++)
			{
			using var controller = new AcController (client, "fixture", false);
			var off = new AcSnapshot { Parameters = new () { Power = PowerState.Off } };
			Task<bool>[] updates =
				[
				Task.Run (() => controller.ApplyAsync (off, new () { TargetTemperature = 22 })),
				Task.Run (() => controller.ApplyAsync (off, new () { Fan = FanSpeed.Low }))
				];
			Assert.That (await Task.WhenAll (updates), Is.All.False);
			Assert.That (await controller.ApplyAsync (off, new ()
				{
				Power = PowerState.On
				}), Is.True);
			Assert.That (controller.HasPendingChanges, Is.False);
			using JsonDocument body = JsonDocument.Parse (handler.LastBody!);
			JsonElement settings = body.RootElement.GetProperty ("parameters");
			Assert.That (settings.GetProperty ("temperatureSet").GetDouble (), Is.EqualTo (22));
			Assert.That (settings.GetProperty ("fanSpeed").GetInt32 (), Is.EqualTo ((int)FanSpeed.Low));
			}

		Assert.That (handler.Count, Is.EqualTo (16));
		}
	[Test]
	public async Task ControllerDisposalCannotResurrectBufferedStateOrDisposeSharedClient ()
		{
		TaskCompletionSource<bool> started = Signal ();
		TaskCompletionSource<bool> release = Signal ();
		using var handler = new ConcurrentHandler (async (_, _) =>
			{
				_ = started.TrySetResult (true);
				_ = await release.Task;
				return Reply ("{\"parameters\":{\"operate\":1}}");
			});
		using var http = new HttpClient (handler);
		using ComfortCloudClient client = await Client (http);
		using var controller = new AcController (client, "fixture", false);
		var off = new AcSnapshot { Parameters = new () { Power = PowerState.Off } };
		Assert.That (await controller.ApplyAsync (off, new ()
			{
			TargetTemperature = 22
			}), Is.False);
		Task on = controller.ApplyAsync (off, new ()
			{
			Power = PowerState.On
			});
		try
			{
			_ = await started.Task.WaitAsync (TimeSpan.FromSeconds (10));
			Task queued = controller.ApplyAsync (off, new ()
				{
				Eco = EcoMode.Quiet
				});
			controller.Dispose ();
			Assert.That (controller.HasPendingChanges, Is.False);
			_ = release.TrySetResult (true);
			CanceledOrDisposed (await FailureOf (on));
			CanceledOrDisposed (await FailureOf (queued));
			Assert.That (controller.HasPendingChanges, Is.False);
			Assert.That ((await client.GetAcStatusAsync ("fixture")).Parameters!.Power, Is.EqualTo (PowerState.On));
			Assert.That (handler.Count, Is.EqualTo (2));
			}
		finally
			{
			_ = release.TrySetResult (true);
			}
		}
	[Test]
	public async Task StopDefersResourceCleanupUntilHolderAndCancellationCallbacksFinish ()
		{
		int cleanup = 0;
		using var gate = new AsyncOperationGate (typeof (ConcurrencyTests), () => Interlocked.Increment (ref cleanup));
		using AsyncOperationGate.Operation operation = await gate.EnterAsync (CancellationToken.None);
		using CancellationTokenRegistration callback = operation.Token.Register (() =>
			{
				operation.Dispose ();
				gate.Dispose ();
				Assert.That (Volatile.Read (ref cleanup), Is.Zero, "Cleanup cannot race an executing cancellation callback.");
			});
		gate.Dispose ();
		Assert.That (cleanup, Is.EqualTo (1));
		operation.Dispose ();
		gate.Dispose ();
		Assert.That (cleanup, Is.EqualTo (1));
		}
	[Test]
	public async Task ThrowingCancellationCallbackStillAllowsExactlyOnceCleanup ()
		{
		int cleanup = 0;
		using var gate = new AsyncOperationGate (typeof (ConcurrencyTests), () => Interlocked.Increment (ref cleanup));
		using AsyncOperationGate.Operation operation = await gate.EnterAsync (CancellationToken.None);
		using CancellationTokenRegistration callback = operation.Token.Register (() => throw new InvalidOperationException ("fixture callback"));
		_ = Assert.Throws<AggregateException> (gate.Dispose);
		Assert.That (cleanup, Is.Zero);
		operation.Dispose ();
		Assert.That (cleanup, Is.EqualTo (1));
		}
	[Test]
	public async Task StopAndWaiterAdmissionRaceNeverUsesDisposedSynchronizationResources ()
		{
		for (int iteration = 0; iteration < 8; iteration++)
			{
			int cleanup = 0;
			using var gate = new AsyncOperationGate (typeof (ConcurrencyTests), () => Interlocked.Increment (ref cleanup));
			using AsyncOperationGate.Operation holder = await gate.EnterAsync (CancellationToken.None);
			Task[] waiters = [.. Enumerable.Range (0, 16).Select (ignoredIndex => Task.Run (async () =>
				{
				try
					{
					using AsyncOperationGate.Operation entered = await gate.EnterAsync (CancellationToken.None);
					}
				catch (OperationCanceledException) { }
				catch (ObjectDisposedException error)
					{
					Assert.That (error.ObjectName, Is.EqualTo (typeof (ConcurrencyTests).FullName));
					}
				}))];
			gate.Dispose ();
			holder.Dispose ();
			await Task.WhenAll (waiters).WaitAsync (TimeSpan.FromSeconds (10));
			Assert.That (cleanup, Is.EqualTo (1));
			_ = Assert.Throws<ObjectDisposedException> (() => gate.Commit (() => 1));
			}
		}
	[Test]
	public async Task OAuthAttemptIsConsumedExactlyOnceAcrossThreads ()
		{
		var authorization = new OAuthAuthorizationRequest { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes (5) };
		int accepted = 0;
		await Task.WhenAll (Enumerable.Range (0, 32).Select (ignoredIndex => Task.Run (() =>
			{
				try
					{
					authorization.Consume ();
					_ = Interlocked.Increment (ref accepted);
					}
				catch (ComfortCloudException) { }
			})));
		Assert.That (accepted, Is.EqualTo (1));
		}
	[Test]
	public async Task LegacyChallengeIsConsumedExactlyOnceAcrossThreads ()
		{
		Guid attempt = Guid.NewGuid ();
		var challenge = new LegacyMfaChallenge (attempt, new ()
			{
			ExpiresAt = DateTimeOffset.UtcNow.AddMinutes (5)
			}, "fake-mfa", null);
		int accepted = 0;
		await Task.WhenAll (Enumerable.Range (0, 32).Select (ignoredIndex => Task.Run (() =>
			{
				try
					{
					challenge.Consume (attempt);
					_ = Interlocked.Increment (ref accepted);
					}
				catch (ComfortCloudException) { }
			})));
		Assert.That (accepted, Is.EqualTo (1));
		}
	[Test]
	public async Task EnergyTrackerChronologicalCheckAndUpdateAreOneAtomicTransition ()
		{
		var tracker = new AcEnergyTracker ();
		DateTimeOffset time = DateTimeOffset.UtcNow;
		_ = tracker.Update (new ()
			{
			Consumption = 1
			}, time);
		int accepted = 0;
		TaskCompletionSource<bool> start = Signal ();
		Task[] tasks = [.. Enumerable.Range (0, 32).Select (index => Task.Run (async () =>
			{
			_ = await start.Task;
			try
				{
				_ = tracker.Update (new () { Consumption = index + 2, HeatingRatio = 0.5, CoolingRatio = 0.5 }, time.AddMinutes (1));
				_ = Interlocked.Increment (ref accepted);
				}
			catch (ArgumentOutOfRangeException) { }
			}))];
		_ = start.TrySetResult (true);
		await Task.WhenAll (tasks);
		Assert.That (accepted, Is.EqualTo (1));
		Assert.That (tracker.PowerWatts, Is.InRange (0, AcEnergyTracker.MAXIMUM_POWER_WATTS));
		}
	[Test]
	public async Task ObservationReadersSeeIndependentCoherentSnapshotsDuringConcurrentUpdates ()
		{
		var observation = new DeviceObservation<AcSnapshot> (new ()
			{
			DeviceGuid = "fixture"
			});
		DateTimeOffset instant = DateTimeOffset.UtcNow;
		_ = observation.Update (new ()
			{
			Parameters = new ()
				{
				TargetTemperature = 1,
				IndoorTemperature = -1
				}
			}, instant);
		Task writer = Task.Run (async () =>
			{
				for (int index = 2; index < 130; index++)
					{
					double value = index / 10.0; // Stay clear of Panasonic's 126-degree unavailable sentinel.
					_ = observation.Update (new ()
						{
						Parameters = new ()
							{
							TargetTemperature = value,
							IndoorTemperature = -value
							}
						}, instant);
					await Task.Yield ();
					}
			});
		Task[] readers = [.. Enumerable.Range (0, 4).Select (ignoredIndex => Task.Run (() =>
			{
			for (int index = 0; index < 128; index++)
				{
				AcSnapshot snapshot = observation.Current!;
				Assert.That (snapshot.Parameters!.TargetTemperature + snapshot.Parameters.IndoorTemperature, Is.Zero);
				Assert.That (observation.ObservedAt, Is.EqualTo (instant));
				snapshot.Parameters.TargetTemperature = -999;
				}
			}))];
		await Task.WhenAll ([writer, .. readers]);
		Assert.That (observation.Current!.Parameters!.TargetTemperature, Is.EqualTo (12.9));
		}
	[Test]
	public async Task SessionPublicationPreservesWholeImmutableCredentialSnapshots ()
		{
		using var handler = new ConcurrentHandler ((_, _) => throw new InvalidOperationException ("No HTTP expected."));
		using var http = new HttpClient (handler);
		using ComfortCloudClient client = await Client (http);
		Task writer = Task.Run (async () =>
			{
				for (int index = 0; index < 128; index++)
					{
					string value = "fake-" + index;
					await client.RestoreSessionAsync (new ()
						{
						AccessToken = value,
						RefreshToken = value,
						ClientId = value,
						ExpiresAt = DateTimeOffset.UtcNow.AddHours (1)
						});
					await Task.Yield ();
					}
			});
		Task[] readers = [.. Enumerable.Range (0, 4).Select (ignoredIndex => Task.Run (() =>
			{
			for (int index = 0; index < 512; index++)
				{
				ComfortCloudSession snapshot = client.CurrentSession!;
				if (snapshot.AccessToken == "fake-access-never-live")
					continue;
				Assert.That (snapshot.RefreshToken, Is.EqualTo (snapshot.AccessToken));
				Assert.That (snapshot.ClientId, Is.EqualTo (snapshot.AccessToken)); } }))];
		await Task.WhenAll ([writer, .. readers]);
		Assert.That (handler.Count, Is.Zero);
		}
	private static async Task<ComfortCloudClient> Client (HttpClient http)
		{
		var client = new ComfortCloudClient (new ()
			{
			AppVersion = "fixture",
			MinimumRequestInterval = TimeSpan.Zero
			}, http);
		await client.RestoreSessionAsync (Fixture.Session ());
		return client;
		}
	private static HttpResponseMessage Reply (string value) => new (HttpStatusCode.OK) { Content = new StringContent (value, Encoding.UTF8, "application/json") };
	[TestCase (false)]
	[TestCase (true)]
	public async Task ScheduleActivationCannotInterleaveWithAnAccountChange (bool aquarea)
		{
		TaskCompletionSource<bool> started = Signal ();
		TaskCompletionSource<bool> release = Signal ();
		string? writeAuthorization = null;
		using var handler = new ConcurrentHandler (async (request, token) =>
			{
				bool write = request.Method == HttpMethod.Post;
				if (aquarea)
					{
					using JsonDocument transfer = JsonDocument.Parse (await request.Content!.ReadAsStringAsync (token));
					write = transfer.RootElement.TryGetProperty ("requestMethod", out JsonElement method) && method.GetString () == "POST";
					}

				if (write)
					{
					writeAuthorization = request.Headers.GetValues ("x-user-authorization-v2").Single ();
					return Reply ("{}");
					}

				_ = started.TrySetResult (true);
				_ = await release.Task;
				return Reply (aquarea ? JsonSerializer.Serialize (ScheduleEditing.CreateAquarea (), _json)
					: JsonSerializer.Serialize (new
						{
						devWeeklyTimerList = new[] { ScheduleEditing.CreateAc ("fixture") }
						}, _json));
			});
		using var http = new HttpClient (handler);
		using ComfortCloudClient client = await Client (http);
		Task activation = aquarea ? client.SetAquareaScheduleEnabledAsync ("fixture", true) : client.SetAcScheduleEnabledAsync ("fixture", true);
		try
			{
			_ = await started.Task.WaitAsync (TimeSpan.FromSeconds (10));
			Task accountChange = client.RestoreSessionAsync (new ()
				{
				AccessToken = "fake-other-account",
				ClientId = "other",
				ExpiresAt = DateTimeOffset.UtcNow.AddHours (1)
				});
			_ = release.TrySetResult (true);
			await Task.WhenAll (activation, accountChange).WaitAsync (TimeSpan.FromSeconds (10));
			Assert.That (writeAuthorization, Is.EqualTo ("Bearer fake-access-never-live"));
			Assert.That (client.CurrentSession!.AccessToken, Is.EqualTo ("fake-other-account"));
			Assert.That (handler.Count, Is.EqualTo (2));
			}
		finally
			{
			_ = release.TrySetResult (true);
			}
		}
	[Test]
	public async Task PendingPolicyReadsAndAcceptanceStayOnTheSameAccount ()
		{
		TaskCompletionSource<bool> started = Signal ();
		TaskCompletionSource<bool> release = Signal ();
		var authorization = new System.Collections.Concurrent.ConcurrentQueue<string> ();
		using var handler = new ConcurrentHandler (async (request, ignoredToken) =>
			{
				authorization.Enqueue (request.Headers.GetValues ("x-user-authorization-v2").Single ());
				if (request.RequestUri!.AbsolutePath.EndsWith ("/documents", StringComparison.Ordinal))
					{
					_ = started.TrySetResult (true);
					_ = await release.Task;
					return Reply ("{\"agreementList\":[{\"type\":1,\"version\":\"fixture-revision\"}]}");
					}

				return Reply (request.Method == HttpMethod.Get ? "{\"agreementList\":[]}" : "{}");
			});
		using var http = new HttpClient (handler);
		using ComfortCloudClient client = await Client (http);
		Task accept = client.EnsureAgreementsAcceptedAsync ();
		try
			{
			_ = await started.Task.WaitAsync (TimeSpan.FromSeconds (10));
			Task accountChange = client.RestoreSessionAsync (new ()
				{
				AccessToken = "fake-other-account",
				ClientId = "other",
				ExpiresAt = DateTimeOffset.UtcNow.AddHours (1)
				});
			_ = release.TrySetResult (true);
			await Task.WhenAll (accept, accountChange).WaitAsync (TimeSpan.FromSeconds (10));
			Assert.That (authorization.Count, Is.EqualTo (3));
			Assert.That (authorization.ToArray (), Is.All.EqualTo ("Bearer fake-access-never-live"));
			Assert.That (client.CurrentSession!.AccessToken, Is.EqualTo ("fake-other-account"));
			}
		finally
			{
			_ = release.TrySetResult (true);
			}
		}
	[Test]
	public async Task RefreshPublishesObservationBeforeQueuedSessionChangeNotification ()
		{
		TaskCompletionSource<bool> started = Signal ();
		TaskCompletionSource<bool> release = Signal ();
		using var handler = new ConcurrentHandler (async (_, _) =>
			{
				_ = started.TrySetResult (true);
				_ = await release.Task;
				return Reply ("{\"parameters\":{\"operate\":1}}");
			});
		using var http = new HttpClient (handler);
		using ComfortCloudClient client = await Client (http);
		var observation = new DeviceObservation<AcSnapshot> (new ()
			{
			DeviceGuid = "fixture"
			});
		int sawObservation = 0;
		client.SessionChanged += (_, _) => Interlocked.Exchange (ref sawObservation, observation.Current is null ? -1 : 1);
		Task refresh = client.RefreshAsync (observation);
		try
			{
			_ = await started.Task.WaitAsync (TimeSpan.FromSeconds (10));
			Task restore = client.RestoreSessionAsync (Fixture.Session ());
			_ = release.TrySetResult (true);
			await Task.WhenAll (refresh, restore).WaitAsync (TimeSpan.FromSeconds (10));
			Assert.That (sawObservation, Is.EqualTo (1));
			}
		finally
			{
			_ = release.TrySetResult (true);
			}
		}
	[Test]
	public async Task ClientDisposalCancelsVersionLookupWithoutPublishingSelectedVersion ()
		{
		using var stream = new GatedStream ("[[\"9.8.7\"]]");
		using var handler = new ConcurrentHandler ((_, _) => Task.FromResult (new HttpResponseMessage (HttpStatusCode.OK) { Content = new StreamContent (stream) }));
		using var http = new HttpClient (handler);
		using var discovery = new AppVersionDiscovery (http);
		using ComfortCloudClient client = await Client (http);
		Task select = client.UpdateAppVersionAsync (discovery);
		try
			{
			_ = await stream.Started.Task.WaitAsync (TimeSpan.FromSeconds (10));
			client.Dispose ();
			_ = stream.Release.TrySetResult (true);
			CanceledOrDisposed (await FailureOf (select));
			Assert.That (client.AppVersion, Is.EqualTo ("fixture"));
			Assert.That (discovery.Current, Is.Null);
			Assert.That (handler.Count, Is.EqualTo (1));
			}
		finally
			{
			_ = stream.Release.TrySetResult (true);
			}
		}
	[Test]
	public async Task CallerCancellationDoesNotStopOtherCallersOrTheSharedClient ()
		{
		using var stream = new GatedStream ("{\"parameters\":{\"operate\":1}}");
		int requests = 0;
		using var handler = new ConcurrentHandler ((_, _) => Task.FromResult (Interlocked.Increment (ref requests) == 1
			? new HttpResponseMessage (HttpStatusCode.OK) { Content = new StreamContent (stream) }
			: Reply ("{\"parameters\":{\"operate\":0}}")));
		using var http = new HttpClient (handler);
		using ComfortCloudClient client = await Client (http);
		using var canceled = new CancellationTokenSource ();
		Task first = client.GetAcStatusAsync ("fixture", cancellationToken: canceled.Token);
		try
			{
			_ = await stream.Started.Task.WaitAsync (TimeSpan.FromSeconds (10));
			Task<AcSnapshot> queued = client.GetAcStatusAsync ("fixture");
			canceled.Cancel ();
			_ = stream.Release.TrySetResult (true);
			Assert.That (await FailureOf (first), Is.InstanceOf<OperationCanceledException> ());
			Assert.That ((await queued.WaitAsync (TimeSpan.FromSeconds (10))).Parameters!.Power, Is.EqualTo (PowerState.Off));
			Assert.That (handler.Count, Is.EqualTo (2));
			}
		finally
			{
			_ = stream.Release.TrySetResult (true);
			}
		}
	[Test]
	public async Task CanceledGateWaiterDoesNotConsumeTheNextPermit ()
		{
		using var gate = new AsyncOperationGate (typeof (ConcurrencyTests));
		using AsyncOperationGate.Operation holder = await gate.EnterAsync (CancellationToken.None);
		using var cancellation = new CancellationTokenSource ();
		Task waiter = gate.EnterAsync (cancellation.Token);
		cancellation.Cancel ();
		Assert.That (await FailureOf (waiter), Is.InstanceOf<OperationCanceledException> ());
		holder.Dispose ();
		using AsyncOperationGate.Operation next = await gate.EnterAsync (CancellationToken.None).WaitAsync (TimeSpan.FromSeconds (10));
		Assert.That (next.Token.IsCancellationRequested, Is.False);
		}
	private sealed class ConcurrentHandler (Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
		{
		private int _count, _disposals;
		private string? _lastBody;
		internal int Count => Volatile.Read (ref _count);
		internal int Disposals => Volatile.Read (ref _disposals);
		internal string? LastBody => Volatile.Read (ref _lastBody);
		protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken token)
			{
			_ = Interlocked.Increment (ref _count);
			Volatile.Write (ref _lastBody, request.Content is null ? null : await request.Content.ReadAsStringAsync (token));
			return await respond (request, token);
			}
		protected override void Dispose (bool disposing)
			{
			if (disposing)
				_ = Interlocked.Increment (ref _disposals);
			base.Dispose (disposing);
			}
		}
	private sealed class GatedStream (string value) : Stream
		{
		private readonly MemoryStream _inner = new (Encoding.UTF8.GetBytes (value));
		private int _reads;
		internal TaskCompletionSource<bool> Started { get; } = Signal ();
		internal TaskCompletionSource<bool> Release { get; } = Signal ();
		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => _inner.Length;
		public override long Position
			{
			get => _inner.Position; set => throw new NotSupportedException ();
			}
		public override async Task<int> ReadAsync (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			{
			await BeginReadAsync ();
			return _inner.Read (buffer, offset, count);
			}
#if !NETFRAMEWORK
		public override async ValueTask<int> ReadAsync (Memory<byte> buffer, CancellationToken cancellationToken = default)
			{
			await BeginReadAsync ();
			return _inner.Read (buffer.Span);
			}
#endif
		private async Task BeginReadAsync ()
			{
			if (Interlocked.Increment (ref _reads) == 1)
				{
				_ = Started.TrySetResult (true);
				_ = await Release.Task;
				}
			// Deliberately ignore cancellation to exercise late transport completion.
			}
		public override int Read (byte[] buffer, int offset, int count) => throw new NotSupportedException ();
		public override void Flush () => throw new NotSupportedException ();
		public override long Seek (long offset, SeekOrigin origin) => throw new NotSupportedException ();
		public override void SetLength (long length) => throw new NotSupportedException ();
		public override void Write (byte[] buffer, int offset, int count) => throw new NotSupportedException ();
		protected override void Dispose (bool disposing)
			{
			if (disposing)
				_inner.Dispose ();
			base.Dispose (disposing);
			}
		}
	}