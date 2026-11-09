// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PanasonicComfortCloudClient.Authentication;

/// <summary>Contains the application-owned context for one OAuth PKCE authorization attempt.</summary>
public sealed class OAuthAuthorizationRequest
	{
	private int _consumed;
	/// <summary>Gets the Panasonic login URL to present in a browser.</summary>
	/// <value>The authorization URI, including the random state and PKCE challenge.</value>
	public Uri AuthorizationUri
		{
		get; init;
		} = null!;
	/// <summary>Gets the PKCE verifier.</summary>
	/// <value>A secret verifier to retain securely until the callback is exchanged.</value>
	public string CodeVerifier
		{
		get; init;
		} = "";
	/// <summary>Gets the callback correlation state.</summary>
	/// <value>The unpredictable state that must match the browser callback.</value>
	public string State
		{
		get; init;
		} = "";
	/// <summary>Gets the context expiration.</summary>
	/// <value>The latest UTC instant at which the callback may be used.</value>
	public DateTimeOffset ExpiresAt
		{
		get; init;
		}
	internal void Consume ()
		{
		if (ExpiresAt <= DateTimeOffset.UtcNow)
			{
			throw new ComfortCloudException ("The authorization attempt expired; start a new attempt.");
			}

		if (Interlocked.Exchange (ref _consumed, 1) != 0)
			{
			throw new ComfortCloudException ("The authorization attempt has already been used.");
			}
		}
	/// <summary>Returns a description without the verifier or state.</summary>
	/// <returns>A description containing only the authorization expiration.</returns>
	public override string ToString () => $"Panasonic authorization, expires {ExpiresAt:O}";
	}