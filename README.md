# PanasonicComfortCloudClient

A standalone, asynchronous **C# 14** client for Panasonic Comfort Cloud, including the Aquarea air-to-water heat-pump API. Targets **.NET 10** and **.NET Framework 4.7.2**. The Windows test console supports interactive use and command-line automation.

Version **1.0.1** fixes ordered session-change notification delivery. [API documentation and guides](https://oznetmaster.github.io/PanasonicComfortCloudClient/). This is an independent implementation, not affiliated with or endorsed by Panasonic. Panasonic's mobile-app cloud interface is not a public compatibility contract.

## Features

- Typed device discovery with Aquarea, air-conditioner and standalone hot-water families.
- OAuth authorization-code/PKCE browser authentication, including Panasonic-hosted MFA, token refresh and cloud client registration.
- Legacy password/CSRF login, classic OTP and hosted Guardian MFA; explicit v1 policy and isolated Aquarea cookie routes.
- Aquarea live or explicitly cached status, zones, tank, operating modes, quiet/powerful modes, special setpoint offsets, energy history and weekly schedules.
- Aquarea branch changes read fresh status and preserve other zones and tank operation.
- Separate AC and HWS status, control, history and scheduling endpoints.
- Numeric enums preserve unknown reported values; missing readings remain null and real zeroes remain zeroes.
- Typed schedule round trips preserve unknown wire fields. AC and Aquarea history/control encodings are separate types.
- Schedule creation/editing helpers, AC capability evaluation, daily energy selection and application-owned energy/change tracking.
- Explicit optional AC power-on/preference buffering and cached app-version discovery.
- Cancellation, request pacing, one request at a time, bounded responses and typed cloud errors.
- Optional `Microsoft.Extensions.Logging` logging; the default logger is silent and credentials and response bodies are never logged.

See the [complete upstream method/field audit](docfx/guides/UPSTREAM-COVERAGE.md), [protocol coverage](docfx/guides/PROTOCOL.md), [authentication](docfx/guides/AUTHENTICATION.md), [console usage](docfx/guides/CONSOLE.md) and [validation status](docfx/guides/VALIDATION.md).

## Build and test

```powershell
dotnet build PanasonicComfortCloudClient.slnx -c Release
dotnet test tests/PanasonicComfortCloudClient.Tests -c Release -f net10.0
dotnet test tests/PanasonicComfortCloudClient.Tests -c Release -f net472
```

Run `net472` tests and the console on Windows. The library has no Crestron SDK reference; ordinary cloning and Visual Studio/VS Code builds do not install or contact processors. Shared build and documentation tasks are available through `build/Invoke.ps1`.

## Read Aquarea status

```csharp
using PanasonicComfortCloudClient;
using PanasonicComfortCloudClient.Models;

using var client = new ComfortCloudClient(new ComfortCloudOptions
{
    AppVersion = currentAndroidComfortCloudAppVersion
});
await client.RestoreSessionAsync(applicationStoredSession, cancellationToken);
var groups = await client.GetGroupsAsync(cancellationToken);
var heatPump = groups.SelectMany(g => g.Devices)
    .Single(d => d.Family == DeviceFamily.Aquarea);
var snapshot = await client.GetAquareaStatusAsync(
    heatPump.DeviceGuid!, StatusSource.Live, cancellationToken);
Console.WriteLine(snapshot.Status?.OutdoorTemperature);
```

The application owns credentials, storage, browser presentation and polling. Subscribe to `SessionChanged` and persist the latest `CurrentSession` atomically when refresh tokens rotate. The client does not write files, open windows, schedule polling or log out on disposal. Use separate clients for separate Panasonic accounts.

Concurrent client operations are serialized, including compound schedule/policy operations. Disposal cancels admitted work and defers owned-resource cleanup until it exits. Trackers synchronize their state; mutable DTOs/builders still require caller-owned coordination. See the [thread-safety and Mono audit](docfx/guides/THREAD-SAFETY.md).

## Windows test console

```powershell
dotnet run --project src/PanasonicComfortCloudClient.Console -- help
dotnet run --project src/PanasonicComfortCloudClient.Console -- login --app-version CURRENT_ANDROID_VERSION
dotnet run --project src/PanasonicComfortCloudClient.Console -- devices
dotnet run --project src/PanasonicComfortCloudClient.Console -- aquarea status --device DISCOVERED_GUID
```

Run without arguments for an interactive prompt. Default login opens a Panasonic ID window backed by Microsoft Edge WebView2. `login --legacy` instead uses masked password/OTP prompts. Neither mode accepts password/token command-line arguments. The console stores its session using Windows DPAPI for the current user. Every device/account-changing command requires `--apply`.

## Validation and limitations

Fake-HTTP protocol tests run on both target frameworks. Actual cloud/device validation is tracked separately and must not be inferred from offline tests. The available live device is an **Aquarea heat pump**; browser authorization, discovery, live status, weekly-schedule reads and energy-history reads have passed on both workstation frameworks; see the validation record for processor results. Bounded quiet-mode and tank-target round trips also passed on the workstation and processor, with original settings restored and confirmed. AC/HWS model coverage is not a claim that those hardware families have been live-tested here.

Cloud acknowledgement is not confirmation that the heat pump changed state. Read status separately. Device writes are never automatically retried after rejection, cancellation or uncertain transport outcomes. Schedule writes replace the full week and must begin with a schedule read from the intended device.

## Reference and licence

Protocol reference: [aio-panasonic-comfort-cloud](https://github.com/sockless-coding/aio-panasonic-comfort-cloud), audited commit `80d7784fa6b35d6f71a7b1f842062e2929d2d653`, including release `2026.10.1` and its four subsequent commits. The maintained product contains C# source, not a Python runtime or Python implementation. See [third-party notices](THIRD-PARTY-NOTICES.txt) and [source provenance](upstream.json).

Copyright (c) 2026 Neil Colvin. [MIT licensed](LICENSE).

## Source and build conventions

The shared [EditorConfig](.editorconfig) is included under **Solution Items**. C# source uses tabs with a width of three and the model projects' brace and spacing rules. Every authored C# file carries the copyright and MIT licence header.

`Directory.Build.props` also owns the shared author/company, copyright, licence, repository and assembly-generation metadata. Its single `Version` value supplies the package version; the SDK derives assembly/file/informational versions. Project files retain their distinct names, titles, products, descriptions and package IDs.

C# is pinned to **14.0**. Nullable reference analysis, recommended .NET analyzers and build-time code-style checks are enabled for both configurations, including the console and tests. Shared conditional groups in [Directory.Build.props](Directory.Build.props), also listed under Solution Items, treat compiler/analyzer warnings as errors in **Debug** and leave them as warnings in **Release**. Detailed formatting/style rules remain in `.editorconfig`. Strict XML validation requires summaries, parameter/type-parameter and return descriptions, property `<value>` descriptions and valid references; CI also checks the generated Pages navigation, search and API output.

The Framework target retains only the compatibility dependencies this client uses: three Hafner attribute packages, required/init support, System.Memory and task extensions. Async interfaces and Unsafe arrive transitively through the JSON/memory dependencies. Unused BCL memory/index/range and hash-code packages are excluded. See the [compatibility package guide](docfx/guides/COMPATIBILITY.md). C# 14 extension members supply the missing modern guard, hashing, hex, cancellation and memory-stream APIs while shared call sites keep the modern .NET APIs. A separate Framework HTTP handler preserves Panasonic's bodyless-GET headers.

Verify formatting and style with:

```powershell
dotnet format whitespace PanasonicComfortCloudClient.slnx --verify-no-changes
dotnet format style PanasonicComfortCloudClient.slnx --verify-no-changes --severity info
dotnet build PanasonicComfortCloudClient.slnx -c Debug
```

See the [bounded live-control test protocol](docfx/guides/LIVE-CONTROL-TESTS.md) before selecting any physical setting test.

## Changes in 1.0.1

Session-change notifications are queued in commit order and delivered serially after the operation gate is released. Concurrent callers wait for their notifications. Reentrant session operations commit and queue their notification without waiting for the current callback, avoiding deadlocks. Persistence across separate clients or processes still needs application-owned coordination.

Release packaging and publication use the [shared publication component](https://github.com/oznetmaster/BuildAutomation/tree/5d8006f140a59c8d1dd92ab70723fe8338650bd2/ReleasePublishing). Set `RELEASE_VERSION=1.0.1` and run `./build/Invoke.ps1 -Task release-assets` for local preparation; it publishes nothing. GitHub publication reuses retained, hash-verified artifacts and the repository's existing NuGet trust identity.

## Debugging NuGet packages

The release workflow publishes matching portable debugging symbols as separate `.snupkg` files on GitHub and NuGet's symbol server. They are optional debugger downloads and do not enlarge the normal library package or enable runtime logging. This policy applies to releases built with the updated workflow; earlier versions may not include symbols. See [the build documentation](build/README.md#symbol-packages) and [Microsoft's symbol-server setup](https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg#nugetorg-symbol-server).
