// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

using NUnit.Framework;

using PanasonicComfortCloudClient.Authentication;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture, Category ("Live"), Category ("LiveControl"), NonParallelizable]
public sealed class LiveAquareaControlTests
	{
	[Test]
	public Task QuietLevelRoundTrip () => Run ("quiet");
	[Test, DependsOnTest (nameof (QuietLevelRoundTrip))]
	public Task TankTemperatureRoundTrip () => Run ("tank-temperature");

	private static async Task Run (string setting)
		{
		var folder = TestContext.Parameters.Get ("TestDataDirectory", AppContext.BaseDirectory);
		var file = TestContext.Parameters.Get ("ComfortCloudSettingsPath", Path.Combine (folder, "ComfortCloudLiveSettings.json"));
		if (!File.Exists (file))
			{
			Assert.Ignore ("Private control-test inputs were not supplied.");
			}

		Settings settings = JsonSerializer.Deserialize<Settings> (File.ReadAllText (file)) ?? throw new InvalidDataException ("Invalid private test settings.");
		if (!TestContext.Parameters.Get ("EnableLiveTests", settings.Enabled) || !settings.AllowDeviceChanges || !Guid.TryParse (settings.ControlRunId, out _))
			{
			Assert.Ignore ("Live control needs explicit suite selection, AllowDeviceChanges and a unique ControlRunId.");
			}

		if (settings.Session is null || settings.Session.ExpiresAt < DateTimeOffset.UtcNow.AddMinutes (10) || string.IsNullOrWhiteSpace (settings.Session.ClientId))
			{
			throw new InvalidOperationException ("Fresh registered credentials are required before physical tests; the test does not reauthenticate.");
			}

		if (string.IsNullOrWhiteSpace (settings.GatewayId) || string.IsNullOrWhiteSpace (settings.AppVersion))
			{
			throw new InvalidDataException ("Expected gateway and app version are required.");
			}

		using var client = new ComfortCloudClient (new ComfortCloudOptions
			{
			AppVersion = settings.AppVersion
			});
		await client.RestoreSessionAsync (settings.Session);
		await AquareaControlRoundTrip.Run (client, settings.GatewayId, setting, Path.Combine (folder, "ControlEvidence"), TestContext.WriteLine);
		}
	private sealed class Settings
		{
		public bool Enabled
			{
			get; set;
			}
		public bool AllowDeviceChanges
			{
			get; set;
			}
		public string ControlRunId
			{
			get; set;
			} = "";
		public string GatewayId
			{
			get; set;
			} = "";
		public string AppVersion
			{
			get; set;
			} = "";
		public ComfortCloudSession? Session
			{
			get; set;
			}
		}
	}