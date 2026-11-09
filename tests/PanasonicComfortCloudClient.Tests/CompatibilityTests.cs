// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

using NUnit.Framework;

using PanasonicComfortCloudClient.Models;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class CompatibilityTests
	{
	[Test]
	public void StaticCryptoExtensionsMatchKnownSha256Vector ()
		{
		Assert.That (Convert.ToHexStringLower (SHA256.HashData (Encoding.UTF8.GetBytes ("abc"))),
			Is.EqualTo ("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
		Assert.That (Convert.ToHexStringLower ([]), Is.Empty);
		}

	[Test]
	public void GuardPreservesCallerArgumentNameAndExceptionType ()
		{
		object? missing = null;
		ArgumentNullException? error = Assert.Throws<ArgumentNullException> (() => ArgumentNullException.ThrowIfNull (missing));
		Assert.That (error!.ParamName, Is.EqualTo (nameof (missing)));
		_ = Assert.Throws<ObjectDisposedException> (() => ObjectDisposedException.ThrowIf (true, this));
		ObjectDisposedException.ThrowIf (false, this);
		}

	[Test]
	public void GenericEnumValidationRejectsUnknownWireValue ()
		{
		Assert.That (Enum.IsDefined (PowerState.On), Is.True);
		Assert.That (Enum.IsDefined ((PowerState)999), Is.False);
		}

	[Test]
	public async Task CompletedTaskWinsOverAnAlreadyCancelledWait ()
		{
		using var cancelled = new CancellationTokenSource ();
		cancelled.Cancel ();
		Assert.That (await Task.FromResult (42).WaitAsync (cancelled.Token), Is.EqualTo (42));
		}

	[Test]
	public async Task CancelledWaitDoesNotCancelTheUnderlyingOperation ()
		{
		var pending = new TaskCompletionSource<int> (TaskCreationOptions.RunContinuationsAsynchronously);
		using var cancellation = new CancellationTokenSource ();
		Task<int> wait = pending.Task.WaitAsync (cancellation.Token);
		cancellation.Cancel ();
		_ = await Assert.CatchAsync<OperationCanceledException> (async () => await wait);
		Assert.That (pending.Task.IsCompleted, Is.False);
		pending.SetResult (19);
		Assert.That (await pending.Task, Is.EqualTo (19));
		}

	[Test]
	public async Task MemoryStreamAndContentApisRetainBytesAndCancellation ()
		{
		using var stream = new MemoryStream ();
		await stream.WriteAsync (new byte[] { 4, 5, 6 }.AsMemory ());
		stream.Position = 0;
		var buffer = new byte[5];
		Assert.That (await stream.ReadAsync (buffer.AsMemory (1, 3)), Is.EqualTo (3));
		Assert.That (buffer, Is.EqualTo (new byte[] { 0, 4, 5, 6, 0 }));
		using var content = new StringContent ("typed-client");
		Assert.That (await content.ReadAsStringAsync (CancellationToken.None), Is.EqualTo ("typed-client"));
		using var cancelled = new CancellationTokenSource ();
		cancelled.Cancel ();
		_ = await Assert.CatchAsync<OperationCanceledException> (async () => await stream.ReadAsync (buffer.AsMemory (), cancelled.Token));
		}
	[Test]
	public async Task TimeoutWaitsDoNotCancelOrLoseTheUnderlyingTask ()
		{
		var source = new TaskCompletionSource<int> (TaskCreationOptions.RunContinuationsAsynchronously);
		_ = await Assert.ThrowsAsync<TimeoutException> (async () => await source.Task.WaitAsync (TimeSpan.Zero));
		_ = await Assert.ThrowsAsync<TimeoutException> (async () => await ((Task)source.Task).WaitAsync (TimeSpan.Zero));
		Assert.That (source.Task.IsCompleted, Is.False);
		source.SetResult (42);
		Assert.That (await source.Task.WaitAsync (TimeSpan.FromSeconds (1)), Is.EqualTo (42));
		await ((Task)source.Task).WaitAsync (Timeout.InfiniteTimeSpan);
		_ = await Assert.ThrowsAsync<ArgumentOutOfRangeException> (async () => await source.Task.WaitAsync (TimeSpan.FromMilliseconds (-2)));
		}
	}