// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PanasonicComfortCloudClient;

public sealed partial class ComfortCloudClient
	{
	private readonly CookieContainer _aquareaCookies = new ();
	/// <summary>Reads a legacy Aquarea cookie-authenticated endpoint into an explicit response model.</summary>
	/// <typeparam name="TResponse">The attribute-defined response type expected from that endpoint.</typeparam>
	/// <param name="relativePath">The path within the configured Aquarea web origin.</param>
	/// <param name="cookies">Optional application-owned cookies; the current access-token cookie is supplied by the library.</param>
	/// <param name="cancellationToken">Cancels waiting, refresh or retrieval.</param>
	/// <returns>The modeled response; cookies are isolated from the identity and Comfort Cloud services.</returns>
	public Task<TResponse> GetLegacyAquareaAsync<TResponse> (string relativePath, IReadOnlyDictionary<string, string>? cookies = null, CancellationToken cancellationToken = default) where TResponse : class =>
		WithGateAsync (async token => JsonSerializer.Deserialize<TResponse> ((await SendAquareaWebCoreAsync (HttpMethod.Get, relativePath, cookies, token).ConfigureAwait (false)).Body, _json) ?? throw new ComfortCloudException ("The legacy Aquarea response omitted its modeled body."), cancellationToken);
	/// <summary>Posts to a legacy Aquarea cookie endpoint with the upstream bodyless request shape.</summary>
	/// <typeparam name="TResponse">The attribute-defined response model.</typeparam>
	/// <param name="relativePath">The path within the configured Aquarea origin.</param>
	/// <param name="cookies">Additional cookies; an accessToken override is not accepted.</param>
	/// <param name="cancellationToken">Cancels waiting, refresh or submission.</param>
	/// <returns>The modeled acknowledged response; uncertain submissions are never retried.</returns>
	public Task<TResponse> PostLegacyAquareaAsync<TResponse> (string relativePath, IReadOnlyDictionary<string, string>? cookies = null, CancellationToken cancellationToken = default) where TResponse : class =>
		WithGateAsync (async token => JsonSerializer.Deserialize<TResponse> ((await SendAquareaWebCoreAsync (HttpMethod.Post, relativePath, cookies, token).ConfigureAwait (false)).Body, _json) ?? throw new ComfortCloudException ("The legacy Aquarea response omitted its modeled body."), cancellationToken);
	/// <summary>Reads an HTML page from the separate cookie-authenticated legacy Aquarea site.</summary>
	/// <param name="relativePath">The path within the configured Aquarea origin.</param>
	/// <param name="cookies">Additional non-token cookies.</param>
	/// <param name="cancellationToken">Cancels waiting, refresh or retrieval.</param>
	/// <returns>The HTML page text. JSON endpoints should use an explicit model with the generic overload.</returns>
	public Task<string> GetLegacyAquareaPageAsync (string relativePath, IReadOnlyDictionary<string, string>? cookies = null, CancellationToken cancellationToken = default) =>
		WithGateAsync (async token => (await SendAquareaWebCoreAsync (HttpMethod.Get, relativePath, cookies, token).ConfigureAwait (false)).Body, cancellationToken);
	private async Task<AquareaWebReply> SendAquareaWebCoreAsync (HttpMethod method, string relativePath, IReadOnlyDictionary<string, string>? cookies, CancellationToken token)
		{
		token.ThrowIfCancellationRequested ();
		Uri origin = _options.AquareaWebBaseUri;
		Uri target = new (origin, relativePath);
		if (target.Scheme != Uri.UriSchemeHttps || target.UserInfo.Length != 0 || target.GetLeftPart (UriPartial.Authority) != origin.GetLeftPart (UriPartial.Authority))
			throw new ArgumentException ("Legacy cookie requests must stay within the configured HTTPS Aquarea origin.", nameof (relativePath));
		if (cookies?.Keys.Any (k => k.Equals ("accessToken", StringComparison.OrdinalIgnoreCase)) == true)
			throw new ArgumentException ("The access-token cookie cannot be overridden.", nameof (cookies));
		await EnsureSessionAsync (token).ConfigureAwait (false);
		if (cookies is not null)
			{
			foreach (KeyValuePair<string, string> cookie in cookies)
				_aquareaCookies.Add (origin, new Cookie (cookie.Key, cookie.Value, "/"));
			}

		_aquareaCookies.Add (origin, new Cookie ("accessToken", CurrentSession!.AccessToken, "/"));
		TimeSpan wait = _nextRequest - _clock.Elapsed;
		if (wait > TimeSpan.Zero)
			await Task.Delay (wait, token).ConfigureAwait (false);
		_nextRequest = _clock.Elapsed + _options.MinimumRequestInterval;
		using var request = new HttpRequestMessage (method, target) { Content = new ByteArrayContent ([]) };
		request.Content.Headers.ContentType = new ("application/x-www-form-urlencoded");
		_ = request.Headers.TryAddWithoutValidation ("User-Agent", BROWSER_AGENT);
		_ = request.Headers.TryAddWithoutValidation ("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
		_ = request.Headers.TryAddWithoutValidation ("Cache-Control", "max-age=0");
		_ = request.Headers.TryAddWithoutValidation ("Upgrade-Insecure-Requests", "1");
		request.Headers.Referrer = origin;
		_ = request.Headers.TryAddWithoutValidation ("Cookie", _aquareaCookies.GetCookieHeader (target));
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource (token);
		deadline.CancelAfter (_options.RequestTimeout);
		using HttpResponseMessage response = await _http.SendAsync (request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait (false);
		deadline.Token.ThrowIfCancellationRequested ();
		if (!response.IsSuccessStatusCode)
			throw new ComfortCloudException ("The legacy Aquarea request was rejected; redirects are not followed.", response.StatusCode);
		if (response.Headers.TryGetValues ("Set-Cookie", out IEnumerable<string>? values))
			{
			foreach (string value in values)
				_aquareaCookies.SetCookies (target, value);
			}

		using Stream stream = await response.Content.ReadAsStreamAsync (deadline.Token).ConfigureAwait (false);
		using var body = new MemoryStream ();
		var buffer = new byte[8192];
		int read;
		while ((read = await stream.ReadAsync (buffer, deadline.Token).ConfigureAwait (false)) != 0)
			{
			if (body.Length + read > 4 * 1024 * 1024)
				throw new ComfortCloudException ("The legacy Aquarea response exceeded four MiB.");
			body.Write (buffer, 0, read);
			}

		deadline.Token.ThrowIfCancellationRequested ();
		return new (Encoding.UTF8.GetString (body.ToArray ()));
		}
	private sealed record AquareaWebReply (string Body);
	}