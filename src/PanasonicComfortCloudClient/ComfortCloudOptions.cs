// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PanasonicComfortCloudClient;

/// <summary>Configures protocol identity, cloud request pacing and application-owned logging.</summary>
public sealed class ComfortCloudOptions
	{
	/// <summary>Gets the separate legacy Aquarea web origin.</summary>
	/// <value>The HTTPS origin to which access-token cookies may be sent.</value>
	public Uri AquareaWebBaseUri { get; init; } = new ("https://aquarea-smart.panasonic.com/");
	/// <summary>Gets the Comfort Cloud service origin.</summary>
	/// <value>The HTTPS cloud API origin.</value>
	public Uri ApiBaseUri
		{
		get; init;
		} = new ("https://accsmart.panasonic.com/");
	/// <summary>Gets the Panasonic identity-service origin.</summary>
	/// <value>The HTTPS Auth0 tenant used by Panasonic ID.</value>
	public Uri AuthenticationBaseUri
		{
		get; init;
		} = new ("https://authglb.digital.panasonic.com/");
	/// <summary>Gets the official application version advertised to the cloud.</summary>
	/// <value>The caller-supplied app version for the selected platform.</value>
	public string AppVersion
		{
		get; init;
		} = "";
	/// <summary>Gets the official application platform identifier.</summary>
	/// <value>One for Android, matching the reference client.</value>
	public int AppType
		{
		get; init;
		} = 1;
	/// <summary>Gets the minimum interval between cloud request starts.</summary>
	/// <value>A nonnegative interval, initially one second.</value>
	public TimeSpan MinimumRequestInterval
		{
		get; init;
		} = TimeSpan.FromSeconds (1);
	/// <summary>Gets the maximum duration of an HTTP operation.</summary>
	/// <value>A positive duration, initially thirty seconds.</value>
	public TimeSpan RequestTimeout
		{
		get; init;
		} = TimeSpan.FromSeconds (30);
	/// <summary>Gets the application-owned logger.</summary>
	/// <value>A logger or the silent null logger; credentials and bodies are never logged.</value>
	public ILogger Logger
		{
		get; init;
		} = NullLogger.Instance;
	}