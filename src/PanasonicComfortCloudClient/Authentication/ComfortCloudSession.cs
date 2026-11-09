// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PanasonicComfortCloudClient.Authentication;

/// <summary>Contains caller-owned OAuth credentials and a Comfort Cloud client identifier.</summary>
/// <remarks>The library does not persist credentials. The application owns secure storage.</remarks>
public sealed class ComfortCloudSession
	{
	/// <summary>Gets the OAuth access token.</summary>
	/// <value>The bearer credential; never write this value to logs.</value>
	public string AccessToken
		{
		get; init;
		} = "";
	/// <summary>Gets the rotating refresh token.</summary>
	/// <value>The refresh credential, or null when none was issued.</value>
	public string? RefreshToken
		{
		get; init;
		}
	/// <summary>Gets the access-token expiration.</summary>
	/// <value>The UTC instant after which refresh is required.</value>
	public DateTimeOffset ExpiresAt
		{
		get; init;
		}
	/// <summary>Gets the registered cloud client identifier.</summary>
	/// <value>The server-issued identifier, or null before registration.</value>
	public string? ClientId
		{
		get; init;
		}
	/// <summary>Gets the granted OAuth scopes.</summary>
	/// <value>The space-separated scopes, or null when absent.</value>
	public string? Scope
		{
		get; init;
		}
	/// <summary>Returns a description without credentials.</summary>
	/// <returns>A description containing only the expiration instant.</returns>
	public override string ToString () => $"Comfort Cloud session, expires {ExpiresAt:O}";
	}