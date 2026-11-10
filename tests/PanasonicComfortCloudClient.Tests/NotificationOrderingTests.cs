// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PanasonicComfortCloudClient.Authentication;

using NUnit.Framework;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class NotificationOrderingTests
	{
	private static readonly string[] _expectedNotifications = ["first", "second"];
	private static ComfortCloudSession Session (string token) => new () { AccessToken = token, ExpiresAt = DateTimeOffset.UtcNow.AddHours (1) };
	[Test]
	public async Task OlderPersistenceCompletesBeforeNewerNotification ()
		{
		using var client = new ComfortCloudClient (new ()
			{
			AppVersion = "fixture"
			});
		using var release = new ManualResetEventSlim ();
		var started = new TaskCompletionSource<bool> (TaskCreationOptions.RunContinuationsAsynchronously);
		string? saved = null;
		client.SessionChanged += (_, args) =>
			{
				if (args.Session.AccessToken == "first")
					{
					_ = started.TrySetResult (true);
					Assert.That (release.Wait (TimeSpan.FromSeconds (10)), Is.True);
					}
				saved = args.Session.AccessToken;
			};
		Task first = Task.Run (() => client.RestoreSessionAsync (Session ("first")));
		try
			{
			await started.Task.WaitAsync (TimeSpan.FromSeconds (10));
			Task second = client.RestoreSessionAsync (Session ("second"));
			Assert.That (client.CurrentSession!.AccessToken, Is.EqualTo ("second"));
			Assert.That (second.IsCompleted, Is.False);
			release.Set ();
			await Task.WhenAll (first, second).WaitAsync (TimeSpan.FromSeconds (10));
			Assert.That (saved, Is.EqualTo ("second"));
			}
		finally { release.Set (); }
		}
	[Test]
	public async Task ReentrantSessionChangeQueuesWithoutDeadlockingAndObserversRemainOrdered ()
		{
		using var client = new ComfortCloudClient (new ()
			{
			AppVersion = "fixture"
			});
		List<string> delivered = [];
		client.SessionChanged += (_, args) =>
			{
				if (args.Session.AccessToken == "first")
					client.RestoreSessionAsync (Session ("second")).GetAwaiter ().GetResult ();
			};
		client.SessionChanged += (_, args) => delivered.Add (args.Session.AccessToken);
		await Task.Run (() => client.RestoreSessionAsync (Session ("first"))).WaitAsync (TimeSpan.FromSeconds (10));
		Assert.That (delivered, Is.EqualTo (_expectedNotifications));
		}
	}