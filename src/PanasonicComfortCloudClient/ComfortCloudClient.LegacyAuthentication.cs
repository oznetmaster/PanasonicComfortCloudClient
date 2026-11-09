// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

using PanasonicComfortCloudClient.Authentication;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient;

public sealed partial class ComfortCloudClient
	{
	private const string BROWSER_AGENT = "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Mobile Safari/537.36";
	private CookieContainer _authenticationCookies = new ();
	private Guid _passwordAttempt;
	private int _legacySteps;

	/// <summary>Runs the upstream username/password form protocol without opening a browser.</summary>
	/// <param name="userName">The Panasonic ID user name; never logged or persisted.</param>
	/// <param name="password">The password, supplied in memory and never retained by the client.</param>
	/// <param name="cancellationToken">Cancels the bounded authentication requests.</param>
	/// <returns>Registered credentials or a pending MFA challenge. Existing credentials are retained until a new token response validates.</returns>
	public Task<LegacyAuthenticationResult> AuthenticateWithPasswordAsync (string userName, string password, CancellationToken cancellationToken = default) => string.IsNullOrWhiteSpace (userName) || string.IsNullOrEmpty (password)
			? throw new ArgumentException ("A user name and password are required.")
			: WithGateAsync (async token =>
			{
				_authenticationCookies = new ();
				_passwordAttempt = Guid.NewGuid ();
				_legacySteps = 0;
				OAuthAuthorizationRequest authorization = CreateAuthorizationRequest ();
				LegacyResponse response = await SendLegacyAsync (HttpMethod.Get, authorization.AuthorizationUri, null, token).ConfigureAwait (false);
				if (response.Location is null)
					throw new ComfortCloudException ("Password authorization did not return a redirect.", response.Status);
				Uri location = new (response.Uri, response.Location);
				if (IsCallback (location))
					return await FinishLegacyAsync (authorization, location, token).ConfigureAwait (false);
				response = await SendLegacyAsync (HttpMethod.Get, TrustedAuthenticationUri (location), null, token).ConfigureAwait (false);
				RequireLegacySuccess (response);
				string csrf = _authenticationCookies.GetCookies (location)["_csrf"]?.Value ?? throw new ComfortCloudException ("The hosted login page omitted its CSRF cookie.");
				Dictionary<string, string> query = OAuthProtocol.Query (location.Query);
				if (!query.TryGetValue ("state", out var state) || string.IsNullOrEmpty (state))
					throw new ComfortCloudException ("The hosted login redirect omitted its state.");
				response = await SendLegacyAsync (HttpMethod.Post, new Uri (_options.AuthenticationBaseUri, "usernamepassword/login"), JsonContent (new PasswordRequest
					{
					UserName = userName,
					Password = password,
					Csrf = csrf,
					State = state
					}), token).ConfigureAwait (false);
				if ((int)response.Status >= 400)
					{
					MfaError? error = null;
					try
						{
						error = JsonSerializer.Deserialize<MfaError> (response.Body, _json);
						}
					catch (JsonException) { /* Error-page content is not included in public exceptions. */ }

					if (error?.Error == "mfa_required" && !string.IsNullOrEmpty (error.Token))
						return new (null, new (_passwordAttempt, authorization, error.Token, null));
					}

				RequireLegacySuccess (response);
				return await ProcessLoginPageAsync (response, authorization, token).ConfigureAwait (false);
			}, cancellationToken);

	/// <summary>Completes a pending classic OTP or Guardian challenge once.</summary>
	/// <param name="challenge">The challenge returned by this client's current password attempt.</param>
	/// <param name="oneTimePassword">The current authenticator code; never logged or persisted.</param>
	/// <param name="cancellationToken">Cancels negotiation, polling, token exchange or registration.</param>
	/// <returns>The registered credential snapshot.</returns>
	public Task<ComfortCloudSession> CompletePasswordMfaAsync (LegacyMfaChallenge challenge, string oneTimePassword, CancellationToken cancellationToken = default)
		{
		ArgumentNullException.ThrowIfNull (challenge);
		return string.IsNullOrWhiteSpace (oneTimePassword)
			? throw new ArgumentException ("An OTP is required.", nameof (oneTimePassword))
			: WithGateAsync (async token =>
			{
				challenge.Consume (_passwordAttempt);
				if (challenge.Guardian is GuardianConfiguration guardian)
					{
					Uri service = TrustedAuthenticationUri (new Uri (guardian.ServiceUrl));
					Uri action = TrustedAuthenticationUri (new Uri (guardian.PostActionUrl));
					LegacyResponse start = await SendLegacyAsync (HttpMethod.Post, new Uri (service.AbsoluteUri.TrimEnd ('/') + "/api/start-flow"), JsonContent (new GuardianStart { Transport = guardian.StateTransport }), token, guardian.RequestToken).ConfigureAwait (false);
					RequireLegacySuccess (start);
					GuardianReply begun = Decode<GuardianReply> (start);
					string transaction = begun.TransactionToken ?? begun.AlternateTransactionToken ?? throw new ComfortCloudException ("Guardian omitted a transaction token.");
					LegacyResponse verify = await SendLegacyAsync (HttpMethod.Post, new Uri (service.AbsoluteUri.TrimEnd ('/') + "/api/verify-otp"), JsonContent (new GuardianOtp { Code = oneTimePassword }), token, transaction).ConfigureAwait (false);
					RequireLegacySuccess (verify);
					string? signature = null;
					for (int poll = 0; poll < 10; poll++)
						{
						LegacyResponse result = await SendLegacyAsync (HttpMethod.Post, new Uri (service.AbsoluteUri.TrimEnd ('/') + "/api/transaction-state"), null, token, transaction).ConfigureAwait (false);
						RequireLegacySuccess (result);
						GuardianReply status = Decode<GuardianReply> (result);
						if (status.State == "rejected")
							throw new ComfortCloudException ("Guardian rejected the MFA transaction.");
						if (status.State == "accepted")
							{
							signature = status.Token;
							break;
							}

						await Task.Delay (TimeSpan.FromSeconds (1), token).ConfigureAwait (false);
						}

					if (string.IsNullOrWhiteSpace (signature))
						throw new ComfortCloudException ("Guardian did not confirm the transaction within ten polls.");
					LegacyResponse response = await SendLegacyAsync (HttpMethod.Post, action, new FormUrlEncodedContent (new Dictionary<string, string> { ["accepted"] = "true", ["signature"] = signature! }), token).ConfigureAwait (false);
					LegacyAuthenticationResult completed = await FollowLegacyAsync (response, challenge.Authorization, token).ConfigureAwait (false);
					return completed.Session ?? throw new ComfortCloudException ("Guardian returned an additional unresolved challenge.");
					}
				// Challenge negotiation is best effort in the upstream protocol. No token grant or device command is replayed.
				try
					{
					_ = await SendLegacyAsync (HttpMethod.Post, new Uri (_options.AuthenticationBaseUri, "mfa/challenge"), JsonContent (new ClassicChallengeRequest { Token = challenge.Token! }), token).ConfigureAwait (false);
					}
				catch (HttpRequestException) { /* The upstream OTP grant also works without negotiation. */ }

				challenge.Authorization.Consume ();
				DateTimeOffset received = DateTimeOffset.UtcNow;
				LegacyResponse granted = await SendLegacyAsync (HttpMethod.Post, new Uri (_options.AuthenticationBaseUri, "oauth/token"), JsonContent (new ClassicOtpRequest { Token = challenge.Token!, Otp = oneTimePassword }), token).ConfigureAwait (false);
				RequireLegacySuccess (granted);
				PublishSession (SessionFromResponse (Decode<TokenResponse> (granted), received, null));
				await RegisterCoreAsync (token).ConfigureAwait (false);
				return CurrentSession!;
			}, cancellationToken);
		}

	private async Task<LegacyAuthenticationResult> ProcessLoginPageAsync (LegacyResponse page, OAuthAuthorizationRequest authorization, CancellationToken token)
		{
		var html = new HtmlDocument ();
		html.LoadHtml (page.Body);
		var fields = new Dictionary<string, string> (StringComparer.Ordinal);
		foreach (HtmlNode input in html.DocumentNode.Descendants ("input").Where (n => n.GetAttributeValue ("type", "").Equals ("hidden", StringComparison.OrdinalIgnoreCase)))
			{
			string name = WebUtility.HtmlDecode (input.GetAttributeValue ("name", ""));
			if (name.Length == 0)
				continue;
			if (fields.ContainsKey (name))
				throw new ComfortCloudException ("The login page contains ambiguous duplicate form fields.");
			fields.Add (name, WebUtility.HtmlDecode (input.GetAttributeValue ("value", "")));
			}

		if (fields.TryGetValue ("mfa_token", out var mfa) && !string.IsNullOrEmpty (mfa))
			return new (null, new (_passwordAttempt, authorization, mfa, null));
		if (page.Body.Contains ("__g_config"))
			{
			string? Field (string name)
				{
				Match match = Regex.Match (page.Body, "\\b" + name + "\\s*:\\s*(\"(?:[^\"\\\\]|\\\\.)*\")", RegexOptions.CultureInvariant, TimeSpan.FromSeconds (1));
				return match.Success ? JsonSerializer.Deserialize<string> (match.Groups[1].Value) : null;
				}

			string? service = Field ("serviceUrl"), action = Field ("postActionURL"), requestToken = Field ("requestToken");
			if (service is not { Length: > 0 } || action is not { Length: > 0 } || requestToken is not { Length: > 0 })
				throw new ComfortCloudException ("The Guardian page omitted required configuration.");
			_ = TrustedAuthenticationUri (new Uri (service));
			_ = TrustedAuthenticationUri (new Uri (action));
			return new (null, new (_passwordAttempt, authorization, null, new ()
				{
				ServiceUrl = service,
				PostActionUrl = action,
				RequestToken = requestToken,
				StateTransport = Field ("stateCheckingMechanism") ?? "polling"
				}));
			}

		if (fields.Count == 0)
			throw new ComfortCloudException ("No login callback fields or modeled MFA challenge were found.");
		LegacyResponse callback = await SendLegacyAsync (HttpMethod.Post, new Uri (_options.AuthenticationBaseUri, "login/callback"), new FormUrlEncodedContent (fields), token).ConfigureAwait (false);
		return await FollowLegacyAsync (callback, authorization, token).ConfigureAwait (false);
		}

	private async Task<LegacyAuthenticationResult> FollowLegacyAsync (LegacyResponse response, OAuthAuthorizationRequest authorization, CancellationToken token)
		{
		for (int redirect = 0; redirect < 5; redirect++)
			{
			if (response.Location is null)
				{
				RequireLegacySuccess (response);
				return await ProcessLoginPageAsync (response, authorization, token).ConfigureAwait (false);
				}

			Uri target = new (response.Uri, response.Location);
			if (IsCallback (target))
				return await FinishLegacyAsync (authorization, target, token).ConfigureAwait (false);
			response = await SendLegacyAsync (HttpMethod.Get, TrustedAuthenticationUri (target), null, token).ConfigureAwait (false);
			}

		throw new ComfortCloudException ("The password login exceeded five redirects.");
		}

	private async Task<LegacyAuthenticationResult> FinishLegacyAsync (OAuthAuthorizationRequest authorization, Uri callback, CancellationToken token)
		{
		Dictionary<string, string> query = OAuthProtocol.Query (callback.Query);
		if (!query.TryGetValue ("code", out var code) || string.IsNullOrWhiteSpace (code) || query.ContainsKey ("error") || !query.TryGetValue ("state", out var state) || state != authorization.State || callback.Fragment.Length != 0)
			throw new ComfortCloudException ("The password authorization callback lacks a valid code and matching state.");
		authorization.Consume ();
		DateTimeOffset received = DateTimeOffset.UtcNow;
		TokenResponse response = await SendCoreAsync<TokenResponse> (HttpMethod.Post, new Uri (_options.AuthenticationBaseUri, "oauth/token"), new TokenRequest { Code = code, CodeVerifier = authorization.CodeVerifier, RedirectUri = OAuthProtocol.REDIRECT_URI, Scope = "openid" }, false, false, token).ConfigureAwait (false);
		PublishSession (SessionFromResponse (response, received, null));
		await RegisterCoreAsync (token).ConfigureAwait (false);
		return new (CurrentSession, null);
		}

	private static bool IsCallback (Uri target) => target.IsAbsoluteUri && target.GetLeftPart (UriPartial.Path) == new Uri (OAuthProtocol.REDIRECT_URI).GetLeftPart (UriPartial.Path);
	private Uri TrustedAuthenticationUri (Uri target) => target.IsAbsoluteUri && target.Scheme == Uri.UriSchemeHttps && target.UserInfo.Length == 0 &&
		(target.GetLeftPart (UriPartial.Authority) == _options.AuthenticationBaseUri.GetLeftPart (UriPartial.Authority) ||
		(target.IsDefaultPort && (target.Host.EndsWith (".digital.panasonic.com", StringComparison.OrdinalIgnoreCase) || target.Host.EndsWith (".auth0.com", StringComparison.OrdinalIgnoreCase))))
		? target : throw new ComfortCloudException ("A hosted authentication URL points outside the configured Panasonic/Auth0 origins.");
	private StringContent JsonContent<T> (T value) => new (JsonSerializer.Serialize (value, _json), Encoding.UTF8, "application/json");
	private T Decode<T> (LegacyResponse response) => JsonSerializer.Deserialize<T> (response.Body, _json) ?? throw new ComfortCloudException ("The authentication service returned an empty modeled response.");
	private static void RequireLegacySuccess (LegacyResponse response)
		{
		if ((int)response.Status is < 200 or >= 300)
			throw new ComfortCloudException ("The hosted authentication request was rejected.", response.Status);
		}

	private async Task<LegacyResponse> SendLegacyAsync (HttpMethod method, Uri uri, HttpContent? content, CancellationToken token, string? bearer = null)
		{
		token.ThrowIfCancellationRequested ();
		_ = TrustedAuthenticationUri (uri);
		if (++_legacySteps > 32)
			throw new ComfortCloudException ("The legacy authentication exchange exceeded its request limit.");
		TimeSpan wait = _nextRequest - _clock.Elapsed;
		if (wait > TimeSpan.Zero)
			await Task.Delay (wait, token).ConfigureAwait (false);
		_nextRequest = _clock.Elapsed + _options.MinimumRequestInterval;
		using var request = new HttpRequestMessage (method, uri) { Content = content };
		_ = request.Headers.TryAddWithoutValidation ("User-Agent", content?.Headers.ContentType?.MediaType == "application/json" || uri.AbsolutePath == "/authorize" ? "okhttp/4.10.0" : BROWSER_AGENT);
		_ = request.Headers.TryAddWithoutValidation ("Auth0-Client", OAuthProtocol.AUTH0_CLIENT);
		string cookies = _authenticationCookies.GetCookieHeader (uri);
		if (cookies.Length != 0)
			_ = request.Headers.TryAddWithoutValidation ("Cookie", cookies);
		if (bearer is not null)
			request.Headers.Authorization = new ("Bearer", bearer);
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource (token);
		deadline.CancelAfter (_options.RequestTimeout);
		using HttpResponseMessage response = await _http.SendAsync (request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait (false);
		deadline.Token.ThrowIfCancellationRequested ();
		if (response.Headers.TryGetValues ("Set-Cookie", out IEnumerable<string>? headers))
			{
			foreach (string header in headers)
				_authenticationCookies.SetCookies (uri, header);
			}

		using Stream stream = await response.Content.ReadAsStreamAsync (deadline.Token).ConfigureAwait (false);
		using var body = new MemoryStream ();
		var buffer = new byte[8192];
		int length;
		while ((length = await stream.ReadAsync (buffer, deadline.Token).ConfigureAwait (false)) != 0)
			{
			if (body.Length + length > 4 * 1024 * 1024)
				throw new ComfortCloudException ("The hosted response exceeded four MiB.");
			body.Write (buffer, 0, length);
			}

		deadline.Token.ThrowIfCancellationRequested ();
		return new (uri, response.StatusCode, response.Headers.Location, Encoding.UTF8.GetString (body.ToArray ()));
		}

	private sealed record LegacyResponse (Uri Uri, HttpStatusCode Status, Uri? Location, string Body);

	private sealed class PasswordRequest
		{
		[JsonPropertyName ("client_id")] public string ClientId { get; } = OAuthProtocol.CLIENT_ID;
		[JsonPropertyName ("redirect_uri")] public string Redirect { get; } = OAuthProtocol.REDIRECT_URI;
		[JsonPropertyName ("tenant")] public string Tenant { get; } = "pdpauthglb-a1";
		[JsonPropertyName ("response_type")] public string ResponseType { get; } = "code";
		[JsonPropertyName ("scope")] public string Scope { get; } = OAuthProtocol.SCOPE;
		[JsonPropertyName ("audience")] public string Audience { get; } = $"https://digital.panasonic.com/{OAuthProtocol.CLIENT_ID}/api/v1/";
		[JsonPropertyName ("_csrf")] public string Csrf { get; init; } = "";
		[JsonPropertyName ("state")] public string State { get; init; } = "";
		[JsonPropertyName ("_intstate")] public string InternalState { get; } = "deprecated";
		[JsonPropertyName ("username")] public string UserName { get; init; } = "";
		[JsonPropertyName ("password")] public string Password { get; init; } = "";
		[JsonPropertyName ("lang")] public string Language { get; } = "en";
		[JsonPropertyName ("connection")] public string Connection { get; } = "PanasonicID-Authentication";
		}
	private sealed class ClassicChallengeRequest
		{
		[JsonPropertyName ("client_id")] public string ClientId { get; } = OAuthProtocol.CLIENT_ID;
		[JsonPropertyName ("mfa_token")] public string Token { get; init; } = "";
		[JsonPropertyName ("challenge_type")] public string ChallengeType { get; } = "otp";
		}
	private sealed class MfaError
		{
		[JsonPropertyName ("error")]
		public string? Error
			{
			get; init;
			}
		[JsonPropertyName ("mfa_token")]
		public string? Token
			{
			get; init;
			}
		}
	private sealed class ClassicOtpRequest
		{
		[JsonPropertyName ("client_id")] public string ClientId { get; } = OAuthProtocol.CLIENT_ID;
		[JsonPropertyName ("grant_type")] public string GrantType { get; } = "http://auth0.com/oauth/grant-type/mfa-otp";
		[JsonPropertyName ("mfa_token")] public string Token { get; init; } = "";
		[JsonPropertyName ("otp")] public string Otp { get; init; } = "";
		}
	private sealed class GuardianStart
		{
		[JsonPropertyName ("state_transport")] public string Transport { get; init; } = "polling";
		}
	private sealed class GuardianOtp
		{
		[JsonPropertyName ("type")] public string Type { get; } = "manual_input"; [JsonPropertyName ("code")] public string Code { get; init; } = "";
		}
	private sealed class GuardianReply
		{
		[JsonPropertyName ("transactionToken")]
		public string? TransactionToken
			{
			get; init;
			}
		[JsonPropertyName ("transaction_token")]
		public string? AlternateTransactionToken
			{
			get; init;
			}
		[JsonPropertyName ("state")]
		public string? State
			{
			get; init;
			}
		[JsonPropertyName ("token")]
		public string? Token
			{
			get; init;
			}
		}
	}