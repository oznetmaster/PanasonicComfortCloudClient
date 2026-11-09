// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PanasonicComfortCloudClient.Authentication;

/// <summary>Supplies a validated session snapshot after credentials change.</summary>
/// <remarks>Creates the session notification.</remarks>
/// <param name="session">The new application-owned credential snapshot.</param>
public sealed class SessionChangedEventArgs (ComfortCloudSession session) : EventArgs
	{
	/// <summary>Gets the new credential snapshot.</summary>
	/// <value>The validated session; applications should protect it when persisting.</value>
	public ComfortCloudSession Session
		{
		get;
		} = session;
	}