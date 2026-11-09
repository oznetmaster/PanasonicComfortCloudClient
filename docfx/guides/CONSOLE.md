# Windows test console

The console targets .NET 10 on Windows. Build it with Visual Studio, VS Code or `dotnet build`. Microsoft Edge WebView2 Runtime is required for the embedded login window. The library itself has no Windows or WebView2 dependency.

Run without arguments for the interactive prompt, or give a complete command for scripts:

```powershell
PanasonicComfortCloud.exe help
PanasonicComfortCloud.exe login --app-version CURRENT_ANDROID_VERSION
PanasonicComfortCloud.exe login --legacy --app-version CURRENT_ANDROID_VERSION
PanasonicComfortCloud.exe app-version
PanasonicComfortCloud.exe devices
PanasonicComfortCloud.exe aquarea status --device DEVICE_GUID
PanasonicComfortCloud.exe aquarea energy --device DEVICE_GUID --period Day --date 20261108 --utc-offset +00:00
PanasonicComfortCloud.exe aquarea schedule get --device DEVICE_GUID --out schedule.json
```

Use `--cached` only when cached Aquarea/AC status is wanted. `--out` creates a new file and refuses to overwrite an existing file. JSON output uses the same typed models as the library.

An explicit zone change file can contain:

```json
{ "zoneStatus": [ { "zoneId": 1, "heatSet": 22 } ] }
```

This changes the heat setting in the zone's own units; a compensation-offset zone does not necessarily use an absolute room temperature. Inspect the returned ranges and current device configuration first.

```powershell
PanasonicComfortCloud.exe aquarea update --device DEVICE_GUID --file change.json --apply
PanasonicComfortCloud.exe aquarea status --device DEVICE_GUID
```

All device/account changes, including schedule replacement, policy acceptance and logout, require `--apply`. There are no command-line password or token options. Browser login uses Panasonic's own page, with MFA where applicable. `login --manual` accepts the complete callback URI. `login --legacy` instead prompts for username, masked password and, when requested, a masked OTP; redirected standard input is supported for controlled automation. Do not put secrets in shell arguments or source files. `app-version` performs an explicit version lookup without needing a stored login.

AC supports `status`, `energy`, `update` and `schedule get/set`; HWS supports `status`, `energy` and `update`. Their period enums and payload models are distinct where the underlying APIs differ. Run `help` for all command names.

Session storage defaults to `%LOCALAPPDATA%\PanasonicComfortCloudClient\default.session`, protected for the current Windows user. `--session PATH` selects another protected session file. The current app version is remembered only after a successful login. Ctrl+C cancels waiting and HTTP operations; it cannot retract an already delivered device command.

Exit codes: `0` completed, `2` input/local failure, `3` cloud rejection, `130` cancellation. A control acknowledgement is not physical confirmation, and uncertain controls are not retried automatically.

Energy queries take a numeric UTC offset such as `+00:00`, `+01:00` or `-03:30`, not an IANA timezone name. The caller selects the appropriate offset for the device and requested period; the client does not guess daylight-saving rules. The library accepts a `TimeSpan` in whole minutes between -14 and +14 hours.

For explicit read-only processor tests, `test-input export --device DEVICE_GUID --utc-offset +00:00 --out PRIVATE_DIRECTORY/ComfortCloudLiveSettings.json --apply` exports a fresh private test-input file. Unlike the normal DPAPI session, **this export contains plaintext tokens**. Keep it out of source control, logs, reports and published artifacts. Transfer it only using the authenticated encrypted test runner, then clear transferred inputs after testing. Export refuses to overwrite an existing file; its `Enabled` switch remains false until the selected live run supplies `EnableLiveTests=true`.
