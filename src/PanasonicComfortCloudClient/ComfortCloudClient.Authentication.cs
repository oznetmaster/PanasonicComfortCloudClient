// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net.Http;

using PanasonicComfortCloudClient.Authentication;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient;

public sealed partial class ComfortCloudClient
	{
	/// <summary>Creates a PKCE login attempt without opening a browser or sending a request.</summary>
	/// <returns>A one-use authorization context; the application presents its URL and retains the verifier securely.</returns>
	public OAuthAuthorizationRequest CreateAuthorizationRequest ()
		{
		_operations.ThrowIfDisposed ();

		var verifier = OAuthProtocol.RandomToken ();
		var state = OAuthProtocol.RandomToken ();
		var values = new Dictionary<string, string>
			{
			["scope"] = OAuthProtocol.SCOPE,
			["audience"] = $"https://digital.panasonic.com/{OAuthProtocol.CLIENT_ID}/api/v1/",
			["protocol"] = "oauth2",
			["response_type"] = "code",
			["code_challenge"] = OAuthProtocol.Challenge (verifier),
			["code_challenge_method"] = "S256",
			["auth0Client"] = OAuthProtocol.AUTH0_CLIENT,
			["client_id"] = OAuthProtocol.CLIENT_ID,
			["redirect_uri"] = OAuthProtocol.REDIRECT_URI,
			["state"] = state
			};
		var query = string.Join ("&", values.Select (p => Uri.EscapeDataString (p.Key) + "=" + Uri.EscapeDataString (p.Value)));
		return new ()
			{
			AuthorizationUri = new Uri (_options.AuthenticationBaseUri, "authorize?" + query),
			CodeVerifier = verifier,
			State = state,
			ExpiresAt = DateTimeOffset.UtcNow.AddMinutes (10)
			};
		}

	/// <summary>Validates a Panasonic browser callback, exchanges its code and registers the cloud client.</summary>
	/// <param name="authorization">The securely retained context for this one-use attempt.</param>
	/// <param name="callbackUri">The complete Panasonic custom-scheme callback, including code and matching state.</param>
	/// <param name="cancellationToken">Cancels waiting, token exchange or registration.</param>
	/// <returns>The validated credential snapshot; no file is written by the library.</returns>
	public Task<ComfortCloudSession> CompleteAuthorizationAsync (OAuthAuthorizationRequest authorization, Uri callbackUri, CancellationToken cancellationToken = default)
		{
		ArgumentNullException.ThrowIfNull (authorization);

		if (callbackUri is null || !callbackUri.IsAbsoluteUri)
			{
			throw new ArgumentException ("An absolute callback URI is required.", nameof (callbackUri));
			}

		var expected = new Uri (OAuthProtocol.REDIRECT_URI);
		if (callbackUri.GetLeftPart (UriPartial.Path) != expected.GetLeftPart (UriPartial.Path) || !string.IsNullOrEmpty (callbackUri.Fragment))
			{
			throw new ComfortCloudException ("The authorization callback target does not match Panasonic's redirect URI.");
			}

		Dictionary<string, string> query = OAuthProtocol.Query (callbackUri.Query);
		return !query.TryGetValue ("state", out var state) || string.IsNullOrEmpty (authorization.State) || state != authorization.State
			? throw new ComfortCloudException ("The authorization callback state does not match.")
			: query.ContainsKey ("error")
			? throw new ComfortCloudException ("Panasonic declined browser authorization.")
			: !query.TryGetValue ("code", out var code) || string.IsNullOrWhiteSpace (code)
			? throw new ComfortCloudException ("The authorization callback did not contain a code.")
			: authorization.CodeVerifier.Length is < 43 or > 128 || authorization.CodeVerifier.Any (c => !(c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') || "-._~".Contains (c)))
			? throw new ComfortCloudException ("The PKCE verifier is invalid.")
			: WithGateAsync (async token =>
		{
			authorization.Consume ();
			DateTimeOffset received = DateTimeOffset.UtcNow;
			TokenResponse response = await SendCoreAsync<TokenResponse> (HttpMethod.Post, new Uri (_options.AuthenticationBaseUri, "oauth/token"), new TokenRequest { Code = code, CodeVerifier = authorization.CodeVerifier, RedirectUri = OAuthProtocol.REDIRECT_URI, Scope = "openid" }, false, false, token).ConfigureAwait (false);
			PublishSession (SessionFromResponse (response, received, null));
			await RegisterCoreAsync (token).ConfigureAwait (false);
			return CurrentSession!;
		}, cancellationToken);
		}

	/// <summary>Refreshes credentials without replaying any device command.</summary>
	/// <param name="cancellationToken">Cancels waiting or OAuth refresh.</param>
	/// <returns>The latest token snapshot, including any rotated refresh token.</returns>
	public Task<ComfortCloudSession> RefreshSessionAsync (CancellationToken cancellationToken = default) => WithGateAsync (RefreshCoreAsync, cancellationToken);

	private async Task<ComfortCloudSession> RefreshCoreAsync (CancellationToken token)
		{
		ComfortCloudSession? previous = CurrentSession;
		if (string.IsNullOrWhiteSpace (previous?.RefreshToken))
			{
			throw new ComfortCloudException ("A refresh token is unavailable; browser authorization is required.");
			}

		DateTimeOffset received = DateTimeOffset.UtcNow;
		TokenResponse response = await SendCoreAsync<TokenResponse> (HttpMethod.Post, new Uri (_options.AuthenticationBaseUri, "oauth/token"),
			 new TokenRequest
				 {
				 GrantType = "refresh_token",
				 RefreshToken = previous!.RefreshToken,
				 Scope = previous.Scope
				 }, false, false, token).ConfigureAwait (false);
		ComfortCloudSession session = SessionFromResponse (response, received, previous);
		PublishSession (session);
		return session;
		}

	private static ComfortCloudSession SessionFromResponse (TokenResponse response, DateTimeOffset received, ComfortCloudSession? previous) => string.IsNullOrWhiteSpace (response.AccessToken) || response.ExpiresIn is not > 0
			? throw new ComfortCloudException ("The OAuth response omitted a valid access token or expiration.")
			: new ()
				{
				AccessToken = response.AccessToken!,
				RefreshToken = response.RefreshToken ?? previous?.RefreshToken,
				ExpiresAt = received.AddSeconds (response.ExpiresIn.Value),
				Scope = response.Scope ?? previous?.Scope,
				ClientId = previous?.ClientId
				};

	private async Task RegisterCoreAsync (CancellationToken token)
		{
		ComfortCloudSession previous = CurrentSession!;
		RegistrationResponse reply = await SendCoreAsync<RegistrationResponse> (HttpMethod.Post, new Uri (_options.ApiBaseUri, "auth/v2/login"), new RegistrationRequest (), true, false, token).ConfigureAwait (false);
		if (string.IsNullOrWhiteSpace (reply.ClientId))
			{
			throw new ComfortCloudException ("Cloud registration did not return a client identifier.");
			}

		PublishSession (new ComfortCloudSession
			{
			AccessToken = previous.AccessToken,
			RefreshToken = previous.RefreshToken,
			ExpiresAt = previous.ExpiresAt,
			Scope = previous.Scope,
			ClientId = reply.ClientId
			});
		}

	/// <summary>Explicitly logs out the cloud session and clears only the in-memory credentials.</summary>
	/// <param name="cancellationToken">Cancels waiting or logout.</param>
	/// <returns>A task completing after the cloud accepts logout.</returns>
	public async Task LogoutAsync (CancellationToken cancellationToken = default) => _ = await WithGateAsync (async token =>
																													{
																														_ = await ApiCoreAsync<ErrorEnvelope> (HttpMethod.Post, "auth/v2/logout", null, token).ConfigureAwait (false);
																														PublishSession (null);
																														return true;
																													}, cancellationToken).ConfigureAwait (false);
	}