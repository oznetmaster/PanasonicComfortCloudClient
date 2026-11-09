# Authentication and credentials

The library implements Panasonic's OAuth authorization-code flow with PKCE (`S256`), including `comfortcloud.control` and `a2w.control` scopes. A browser presents Panasonic's own password/MFA screens. It then passes the complete custom-scheme callback to `CompleteAuthorizationAsync`.

```csharp
var request = client.CreateAuthorizationRequest();
// The application presents request.AuthorizationUri and captures its callback.
var session = await client.CompleteAuthorizationAsync(request, callbackUri, token);
```

The callback must have the expected Panasonic scheme/host/path and matching state. Duplicate callback parameters, OAuth errors, expired contexts and reused contexts fail. The PKCE verifier is generated with a cryptographic random-number generator. A failed exchange is not automatically replayed: begin a new authorization attempt if its outcome is uncertain.

The library stores credentials in memory only. The application owns secure persistence and browser presentation. `CurrentSession` supplies a credential snapshot. Refresh-token rotation is retained, and a missing replacement refresh token preserves the existing token. Subscribers to `SessionChanged` run outside the request gate. Persist the latest `CurrentSession` under the application's own storage lock; notifications from concurrent callers should not be treated as a serialized persistence queue. Subscriber errors never replay a completed device operation.

The Windows console protects its session with DPAPI `CurrentUser`, using atomic file replacement. The default file is `%LOCALAPPDATA%\PanasonicComfortCloudClient\default.session`. It cannot simply be copied to another Windows user or a processor and decrypted there. The browser profile is separate from the token file; password saving and general autofill are disabled by the test client. Keep all credentials and browser profiles outside source control.

Supply the current official **Android** app version. This protocol identity is deliberately configurable because Panasonic rejects retired app versions with code `4106`. `AppVersionDiscovery` can explicitly look up the Play Store version with AppBrain fallback, or query the upstream community gist. Its two-day cache can be restored by the application. `UpdateAppVersionAsync` explicitly selects an observed version; a failed device write is never replayed automatically. Code `4103` indicates that the account needs to review and accept updated policies. `GetPendingAgreementsAsync` and explicit acceptance APIs support that process; login never accepts policies.

## Legacy authentication

`AuthenticateWithPasswordAsync` implements the reference's CSRF/hidden-form login. It returns either a session or a `LegacyMfaChallenge`. Complete classic OTP or hosted Guardian verification with `CompletePasswordMfaAsync`. Challenge contexts are bound to the originating client, expire after ten minutes, and are consumed once. Failed attempts do not replace an existing session. Redirect origins, response sizes, total requests and Guardian polling are bounded.

Passwords, OTPs, cookie headers and returned HTML are not logged or retained in session storage. The console's `login --legacy` reads username, password and OTP from prompts (or standard input); password and OTP prompts are masked. There are no command-line password/token arguments. Browser PKCE remains available independently.

Legacy password, OTP and Guardian paths have fake-credential protocol tests; they have not been tested against the real account. Real-device tests reuse the existing browser-authorized session. The explicit legacy agreement and Aquarea cookie APIs do not silently replace current endpoints after a failure.

`Dispose` stops client work but does not log out or delete credentials. `LogoutAsync` is explicit. An injected `HttpClient` remains application-owned and its handler must disable automatic redirects to avoid forwarding custom cloud authorization headers to other origins.
