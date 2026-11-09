// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PanasonicComfortCloudClient.Compatibility;

// C# 14 extension members keep shared call sites on the modern APIs. These
// implementations are compiled only where the framework lacks those members.
internal static class FrameworkMd5Extensions
	{
	extension(MD5)
		{
		[SuppressMessage ("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "Legacy device identifiers only; never used for authentication or security.")]
		public static byte[] HashData (ReadOnlySpan<byte> source)
			{
			using var algorithm = MD5.Create ();
			return algorithm.ComputeHash (source.ToArray ());
			}
		}
	}

internal static class FrameworkApiExtensions
	{
	extension(ArgumentOutOfRangeException)
		{
		public static void ThrowIfNegativeOrZero (int value, [CallerArgumentExpression (nameof (value))] string? paramName = null)
			{
			if (value <= 0)
				throw new ArgumentOutOfRangeException (paramName, value, "The value must be positive.");
			}
		}
	extension(ArgumentNullException)
		{
		public static void ThrowIfNull ([NotNull] object? argument, [CallerArgumentExpression (nameof (argument))] string? paramName = null)
			{
			if (argument is null)
				{
				throw new ArgumentNullException (paramName);
				}
			}
		}
	extension(ObjectDisposedException)
		{
		public static void ThrowIf (bool condition, Type type)
			{
			if (condition)
				throw new ObjectDisposedException (type.FullName);
			}
		public static void ThrowIf (bool condition, object instance)
			{
			if (condition)
				{
				throw new ObjectDisposedException (instance.GetType ().FullName);
				}
			}
		}
	extension(Enum)
		{
		public static bool IsDefined<TEnum> (TEnum value) where TEnum : struct, Enum => Enum.IsDefined (typeof (TEnum), value);
		}
	extension(SHA256)
		{
		public static byte[] HashData (ReadOnlySpan<byte> source)
			{
			using var algorithm = SHA256.Create ();
			return algorithm.ComputeHash (source.ToArray ());
			}
		}
	extension(Convert)
		{
		public static string ToHexStringLower (ReadOnlySpan<byte> bytes)
			{
			var result = new char[checked(bytes.Length * 2)];
			const string DIGITS = "0123456789abcdef";
			for (int i = 0; i < bytes.Length; i++)
				{
				result[i * 2] = DIGITS[bytes[i] >> 4];
				result[i * 2 + 1] = DIGITS[bytes[i] & 15];
				}

			return new string (result);
			}
		}
	extension<T> (Task<T> task)
		{
		public async Task<T> WaitAsync (TimeSpan timeout)
			{
			await WaitWithTimeoutAsync (task, timeout).ConfigureAwait (false);
			return await task.ConfigureAwait (false);
			}
		public async Task<T> WaitAsync (CancellationToken cancellationToken)
			{
			if (!cancellationToken.CanBeCanceled || task.IsCompleted)
				{
				return await task.ConfigureAwait (false);
				}

			cancellationToken.ThrowIfCancellationRequested ();

			var cancelled = new TaskCompletionSource<bool> (TaskCreationOptions.RunContinuationsAsynchronously);
			using CancellationTokenRegistration registration = cancellationToken.Register (() => cancelled.TrySetResult (true));
			if (await Task.WhenAny (task, cancelled.Task).ConfigureAwait (false) != task)
				{
				_ = task.ContinueWith (completed => _ = completed.Exception, CancellationToken.None,
					TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
				throw new OperationCanceledException (cancellationToken);
				}

			return await task.ConfigureAwait (false);
			}
		}
	extension(Task task)
		{
		public Task WaitAsync (TimeSpan timeout) => WaitWithTimeoutAsync (task, timeout);
		}
	private static async Task WaitWithTimeoutAsync (Task task, TimeSpan timeout)
		{
		using var deadline = new CancellationTokenSource ();
		Task timer = Task.Delay (timeout, deadline.Token);
		try
			{
			if (task.IsCompleted || await Task.WhenAny (task, timer).ConfigureAwait (false) == task)
				{
				await task.ConfigureAwait (false);
				return;
				}

			_ = task.ContinueWith (completed => _ = completed.Exception, CancellationToken.None,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			throw new TimeoutException ();
			}
		finally
			{
			deadline.Cancel ();
			}
		}
	extension(HttpContent content)
		{
		public Task<Stream> ReadAsStreamAsync (CancellationToken cancellationToken)
			{
			cancellationToken.ThrowIfCancellationRequested ();
			return content.ReadAsStreamAsync ().WaitAsync (cancellationToken);
			}
		public Task<string> ReadAsStringAsync (CancellationToken cancellationToken)
			{
			cancellationToken.ThrowIfCancellationRequested ();
			return content.ReadAsStringAsync ().WaitAsync (cancellationToken);
			}
		}
	extension(Stream stream)
		{
		public async ValueTask<int> ReadAsync (Memory<byte> buffer, CancellationToken cancellationToken = default)
			{
			cancellationToken.ThrowIfCancellationRequested ();
			// A fresh array remains owned by any pending legacy I/O after cancellation,
			// protecting the caller's memory when the old stream ignores cancellation.
			// Returning a pooled array at that point would allow a concurrent overwrite.
			var temporary = new byte[buffer.Length];
			int read = await stream.ReadAsync (temporary, 0, temporary.Length, cancellationToken).WaitAsync (cancellationToken).ConfigureAwait (false);
			temporary.AsSpan (0, read).CopyTo (buffer.Span);
			return read;
			}
		public ValueTask WriteAsync (ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
			{
			cancellationToken.ThrowIfCancellationRequested ();
			if (!MemoryMarshal.TryGetArray (buffer, out ArraySegment<byte> segment))
				{
				segment = new (buffer.ToArray ());
				}

			return new ValueTask (stream.WriteAsync (segment.Array!, segment.Offset, segment.Count, cancellationToken));
			}
		}
	}
#endif