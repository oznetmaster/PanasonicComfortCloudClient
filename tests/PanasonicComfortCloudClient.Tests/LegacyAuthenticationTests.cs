// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using NUnit.Framework;

using PanasonicComfortCloudClient.Authentication;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class LegacyAuthenticationTests
	{
	private const string FORM = "<html><form><input type='hidden' name='wa' value='fixture'/><input type='hidden' name='wresult' value='&lt;result&gt;&amp;value'/><input type='hidden' name='wctx' value='ctx'></form></html>";
	private const string OTP_FORM = "<input type='hidden' name='mfa_token' value='fake-mfa-token'/>";
	private static Func<string> Seed (ScriptedHandler handler, string page = FORM, bool csrf = true, HttpStatusCode status = HttpStatusCode.OK)
		{
		string state = "";
		handler.Reply ((request, _) =>
			{
				state = OAuthProtocol.Query (request.Uri.Query)["state"];
				return Task.FromResult (Reply (HttpStatusCode.Redirect, "", "/u/login?state=tenant-state"));
			});
		handler.Reply ((_, _) => Task.FromResult (Reply (HttpStatusCode.OK, "<html>Panasonic sign-in</html>", cookie: csrf ? "_csrf=fake-csrf; Path=/; Secure; HttpOnly" : null)));
		handler.Reply (page, status);
		return () => state;
		}
	private static HttpResponseMessage Reply (HttpStatusCode status, string body, string? location = null, string? cookie = null)
		{
		var response = new HttpResponseMessage (status) { Content = new StringContent (body, Encoding.UTF8, "text/html") };
		if (location is not null)
			response.Headers.Location = new (location, UriKind.RelativeOrAbsolute);
		if (cookie is not null)
			_ = response.Headers.TryAddWithoutValidation ("Set-Cookie", cookie);
		return response;
		}
	private static void Grant (ScriptedHandler handler)
		{
		handler.Reply ("{\"access_token\":\"fake-new-access\",\"refresh_token\":\"fake-new-refresh\",\"expires_in\":3600}");
		handler.Reply ("{\"clientId\":\"fake-registered\"}");
		}
	private static string Guardian (string service = "https://authglb.digital.panasonic.com/guardian") =>
		"<script>window.__g_config={serviceUrl:\"" + service + "\",postActionURL:\"https://authglb.digital.panasonic.com/mfa/callback\",requestToken:\"fake-request-token\",stateCheckingMechanism:\"polling\"};</script>";

	[Test]
	public async Task PasswordFlowUsesCsrfCookiesHiddenFieldsPkceAndRegistration ()
		{
		var handler = new ScriptedHandler ();
		Func<string> state = Seed (handler);
		handler.Reply ((_, _) => Task.FromResult (Reply (HttpStatusCode.Redirect, "", OAuthProtocol.REDIRECT_URI + "?code=fake-code&state=" + state ())));
		Grant (handler);
		using ComfortCloudClient client = await Fixture.Client (handler);
		LegacyAuthenticationResult result = await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password");
		Assert.That (result.Challenge, Is.Null);
		Assert.That (result.Session!.ClientId, Is.EqualTo ("fake-registered"));
		Assert.That (handler._requests[1].Uri.AbsolutePath, Is.EqualTo ("/u/login"));
		using JsonDocument password = JsonDocument.Parse (handler._requests[2].Body!);
		Assert.That (password.RootElement.GetProperty ("_csrf").GetString (), Is.EqualTo ("fake-csrf"));
		Assert.That (password.RootElement.GetProperty ("state").GetString (), Is.EqualTo ("tenant-state"));
		Assert.That (handler._requests[2].Headers["Cookie"], Does.Contain ("_csrf=fake-csrf"));
		Assert.That (OAuthProtocol.Query (handler._requests[3].Body!)["wresult"], Is.EqualTo ("<result>&value"));
		using JsonDocument exchange = JsonDocument.Parse (handler._requests[4].Body!);
		string verifier = exchange.RootElement.GetProperty ("code_verifier").GetString ()!;
		Assert.That (OAuthProtocol.Challenge (verifier), Is.EqualTo (OAuthProtocol.Query (handler._requests[0].Uri.Query)["code_challenge"]));
		Assert.That (handler._requests[^1].Headers.ContainsKey ("Cookie"), Is.False, "Identity cookies cannot leak into the cloud API.");
		}

	[Test]
	public async Task ExistingAuthorizationCanReturnDirectlyToTheCallback ()
		{
		var handler = new ScriptedHandler ();
		handler.Reply ((request, _) => Task.FromResult (Reply (HttpStatusCode.Redirect, "", OAuthProtocol.REDIRECT_URI + "?code=fake-code&state=" + OAuthProtocol.Query (request.Uri.Query)["state"])));
		Grant (handler);
		using ComfortCloudClient client = await Fixture.Client (handler);
		Assert.That ((await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password")).Session, Is.Not.Null);
		Assert.That (handler._requests.Count, Is.EqualTo (3));
		}

	[Test]
	public async Task MissingCsrfStopsBeforeSendingThePassword ()
		{
		var handler = new ScriptedHandler ();
		_ = Seed (handler, csrf: false);
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password"));
		Assert.That (handler._requests.Count, Is.EqualTo (2));
		Assert.That (client.CurrentSession!.AccessToken, Is.EqualTo ("fake-access-never-live"));
		}

	[Test]
	public async Task WrongFinalStatePreservesExistingCredentials ()
		{
		var handler = new ScriptedHandler ();
		_ = Seed (handler);
		handler.Reply ((_, _) => Task.FromResult (Reply (HttpStatusCode.Redirect, "", OAuthProtocol.REDIRECT_URI + "?code=fake-code&state=wrong")));
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password"));
		Assert.That (client.CurrentSession!.AccessToken, Is.EqualTo ("fake-access-never-live"));
		Assert.That (handler._requests.Count, Is.EqualTo (4));
		}

	[TestCase (false, false)]
	[TestCase (true, false)]
	[TestCase (false, true)]
	[TestCase (true, true)]
	public async Task ClassicMfaSupportsHiddenAndJsonChallengesAndCannotReplay (bool jsonChallenge, bool negotiationNetworkFailure)
		{
		var handler = new ScriptedHandler ();
		_ = Seed (handler, jsonChallenge ? "{\"error\":\"mfa_required\",\"mfa_token\":\"fake-mfa-token\"}" : OTP_FORM, status: jsonChallenge ? HttpStatusCode.Unauthorized : HttpStatusCode.OK);
		using ComfortCloudClient client = await Fixture.Client (handler);
		LegacyMfaChallenge challenge = (await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password")).Challenge!;
		Assert.That (challenge.Kind, Is.EqualTo (LegacyMfaKind.Otp));
		Assert.That (challenge.ToString (), Does.Not.Contain ("fake-mfa-token"));
		if (negotiationNetworkFailure)
			handler.Reply ((_, _) => throw new HttpRequestException ("Fixture negotiation failed."));
		else
			handler.Reply ("{}", HttpStatusCode.BadRequest); // Best-effort challenge negotiation can fail.
		Grant (handler);
		Assert.That ((await client.CompletePasswordMfaAsync (challenge, "000000")).ClientId, Is.EqualTo ("fake-registered"));
		using JsonDocument grant = JsonDocument.Parse (handler._requests[4].Body!);
		Assert.That (grant.RootElement.GetProperty ("grant_type").GetString (), Is.EqualTo ("http://auth0.com/oauth/grant-type/mfa-otp"));
		Assert.That (grant.RootElement.GetProperty ("otp").GetString (), Is.EqualTo ("000000"));
		int requests = handler._requests.Count;
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.CompletePasswordMfaAsync (challenge, "000000"));
		Assert.That (handler._requests.Count, Is.EqualTo (requests));
		}
	[Test]
	public async Task PasswordRejectionDoesNotExposeTheResponseBodyOrReplaceCredentials ()
		{
		var handler = new ScriptedHandler ();
		_ = Seed (handler, "<html>private fake-password detail</html>", status: HttpStatusCode.Forbidden);
		using ComfortCloudClient client = await Fixture.Client (handler);
		ComfortCloudException? error = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password"));
		Assert.That (error!.Message, Does.Not.Contain ("fake-password").And.Not.Contain ("private"));
		Assert.That (client.CurrentSession!.AccessToken, Is.EqualTo ("fake-access-never-live"));
		}
	[Test]
	public async Task CallbackRedirectLoopsAreBounded ()
		{
		var handler = new ScriptedHandler ();
		_ = Seed (handler);
		for (int redirect = 0; redirect < 6; redirect++)
			handler.Reply ((_, _) => Task.FromResult (Reply (HttpStatusCode.Redirect, "", "/loop")));
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password"));
		Assert.That (handler._requests.Count, Is.EqualTo (9));
		}

	[Test]
	public async Task ChallengeFromAnotherClientCannotSubmitOtp ()
		{
		var handler = new ScriptedHandler ();
		_ = Seed (handler, OTP_FORM);
		using ComfortCloudClient original = await Fixture.Client (handler);
		LegacyMfaChallenge challenge = (await original.AuthenticateWithPasswordAsync ("fake-user", "fake-password")).Challenge!;
		var otherHandler = new ScriptedHandler ();
		using ComfortCloudClient other = await Fixture.Client (otherHandler);
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await other.CompletePasswordMfaAsync (challenge, "000000"));
		Assert.That (otherHandler._requests, Is.Empty);
		}

	[Test]
	public async Task RedirectedMfaPageIsRecognized ()
		{
		var handler = new ScriptedHandler ();
		_ = Seed (handler);
		handler.Reply ((_, _) => Task.FromResult (Reply (HttpStatusCode.Redirect, "", "/resume")));
		handler.Reply ((_, _) => Task.FromResult (Reply (HttpStatusCode.Redirect, "", "/mfa")));
		handler.Reply (OTP_FORM);
		using ComfortCloudClient client = await Fixture.Client (handler);
		Assert.That ((await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password")).Challenge!.Kind, Is.EqualTo (LegacyMfaKind.Otp));
		}

	[Test]
	public async Task GuardianPerformsTransactionVerificationPollingFormPostAndCodeExchange ()
		{
		var handler = new ScriptedHandler ();
		Func<string> state = Seed (handler, Guardian ());
		using ComfortCloudClient client = await Fixture.Client (handler);
		LegacyMfaChallenge challenge = (await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password")).Challenge!;
		Assert.That (challenge.Kind, Is.EqualTo (LegacyMfaKind.Guardian));
		handler.Reply ("{\"transactionToken\":\"fake-transaction\"}", HttpStatusCode.Created);
		handler.Reply ("{}", HttpStatusCode.NoContent);
		handler.Reply ("{\"state\":\"pending\"}");
		handler.Reply ("{\"state\":\"accepted\",\"token\":\"fake-signature\"}");
		handler.Reply ((_, _) => Task.FromResult (Reply (HttpStatusCode.Redirect, "", OAuthProtocol.REDIRECT_URI + "?code=fake-code&state=" + state ())));
		Grant (handler);
		Assert.That ((await client.CompletePasswordMfaAsync (challenge, "000000")).ClientId, Is.EqualTo ("fake-registered"));
		Assert.That (handler._requests[3].Headers["Authorization"], Is.EqualTo ("Bearer fake-request-token"));
		Assert.That (handler._requests[4].Headers["Authorization"], Is.EqualTo ("Bearer fake-transaction"));
		Assert.That (OAuthProtocol.Query (handler._requests[7].Body!)["signature"], Is.EqualTo ("fake-signature"));
		}

	[TestCase ("https://untrusted.invalid/guardian")]
	[TestCase ("http://authglb.digital.panasonic.com/guardian")]
	public async Task GuardianRefusesUntrustedAndUnencryptedOrigins (string service)
		{
		var handler = new ScriptedHandler ();
		_ = Seed (handler, Guardian (service));
		using ComfortCloudClient client = await Fixture.Client (handler);
		_ = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password"));
		Assert.That (handler._requests.Count, Is.EqualTo (3));
		}

	[Test]
	public async Task GuardianRejectionDoesNotExchangeCodeOrExposeOtp ()
		{
		var handler = new ScriptedHandler ();
		_ = Seed (handler, Guardian ());
		using ComfortCloudClient client = await Fixture.Client (handler);
		LegacyMfaChallenge challenge = (await client.AuthenticateWithPasswordAsync ("fake-user", "fake-password")).Challenge!;
		handler.Reply ("{\"transaction_token\":\"fake-transaction\"}");
		handler.Reply ("{}");
		handler.Reply ("{\"state\":\"rejected\"}");
		ComfortCloudException? error = await Assert.ThrowsAsync<ComfortCloudException> (async () => await client.CompletePasswordMfaAsync (challenge, "000000"));
		Assert.That (error!.Message, Does.Not.Contain ("000000").And.Not.Contain ("fake-transaction"));
		Assert.That (client.CurrentSession!.AccessToken, Is.EqualTo ("fake-access-never-live"));
		}
	}