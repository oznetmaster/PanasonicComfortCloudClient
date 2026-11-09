// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

using PanasonicComfortCloudClient.Models;

namespace PanasonicComfortCloudClient.Protocol;

internal sealed class ErrorEnvelope
	{
	[JsonPropertyName ("code")]
	public string? Code
		{
		get; set;
		}
	[JsonPropertyName ("error")]
	public string? Error
		{
		get; set;
		}
	[JsonPropertyName ("result")]
	public int? Result
		{
		get; set;
		}
	}
internal sealed class GroupEnvelope
	{
	[JsonPropertyName ("groupList")]
	public List<ComfortCloudGroup>? Groups
		{
		get; set;
		}
	}
internal sealed class HistoryEnvelope
	{
	[JsonPropertyName ("historyDataList")]
	public List<EnergyHistoryEntry>? Entries
		{
		get; set;
		}
	}
internal sealed class HistoryEnvelope<T> where T : class
	{
	[JsonPropertyName ("historyDataList")]
	public List<T>? Entries
		{
		get; set;
		}
	}
internal sealed class AgreementEnvelope
	{
	[JsonPropertyName ("documentList")]
	public List<AgreementDocument>? Documents
		{
		get; set;
		}
	[JsonPropertyName ("agreementList")]
	public List<AgreementAcceptance>? Agreements
		{
		get; set;
		}
	}
internal sealed class RegistrationRequest
	{
	[JsonPropertyName ("language")]
	public int Language
		{
		get; set;
		}
	}
internal sealed class RegistrationResponse
	{
	[JsonPropertyName ("clientId")]
	public string? ClientId
		{
		get; set;
		}
	}
internal sealed class TokenRequest
	{
	[JsonPropertyName ("client_id")]
	public string ClientId
		{
		get; set;
		} = OAuthProtocol.CLIENT_ID;
	[JsonPropertyName ("grant_type")]
	public string GrantType
		{
		get; set;
		} = "authorization_code";
	[JsonPropertyName ("scope")]
	public string? Scope
		{
		get; set;
		}
	[JsonPropertyName ("code")]
	public string? Code
		{
		get; set;
		}
	[JsonPropertyName ("code_verifier")]
	public string? CodeVerifier
		{
		get; set;
		}
	[JsonPropertyName ("redirect_uri")]
	public string? RedirectUri
		{
		get; set;
		}
	[JsonPropertyName ("refresh_token")]
	public string? RefreshToken
		{
		get; set;
		}
	}
internal sealed class TokenResponse
	{
	[JsonPropertyName ("access_token")]
	public string? AccessToken
		{
		get; set;
		}
	[JsonPropertyName ("refresh_token")]
	public string? RefreshToken
		{
		get; set;
		}
	[JsonPropertyName ("expires_in")]
	public int? ExpiresIn
		{
		get; set;
		}
	[JsonPropertyName ("scope")]
	public string? Scope
		{
		get; set;
		}
	}
internal sealed class TransferRequest<T>
	{
	[JsonPropertyName ("apiName")]
	public string ApiName
		{
		get; set;
		} = "";
	[JsonPropertyName ("requestMethod")]
	public string Method
		{
		get; set;
		} = "GET";
	[JsonPropertyName ("bodyParam")]
	public T? Body
		{
		get; set;
		}
	}
internal sealed class AquareaControlRequest : ExtensibleModel
	{
	[JsonPropertyName ("gwid")]
	public string GatewayId
		{
		get; set;
		} = "";
	[JsonPropertyName ("operationStatus")]
	public PowerState? Power
		{
		get; set;
		}
	[JsonPropertyName ("operationMode")]
	public AquareaMode? Mode
		{
		get; set;
		}
	[JsonPropertyName ("quietMode")]
	public QuietMode? Quiet
		{
		get; set;
		}
	[JsonPropertyName ("forceDHW")]
	public PowerState? ForceHotWater
		{
		get; set;
		}
	[JsonPropertyName ("forceHeater")]
	public PowerState? ForceHeater
		{
		get; set;
		}
	[JsonPropertyName ("holidayTimer")]
	public PowerState? HolidayTimer
		{
		get; set;
		}
	[JsonPropertyName ("powerful")]
	public PowerfulDuration? Powerful
		{
		get; set;
		}
	[JsonPropertyName ("specialStatus")]
	public SpecialStatus? Special
		{
		get; set;
		}
	[JsonPropertyName ("forcedefrost")]
	public int? ForceDefrost
		{
		get; set;
		}
	[JsonPropertyName ("tankStatus")]
	public AquareaTankUpdate? Tank
		{
		get; set;
		}
	[JsonPropertyName ("zoneStatus")]
	public List<AquareaZoneUpdate>? Zones
		{
		get; set;
		}
	}
internal sealed class HistoryRequest<T>
	{
	[JsonPropertyName ("deviceGuid")]
	public string? DeviceGuid
		{
		get; set;
		}
	[JsonPropertyName ("gwid")]
	public string? GatewayId
		{
		get; set;
		}
	[JsonPropertyName ("dataMode")]
	public T? Period
		{
		get; set;
		}
	[JsonPropertyName ("date")]
	public string Date
		{
		get; set;
		} = "";
	[JsonPropertyName ("osTimezone")]
	public string TimeZone
		{
		get; set;
		} = "";
	}
internal sealed class DeviceControlRequest<T>
	{
	[JsonPropertyName ("deviceGuid")]
	public string DeviceGuid
		{
		get; set;
		} = "";
	[JsonPropertyName ("parameters")]
	public T? Parameters
		{
		get; set;
		}
	}
internal sealed class AgreementRequest
	{
	[JsonPropertyName ("agreementList")]
	public IReadOnlyList<AgreementAcceptance>? Agreements
		{
		get; set;
		}
	}