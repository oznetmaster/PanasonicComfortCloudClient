// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NUnit.Framework;

using PanasonicComfortCloudClient.ConsoleApp;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class ConsoleArgumentTests
	{
	[Test]
	public void LegacyLoginIsAnExplicitFlagWithoutCommandLineSecrets ()
		{
		CommandLine command = CommandLine.Parse (["login", "--legacy", "--app-version", "5.1.2"]);
		Assert.That (command.Has ("legacy"), Is.True);
		_ = Assert.Throws<ArgumentException> (() => CommandLine.Parse (["login", "--password", "fake-password"]));
		}
	[Test]
	public void UtcOffsetAcceptsSignedMinuteOffsets () => Assert.That (CommandLine.Parse (["--utc-offset", "-03:30"]).UtcOffset (), Is.EqualTo (TimeSpan.FromMinutes (-210)));
	[TestCase ("Europe/London")]
	[TestCase ("+14:01")]
	[TestCase ("+01:60")]
	public void UtcOffsetRejectsNamesAndInvalidValues (string value) => Assert.Throws<ArgumentException> (() => CommandLine.Parse (["--utc-offset", value]).UtcOffset ());

	[Test]
	public void ExplicitApplyFlagIsRequiredForMutation ()
		{
		var command = CommandLine.Parse (["aquarea", "update", "--device", "one", "--file", "changes.json"]);
		_ = Assert.Throws<ArgumentException> (command.RequireApply);
		}
	[Test]
	public void ExplicitApplyIsRecognizedWithoutEatingNextArgument ()
		{
		var command = CommandLine.Parse (["aquarea", "update", "--apply", "--device", "one"]);
		command.RequireApply ();
		Assert.That (command.Require ("device"), Is.EqualTo ("one"));
		}
	[Test]
	public void InteractivePathsMayContainSpaces () => Assert.That (CommandLine.SplitInteractive ("aquarea update --file \"C:\\test files\\update.json\" --apply")[3], Is.EqualTo ("C:\\test files\\update.json"));
	[Test]
	public void IncompleteInteractiveQuoteFails () => Assert.Throws<ArgumentException> (() => CommandLine.SplitInteractive ("devices --session \"unfinished"));
	[Test]
	public void DuplicateOptionsFail () => Assert.Throws<ArgumentException> (() => CommandLine.Parse (["devices", "--device", "one", "--device", "two"]));
	[Test]
	public void UnknownOptionsFailBeforeAnyNetworkCall () => Assert.Throws<ArgumentException> (() => CommandLine.Parse (["devices", "--password", "not-accepted"]));
	[Test]
	public void MissingOptionValueFails () => Assert.Throws<ArgumentException> (() => CommandLine.Parse (["devices", "--device"]));
	}