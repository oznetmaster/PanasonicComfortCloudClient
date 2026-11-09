// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace PanasonicComfortCloudClient.Models;

public sealed partial class AquareaStatus
	{
	/// <summary>Gets or sets the bivalent capability.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("bivalent")]
	public int? Bivalent
		{
		get; set;
		}

	/// <summary>Gets or sets the actual bivalent indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("bivalentActual")]
	public int? BivalentActual
		{
		get; set;
		}

	/// <summary>Gets or sets the multiple outdoor-unit indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("multiOdConnection")]
	public int? MultiOutdoorConnection
		{
		get; set;
		}

	/// <summary>Gets or sets the control-box indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("controlBox")]
	public int? ControlBox
		{
		get; set;
		}

	/// <summary>Gets or sets the external-heater indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("externalHeater")]
	public int? ExternalHeater
		{
		get; set;
		}

	/// <summary>Gets or sets the model-series selection.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("modelSeriesSelection")]
	public int? ModelSeries
		{
		get; set;
		}

	/// <summary>Gets or sets the standalone operation indicator.</summary>
	/// <value>The reported value, or null when absent.</value>
	[JsonPropertyName ("standAlone")]
	public int? Standalone
		{
		get; set;
		}
	}