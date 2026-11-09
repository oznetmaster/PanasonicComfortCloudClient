// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

using NUnit.Framework;

using PanasonicComfortCloudClient.Authentication;
using PanasonicComfortCloudClient.Models;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture, Category ("Live")]
public sealed class LiveAquareaTests
	{
	private static readonly JsonSerializerOptions _settingsJson = new () { PropertyNameCaseInsensitive = true };
	private static async Task<(ComfortCloudClient Client, LiveSettings Settings)> Connect ()
		{
		var folder = TestContext.Parameters.Get ("TestDataDirectory", AppContext.BaseDirectory);
		var path = TestContext.Parameters.Get ("ComfortCloudSettingsPath", Path.Combine (folder, "ComfortCloudLiveSettings.json"));
		if (!File.Exists (path))
			{
			Assert.Ignore ("Private Aquarea live-test settings were not supplied.");
			}

		LiveSettings settings;
		try
			{
			settings = JsonSerializer.Deserialize<LiveSettings> (File.ReadAllText (path), _settingsJson) ?? throw new InvalidDataException ();
			}
		catch (JsonException)
			{
			throw new InvalidDataException ("Invalid private live-test settings. Contents are not logged.");
			}

		if (!TestContext.Parameters.Get ("EnableLiveTests", settings.Enabled))
			{
			Assert.Ignore ("Live testing is not explicitly enabled.");
			}

		if (settings.Session is null || settings.Session.ExpiresAt < DateTimeOffset.UtcNow.AddMinutes (5) || string.IsNullOrWhiteSpace (settings.Session.ClientId))
			{
			Assert.Ignore ("A freshly authorized registered session is required; these read-only tests do not rotate stored credentials.");
			}

		if (string.IsNullOrWhiteSpace (settings.AppVersion) || string.IsNullOrWhiteSpace (settings.GatewayId))
			{
			throw new InvalidDataException ("App version and expected Aquarea gateway ID are required.");
			}

		var client = new ComfortCloudClient (new ComfortCloudOptions
			{
			AppVersion = settings.AppVersion
			});
		await client.RestoreSessionAsync (settings.Session!);
		return (client, settings);
		}

	[Test]
	public async Task ReadDiscovery ()
		{
		(ComfortCloudClient? client, LiveSettings? settings) = await Connect ();
		using (client)
			{
			IReadOnlyList<ComfortCloudGroup> groups = await client.GetGroupsAsync ();
			Assert.That (groups.SelectMany (g => g.Devices).Any (d => d.DeviceGuid == settings.GatewayId && d.Family == DeviceFamily.Aquarea), Is.True, "Expected Aquarea device was not discovered.");
			}
		}
	[Test]
	public async Task ReadLiveStatus ()
		{
		(ComfortCloudClient? client, LiveSettings? settings) = await Connect ();
		using (client)
			{
			AquareaSnapshot snapshot = await client.GetAquareaStatusAsync (settings.GatewayId);
			Assert.That (snapshot.Source, Is.EqualTo (StatusSource.Live));
			Assert.That (snapshot.Status, Is.Not.Null);
			}
		}
	[Test]
	public async Task ReadEnergyHistory ()
		{
		(ComfortCloudClient? client, LiveSettings? settings) = await Connect ();
		using (client)
			{
			IReadOnlyList<EnergyHistoryEntry> history = await client.GetAquareaEnergyAsync (settings.GatewayId, EnergyPeriod.Day, DateTime.Today, settings.UtcOffset);
			Assert.That (history, Is.Not.Null);
			}
		}
	[Test]
	public async Task ReadWeeklySchedule ()
		{
		(ComfortCloudClient? client, LiveSettings? settings) = await Connect ();
		using (client)
			{
			AquareaSchedule schedule = await client.GetAquareaScheduleAsync (settings.GatewayId);
			Assert.That (schedule.Timer, Is.Not.Null);
			}
		}

	private sealed class LiveSettings
		{
		public bool Enabled
			{
			get; set;
			}
		public string AppVersion
			{
			get; set;
			} = "";
		public string GatewayId
			{
			get; set;
			} = "";
		public TimeSpan UtcOffset
			{
			get; set;
			}
		public ComfortCloudSession? Session
			{
			get; set;
			}
		}
	}