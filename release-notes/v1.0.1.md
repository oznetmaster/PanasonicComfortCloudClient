# PanasonicComfortCloudClient 1.0.1

This patch corrects the audited reliability issues while retaining the existing public API and supported frameworks.

- Session-change notifications are queued in commit order and delivered serially after the operation gate is released. Concurrent callers wait for their notifications. Reentrant session operations commit and queue their notification without waiting for the current callback, avoiding deadlocks. Persistence across separate clients or processes still needs application-owned coordination.

## Validation

- Offline workstation tests passed in Debug and Release on .NET 10 and .NET Framework 4.7.2: Panasonic net10.0: 392; Panasonic net472: 395. No failures or skips.
- 24 selected tests passed in the shared 374-case processor run on Mono 6.12.0.107 (32-bit), with no failures or skips. The merged host's resolved dependency graph was tested; these are not eight independent release-package tests.
- Fake transports and credentials exercised ordering, cancellation, shutdown and failure recovery. No new household-device or cloud control was performed. Pairing storage is unchanged.
- Strict XML, formatting and style checks passed. Package and companion-app preparation is verified separately by the shared release pipeline.

See [the changelog](https://github.com/oznetmaster/PanasonicComfortCloudClient/blob/v1.0.1/CHANGELOG.md) and [validation provenance](https://github.com/oznetmaster/PanasonicComfortCloudClient/blob/v1.0.1/release-validation/v1.0.1.json).
