# Changelog

## [1.0.0](https://github.com/oznetmaster/PanasonicComfortCloudClient/releases/tag/v1.0.0) — 9 November 2026

- Audited thread safety for Mono and both .NET targets: coordinated operation admission/cancellation/disposal, prevented late snapshot publication and buffered-preference resurrection, synchronized tracker state, and retained a single client gate across schedule/policy transactions. Added 22 focused concurrency cases and timeout-wait compatibility coverage.
- Completed the pinned upstream method/field inventory: 201 modeled wire fields, 166 public callables and 300 property definitions, with explicit .NET policy/lifecycle adaptations.
- Added missing metadata, CAC capabilities, AC zone/sentinel fields, Aquarea feature flags and HWS numeric-hour schedules/limits.
- Corrected Aquarea schedule `tankTemperature` serialization and separated HWS history temperature/cost fields from AC/Aquarea history.
- Added daily energy queries, reset-aware inferred power, explicit change tracking, schedule editing and AC preference/command helpers.
- Added legacy password/CSRF, classic OTP, hosted Guardian, v1 agreement and isolated Aquarea-cookie routes; masked legacy console login and explicit app-version discovery.
- Added frozen field-coverage checks, fake schedule lifecycle round trips, authentication failure cases and unknown-field retention tests.
- Initial independent C# 14 client targeting .NET 10 and .NET Framework 4.7.2.
- Typed Comfort Cloud discovery and Aquarea, AC and standalone HWS endpoints.
- Browser OAuth/PKCE authentication, token refresh and explicit policy acceptance.
- Aquarea controls preserve other branches using a fresh status observation.
- Weekly schedule validation and unknown-field round trips.
- Windows console with interactive/command-line modes and protected session storage.
- Portable protocol tests, shared build tasks and generated API documentation.
- Live-verified lowercase Aquarea headers, exact JSON content type and typed UTC offsets; portable Framework GET-header shim with cancellation/error/redirect checks.
- Read-only Aquarea discovery, live status, history and weekly schedule verified on both workstation targets and the processor; physical writes and other device families remain separately qualified.
- Matched the maintained-client source conventions: shared EditorConfig and solution items, three-column tabs, complete source licence headers and explicit project/package metadata. Directory.Build.props supplies shared Debug/Release conditional analyzer/style settings, with warnings promoted to errors only in Debug; nullable analysis remains enabled in both.
- Centralized shared assembly/package metadata in Directory.Build.props, with SDK-derived versions from one Version value; retained project-specific identities and descriptions.
- Removed unused compatibility packages and replaced the broad Hafner metapackage with the three attribute packages used by this client.
- Added Framework compatibility packages and C# 14 extension-member shims, retaining modern API calls; parity tests cover guards, hashing, enums, cancellation and memory I/O.
- Added separately gated restorative live tests with durable baselines, independent restoration and fake-credential failure coverage.
