// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using PanasonicComfortCloudClient.Authentication;
using PanasonicComfortCloudClient.Concurrency;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient;

/// <summary>Provides serialized, cancellable access to Panasonic Comfort Cloud without platform-specific dependencies.</summary>
/// <remarks>Concurrent operations are serialized, including compound read/modify/write helpers. Disposal rejects new operations and cancels admitted work; owned transport resources are released after that work exits. Owns in-memory session state only. Storage, browser presentation and polling schedules belong to the application.</remarks>
public sealed partial class ComfortCloudClient : IDisposable
	{
	private static readonly Action<ILogger, Exception?> _sessionSubscriberFailed = LoggerMessage.Define (LogLevel.Warning, new EventId (1), "A session-change subscriber failed; the completed cloud operation was not repeated.");
	private static readonly Action<ILogger, int, Exception?> _requestRejected = LoggerMessage.Define<int> (LogLevel.Warning, new EventId (2), "Comfort Cloud rejected a request with HTTP status {Status}.");
	private readonly ComfortCloudOptions _options;
	private readonly HttpClient _http;
	private readonly bool _ownsHttp;
	private readonly AsyncOperationGate _operations;
#pragma warning disable IDE0330 // Monitor semantics are shared with processor Mono.
	private readonly object _notificationSync = new ();
#pragma warning restore IDE0330
	private readonly Queue<SessionNotification> _notifications = new ();
	private readonly AsyncLocal<bool> _insideNotification = new ();
	private bool _notifying;
	private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew ();
	private readonly JsonSerializerOptions _json = ComfortCloudJson.CreateOptions ();
	private TimeSpan _nextRequest;
	private ComfortCloudSession? _session;
	private string _appVersion;

	/// <summary>Creates a cloud client without sending any requests.</summary>
	/// <param name="options">Protocol identity, pacing, timeouts and optional logging.</param>
	/// <param name="httpClient">An optional application-owned client; its handler must disable automatic redirects.</param>
	public ComfortCloudClient (ComfortCloudOptions options, HttpClient? httpClient = null)
		{
		_options = options ?? throw new ArgumentNullException (nameof (options));
		_appVersion = options.AppVersion;
		if (string.IsNullOrWhiteSpace (options.AppVersion))
			{
			throw new ArgumentException ("Supply the current official app version.", nameof (options));
			}

		if (!options.ApiBaseUri.IsAbsoluteUri || options.ApiBaseUri.Scheme != Uri.UriSchemeHttps || !options.AuthenticationBaseUri.IsAbsoluteUri || options.AuthenticationBaseUri.Scheme != Uri.UriSchemeHttps)
			{
			throw new ArgumentException ("Cloud and authentication origins must use HTTPS.", nameof (options));
			}

		if (options.MinimumRequestInterval < TimeSpan.Zero || options.RequestTimeout <= TimeSpan.Zero || options.RequestTimeout.TotalMilliseconds > int.MaxValue)
			{
			throw new ArgumentOutOfRangeException (nameof (options), "Invalid request timing.");
			}

		if (options.AppType is not (1 or 2))
			{
			throw new ArgumentOutOfRangeException (nameof (options), "Unsupported application platform.");
			}

		_ownsHttp = httpClient is null;
		_http = httpClient ?? new HttpClient (
#if NETFRAMEWORK
			new BodylessGetHandler (options.RequestTimeout)
#else
			new HttpClientHandler
				{
				AllowAutoRedirect = false,
				UseCookies = false
				}
#endif
		);
		_operations = new (typeof (ComfortCloudClient), () =>
			{
				if (_ownsHttp)
					_http.Dispose ();
			});
		}

	/// <summary>Gets the latest validated in-memory credential snapshot.</summary>
	/// <value>The current credentials, or null before authentication; the caller owns persistence.</value>
	public ComfortCloudSession? CurrentSession => Volatile.Read (ref _session);

	/// <summary>Occurs after validated session credentials change and after the request gate is released.</summary>
	/// <remarks>Subscribers may persist tokens. Notifications are ordered and subscriber exceptions are isolated. A session operation initiated inside a subscriber commits its state without waiting for its own queued notification, so reentrant subscribers do not deadlock.</remarks>
	public event EventHandler<SessionChangedEventArgs>? SessionChanged;

	/// <summary>Restores application-owned credentials without validating them against the cloud.</summary>
	/// <param name="session">A previously saved session; expiration is checked before the next cloud operation.</param>
	/// <param name="cancellationToken">Cancels waiting for the client gate.</param>
	/// <returns>A task completing when the in-memory credentials are replaced.</returns>
	public async Task RestoreSessionAsync (ComfortCloudSession session, CancellationToken cancellationToken = default)
		{
		if (session is null || string.IsNullOrWhiteSpace (session.AccessToken))
			{
			throw new ArgumentException ("An access token is required.", nameof (session));
			}

		_ = await WithGateAsync (token =>
		{
			PublishSession (session);
			return Task.FromResult (true);
		}, cancellationToken).ConfigureAwait (false);
		}

	private async Task<T> WithGateAsync<T> (Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
		{
		using AsyncOperationGate.Operation operation = await _operations.EnterAsync (cancellationToken).ConfigureAwait (false);
		ComfortCloudSession? old = CurrentSession;
		try
			{
			T result = await action (operation.Token).ConfigureAwait (false);
			operation.Token.ThrowIfCancellationRequested ();
			return result;
			}
		finally
			{
			ComfortCloudSession? current = CurrentSession;
			SessionNotification? notification = null;
			bool drain = false;
			if (!_operations.IsDisposed && current is not null && !ReferenceEquals (old, current))
				{
				// Queue while the operation still owns serialization: releasing first
				// would allow a newer session to be queued ahead of this one.
				lock (_notificationSync)
					{
					notification = new (current, SessionChanged);
					_notifications.Enqueue (notification);
					drain = !_notifying;
					_notifying = true;
					}
				}
			operation.Dispose ();
			if (drain)
				DrainNotifications ();
			if (notification is not null && !_insideNotification.Value)
				await notification.Completion.Task.ConfigureAwait (false);
			}
		}

	private void DrainNotifications ()
		{
		bool previous = _insideNotification.Value;
		_insideNotification.Value = true;
		try
			{
			while (true)
				{
				SessionNotification next;
				lock (_notificationSync)
					{
					if (_notifications.Count == 0)
						{
						_notifying = false;
						return;
						}
					next = _notifications.Dequeue ();
					}
				try
					{
					if (_operations.IsDisposed || next.Handlers is null)
						continue;
					foreach (EventHandler<SessionChangedEventArgs> handler in next.Handlers.GetInvocationList ().Cast<EventHandler<SessionChangedEventArgs>> ())
						{
						try
							{
							handler (this, new SessionChangedEventArgs (next.Session));
							}
						catch
							{
							try
								{
								_sessionSubscriberFailed (_options.Logger, null);
								}
							catch { /* A faulty logger must not strand queued credential notifications. */ }
							}
						}
					}
				finally { _ = next.Completion.TrySetResult (true); }
				}
			}
		finally { _insideNotification.Value = previous; }
		}

	private sealed record SessionNotification (ComfortCloudSession Session, EventHandler<SessionChangedEventArgs>? Handlers)
		{
		internal TaskCompletionSource<bool> Completion { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);
		}

	private async Task EnsureSessionAsync (CancellationToken token)
		{
		ComfortCloudSession session = CurrentSession ?? throw new ComfortCloudException ("Authentication is required.");
		if (session.ExpiresAt <= DateTimeOffset.UtcNow.AddSeconds (30))
			{
			_ = await RefreshCoreAsync (token).ConfigureAwait (false);
			}

		if (string.IsNullOrWhiteSpace (CurrentSession!.ClientId))
			{
			await RegisterCoreAsync (token).ConfigureAwait (false);
			}
		}

	private async Task<T> ApiCoreAsync<T> (HttpMethod method, string path, object? body, CancellationToken token)
		{
		await EnsureSessionAsync (token).ConfigureAwait (false);
		return await SendCoreAsync<T> (method, new Uri (_options.ApiBaseUri, path), body, true, true, token).ConfigureAwait (false);
		}

	private async Task<T> SendCoreAsync<T> (HttpMethod method, Uri uri, object? body, bool cloudHeaders, bool includeClient, CancellationToken token)
		{
		token.ThrowIfCancellationRequested ();
		TimeSpan delay = _nextRequest - _clock.Elapsed;
		if (delay > TimeSpan.Zero)
			{
			await Task.Delay (delay, token).ConfigureAwait (false);
			}

		_nextRequest = _clock.Elapsed + _options.MinimumRequestInterval;
		using var request = new HttpRequestMessage (method, uri);
		if (body is not null)
			{
			request.Content = new StringContent (JsonSerializer.Serialize (body, body.GetType (), _json), Encoding.UTF8, "application/json");
			}

		_ = request.Headers.TryAddWithoutValidation ("accept", cloudHeaders ? "application/json; charset=utf-8" : "application/json");
		if (cloudHeaders)
			{
			// The mobile protocol includes Content-Type even on bodyless GET requests.
			request.Content ??= new ByteArrayContent ([]);
			request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue ("application/json");
			ComfortCloudSession session = CurrentSession ?? throw new ComfortCloudException ("Authentication is required.");
			var timestamp = RequestSigning.Timestamp (DateTimeOffset.UtcNow);
			_ = request.Headers.TryAddWithoutValidation ("user-agent", "G-RAC");
			_ = request.Headers.TryAddWithoutValidation ("x-app-name", "Comfort Cloud");
			_ = request.Headers.TryAddWithoutValidation ("x-app-timestamp", timestamp);
			_ = request.Headers.TryAddWithoutValidation ("x-app-type", _options.AppType.ToString (CultureInfo.InvariantCulture));
			_ = request.Headers.TryAddWithoutValidation ("x-app-version", Volatile.Read (ref _appVersion));
			_ = request.Headers.TryAddWithoutValidation ("x-cfc-api-key", RequestSigning.ApiKey (timestamp, session.AccessToken));
			_ = request.Headers.TryAddWithoutValidation ("x-user-authorization-v2", "Bearer " + session.AccessToken);
			if (includeClient && !string.IsNullOrWhiteSpace (session.ClientId))
				{
				_ = request.Headers.TryAddWithoutValidation ("x-client-id", session.ClientId);
				}
			}
		else
			{
			_ = request.Headers.TryAddWithoutValidation ("Auth0-Client", OAuthProtocol.AUTH0_CLIENT);
			_ = request.Headers.TryAddWithoutValidation ("user-agent", "okhttp/4.10.0");
			}

		using var deadline = CancellationTokenSource.CreateLinkedTokenSource (token);
		deadline.CancelAfter (_options.RequestTimeout);
		using HttpResponseMessage response = await _http.SendAsync (request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait (false);
		deadline.Token.ThrowIfCancellationRequested ();
		using Stream stream = await response.Content.ReadAsStreamAsync (deadline.Token).ConfigureAwait (false);
		using var bytes = new MemoryStream ();
		var buffer = new byte[8192];
		int length;
		while ((length = await stream.ReadAsync (buffer, deadline.Token).ConfigureAwait (false)) != 0)
			{
			if (bytes.Length + length > 4 * 1024 * 1024)
				{
				throw new ComfortCloudException ("Cloud response exceeded the four MiB limit.", response.StatusCode);
				}

			bytes.Write (buffer, 0, length);
			}

		var payload = bytes.ToArray ();
		deadline.Token.ThrowIfCancellationRequested ();
		ErrorEnvelope? error = null;
		if (payload.Length != 0)
			{
			try
				{
				error = JsonSerializer.Deserialize<ErrorEnvelope> (payload, _json);
				}
			catch (JsonException)
				{ /* The requested typed model below still validates a successful payload. */
				}
			}

		bool protocolError = error?.Error is not null || (error?.Code is not null && error.Code != "0") || error?.Result is not (null or 0);
		if (!response.IsSuccessStatusCode || protocolError)
			{
			TimeSpan? retry = response.Headers.RetryAfter?.Delta;
			if (retry is null && response.Headers.RetryAfter?.Date is DateTimeOffset date)
				{
				retry = date - DateTimeOffset.UtcNow;
				}

			var code = error?.Code ?? error?.Error;
			var description = code switch
				{
					"4100" => "Cloud authentication expired; explicit reauthentication may be required.",
					"4103" => "The account must explicitly accept updated Panasonic policies.",
					"4106" => "The cloud rejected the configured official application version.",
					"mfa_required" => "Panasonic requires multifactor authentication; use browser authorization.",
					_ => "The cloud request was rejected. No automatic retry was performed."
					};
			_requestRejected (_options.Logger, (int)response.StatusCode, null);
			throw new ComfortCloudException (description, response.StatusCode, code, retry);
			}

		return payload.Length == 0
			? typeof (T) == typeof (ErrorEnvelope)
				? (T)(object)new ErrorEnvelope ()
				: throw new ComfortCloudException ("Cloud response did not contain a required JSON body.", response.StatusCode)
			: JsonSerializer.Deserialize<T> (payload, _json) ?? throw new ComfortCloudException ("Cloud response contained a null body.", response.StatusCode);
		}

	private static string RequiredId (string value) => string.IsNullOrWhiteSpace (value) ? throw new ArgumentException ("A device identifier is required.", nameof (value)) : value;

	private static string FormatUtcOffset (TimeSpan offset)
		{
		if (offset.Ticks % TimeSpan.TicksPerMinute != 0 || offset < TimeSpan.FromHours (-14) || offset > TimeSpan.FromHours (14))
			{
			throw new ArgumentOutOfRangeException (nameof (offset), "The UTC offset must be in whole minutes between -14:00 and +14:00.");
			}

		int minutes = (int)offset.TotalMinutes;
		return (minutes < 0 ? "-" : "+") + (Math.Abs (minutes) / 60).ToString ("00", CultureInfo.InvariantCulture) + ":" + (Math.Abs (minutes) % 60).ToString ("00", CultureInfo.InvariantCulture);
		}

	/// <summary>Cancels outstanding client work and releases an internally created HTTP client.</summary>
	/// <remarks>An injected HTTP client remains owned by the application. Disposal does not log out or delete tokens, wait synchronously for operations, or dispose resources still used by admitted work. A response that ignores cancellation cannot publish a new session after disposal.</remarks>
	public void Dispose () => _operations.Dispose ();
	private void PublishSession (ComfortCloudSession? session) => _operations.Commit (() => Volatile.Write (ref _session, session));
	}