// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

using PanasonicComfortCloudClient.Models;
using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient.Tests;

internal static class AquareaControlRoundTrip
	{
	internal static async Task Run (ComfortCloudClient client, string gateway, string setting, string evidenceRoot,
		 Action<string> report, TimeSpan? interval = null, TimeSpan? observationWindow = null)
		{
		_ = Directory.CreateDirectory (evidenceRoot);
		JsonSerializerOptions json = ComfortCloudJson.CreateOptions (true);
		var journalPath = Path.Combine (evidenceRoot, "control-journal.json");
		if (File.Exists (journalPath) && JsonSerializer.Deserialize<Journal> (File.ReadAllText (journalPath), json)?.Restored != true)
			{
			throw new InvalidOperationException ("A prior control test has unconfirmed restoration. No further change is permitted.");
			}

		string run = setting + "-" + Guid.NewGuid ().ToString ("N");
		string directory = Path.Combine (evidenceRoot, run);
		_ = Directory.CreateDirectory (directory);
		void Save (string name, object value) => File.WriteAllText (Path.Combine (directory, name + ".json"), JsonSerializer.Serialize (value, json));
		void JournalSave (Journal value)
			{
			string temporary = journalPath + ".tmp";
			using (var file = new FileStream (temporary, FileMode.Create, FileAccess.Write, FileShare.None))
				{
				var bytes = JsonSerializer.SerializeToUtf8Bytes (value, json);
				file.Write (bytes, 0, bytes.Length);
				file.Flush (true);
				}

			if (File.Exists (journalPath))
				{
				File.Replace (temporary, journalPath, null);
				}
			else
				{
				File.Move (temporary, journalPath);
				}
			}

		using var budget = new CancellationTokenSource (TimeSpan.FromMinutes (2));
		AquareaSnapshot before = await client.GetAquareaStatusAsync (gateway, cancellationToken: budget.Token);
		if (before.IsOwner != true || before.Status?.UncontrollableGateway != false)
			{
			throw new InvalidOperationException ("Verified ownership and available gateway control are required.");
			}

		Configuration original = Capture (before.Status);
		if (original.Zones.Length == 0 || original.Zones.Any (z => z.Id is null || z.Power is not (PowerState.Off or PowerState.On)) || original.Zones.Select (z => z.Id).Distinct ().Count () != original.Zones.Length)
			{
			throw new InvalidOperationException ("Known unique zone identities and operation states are required.");
			}

		AquareaSchedule schedule = await client.GetAquareaScheduleAsync (gateway, budget.Token);
		if (schedule.Timer?.Operation != 0)
			{
			throw new InvalidOperationException ("These short tests require the weekly schedule to be disabled already; the test does not change it.");
			}

		string scheduleBefore = JsonSerializer.Serialize (schedule, json);
		AquareaUpdate change, restore;
		Configuration expected;
		if (setting == "quiet")
			{
			if (original.Quiet != QuietMode.Off)
				{
				throw new InvalidOperationException ("The quiet test requires quiet mode already off.");
				}

			change = new ()
				{
				Quiet = QuietMode.Level1
				};
			restore = new ()
				{
				Quiet = original.Quiet
				};
			expected = original with
				{
				Quiet = QuietMode.Level1
				};
			}
		else if (setting == "tank-temperature")
			{
			AquareaTank? tank = before.Status.Tank;
			if (original.Mode is not (AquareaReportedMode.Heat or AquareaReportedMode.Cool or AquareaReportedMode.AutoHeat or AquareaReportedMode.AutoCool) || tank?.Power is not (PowerState.Off or PowerState.On) ||
				 tank.HeatSetpoint is not double value || tank.HeatMinimum is not double minimum || tank.HeatMaximum is not double maximum ||
				 double.IsNaN (value) || double.IsInfinity (value) || value - 1 < minimum || value > maximum)
				{
				throw new InvalidOperationException ("A known mode, tank state and bounded target permitting a one-degree reduction are required.");
				}

			change = new ()
				{
				Tank = new ()
					{
					HeatSetpoint = value - 1
					}
				};
			restore = new ()
				{
				Tank = new ()
					{
					HeatSetpoint = value
					}
				};
			expected = original with
				{
				Tank = original.Tank! with
					{
					Target = value - 1
					}
				};
			}
		else
			{
			throw new ArgumentException ("Unsupported bounded control test.", nameof (setting));
			}

		Save ("before", before);
		Save ("schedule-before", schedule);
		Save ("change", change);
		Save ("restore", restore);
		Configuration stable = Capture ((await client.GetAquareaStatusAsync (gateway, cancellationToken: budget.Token)).Status!, original);
		if (Serialize (stable) != Serialize (original))
			{
			throw new InvalidOperationException ("Configuration changed during preflight. No command was sent.");
			}

		var journal = new Journal
			{
			Run = run,
			Setting = setting,
			Original = original,
			Expected = expected
			};
		JournalSave (journal);
		Exception? changeFailure = null;
		Exception? restorationFailure = null;
		try
			{
			journal.Attempted = true;
			JournalSave (journal);
			// Exactly one change request. A lost acknowledgement never replays it.
			await client.UpdateAquareaAsync (gateway, change, budget.Token);
			await Confirm (expected, original, "changed", budget.Token);
			journal.ChangeConfirmed = true;
			JournalSave (journal);
			}
		catch (Exception ex)
			{
			changeFailure = ex;
			}
		finally
			{
			try
				{
				await Restore ();
				}
			catch (Exception ex)
				{
				restorationFailure = ex;
				JournalSave (journal);
				report ("RESTORATION UNCONFIRMED. Further control tests are blocked. Run=" + run);
				}
			}

		if (restorationFailure is not null)
			{
			throw new AggregateException ("Restoration was not confirmed; inspect the saved baseline and journal before further changes.", changeFailure is null ? [restorationFailure] : [changeFailure, restorationFailure]);
			}

		if (changeFailure is not null)
			{
			throw new InvalidOperationException ("The change test failed; restoration of the original configuration was confirmed.", changeFailure);
			}

		async Task Restore ()
			{
			using var restoration = new CancellationTokenSource (TimeSpan.FromSeconds (60));
			Configuration current = Capture ((await client.GetAquareaStatusAsync (gateway, cancellationToken: restoration.Token)).Status!, original);
			bool originalTarget = TargetEqual (current, original, setting), expectedTarget = TargetEqual (current, expected, setting);
			if (!originalTarget && !expectedTarget)
				{
				throw new InvalidOperationException ("The target has a third-party value; refusing to overwrite an unowned change.");
				}

			if (!originalTarget)
				{
				await client.UpdateAquareaAsync (gateway, restore, restoration.Token);
				}

			await Confirm (original, original, "restored", restoration.Token);
			AquareaSchedule finalSchedule = await client.GetAquareaScheduleAsync (gateway, restoration.Token);
			if (JsonSerializer.Serialize (finalSchedule, json) != scheduleBefore)
				{
				throw new InvalidOperationException ("The weekly schedule changed during the test.");
				}

			journal.Restored = true;
			JournalSave (journal);
			report (JsonSerializer.Serialize (new
				{
				Setting = setting,
				journal.ChangeConfirmed,
				RestorationConfirmed = true,
				Original = original,
				Tested = expected,
				SchedulePreserved = true
				}, json));
			}

		async Task Confirm (Configuration desired, Configuration mask, string phase, CancellationToken token)
			{
			var clock = System.Diagnostics.Stopwatch.StartNew ();
			int matches = 0, observation = 0;
			TimeSpan window = observationWindow ?? TimeSpan.FromSeconds (30);
			TimeSpan pause = interval ?? TimeSpan.FromSeconds (3);
			while (clock.Elapsed < window)
				{
				AquareaSnapshot snapshot = await client.GetAquareaStatusAsync (gateway, cancellationToken: token);
				Save (phase + "-" + (++observation), snapshot);
				Configuration state = Capture (snapshot.Status!, mask);
				if (!OthersEqual (state, original, setting))
					{
					throw new InvalidOperationException ("An unrelated reported configuration value changed during " + phase + ".");
					}

				matches = Serialize (state) == Serialize (desired) ? matches + 1 : 0;
				if (matches == 2)
					{
					return;
					}

				await Task.Delay (pause, token);
				}

			throw new TimeoutException ("Two consecutive live observations did not confirm " + phase + ".");
			}
		}

	private static string Serialize (Configuration value) => JsonSerializer.Serialize (value);
	private static bool TargetEqual (Configuration left, Configuration right, string setting) => setting == "quiet" ? left.Quiet == right.Quiet : left.Tank?.Target == right.Tank?.Target;
	private static bool OthersEqual (Configuration value, Configuration baseline, string setting) => Serialize (setting == "quiet" ? value with
		{
		Quiet = baseline.Quiet
		} : value with
			{
			Tank = value.Tank is null ? null : value.Tank with
				{
				Target = baseline.Tank?.Target
				}
			}) == Serialize (baseline);
	private static Configuration Capture (AquareaStatus status, Configuration? mask = null) => new ()
		{
		Mode = status.Mode,
		Power = mask is not null && mask.Power is null ? null : status.Power,
		Quiet = status.Quiet,
		ForceHotWater = status.ForceHotWater,
		ForceHeater = status.ForceHeater,
		Holiday = status.HolidayTimer,
		Powerful = status.Powerful,
		Special = status.Special,
		Tank = status.Tank is null ? null : new (status.Tank.Power, status.Tank.HeatSetpoint),
		Zones = [.. (status.Zones ?? []).OrderBy (z => z.Id).Select (z =>
		{
			Zone? old = mask?.Zones.SingleOrDefault (v => v.Id == z.Id);
			return new Zone (z.Id, z.Power, mask is not null && old?.Heat is null ? null : z.HeatSetpoint, mask is not null && old?.Cool is null ? null : z.CoolSetpoint);
		})]
		};
	internal sealed record Configuration
		{
		public AquareaReportedMode? Mode
			{
			get; init;
			}
		public PowerState? Power
			{
			get; init;
			}
		public QuietMode? Quiet
			{
			get; init;
			}
		public PowerState? ForceHotWater
			{
			get; init;
			}
		public PowerState? ForceHeater
			{
			get; init;
			}
		public PowerState? Holiday
			{
			get; init;
			}
		public PowerfulDuration? Powerful
			{
			get; init;
			}
		public SpecialStatus? Special
			{
			get; init;
			}
		public Tank? Tank
			{
			get; init;
			}
		public Zone[] Zones
			{
			get; init;
			} = [];
		}
	internal sealed record Tank (PowerState? Power, double? Target);
	internal sealed record Zone (int? Id, PowerState? Power, double? Heat, double? Cool);
	internal sealed class Journal
		{
		public string Run
			{
			get; set;
			} = "";
		public string Setting
			{
			get; set;
			} = "";
		public Configuration? Original
			{
			get; set;
			}
		public Configuration? Expected
			{
			get; set;
			}
		public bool Attempted
			{
			get; set;
			}
		public bool ChangeConfirmed
			{
			get; set;
			}
		public bool Restored
			{
			get; set;
			}
		}
	}