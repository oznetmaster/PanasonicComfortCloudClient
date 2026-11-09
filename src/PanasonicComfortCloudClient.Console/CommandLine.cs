// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PanasonicComfortCloudClient.ConsoleApp;

internal sealed class CommandLine
	{
	internal readonly List<string> _words = [];
	private readonly Dictionary<string, string> _options = new (StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> _flags = new (StringComparer.OrdinalIgnoreCase) { "apply", "cached", "help", "manual", "legacy"
};
	internal static CommandLine Parse (string[] args)
		{
		var parsed = new CommandLine ();
		for (int i = 0; i < args.Length; i++)
			{
			if (!args[i].StartsWith ("--", StringComparison.Ordinal))
				{
				parsed._words.Add (args[i]);
				continue;
				}

			var name = args[i][2..];
			if (name.Length == 0 || parsed._options.ContainsKey (name))
				{
				throw new ArgumentException ("Invalid or duplicate option.");
				}

			if (_flags.Contains (name))
				{
				parsed._options.Add (name, "true");
				}
			else
				{
				if (++i >= args.Length || args[i].StartsWith ("--", StringComparison.Ordinal))
					{
					throw new ArgumentException ("Missing option value: " + name);
					}

				parsed._options.Add (name, args[i]);
				}
			}

		var known = new HashSet<string> (_flags, StringComparer.OrdinalIgnoreCase) { "app-version", "session", "device", "period", "date", "utc-offset", "file", "out"
};
		return parsed._options.Keys.Any (k => !known.Contains (k)) ? throw new ArgumentException ("Unknown command-line option.") : parsed;
		}
	internal string? Get (string name) => _options.TryGetValue (name, out var value) ? value : null;
	internal string Require (string name) => Get (name) is string value && !string.IsNullOrWhiteSpace (value) ? value : throw new ArgumentException ("Missing --" + name + ".");
	internal bool Has (string name) => _options.ContainsKey (name);
	internal TimeSpan UtcOffset ()
		{
		var text = Require ("utc-offset");
		if (text.Length != 6 || text[0] is not ('+' or '-') || text[3] != ':' || new[] { text[1], text[2], text[4], text[5]
}.Any (c => c is < '0' or > '9'))
			{
			throw new ArgumentException ("Supply --utc-offset as +HH:MM or -HH:MM, not a timezone name.");
			}

		int hours = (text[1] - '0') * 10 + text[2] - '0', minutes = (text[4] - '0') * 10 + text[5] - '0';
		return minutes > 59 || hours > 14 || (hours == 14 && minutes != 0)
			? throw new ArgumentException ("The UTC offset is outside -14:00 through +14:00.")
			: TimeSpan.FromMinutes ((hours * 60 + minutes) * (text[0] == '-' ? -1 : 1));
		}
	internal void RequireApply ()
		{
		if (!Has ("apply"))
			{
			throw new ArgumentException ("This changes the account or device. Add --apply to execute the explicit operation.");
			}
		}
	internal static string[] SplitInteractive (string line)
		{
		var tokens = new List<string> ();
		var current = new System.Text.StringBuilder ();
		char quote = '\0';
		foreach (var c in line)
			{
			if (quote != '\0')
				{
				if (c == quote)
					{
					quote = '\0';
					}
				else
					{
					_ = current.Append (c);
					}
				}
			else if (c is '\'' or '"')
				{
				quote = c;
				}
			else if (char.IsWhiteSpace (c))
				{
				if (current.Length > 0)
					{
					tokens.Add (current.ToString ());
					_ = current.Clear ();
					}
				}
			else
				{
				_ = current.Append (c);
				}
			}

		if (quote != '\0')
			{
			throw new ArgumentException ("Unclosed quote.");
			}

		if (current.Length > 0)
			{
			tokens.Add (current.ToString ());
			}

		return [.. tokens];
		}
	}