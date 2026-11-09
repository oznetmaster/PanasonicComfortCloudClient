// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

using PanasonicComfortCloudClient;
using PanasonicComfortCloudClient.Authentication;
using PanasonicComfortCloudClient.ConsoleApp;
using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

return await RunAsync (args);

static async Task<int> RunAsync (string[] arguments)
	{
	using var cancel = new CancellationTokenSource ();
	System.Console.CancelKeyPress += (_, e) =>
	{
		e.Cancel = true;
		cancel.Cancel ();
	};
	if (arguments.Length != 0)
		{
		return await ExecuteAsync (arguments, cancel.Token);
		}

	System.Console.WriteLine ("Panasonic Comfort Cloud — interactive test console. Type help or exit.");
	while (!cancel.IsCancellationRequested)
		{
		System.Console.Write ("comfort-cloud> ");
		var line = System.Console.ReadLine ();
		if (line is null || line.Trim ().Equals ("exit", StringComparison.OrdinalIgnoreCase))
			{
			return 0;
			}

		try
			{
			if (!string.IsNullOrWhiteSpace (line))
				{
				_ = await ExecuteAsync (CommandLine.SplitInteractive (line), cancel.Token);
				}
			}
		catch (ArgumentException ex)
			{
			System.Console.Error.WriteLine (ex.Message);
			}
		}

	return 130;
	}

static async Task<int> ExecuteAsync (string[] args, CancellationToken token)
	{
	try
		{
		var command = CommandLine.Parse (args);
		var action = string.Join (" ", command._words).ToLowerInvariant ();
		if (action is "help" or "" || command.Has ("help"))
			{
			Help ();
			return 0;
			}

		if (action == "app-version")
			{
			using var discovery = new AppVersionDiscovery ();
			System.Console.WriteLine (JsonSerializer.Serialize (await discovery.RefreshAsync (token), ComfortCloudJson.CreateOptions (true)));
			return 0;
			}

		var readCommands = new[] { "devices", "session", "aquarea status", "aquarea energy", "aquarea schedule get", "ac status", "ac energy", "ac schedule get", "hws status", "hws energy", "agreements documents", "agreements status"
};
		var writeCommands = new[] { "aquarea update", "aquarea schedule set", "ac update", "ac schedule set", "hws update", "agreements accept", "test-input export", "logout"
};
		if (action != "login" && !readCommands.Contains (action) && !writeCommands.Contains (action))
			{
			throw new ArgumentException ("Unknown command. Run help.");
			}

		if (writeCommands.Contains (action))
			{
			command.RequireApply ();
			}

		var folder = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "PanasonicComfortCloudClient");
		var store = new SessionStore (command.Get ("session") ?? Path.Combine (folder, "default.session"));
		var preferencesPath = Path.Combine (folder, "preferences.json");
		Preferences preferences = File.Exists (preferencesPath) ? JsonSerializer.Deserialize<Preferences> (await File.ReadAllTextAsync (preferencesPath, token)) ?? new () : new Preferences ();
		var version = command.Get ("app-version") ?? preferences.AppVersion;
		if (string.IsNullOrWhiteSpace (version))
			{
			throw new ArgumentException ("Supply --app-version with the current Android Comfort Cloud app version. It is remembered after successful login.");
			}

		using var client = new ComfortCloudClient (new ComfortCloudOptions
			{
			AppVersion = version
			});
		ComfortCloudSession? saved = store.Load ();
		if (saved is not null)
			{
			await client.RestoreSessionAsync (saved, token);
			}

		var persistenceGate = new object ();
		Exception? persistenceFailure = null;
		client.SessionChanged += (_, _) =>
		{
			lock (persistenceGate)
				{
				try
					{
					if (client.CurrentSession is
							{
								} session)
						{
						store.Save (session);
						}
					}
				catch (Exception ex)
					{
					persistenceFailure = ex;
					}
				}
		};
		JsonSerializerOptions options = ComfortCloudJson.CreateOptions (true);
		async Task PrintAsync<T> (T value)
			{
			var output = JsonSerializer.Serialize (value, options);
			if (command.Get ("out") is string destination)
				{
				await using var file = new FileStream (Path.GetFullPath (destination), FileMode.CreateNew, FileAccess.Write, FileShare.None);
				await using var writer = new StreamWriter (file);
				await writer.WriteAsync (output);
				}
			else
				{
				System.Console.WriteLine (output);
				}
			}

		T Input<T> () => JsonSerializer.Deserialize<T> (File.ReadAllText (command.Require ("file")), options) ?? throw new ArgumentException ("Input JSON contains a null value.");
		string Device () => command.Require ("device");
		DateTime Date () => DateTime.ParseExact (command.Require ("date"), "yyyyMMdd", CultureInfo.InvariantCulture);
		TimeSpan Zone () => command.UtcOffset ();
		EnergyPeriod Period () => Enum.Parse<EnergyPeriod> (command.Require ("period"), true);
		try
			{
			switch (action)
				{
				case "login":
					OAuthAuthorizationRequest authorization = client.CreateAuthorizationRequest ();
					Uri callback;
					using (var loginTimeout = CancellationTokenSource.CreateLinkedTokenSource (token))
						{
						loginTimeout.CancelAfter (TimeSpan.FromMinutes (10));
						if (command.Has ("legacy"))
							{
							if (command.Has ("manual"))
								throw new ArgumentException ("Choose either legacy password login or manual browser login.");
							System.Console.Write ("Panasonic ID: ");
							string userName = System.Console.ReadLine () ?? throw new OperationCanceledException ();
							System.Console.Write ("Password (not echoed): ");
							LegacyAuthenticationResult result = await client.AuthenticateWithPasswordAsync (userName, ReadSecret (loginTimeout.Token), loginTimeout.Token);
							if (result.Challenge is LegacyMfaChallenge challenge)
								{
								System.Console.Write ($"{challenge.Kind} authenticator code (not echoed): ");
								_ = await client.CompletePasswordMfaAsync (challenge, ReadSecret (loginTimeout.Token), loginTimeout.Token);
								}
							}
						else
							{
							if (command.Has ("manual"))
								{
								System.Console.WriteLine ("Open this Panasonic login URL in a browser:");
								System.Console.WriteLine (authorization.AuthorizationUri);
								System.Console.Write ("Paste the complete Panasonic callback URI: ");
								callback = new Uri (System.Console.ReadLine () ?? throw new OperationCanceledException ());
								}
							else
								{
								callback = await LoginBrowser.OpenAsync (authorization.AuthorizationUri, loginTimeout.Token);
								}

							_ = await client.CompleteAuthorizationAsync (authorization, callback, loginTimeout.Token);
							}
						}

					_ = Directory.CreateDirectory (folder);
					preferences.AppVersion = version;
					await File.WriteAllTextAsync (preferencesPath, JsonSerializer.Serialize (preferences), token);
					await PrintAsync (new
						{
						Authenticated = true,
						client.CurrentSession!.ExpiresAt
						});
					break;
				case "devices":
					IReadOnlyList<ComfortCloudGroup> groups = await client.GetGroupsAsync (token);
					await PrintAsync (groups.Select (g => new
						{
						g.Id,
						g.Name,
						Devices = g.Devices.Select (d => new
							{
							d.DeviceGuid,
							d.Name,
							d.Model,
							Family = d.Family.ToString ()
							})
						}));
					break;
				case "session":
					await PrintAsync (new
						{
						Authenticated = client.CurrentSession is not null,
						client.CurrentSession?.ExpiresAt
						});
					break;
				case "aquarea status":
					AquareaSnapshot snapshot = await client.GetAquareaStatusAsync (Device (), command.Has ("cached") ? StatusSource.Cached : StatusSource.Live, token);
					await PrintAsync (new
						{
						Source = snapshot.Source.ToString (),
						snapshot.ObservedAt,
						Data = snapshot
						});
					break;
				case "aquarea energy":
					await PrintAsync (await client.GetAquareaEnergyAsync (Device (), Period (), Date (), Zone (), token));
					break;
				case "aquarea schedule get":
					await PrintAsync (await client.GetAquareaScheduleAsync (Device (), token));
					break;
				case "aquarea schedule set":
					await client.SetAquareaScheduleAsync (Device (), Input<AquareaSchedule> (), token);
					break;
				case "aquarea update":
					await client.UpdateAquareaAsync (Device (), Input<AquareaUpdate> (), token);
					break;
				case "ac status":
					await PrintAsync (await client.GetAcStatusAsync (Device (), command.Has ("cached") ? StatusSource.Cached : StatusSource.Live, token));
					break;
				case "ac energy":
					await PrintAsync (await client.GetAcEnergyAsync (Device (), Enum.Parse<AcEnergyPeriod> (command.Require ("period"), true), Date (), Zone (), token));
					break;
				case "ac schedule get":
					await PrintAsync (await client.GetAcScheduleAsync (Device (), token));
					break;
				case "ac schedule set":
					await client.SetAcScheduleAsync (Input<AcSchedule> (), token);
					break;
				case "ac update":
					await client.UpdateAcAsync (Device (), Input<AcUpdate> (), token);
					break;
				case "hws status":
					await PrintAsync (await client.GetHotWaterStatusAsync (Device (), token));
					break;
				case "hws energy":
					await PrintAsync (await client.GetHotWaterEnergyAsync (Device (), Period (), Date (), Zone (), token));
					break;
				case "hws update":
					await client.UpdateHotWaterAsync (Device (), Input<HotWaterUpdate> (), token);
					break;
				case "agreements documents":
					await PrintAsync (await client.GetAgreementDocumentsAsync (true, cancellationToken: token));
					break;
				case "agreements status":
					await PrintAsync (await client.GetAgreementStatusAsync (token));
					break;
				case "agreements accept":
					await client.AcceptAgreementsAsync (Input<List<AgreementAcceptance>> (), token);
					break;
				case "test-input export":
					var destination = command.Require ("out");
					var testDevice = Device ();
					TimeSpan testZone = Zone ();
					if (client.CurrentSession is null)
						{
						throw new ArgumentException ("Log in before exporting private test inputs.");
						}

					if (client.CurrentSession.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes (5))
						{
						_ = await client.RefreshSessionAsync (token);
						}

					var testInput = new
						{
						Enabled = false,
						AppVersion = version,
						GatewayId = testDevice,
						UtcOffset = testZone,
						Session = client.CurrentSession
						};
					await using (var output = new FileStream (Path.GetFullPath (destination), FileMode.CreateNew, FileAccess.Write, FileShare.None))
						{
						await JsonSerializer.SerializeAsync (output, testInput, cancellationToken: token);
						}

					System.Console.WriteLine ("Private live-test credentials exported. Keep this file out of source control and reports; transfer only through the authenticated test runner.");
					break;
				case "logout":
					await client.LogoutAsync (token);
					store.Delete ();
					break;
				}
			}
		finally
			{
			lock (persistenceGate)
				{
				if (client.CurrentSession is
						{
							} session)
					{
					store.Save (session);
					}
				}
			}

		if (persistenceFailure is not null)
			{
			throw new IOException ("Session persistence failed. The cloud operation was not repeated.", persistenceFailure);
			}

		if (writeCommands.Contains (action) && action != "test-input export")
			{
			System.Console.WriteLine ("Cloud acknowledgement received. Read status separately to confirm the device state.");
			}

		return 0;
		}
	catch (OperationCanceledException)
		{
		System.Console.Error.WriteLine ("Operation cancelled; an in-flight device change may need a status check.");
		return 130;
		}
	catch (ComfortCloudException ex)
		{
		System.Console.Error.WriteLine ($"{ex.Message} HTTP={ex.StatusCode}; code={ex.ErrorCode}; retry-after={ex.RetryAfter}");
		return 3;
		}
	catch (Exception ex)
		{
		System.Console.Error.WriteLine (ex.Message);
		return 2;
		}
	}

static string ReadSecret (CancellationToken token)
	{
	if (System.Console.IsInputRedirected)
		return System.Console.ReadLine () ?? throw new OperationCanceledException ();
	var secret = new System.Text.StringBuilder ();
	while (true)
		{
		token.ThrowIfCancellationRequested ();
		ConsoleKeyInfo key = System.Console.ReadKey (true);
		if (key.Key == ConsoleKey.Enter)
			{
			System.Console.WriteLine ();
			return secret.ToString ();
			}

		if (key.Key == ConsoleKey.Escape)
			throw new OperationCanceledException (token);
		if (key.Key == ConsoleKey.Backspace)
			{
			if (secret.Length != 0)
				secret.Length--;
			}
		else if (!char.IsControl (key.KeyChar))
			{
			_ = secret.Append (key.KeyChar);
			}
		}
	}

static void Help () => System.Console.WriteLine ("""
Panasonic Comfort Cloud — Windows interactive and command-line test client
  Run with no arguments for the interactive prompt. Type exit to leave.
  login --app-version VERSION             Panasonic browser login, including MFA
  login --legacy --app-version VERSION    Password/OTP prompts; secrets are never command-line arguments
  app-version                            Explicit Play Store/AppBrain version lookup; no cloud command replay
  devices                                 List groups and detected device families
  session                                 Show credential expiration only
  aquarea status --device GUID [--cached]  Read live or explicitly cached state
  aquarea energy --device GUID --period Day|Month|Year --date YYYYMMDD --utc-offset +HH:MM
  aquarea schedule get --device GUID [--out NEW-FILE]
  aquarea update --device GUID --file UPDATE.json --apply
  aquarea schedule set --device GUID --file SCHEDULE.json --apply
  ac status|energy|schedule get|schedule set|update    Equivalent AC operations
  hws status|energy|update                           Dedicated HWS operations
  agreements documents|status
  agreements accept --file REVIEWED-VERSIONS.json --apply
  test-input export --device GUID --utc-offset +HH:MM --out PRIVATE-FILE --apply
  logout --apply
Global: --session PATH overrides the DPAPI-protected current-user session file.
No passwords or tokens are accepted on the command line. No control is sent without --apply.
Cloud acknowledgement is not physical confirmation. Device controls are never retried automatically.
""");

internal sealed class Preferences
	{
	public string? AppVersion
		{
		get; set;
		}
	}