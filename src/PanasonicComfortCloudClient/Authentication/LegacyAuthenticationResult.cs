// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PanasonicComfortCloudClient.Authentication;

/// <summary>Identifies the upstream password-login MFA protocols.</summary>
public enum LegacyMfaKind
	{
	/// <summary>The Auth0 mfa-otp token grant.</summary>
	Otp,
	/// <summary>The hosted Guardian transaction protocol.</summary>
	Guardian
	}

/// <summary>Contains either a completed password login or a pending one-use MFA challenge.</summary>
public sealed class LegacyAuthenticationResult
	{
	internal LegacyAuthenticationResult (ComfortCloudSession? session, LegacyMfaChallenge? challenge)
		{
		Session = session;
		Challenge = challenge;
		}
	/// <summary>Gets the validated credentials after registration succeeds.</summary>
	/// <value>The session, or null while MFA is pending.</value>
	public ComfortCloudSession? Session
		{
		get;
		}
	/// <summary>Gets the challenge to complete with an OTP.</summary>
	/// <value>A client-bound one-use challenge, or null after completion.</value>
	public LegacyMfaChallenge? Challenge
		{
		get;
		}
	}

/// <summary>Retains pending MFA material in memory without exposing or formatting its tokens.</summary>
public sealed class LegacyMfaChallenge
	{
	private int _consumed;
	internal LegacyMfaChallenge (Guid attempt, OAuthAuthorizationRequest authorization, string? token, GuardianConfiguration? guardian)
		{
		Attempt = attempt;
		Authorization = authorization;
		Token = token;
		Guardian = guardian;
		}
	internal Guid Attempt
		{
		get;
		}
	internal OAuthAuthorizationRequest Authorization
		{
		get;
		}
	internal string? Token
		{
		get;
		}
	internal GuardianConfiguration? Guardian
		{
		get;
		}
	/// <summary>Gets the MFA protocol required by the hosted page.</summary>
	/// <value>Classic OTP or hosted Guardian.</value>
	public LegacyMfaKind Kind => Guardian is null ? LegacyMfaKind.Otp : LegacyMfaKind.Guardian;
	/// <summary>Gets the challenge expiration.</summary>
	/// <value>The original authorization attempt's expiry.</value>
	public DateTimeOffset ExpiresAt => Authorization.ExpiresAt;
	internal void Consume (Guid attempt)
		{
		if (attempt != Attempt || ExpiresAt <= DateTimeOffset.UtcNow || Interlocked.Exchange (ref _consumed, 1) != 0)
			throw new ComfortCloudException ("The MFA challenge is expired, superseded, already used or belongs to another client.");
		}
	/// <summary>Describes the protocol without revealing challenge material.</summary>
	/// <returns>The MFA kind and expiry only.</returns>
	public override string ToString () => $"Panasonic MFA {Kind}, expires {ExpiresAt:O}";
	}

internal sealed class GuardianConfiguration
	{
	internal string ServiceUrl { get; init; } = "";
	internal string PostActionUrl { get; init; } = "";
	internal string RequestToken { get; init; } = "";
	internal string StateTransport { get; init; } = "polling";
	}