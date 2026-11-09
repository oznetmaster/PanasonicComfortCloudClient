// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Text.Json;

using NUnit.Framework;

using PanasonicComfortCloudClient.Authentication;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class AuthenticationTests
	{
	[Test]
	public async Task LoginUsesPkceStateAndRegistersClientWithoutExposingToken ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"access_token\":\"new-token\",\"refresh_token\":\"rotated-refresh\",\"expires_in\":3600}");
		handler.Reply ("{\"clientId\":\"registered-client\"}");
		using var client = new ComfortCloudClient (new ComfortCloudOptions
			{
			AppVersion = "fixture",
			MinimumRequestInterval = TimeSpan.Zero
			}, new HttpClient (handler));
		OAuthAuthorizationRequest authorization = client.CreateAuthorizationRequest ();
		Dictionary<string, string> parameters = OAuthProtocol.Query (authorization.AuthorizationUri.Query);
		Assert.That (parameters["code_challenge"], Is.EqualTo (OAuthProtocol.Challenge (authorization.CodeVerifier)));
		Assert.That (parameters["scope"], Does.Contain ("a2w.control"));
		Assert.That (handler._requests, Is.Empty);
		ComfortCloudSession session = await client.CompleteAuthorizationAsync (authorization, new Uri (OAuthProtocol.REDIRECT_URI + "?state=" + authorization.State + "&code=fixture-code"));
		Assert.That (session.ClientId, Is.EqualTo ("registered-client"));
		Assert.That (session.ToString (), Does.Not.Contain ("new-token"));
		Assert.That (authorization.ToString (), Does.Not.Contain (authorization.CodeVerifier));
		Assert.That (handler._requests[0].Headers.ContainsKey ("X-USER-AUTHORIZATION-V2"), Is.False);
		Assert.That (handler._requests[1].Headers.ContainsKey ("X-CLIENT-ID"), Is.False);
		using var tokenRequest = JsonDocument.Parse (handler._requests[0].Body!);
		Assert.That (tokenRequest.RootElement.GetProperty ("code_verifier").GetString (), Is.EqualTo (authorization.CodeVerifier));
		}

	[TestCase ("wrong-state", false)]
	[TestCase ("expected", true)]
	public async Task InvalidCallbackDoesNotSendCredentials (string state, bool wrongHost)
		{
		var handler = new ScriptedHandler ();
		using var client = new ComfortCloudClient (new ComfortCloudOptions
			{
			AppVersion = "fixture"
			}, new HttpClient (handler));
		var context = new OAuthAuthorizationRequest
			{
			State = "expected",
			CodeVerifier = new string ('x', 43),
			ExpiresAt = DateTimeOffset.UtcNow.AddMinutes (2)
			};
		var callback = new Uri ((wrongHost ? "https://untrusted.invalid/callback" : OAuthProtocol.REDIRECT_URI) + "?state=" + state + "&code=x");
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.CompleteAuthorizationAsync (context, callback));
		Assert.That (handler._requests, Is.Empty);
		}

	[Test]
	public async Task CodeExchangeCannotBeReplayedAfterTransportFailure ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ((_, _) => throw new HttpRequestException ("Fixture connection lost."));
		using var client = new ComfortCloudClient (new ComfortCloudOptions
			{
			AppVersion = "fixture",
			MinimumRequestInterval = TimeSpan.Zero
			}, new HttpClient (handler));
		OAuthAuthorizationRequest context = client.CreateAuthorizationRequest ();
		var callback = new Uri (OAuthProtocol.REDIRECT_URI + "?state=" + context.State + "&code=x");
		_ = await Assert.ThrowsAsync<HttpRequestException> (async () => await client.CompleteAuthorizationAsync (context, callback));
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.CompleteAuthorizationAsync (context, callback));
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[Test]
	public async Task ConcurrentExpiredCallsShareOneRefreshAndOneRequestAtATime ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"access_token\":\"new-access\",\"refresh_token\":\"new-refresh\",\"expires_in\":3600}");
		for (int i = 0; i < 20; i++)
			{
			handler.Reply ("{\"groupList\":[]}");
			}

		using ComfortCloudClient client = await Fixture.Client (handler);
		await client.RestoreSessionAsync (Fixture.Session (true));
		_ = await Task.WhenAll (Enumerable.Range (0, 20).Select (_ => client.GetGroupsAsync ()));
		Assert.That (handler._requests.Count (r => r.Uri.AbsolutePath == "/oauth/token"), Is.EqualTo (1));
		Assert.That (handler._maximumActive, Is.EqualTo (1));
		Assert.That (client.CurrentSession!.RefreshToken, Is.EqualTo ("new-refresh"));
		Assert.That (handler._requests.Skip (1).All (r => r.Headers["X-USER-AUTHORIZATION-V2"] == "Bearer new-access"), Is.True);
		}

	[Test]
	public async Task RefreshWithoutRotationPreservesExistingRefreshToken ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"access_token\":\"renewed\",\"expires_in\":3600}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		ComfortCloudSession session = await client.RefreshSessionAsync ();
		Assert.That (session.RefreshToken, Is.EqualTo (Fixture.Session ().RefreshToken));
		}

	[Test]
	public async Task InvalidRefreshResponseDoesNotReplaceExistingSession ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"access_token\":\"renewed\"}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		ComfortCloudSession? original = client.CurrentSession;
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.RefreshSessionAsync ());
		Assert.That (client.CurrentSession, Is.SameAs (original));
		}

	[Test]
	public async Task SessionNotificationRunsOutsideRequestGate ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"access_token\":\"renewed\",\"expires_in\":3600}");
		handler.Reply ("{\"groupList\":[]}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		client.SessionChanged += (_, _) => client.GetGroupsAsync ().GetAwaiter ().GetResult ();
		_ = await client.RefreshSessionAsync ();
		Assert.That (handler._requests.Count, Is.EqualTo (2));
		}

	[Test]
	public async Task ThrowingSubscriberDoesNotReplayCompletedRequest ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{\"access_token\":\"renewed\",\"expires_in\":3600}");
		using ComfortCloudClient client = await Fixture.Client (handler);
		client.SessionChanged += (_, _) => throw new InvalidOperationException ("Fixture persistence failure.");
		_ = await client.RefreshSessionAsync ();
		Assert.That (handler._requests.Count, Is.EqualTo (1));
		}

	[Test]
	public async Task DisposingLibraryDoesNotLogoutOrDisposeInjectedHttpClient ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ("{}");
		using var http = new HttpClient (handler);
		var client = new ComfortCloudClient (new ComfortCloudOptions
			{
			AppVersion = "fixture"
			}, http);
		client.Dispose ();
		Assert.That (handler._requests, Is.Empty);
		using HttpResponseMessage response = await http.GetAsync ("https://fixture.invalid/");
		Assert.That (response.StatusCode, Is.EqualTo (HttpStatusCode.OK));
		}
	}