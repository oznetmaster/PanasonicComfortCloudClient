# Protocol coverage and behavior

Reference: `aio-panasonic-comfort-cloud` commit `80d7784fa6b35d6f71a7b1f842062e2929d2d653`, including release `2026.10.1`. The source snapshot is a protocol reference; the product is entirely C#. The [method and field audit](UPSTREAM-COVERAGE.md) records every mapping and deliberate .NET adaptation.

| Area | Protocol | Public API |
| --- | --- | --- |
| Authentication | Auth0 `/authorize`, `/oauth/token`, PKCE; cloud `/auth/v2/login` | Authorization context, callback exchange, session restoration/refresh |
| Discovery | `GET /device/group`, both device-list formats | `GetGroupsAsync` |
| Aquarea status | `/remote/v1/app/common/transfer` proxy for `/remote/v1/api/devices` | `GetAquareaStatusAsync` |
| Aquarea controls | Same proxy, POST devices with typed `bodyParam` | `UpdateAquareaAsync` |
| Aquarea energy | Proxy for `/remote/v1/api/consumption` | `GetAquareaEnergyAsync` |
| Aquarea schedule | Proxy for `/remote/v1/api/data/weeklyTimer` | Get/set Aquarea schedule |
| AC state/control | `/deviceStatus`, `/deviceStatus/now`, `/deviceStatus/control` | Get/update AC |
| AC history | `/deviceHistoryData` | `GetAcEnergyAsync` |
| AC schedule | `/weeklyTimer/{guid}`, `/weeklyTimer` | Get/set AC schedule |
| Standalone HWS | `/hphw/deviceStatus`, `/hphw/deviceStatus/control`, `/hphw/deviceHistoryData` | Get/update HWS and energy |
| Policies | `/auth/v2/agreement/documents`, `/auth/v2/agreement/status` | Read documents/status and explicitly accept exact versions |

Requests reproduce the reference's mobile-app header signing. Device identifiers are escaped for their endpoint, with the reference AC/HWS slash substitution. Aquarea uses its own gateway query identifiers. The cloud API and OAuth origin use HTTPS. Responses are bounded to four MiB and deserialized into attribute-defined typed models; the API does not expose raw JSON responses. Internal extension-data fields retain unknown schedule keys during round trips.

Aquarea reported modes (`Heat=1`, `Cool=2`) differ from writable modes (`Heat=2`, `Cool=3`, `Auto=8`). Aquarea/HWS history periods also differ from AC periods. Separate enums prevent accidental interchange. Unknown numeric response enum values survive deserialization without pretending to be off or normal. Missing readings are null, including AC's invalid-temperature sentinel `126`; measured zero remains zero.

The client serializes requests and applies a configurable minimum start interval. It does not schedule polling. `Retry-After` is reported to the caller, which owns any subsequent backoff. A rejected live read does not become cached success; the caller must explicitly request cached data. Receipt time is not a device measurement timestamp.

Aquarea zone/tank/mode changes obtain a fresh live baseline and preserve other known branches. Unknown branch operation prevents the replacement rather than inventing off values. Setpoints are checked against returned bounds. Eco/Comfort offsets require the current special status and applicable offsets. External applications can still race the read/modify/write interval. Every command returns on cloud acknowledgement, not physical confirmation; callers should read status afterward.

AC and Aquarea schedule writes require all seven days and at most six slots per day. Their times use `HH:mm`, duplicate times fail, and slots are sorted/padded without modifying the caller's object. HWS intervals use numeric device hours and retain their availability flags. Unknown active-pattern modes and unmodeled fields are retained. Read a schedule from the intended device before editing it; AC/Aquarea writes replace the full week.

Legacy CSRF/password, classic OTP and hosted Guardian verification are implemented alongside browser PKCE. Explicit v1 policy and legacy Aquarea cookie transports are also available. Cookie jars are isolated, redirects remain disabled for legacy Aquarea calls, and modeled responses stay typed. These compatibility paths have fake-credential coverage; availability still depends on the service, device and account.

The schedule editor supplies Monday-first weekday access, add/replace/remove/clear helpers, limits and activation convenience methods. AC helpers evaluate CAC fan/swing capabilities, build coupled settings and optionally buffer preferences while off. `DeviceObservation<T>` and `AcEnergyTracker` are application-owned state trackers; the client does not begin polling or persist them. Inferred energy power is bounded to 5000 W, handles counter resets and becomes zero after 25 minutes without a consumption change; missing channels remain unknown.

Device identification exposes `deviceModuleNumber` and `modelVersion` when supplied. The available Aquarea reports neither, nor serial/firmware details. Its status contains a model-series code. A read of the unused upstream `a2wInfo` route returned only a success acknowledgement. A cloud identifier is not assumed to be an equipment serial number, and `modelVersion` is not labelled firmware.

Live Aquarea validation found two wire requirements that are easy to miss: custom `x-...` header names must retain the upstream lowercase spelling, and cloud requests require `Content-Type: application/json` without a charset suffix, including bodyless GETs. The net472 build includes a small portable `HttpWebRequest` shim for those GETs because Framework `HttpClientHandler` rejects even empty GET content; other requests continue through `HttpClientHandler`. Redirects remain disabled and cancellation aborts the request. The net10 build uses its normal handler. Application-injected HTTP clients must support the same bodyless-GET header requirement.

History `osTimezone` is a signed numeric UTC offset (`+HH:MM`), produced from the public API's `TimeSpan` argument. A timezone name such as `Europe/London` is rejected locally by the console. Current Aquarea branch-on mode mapping also incorporates upstream commit `80d7784fa6b35d6f71a7b1f842062e2929d2d653`.
