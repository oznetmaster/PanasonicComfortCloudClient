# Validation status

Release **1.0.0** is built in the maintained checkout at `C:\Projects\PanasonicComfortCloudClient` with C# **14.0**, targeting **net10.0** and **net472**. The Windows console targets **net10.0-windows**. Hardware coverage below is limited to one Aquarea heat pump.

## Current verification — 9 November 2026

- Debug and Release solution builds pass with zero warnings and errors. Shared conditional groups in Directory.Build.props promote compiler/analyzer warnings to errors in Debug and retain warnings in Release. Recommended .NET analyzers, build-time code-style checks and nullable reference analysis remain enabled on both targets and the console.
- All **390** offline cases pass on .NET 10 and **393** on .NET Framework 4.7.2. The latter includes three Framework HTTP-handler tests. Coverage includes 201 frozen upstream field cases, model round trips, capabilities, energy tracking, schedule creation/modification/deletion, legacy authentication, policy/version helpers, cancellation and restoration after uncertain/failed control operations. The concurrency audit adds 22 focused race/publication cases and one timeout-wait compatibility case.
- The shared EditorConfig is included in Solution Items. Whitespace and style verification pass. All **93** authored C# source files have the copyright and MIT licence header.
- The strict XML checker verifies **554 declarations** in each of Debug/net472, Release/net472, Debug/net10.0 and Release/net10.0. Generated Pages documentation builds without warnings; common navigation, search and all API output groups pass validation.
- The local NuGet package has the intended authorship, copyright, MIT licence, repository metadata, Framework compatibility dependencies and separate XML documentation for both targets. Its dependency declarations exclude unused compatibility packages.
- Earlier on 9 November, the workstation's .NET 10 live control suite passed both bounded round trips: **Quiet Off → Level 1 → Off**, and **tank target 59°C → 58°C → 59°C**. Two consecutive fresh reads confirmed each change and restoration. The complete weekly schedule and other reported configuration settings remained unchanged. Those physical changes were not repeated for the concurrency audit, which used fake race-test traffic and real read-only checks.

## Final dependency-trim validation — 9 November 2026

The 1.0.0 candidate replaces the broad Hafner metapackage with three used attribute packages, removes the unused HashCode package, and removes redundant direct AsyncInterfaces/Unsafe references while retaining their transitive runtime dependencies. Bcl.Memory is excluded from the library and retained only for net472 tests that use Index/Range. See the [compatibility guide](COMPATIBILITY.md).

Both solution configurations build with zero warnings and errors. All **390 .NET 10** and **393 net472** offline tests pass, followed by **four read-only live tests on each workstation framework**. The shared strict XML checker validates **554 declarations** for each target/configuration, and the generated documentation passes the common layout/search/API checks. Formatting and style checks also pass.

The newly frozen processor host **1.0.000.0001**, SHA-256 `60d9b39af4e63829f0523a2811f0b2fa78f3c8355bf9d0a3c236ab7174e46cf9`, passes **393 offline tests and four read-only live tests**, with no failures or skips, on the same 32-bit Mono runtime described below. This host is a separate external test artifact, not a public library dependency. No new physical setting or schedule write was sent during this final pass. Cleanup verifies the original processor configuration and unchanged protected workstation session, removes private inputs and the temporary host/package, and releases the shared processor lease.

## Processor verification — 9 November 2026

The separate `CrestronLibraryTests` collection packages the same net472 source tests. Its three suites separate 393 automatic cases, four manual read-only cases and two manual control cases. The read-only suite explicitly excludes `LiveControl`; the control suite was not selected in the concurrency audit.

The merged/patched assembly passes all **393 offline tests twice** on the workstation. The frozen concurrency package, local test version **0.1.000.0006**, has SHA-256 `85aab73e879c9baeb11787fdfc0957c70257f9fbbfbd63df84a8e32cac0b2b28`. It uses the normal dependency assets and ManifestUtil 30; its minimum runtime SDK remains 27.

On the lab processor running Crestron Home **4.013.0134**, the concurrency package passes **393 offline tests twice**, then **four read-only live tests** (discovery, live status, energy history and weekly schedule), with no failures or skips. Its recorded runtime is **Mono 6.12.0.107**, CLR `4.0.30319.42000`, with **four-byte pointers (32-bit)**. All 22 focused concurrency cases pass in both processor runs. A Framework IL audit also verifies Monitor-based locking, no `System.Threading.Lock` dependency, and compiler-generated `Interlocked.CompareExchange` event subscription.

The earlier 370-case package **0.1.000.0004**, SHA-256 `5c1070418e30a2ffb2d85659f8adc3cf70f7b0fd307ba75a1917075cf2a88f51`, passed 370 offline cases, four live reads and both restorative controls before this concurrency audit. Its 24 private control receipts remain retained. The earlier 98-case package 0.1.000.0003 also passed its separate checks. These historical physical results are not substituted for the current concurrency checks.

Private inputs travel through the authenticated encrypted test channel. Cleanup clears transferred inputs, removes the temporary test instance and its stored package, and verifies preservation of the ten original devices, identities, room assignments, programming and media configuration. No processor reboot or firmware change is needed for the V2 test host.

## Earlier read-only validation — 8 November 2026

Before the source-convention and compatibility updates, 84 offline cases passed on .NET 10 and 87 on net472. Browser OAuth/PKCE login succeeded, with credentials saved by the console using current-user Windows DPAPI. Four read-only live tests passed on both workstation frameworks and the processor. The console separately read status and schedule and returned 24 hourly energy entries.

## Failures retained and corrected

Early attempts remain private and are not counted as passes. Live testing established mandatory bodyless-GET JSON content headers, exact lowercase custom-header names on the Aquarea transfer service, and numeric `osTimezone` offsets. The default net472 HTTP handler rejects empty GET content; its transport shim has cancellation, HTTP-error and redirect regression coverage.

An earlier invalid timezone assertion and a processor live-test selection that skipped NUnit Explicit fixtures were corrected before their completed passing reruns. Separate TRX files and processor result files are authoritative; an early two-framework command that reused a filename is historical only.

The stricter model-aligned analysis also identified test-code cleanup: cached JSON options, ordinal/culture-invariant comparisons, and restoration failures reported after the `finally` clause rather than raised within it. Fake-credential cases verify lost acknowledgements, failed observations, rejected restoration, blocking of later writes and refusal to overwrite a third-party target value.

The expanded audit retained three additional failures before their passing reruns: inherited HWS history emitted an incompatible duplicate temperature field; the merged serializer required explicit dictionary-key support in its string converter; and ManifestUtil mistook the embedded audit inventory's `.json` resource name for a second driver manifest. HWS history now has its own typed model, the converter supports property names, and the audit resource has a distinct `.data` name. No parser-asset override or unsupported assembly surgery is retained.

Concurrency test revision **0.1.000.0005** completed on Mono with 392 passes and one failed stress fixture: the generated target temperature passed through Panasonic's `126` unavailable sentinel and correctly deserialized to null. That failed result and frozen package are retained privately. Revision 0.1.000.0006 uses non-sentinel values while retaining its original snapshot-coherence assertion; all 393 cases subsequently passed twice. This was a fixture error, not evidence of a Mono memory-model defect. See the [thread-safety audit](THREAD-SAFETY.md) for the corrected production races and ownership contract.

## Remaining scope

These two setting round trips do not establish mode-change, on/off, zone-target, schedule-write, holiday, forced-heater, defrost, policy-acceptance or endurance behavior. AC and standalone HWS hardware are not available here and have not been live-tested. Temperature measurements may change naturally; an absent baseline value cannot be claimed as an observed preserved setting. See the [bounded live-control protocol](LIVE-CONTROL-TESTS.md).

Legacy password, classic OTP and Guardian authentication use fake credentials only; real tests reuse the preserved browser session. Schedule creation/modification/deletion has fake-server round-trip coverage for AC/Aquarea, and numeric-hour HWS schedule controls are covered offline. No live schedule write has been sent. See the [full method/field audit](UPSTREAM-COVERAGE.md) for provenance and deliberate .NET adaptations.

The library has no Crestron dependencies. Processor packaging belongs to the external test collection; normal solution builds do not deploy or contact processors. Authentication presentation, secure storage and polling are application responsibilities. Credentials, device identifiers and lab receipts remain private.
