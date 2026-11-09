# Bounded live Aquarea setting tests

These tests change a real heat pump. Run them only with explicit authorization for the identified device and while other operators leave its settings alone. Read-only live tests do not authorize control tests.

The separate `LiveControl` category and processor suite require manual selection, fresh registered credentials, `AllowDeviceChanges=true`, and a unique `ControlRunId` in a short-lived private input file. Normal offline tests exclude every `Live` case. The read-only processor suite also excludes `LiveControl`.

## Protocol

1. Read live state, verify account ownership and that the gateway is controllable, and save a typed baseline. Obtain the weekly schedule; this protocol proceeds only if it is already disabled.
2. Save the intended change and exact restoration request, then verify the reported configuration has not changed during preflight. Persist an unresolved journal before sending a command.
3. Exercise quiet mode only when it starts off: Off → Level 1 → Off. Separately, lower the reported tank target by 1°C within its advertised limits, then restore the exact original target. Do not change zone power, tank power, operating mode, schedules, holidays, forced heaters or defrost settings.
4. Send each change once. A lost acknowledgement is uncertain execution and is never replayed.
5. Require two consecutive fresh device reads to confirm the test value, checking other reported configuration values on each read. Temperatures and other measurements are expected to vary and are not configuration checks. Values absent from the baseline cannot be claimed as preserved measurements of unknown settings.
6. Run restoration even if the change or confirmation fails, using a separate timeout unaffected by cancellation of the main test. Restore only the field owned by the test, preserving current other branches through the library's fresh-read control path. A third value in the target field indicates a possible external change and blocks an automatic overwrite.
7. Require two consecutive fresh reads of the restored configuration, then verify the complete weekly schedule is unchanged. Unconfirmed restoration leaves the persistent journal unresolved and blocks all later setting tests.
8. Retain private baseline/change/readback/restoration receipts. Clear transferred inputs, remove temporary processor test installations, and delete temporary plaintext exports after testing. Keep the normal DPAPI session.

Confirmation reads wait three seconds between samples, with a thirty-second observation window. The main test is bounded to two minutes; restoration has its own sixty-second budget. A cloud acknowledgement alone is never counted as physical confirmation.

## Before physical execution

Fault-injection cases use fake credentials to prove successful restoration, recovery after lost acknowledgement, recovery after a failed read, blocking after a rejected restoration, and refusal to overwrite an unrelated target value. Run these on each applicable runtime before its physical tests.

## Inputs and selection

Use the Windows console's `test-input export` into a private directory. Add `AllowDeviceChanges` and `ControlRunId` only for the current authorized test. Keep `Enabled=false`; the explicitly selected live run supplies `EnableLiveTests=true`. The file contains tokens and must not enter source control, public artifacts or reports.

Workstation selection uses `TestCategory=LiveControl`. On the processor, select only the manual `panasonic-comfort-cloud-controls` suite and both `QuietLevelRoundTrip` and `TankTemperatureRoundTrip`. The tank test depends on a successful quiet test. Results and the `ControlEvidence` journal belong to the private test-input directory.

Observed outcomes are recorded in the validation guide. These two round trips do not establish mode-change, on/off, weekly-schedule-write, holiday, defrost, or endurance coverage.
