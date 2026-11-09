# Upstream API and field coverage audit

Audited on **9 November 2026** against `aio-panasonic-comfort-cloud` commit **80d7784fa6b35d6f71a7b1f842062e2929d2d653**, the verified current upstream HEAD. This includes release `2026.10.1` and its four subsequent commits. The pinned [source tree](https://github.com/sockless-coding/aio-panasonic-comfort-cloud/tree/80d7784fa6b35d6f71a7b1f842062e2929d2d653) is the behavior/protocol reference, not a Python runtime dependency.

The [machine-readable inventory](../../docs/upstream-coverage.json) records 30 source-file hashes, **201 modeled wire-field mappings**, **166 public callable mappings**, and **300 property definitions** (including the private `_weekly_timer` adapter). Constructors, Python magic methods and Python-only test data are not separate protocol capabilities. Internal URL/header/transport helpers were also reviewed; their behavior is covered by the typed request methods and protocol tests.

Each modeled wire field must resolve to a public typed property and deserialize/serialize its value correctly. Retaining a value only in extension data does **not** count as typed field coverage. Separate tests verify unknown-field retention in every response/schedule family. The frozen inventory is embedded in the test assembly, including the merged processor test package.

## Findings implemented

- AC discovery metadata, all feature flags and CAC fan/direction indicators; device capabilities, zone damper/spill/temperature fields and unavailable-temperature handling.
- Aquarea bivalent/outdoor-unit/control-box/heater/series/standalone indicators and derived tank/cooling/fault helpers.
- HWS outdoor readings, temperature-setting limits, numeric schedule hours and interval availability flags.
- AC consumption fractions, daily-entry selection and explicit inferred-power tracking; reset-aware deltas, 25-minute stale-zero rule and 5000 W clamp. Missing channels remain unknown.
- A separate HWS history model for `cost`, `tankTemperature` and `outdoorTemperature`. A retained regression caught duplicate/incompatible inherited temperature fields before the model was separated.
- All modeled AC/Aquarea schedule settings, create/add/replace/remove/clear operations, weekday selection, edit permission, activation convenience methods and schedule-limit access. Aquarea tank patterns now use the upstream `tankTemperature` key, correcting the earlier `tankHeat` mapping.
- AC command construction, coupled swing/iAUTO-X behavior, explicit optional power-on policy and application-owned preference buffering.
- Type-filtered policy queries, pending-version calculation, explicit current-policy acceptance and retained v1 agreement routes.
- Legacy password/CSRF/hidden-form authentication, classic OTP and hosted Guardian transaction verification, plus browser PKCE authentication. MFA contexts are bounded, client-bound and one-use; no password is retained. The console supports masked password/OTP prompts or standard input, with no command-line secret arguments.
- Isolated legacy Aquarea cookie transports with typed responses, and explicit Play Store/AppBrain/community-gist version lookup with an application-restorable two-day cache.

## Deliberate .NET adaptations

The implemented capabilities do not imply copying every Python policy or object lifecycle:

- Attribute-defined typed models replace Python raw dictionaries and diagnostic payload dumps. Unknown wire fields survive round trips internally; callers do not need to parse JSON to read modeled data.
- `DeviceObservation<T>` explicitly owns snapshot/change tracking, and `AcEnergyTracker` explicitly owns a device's time series. The transport client does not poll, write files or maintain hidden UI state. DTO setters do not generate events or optimistically confirm physical commands.
- Live and cached reads are explicitly selected. Failed live reads remain failures until the application explicitly requests cached data; the Python client's hidden ten-read fallback counter is not imposed on applications.
- The caller supplies the device/date-applicable numeric UTC offset and date. The processor/workstation timezone is not assumed to be the device timezone.
- App-version lookup and protocol-version selection are explicit. A version error does not cause an uncertain POST/PUT to be replayed. Automatic reauthentication on a generic discovery failure is also replaced with explicit login/session operations.
- Passwords, tokens, HTML bodies and cookie headers are never logged. Library disposal does not perform upstream's context-manager logout; logout is an explicit API call.
- The library retains credentials in memory only. The Windows console provides DPAPI persistence; other applications supply secure storage. Missing measurements/capabilities remain nullable instead of acquiring Python's zero/off defaults.
- `AcCommandBuilder` and `AcController` provide the reference's optional automatic-power and queued-preference behavior. Plain `UpdateAcAsync` continues to submit explicit settings without silently adding power-on.
- Agreement acceptance has an explicit named method and is never performed by login. The legacy v1 routes are retained for compatibility but upstream reports that the service normally rejects them with 403; rejection is propagated without silent substitution.

## Device identification

Discovery exposes the reported `deviceModuleNumber`, `modelVersion`, group, permissions and capabilities. **`modelVersion` is not labelled as firmware.** The available Aquarea's current discovery/status responses omit a model string, serial number and firmware version; its status contains `modelSeriesSelection`. The unused upstream `/device/a2wInfo/{guid}` URL was read once and returned only `{"result":0}`. No serial/firmware schema is invented from that acknowledgement, and this observation does not establish that every Panasonic endpoint lacks such information.

## Validation boundaries

See [validation status](VALIDATION.md) for exact current runtime results. The available hardware is one Aquarea. AC and standalone HWS protocol tests use fake responses. Legacy password/OTP/Guardian tests use fake credentials; the existing successful browser session is preserved for live device tests. A fake test is not reported as a hardware test. Schedule creation/modification/deletion has full fake-server round-trip coverage; live schedule writes are separately gated and must not be inferred from earlier read-only or quiet/tank-target tests.

## Method mapping

| Upstream source | Callable | C# equivalent | Treatment |
| --- | --- | --- | --- |
| apiclient/__init__.py | ApiClient.complete_browser_authentication | CompleteAuthorizationAsync | Implemented equivalent |
| apiclient/__init__.py | ApiClient.get_browser_authorization_url | CreateAuthorizationRequest | Implemented equivalent |
| apiclient/__init__.py | ApiClient.reauthenticate | AuthenticateWithPasswordAsync or a new browser authorization | Typed/explicit-policy adaptation |
| apiclient/__init__.py | ApiClient.refresh_token | RefreshSessionAsync | Implemented equivalent |
| apiclient/__init__.py | ApiClient.start_session | RestoreSessionAsync / AuthenticateWithPasswordAsync, followed by GetGroupsAsync | Typed/explicit-policy adaptation |
| apiclient/_ac.py | AirConditionerMixin.get_device | GetAcStatusAsync | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.has_pending_changes | AcController.HasPendingChanges | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.history | GetAcEnergyAsync | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.new_change_request | AcController.CreateChangeRequest / AcCommandBuilder | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.set_device | UpdateAcAsync | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.set_device_raw | UpdateAcAsync with typed AcUpdate | Typed/explicit-policy adaptation |
| apiclient/_ac.py | AirConditionerMixin.set_eco_function_mode | AcCommandBuilder.SetEcoFunction / AcUpdate.EcoFunction | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.set_eco_navi_mode | AcCommandBuilder.SetEcoNavi / AcUpdate.EcoNavi | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.set_horizontal_swing | AcCommandBuilder.SetHorizontalSwing / AcUpdate.HorizontalSwing | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.set_inside_cleaning | AcCommandBuilder.SetInsideCleaning / AcUpdate.InsideCleaning | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.set_nanoe_mode | AcCommandBuilder.SetNanoe / AcUpdate.Nanoe | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.set_vertical_swing | AcCommandBuilder.SetVerticalSwing / AcUpdate.VerticalSwing | Implemented equivalent |
| apiclient/_ac.py | AirConditionerMixin.try_update_device | RefreshAsync(DeviceObservation<AcSnapshot>) | Implemented equivalent |
| apiclient/_agreements.py | AgreementsMixin.accept_agreement | AcceptLegacyAgreementAsync | Implemented equivalent |
| apiclient/_agreements.py | AgreementsMixin.accept_agreements | AcceptAgreementsAsync | Implemented equivalent |
| apiclient/_agreements.py | AgreementsMixin.check_agreement_status | GetLegacyAgreementStatusAsync | Implemented equivalent |
| apiclient/_agreements.py | AgreementsMixin.ensure_all_agreements_accepted | EnsureAgreementsAcceptedAsync (explicit only) | Implemented equivalent |
| apiclient/_agreements.py | AgreementsMixin.get_agreement_documents | GetAgreementDocumentsAsync, including type-filter overload | Implemented equivalent |
| apiclient/_agreements.py | AgreementsMixin.get_agreement_status | GetAgreementStatusAsync, including type-filter overload | Implemented equivalent |
| apiclient/_agreements.py | AgreementsMixin.get_pending_agreements | GetPendingAgreementsAsync | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.async_get_aquarea_consumption | GetAquareaEnergyAsync | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.get_aquarea_device | GetAquareaStatusAsync | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.get_aquarea_weekly_timer | GetAquareaScheduleAsync | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.request_aquarea_defrost | UpdateAquareaAsync with ForceDefrost=1 | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_force_dhw | UpdateAquareaAsync with ForceHotWater | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_force_heater | UpdateAquareaAsync with ForceHeater | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_holiday_timer | UpdateAquareaAsync with HolidayTimer | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_operation_mode | UpdateAquareaAsync with Mode | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_operation_state | UpdateAquareaAsync with explicit Mode/Power/Tank/Zones | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_operation_status | UpdateAquareaAsync with Power | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_powerful_time | UpdateAquareaAsync with Powerful | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_quiet_mode | UpdateAquareaAsync with Quiet | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_special_status | UpdateAquareaAsync with Special and fresh branch-preserving offsets | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_tank_operation_status | UpdateAquareaAsync with Tank.Power | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_tank_temperature | UpdateAquareaAsync with Tank.HeatSetpoint | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_weekly_timer | SetAquareaScheduleAsync | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_weekly_timer_enabled | SetAquareaScheduleEnabledAsync | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_zone_operation_status | UpdateAquareaAsync with Zones[].Power | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.set_aquarea_zone_temperature | UpdateAquareaAsync with Zones[].HeatSetpoint/CoolSetpoint | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.try_update_aquarea_device | RefreshAsync(DeviceObservation<AquareaSnapshot>) | Implemented equivalent |
| apiclient/_aquarea.py | AquareaMixin.try_update_aquarea_weekly_timer | GetAquareaScheduleAsync + DeviceObservation<AquareaSchedule>.Update | Implemented equivalent |
| apiclient/_devices.py | DeviceDiscoveryMixin.check_aquarea | GetDevicesAsync(Unknown), then explicit GetAquareaStatusAsync | Typed/explicit-policy adaptation |
| apiclient/_devices.py | DeviceDiscoveryMixin.dump | GetAcStatusAsync returning a typed snapshot | Typed/explicit-policy adaptation |
| apiclient/_devices.py | DeviceDiscoveryMixin.get_devices | GetGroupsAsync / GetDevicesAsync, filtered by DeviceFamily | Implemented equivalent |
| apiclient/_energy.py | EnergyMixin.async_get_energy | GetAcDailyEnergyAsync | Implemented equivalent |
| apiclient/_energy.py | EnergyMixin.async_try_update_energy | GetAcDailyEnergyAsync + AcEnergyTracker.Update | Implemented equivalent |
| apiclient/_hws.py | HwsMixin.async_get_hws_consumption | GetHotWaterDailyEnergyAsync | Implemented equivalent |
| apiclient/_hws.py | HwsMixin.async_set_hws_weekly_settings | UpdateHotWaterAsync with WeeklySettings | Implemented equivalent |
| apiclient/_hws.py | HwsMixin.async_try_update_hws_consumption | GetHotWaterDailyEnergyAsync + DeviceObservation<HotWaterEnergyHistoryEntry>.Update | Implemented equivalent |
| apiclient/_hws.py | HwsMixin.get_hws_device | GetHotWaterStatusAsync | Implemented equivalent |
| apiclient/_hws.py | HwsMixin.set_hws_boost_mode | UpdateHotWaterAsync with Boost | Implemented equivalent |
| apiclient/_hws.py | HwsMixin.set_hws_holiday_mode | UpdateHotWaterAsync with Mode=Holiday | Implemented equivalent |
| apiclient/_hws.py | HwsMixin.set_hws_operation_mode | UpdateHotWaterAsync with Mode | Implemented equivalent |
| apiclient/_hws.py | HwsMixin.set_hws_schedule_mode | UpdateHotWaterAsync with Mode=Schedule | Implemented equivalent |
| apiclient/_hws.py | HwsMixin.try_update_hws_device | RefreshAsync(DeviceObservation<HotWaterSnapshot>) | Implemented equivalent |
| apiclient/_protocol.py | ApiClientCore.execute_get | Typed API methods through serialized ApiCoreAsync<T>/SendCoreAsync<T> | Typed/explicit-policy adaptation |
| apiclient/_protocol.py | ApiClientCore.execute_post | Typed API methods through serialized ApiCoreAsync<T>/SendCoreAsync<T> | Typed/explicit-policy adaptation |
| apiclient/_protocol.py | ApiClientCore.execute_put | Typed API methods through serialized ApiCoreAsync<T>/SendCoreAsync<T> | Typed/explicit-policy adaptation |
| apiclient/_protocol.py | ApiClientCore.get_aquarea_device | GetAquareaStatusAsync | Implemented equivalent |
| apiclient/_timezone.py | (module).get_current_time_zone | Explicit date-applicable TimeSpan UTC offset; application may use TimeZoneInfo.GetUtcOffset | Typed/explicit-policy adaptation |
| apiclient/_weeklytimer.py | WeeklyTimerMixin.get_weekly_timer | GetAcScheduleAsync | Implemented equivalent |
| apiclient/_weeklytimer.py | WeeklyTimerMixin.set_weekly_timer | SetAcScheduleAsync | Implemented equivalent |
| apiclient/_weeklytimer.py | WeeklyTimerMixin.set_weekly_timer_enabled | SetAcScheduleEnabledAsync | Implemented equivalent |
| apiclient/_weeklytimer.py | WeeklyTimerMixin.try_update_weekly_timer | GetAcScheduleAsync + DeviceObservation<AcSchedule>.Update | Implemented equivalent |
| aquareadevice.py | AquareaDevice.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| ccappversion.py | CCAppVersion.get | AppVersionDiscovery.GetAsync | Implemented equivalent |
| ccappversion.py | CCAppVersion.refresh | AppVersionDiscovery.RefreshAsync | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.build | AcCommandBuilder.Build | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_eco_function_mode | AcCommandBuilder.SetEcoFunction | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_eco_mode | AcCommandBuilder.SetEcoMode | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_eco_navi_mode | AcCommandBuilder.SetEcoNavi | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_fan_speed | AcCommandBuilder.SetFanSpeed | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_horizontal_swing | AcCommandBuilder.SetHorizontalSwing | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_hvac_mode | AcCommandBuilder.SetMode | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_iautox_mode | AcCommandBuilder.SetIAutoX | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_inside_cleaning_mode | AcCommandBuilder.SetInsideCleaning | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_nanoe_mode | AcCommandBuilder.SetNanoe | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_power_mode | AcCommandBuilder.SetPower | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_target_temperature | AcCommandBuilder.SetTargetTemperature | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_vertical_swing | AcCommandBuilder.SetVerticalSwing | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_zone_damper | AcCommandBuilder.SetZoneDamper | Implemented equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.set_zone_mode | AcCommandBuilder.SetZonePower | Implemented equivalent |
| helpers.py | (module).check_response | Typed response validation and ComfortCloudException | Implemented equivalent |
| helpers.py | (module).has_new_version_been_published | ComfortCloudException.ErrorCode == 4106, explicit UpdateAppVersionAsync | Typed/explicit-policy adaptation |
| hwsdevice.py | HwsDevice.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/aquarea.py | (module).read_enum | NumericEnumConverter and nullable typed models | Implemented equivalent |
| models/aquarea.py | (module).read_value | Attribute-defined nullable typed deserialization | Implemented equivalent |
| models/aquarea.py | AquareaConsumption.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/aquarea.py | AquareaDeviceParameters.get_zone | AcParameters.GetZone / AquareaStatus.GetZone | Implemented equivalent |
| models/aquarea.py | AquareaDeviceParameters.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/aquarea.py | AquareaTankStatus.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/aquarea.py | AquareaZoneStatus.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer.day | ScheduleEditing.Day(DayOfWeek) | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer.to_dict | JsonSerializer.Serialize with ComfortCloudJson.CreateOptions; endpoint-specific request wrappers | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer.zone_limits | AquareaSchedule.ZoneLimits | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.set_zone | AquareaTimerPattern.SetZone | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.set_zone_cool | AquareaTimerPattern.SetZoneCool | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.set_zone_heat | AquareaTimerPattern.SetZoneHeat | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.set_zone_on_off | AquareaTimerPattern.SetZonePower | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.to_dict | JsonSerializer.Serialize with ComfortCloudJson.CreateOptions; endpoint-specific request wrappers | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.zone_cool | AquareaTimerPattern.ZoneCool | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.zone_heat | AquareaTimerPattern.ZoneHeat | Implemented equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.zone_on_off | AquareaTimerPattern.ZonePower | Implemented equivalent |
| models/hws.py | (module).read_enum | NumericEnumConverter and nullable typed models | Implemented equivalent |
| models/hws.py | (module).read_value | Attribute-defined nullable typed deserialization | Implemented equivalent |
| models/hws.py | HwsConsumption.copy | DeviceObservation<T>.Update copies a typed snapshot | Implemented equivalent |
| models/hws.py | HwsConsumption.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/hws.py | HwsDailySettings.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/hws.py | HwsDailySettings.to_dict | JsonSerializer.Serialize with ComfortCloudJson.CreateOptions; endpoint-specific request wrappers | Implemented equivalent |
| models/hws.py | HwsDeviceParameters.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/hws.py | HwsInhibitTimeSettings.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/hws.py | HwsInhibitTimeSettings.to_dict | JsonSerializer.Serialize with ComfortCloudJson.CreateOptions; endpoint-specific request wrappers | Implemented equivalent |
| models/hws.py | HwsParameters.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/hws.py | HwsSet1Settings.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/hws.py | HwsSet1Settings.to_dict | JsonSerializer.Serialize with ComfortCloudJson.CreateOptions; endpoint-specific request wrappers | Implemented equivalent |
| models/hws.py | HwsSet2Settings.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/hws.py | HwsSet2Settings.to_dict | JsonSerializer.Serialize with ComfortCloudJson.CreateOptions; endpoint-specific request wrappers | Implemented equivalent |
| models/hws.py | HwsTemperatureSettingLimits.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/weeklytimer.py | WeeklyTimer.day | ScheduleEditing.Day(DayOfWeek) | Implemented equivalent |
| models/weeklytimer.py | WeeklyTimer.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| models/weeklytimer.py | WeeklyTimer.to_dict | JsonSerializer.Serialize with ComfortCloudJson.CreateOptions; endpoint-specific request wrappers | Implemented equivalent |
| models/weeklytimer.py | WeeklyTimerDay.add_pattern | ScheduleEditing.AddPattern | Implemented equivalent |
| models/weeklytimer.py | WeeklyTimerDay.clear | ScheduleEditing.Clear | Implemented equivalent |
| models/weeklytimer.py | WeeklyTimerDay.remove_pattern | ScheduleEditing.RemovePattern, time or identity overload | Implemented equivalent |
| models/weeklytimer.py | WeeklyTimerDay.set_patterns | ScheduleEditing.SetPatterns | Implemented equivalent |
| models/weeklytimer.py | WeeklyTimerDay.to_dict | JsonSerializer.Serialize with ComfortCloudJson.CreateOptions; endpoint-specific request wrappers | Implemented equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.to_dict | JsonSerializer.Serialize with ComfortCloudJson.CreateOptions; endpoint-specific request wrappers | Implemented equivalent |
| models/weeklytimer.py | WeeklyTimerZone.to_dict | JsonSerializer.Serialize with ComfortCloudJson.CreateOptions; endpoint-specific request wrappers | Implemented equivalent |
| panasonicauthentication.py | (module).generate_random_string | OAuthProtocol.RandomToken (cryptographic randomness) | Implemented equivalent |
| panasonicauthentication.py | (module).get_querystring_parameter_from_header_entry_url | OAuthProtocol.Query applied to validated Location | Implemented equivalent |
| panasonicauthentication.py | (module).get_querystring_parameter_from_url | OAuthProtocol.Query | Implemented equivalent |
| panasonicauthentication.py | PanasonicAuthentication.authenticate | AuthenticateWithPasswordAsync | Implemented equivalent |
| panasonicauthentication.py | PanasonicAuthentication.build_authorization_url | CreateAuthorizationRequest | Implemented equivalent |
| panasonicauthentication.py | PanasonicAuthentication.complete_browser_authentication | CompleteAuthorizationAsync | Implemented equivalent |
| panasonicauthentication.py | PanasonicAuthentication.refresh_token | RefreshSessionAsync | Implemented equivalent |
| panasonicauthentication.py | PanasonicAuthentication.verify_mfa | CompletePasswordMfaAsync | Implemented equivalent |
| panasonicdevice.py | (module).calculate_energy_diff | AcEnergyTracker reset-aware delta calculation | Implemented equivalent |
| panasonicdevice.py | (module).read_enum | NumericEnumConverter and nullable typed models | Implemented equivalent |
| panasonicdevice.py | (module).read_value | Attribute-defined nullable typed deserialization | Implemented equivalent |
| panasonicdevice.py | PanasonicDevice.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.get_zone | AcParameters.GetZone / AquareaStatus.GetZone | Implemented equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| panasonicdevice.py | PanasonicDeviceZone.load | Typed deserialization + DeviceObservation<T>.Update | Implemented equivalent |
| panasonicrequestheader.py | PanasonicRequestHeader.get | RequestSigning + cloud transport headers | Implemented equivalent |
| panasonicrequestheader.py | PanasonicRequestHeader.get_aqua_headers | SendAquareaWebCoreAsync, isolated cookies and web headers | Implemented equivalent |
| panasonicsession.py | PanasonicSession.execute_aqua_get | GetLegacyAquareaAsync<T> / GetLegacyAquareaPageAsync | Implemented equivalent |
| panasonicsession.py | PanasonicSession.execute_aqua_post | PostLegacyAquareaAsync<T> | Implemented equivalent |
| panasonicsession.py | PanasonicSession.execute_get | Typed API methods through serialized ApiCoreAsync<T>/SendCoreAsync<T> | Typed/explicit-policy adaptation |
| panasonicsession.py | PanasonicSession.execute_post | Typed API methods through serialized ApiCoreAsync<T>/SendCoreAsync<T> | Typed/explicit-policy adaptation |
| panasonicsession.py | PanasonicSession.execute_put | Typed API methods through serialized ApiCoreAsync<T>/SendCoreAsync<T> | Typed/explicit-policy adaptation |
| panasonicsession.py | PanasonicSession.reauthenticate | AuthenticateWithPasswordAsync or a new browser authorization | Typed/explicit-policy adaptation |
| panasonicsession.py | PanasonicSession.start_session | RestoreSessionAsync / AuthenticateWithPasswordAsync, followed by GetGroupsAsync | Typed/explicit-policy adaptation |
| panasonicsession.py | PanasonicSession.stop_session | LogoutAsync | Implemented equivalent |
| panasonicsession.py | PanasonicSession.update_app_version | UpdateAppVersionAsync | Implemented equivalent |
| panasonicsettings.py | PanasonicSettings.clear | Explicit LogoutAsync + application SessionStore.Delete | Application-owned persistence |
| panasonicsettings.py | PanasonicSettings.is_ready | Application-owned secure storage (console SessionStore.Load) | Implemented equivalent |
| panasonicsettings.py | PanasonicSettings.set_token | RestoreSessionAsync / validated token responses | Implemented equivalent |

## Property mapping

| Upstream source | Property | C# equivalent | Treatment |
| --- | --- | --- | --- |
| panasonicdevice.py | PanasonicDeviceInfo.id | DeviceInfo.StableId | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.guid | DeviceInfo.DeviceGuid | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.name | DeviceInfo.Name | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.group | DeviceInfo.GroupName | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.model | DeviceInfo.Model | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.is_valid | DeviceInfo.IsValidAirConditioner | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.is_aquarea | DeviceInfo.Family == DeviceFamily.Aquarea | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.is_hws | DeviceInfo.Family == DeviceFamily.HotWater | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.is_cac | DeviceInfo.IsCommercial | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.raw | Attribute-defined typed model + unknown-field round trips using ComfortCloudJson | Typed adaptation; no public raw JSON |
| panasonicdevice.py | PanasonicDeviceInfo.status_data_mode | StatusSource argument on GetAcStatusAsync/GetAquareaStatusAsync | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.device_type | DeviceInfo.DeviceType | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.permission | DeviceInfo.Permission | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.temperature_unit | DeviceInfo.TemperatureUnit | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.summer_house | DeviceInfo.SummerHouse | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.nanoe_stand_alone | DeviceInfo.NanoeStandalone | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.auto_mode | DeviceInfo.SupportsAuto | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.power_consumption_flag | DeviceInfo.SupportsEnergy | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.mode_avl_list | DeviceInfo.ModeAvailability | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.model_version | DeviceInfo.ModelVersion | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceInfo.coordinable_flag | DeviceInfo.Coordinable | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| panasonicdevice.py | PanasonicDeviceParameters.power | AcParameters.Power | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.mode | AcParameters.Mode | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.fan_speed | AcParameters.Fan | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.horizontal_swing_mode | AcParameters.HorizontalSwingMode | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.vertical_swing_mode | AcParameters.VerticalSwingMode | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.eco_mode | AcParameters.Eco | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.nanoe_mode | AcParameters.Nanoe | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.eco_navi_mode | AcParameters.EcoNavi | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.eco_function_mode | AcParameters.EcoFunction | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.target_temperature | AcParameters.TargetTemperature | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.inside_temperature | AcParameters.IndoorTemperature | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.outside_temperature | AcParameters.OutdoorTemperature | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.iautox_mode | AcParameters.IAutoX | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.zones | AcParameters.Zones | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.air_direction | AcParameters.AirDirection | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.air_quality | AcParameters.AirQuality | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.last_setting_mode | AcParameters.LastSettingMode | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.inside_cleaning | AcParameters.InsideCleaning | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceParameters.fireplace | AcParameters.Fireplace | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.id | DeviceObservation<T>.Device.StableId | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.timestamp | AcSnapshot.DeviceTimestamp | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.info | DeviceObservation<T>.Device | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.features | AcSnapshot | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.parameters | AcSnapshot.Parameters | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.last_update | DeviceObservation<T>.LastChanged | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.has_eco_navi | AcCapabilities.HasEcoNavi | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.has_eco_function | AcCapabilities.HasEcoFunction | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.has_nanoe | AcCapabilities.HasNanoe | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.has_zones | AcCapabilities.HasZones | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.has_inside_cleaning | AcCapabilities.HasInsideCleaning | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.has_horizontal_swing | AcCapabilities.HasHorizontalSwing | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.supports_fan_auto | AcCapabilities.SupportsFanAuto | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.supported_fan_speeds | AcCapabilities.SupportedFanSpeeds | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.supports_quiet_mode | AcCapabilities.SupportsQuiet | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.supports_powerful_mode | AcCapabilities.SupportsPowerful | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.supported_eco_modes | AcCapabilities.SupportedEcoModes | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.has_inside_temperature | AcCapabilities.HasIndoorTemperature | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.has_outside_temperature | AcCapabilities.HasOutdoorTemperature | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.has_iauto_x | AcCapabilities.HasIAutoX | Typed equivalent |
| panasonicdevice.py | PanasonicDevice.in_summer_house_mode | AcCapabilities.InSummerHouseMode | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| panasonicdevice.py | PanasonicDeviceFeatures.permission | AcSnapshot.Permission | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.summer_house | AcSnapshot.SummerHouse | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.iauto_x_mode | AcSnapshot.SupportsIAutoX | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.nanoe | AcSnapshot.SupportsNanoe | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.nanoe_stand_alone | AcSnapshot.SupportsNanoeStandalone | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.auto_mode | AcSnapshot.SupportsAuto | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.heat_mode | AcSnapshot.SupportsHeat | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.fan_mode | AcSnapshot.SupportsFan | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.dry_mode | AcSnapshot.SupportsDry | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.cool_mode | AcSnapshot.SupportsCool | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.eco_navi | AcSnapshot.SupportsEcoNavi | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.powerful_mode | AcSnapshot.SupportsPowerful | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.quiet_mode | AcSnapshot.SupportsQuiet | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.air_swing_lr | AcSnapshot.SupportsHorizontalSwing | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.auto_swing_ud | AcSnapshot.SupportsAutoVerticalSwing | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.eco_function | AcSnapshot.EcoFunctionCapability | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.temperature_unit | AcSnapshot.TemperatureUnit | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.clothes_drying | AcSnapshot.SupportsClothesDrying | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.inside_cleaning | AcSnapshot.SupportsInsideCleaning | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.fireplace | AcSnapshot.SupportsFireplace | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.off_timer | AcSnapshot.SupportsOffTimer | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.power_consumption_flag | AcSnapshot.SupportsEnergy | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.paired_flag | AcSnapshot.Paired | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.device_nanoe | AcSnapshot.DeviceNanoe | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.mode_avl_list | AcSnapshot.ModeAvailability | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.fan_speed_mode | AcSnapshot.FanSpeedSteps | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceFeatures.fan_direction_mode | AcSnapshot.FanDirectionMode | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceZone.id | DeviceObservation<T>.Device.StableId | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceZone.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| panasonicdevice.py | PanasonicDeviceZone.name | AcZone.Name | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceZone.mode | AcZone.Power | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceZone.level | AcZone.DamperLevel | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceZone.spill | AcZone.Spill | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceZone.temperature | AcZone.Temperature | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceZone.has_temperature | AcZone.Temperature.HasValue | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.id | DeviceObservation<T>.Device.StableId | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.info | DeviceObservation<T>.Device | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.consumption | AcEnergyTracker.Consumption | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.heating_rate | AcEnergyTracker.HeatingRatio | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.heating_consumption | AcEnergyTracker.HeatingConsumption | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.cooling_rate | AcEnergyTracker.CoolingRatio | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.cooling_consumption | AcEnergyTracker.CoolingConsumption | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.current_power | AcEnergyTracker.PowerWatts | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.cooling_power | AcEnergyTracker.CoolingPowerWatts | Typed equivalent |
| panasonicdevice.py | PanasonicDeviceEnergy.heating_power | AcEnergyTracker.HeatingPowerWatts | Typed equivalent |
| aquareadevice.py | AquareaDevice.id | DeviceObservation<T>.Device.StableId | Typed equivalent |
| aquareadevice.py | AquareaDevice.info | DeviceObservation<T>.Device | Typed equivalent |
| aquareadevice.py | AquareaDevice.parameters | AquareaSnapshot.Status | Typed equivalent |
| aquareadevice.py | AquareaDevice.last_update | DeviceObservation<T>.LastChanged | Typed equivalent |
| aquareadevice.py | AquareaDevice.operation | AquareaSnapshot.Operation | Typed equivalent |
| aquareadevice.py | AquareaDevice.owner_flag | AquareaSnapshot.IsOwner | Typed equivalent |
| aquareadevice.py | AquareaDevice.a2w_name | AquareaSnapshot.Name | Typed equivalent |
| hwsdevice.py | HwsDevice.id | DeviceObservation<T>.Device.StableId | Typed equivalent |
| hwsdevice.py | HwsDevice.info | DeviceObservation<T>.Device | Typed equivalent |
| hwsdevice.py | HwsDevice.device_parameters | HotWaterSnapshot | Typed equivalent |
| hwsdevice.py | HwsDevice.parameters | HotWaterSnapshot.Parameters | Typed equivalent |
| hwsdevice.py | HwsDevice.last_update | DeviceObservation<T>.LastChanged | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| models/aquarea.py | AquareaZoneStatus.id | DeviceObservation<T>.Device.StableId | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.name | AquareaZone.Name | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.operation_status | AquareaZone.Power | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.temperature | AquareaZone.Temperature | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.heat_min | AquareaZone.HeatMinimum | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.heat_max | AquareaZone.HeatMaximum | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.heat_set | AquareaZone.HeatSetpoint | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.cool_min | AquareaZone.CoolMinimum | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.cool_max | AquareaZone.CoolMaximum | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.cool_set | AquareaZone.CoolSetpoint | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.supports_cooling | AquareaZone.SupportsCooling | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.eco_heat | AquareaZone.EcoHeat | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.eco_cool | AquareaZone.EcoCool | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.comfort_heat | AquareaZone.ComfortHeat | Typed equivalent |
| models/aquarea.py | AquareaZoneStatus.comfort_cool | AquareaZone.ComfortCool | Typed equivalent |
| models/aquarea.py | AquareaTankStatus.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| models/aquarea.py | AquareaTankStatus.operation_status | AquareaTank.Power | Typed equivalent |
| models/aquarea.py | AquareaTankStatus.temperature | AquareaTank.Temperature | Typed equivalent |
| models/aquarea.py | AquareaTankStatus.heat_min | AquareaTank.HeatMinimum | Typed equivalent |
| models/aquarea.py | AquareaTankStatus.heat_max | AquareaTank.HeatMaximum | Typed equivalent |
| models/aquarea.py | AquareaTankStatus.heat_set | AquareaTank.HeatSetpoint | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| models/aquarea.py | AquareaDeviceParameters.operation_status | AquareaStatus.Power | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.operation_mode | AquareaStatus.Mode | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.device_mode_status | AquareaStatus.DeviceStatus | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.temperature_outdoor | AquareaStatus.OutdoorTemperature | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.direction | AquareaStatus.Direction | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.pump_duty | AquareaStatus.PumpDuty | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.quiet_mode | AquareaStatus.Quiet | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.force_dhw | AquareaStatus.ForceHotWater | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.force_heater | AquareaStatus.ForceHeater | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.holiday_timer | AquareaStatus.HolidayTimer | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.powerful_time | AquareaStatus.Powerful | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.special_status | AquareaStatus.Special | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.supports_cooling | AquareaStatus.SupportsCooling | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.has_tank | AquareaStatus.HasTank | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.tank | AquareaStatus.Tank | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.water_pressure | AquareaStatus.WaterPressure | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.bivalent | AquareaStatus.Bivalent | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.bivalent_actual | AquareaStatus.BivalentActual | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.multi_od_connection | AquareaStatus.MultiOutdoorConnection | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.control_box | AquareaStatus.ControlBox | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.external_heater | AquareaStatus.ExternalHeater | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.model_series_selection | AquareaStatus.ModelSeries | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.stand_alone | AquareaStatus.Standalone | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.uncontrollable_taw1_flag | AquareaStatus.UncontrollableGateway | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.service_type | AquareaStatus.ServiceType | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.fault_status | AquareaStatus.Faults | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.is_on_error | AquareaStatus.IsOnError | Typed equivalent |
| models/aquarea.py | AquareaDeviceParameters.zones | AquareaStatus.Zones | Typed equivalent |
| models/aquarea.py | AquareaConsumption.heat_consumption | EnergyHistoryEntry.HeatingEnergy | Typed equivalent |
| models/aquarea.py | AquareaConsumption.cool_consumption | EnergyHistoryEntry.CoolingEnergy | Typed equivalent |
| models/aquarea.py | AquareaConsumption.tank_consumption | EnergyHistoryEntry.HotWaterEnergy | Typed equivalent |
| models/aquarea.py | AquareaConsumption.heat_cost | EnergyHistoryEntry.HeatingCost | Typed equivalent |
| models/aquarea.py | AquareaConsumption.cool_cost | EnergyHistoryEntry.CoolingCost | Typed equivalent |
| models/aquarea.py | AquareaConsumption.tank_cost | EnergyHistoryEntry.HotWaterCost | Typed equivalent |
| models/aquarea.py | AquareaConsumption.data_time | EnergyHistoryEntry.DateTime | Typed equivalent |
| models/aquarea.py | AquareaConsumption.outdoor_temp | EnergyHistoryEntry.OutdoorTemperature | Typed equivalent |
| models/aquarea.py | AquareaConsumption.total_consumption | EnergyHistoryEntry.ReportedTotal | Typed equivalent |
| models/hws.py | HwsTemperatureSettingLimits.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| models/hws.py | HwsTemperatureSettingLimits.set1_end_temperature | HotWaterTemperatureLimits.FirstEndTemperature | Typed equivalent |
| models/hws.py | HwsTemperatureSettingLimits.set2_start_temperature_min | HotWaterTemperatureLimits.SecondStartMinimum | Typed equivalent |
| models/hws.py | HwsTemperatureSettingLimits.set2_start_temperature_max | HotWaterTemperatureLimits.SecondStartMaximum | Typed equivalent |
| models/hws.py | HwsTemperatureSettingLimits.set2_end_temperature_min | HotWaterTemperatureLimits.SecondEndMinimum | Typed equivalent |
| models/hws.py | HwsTemperatureSettingLimits.set2_end_temperature_max | HotWaterTemperatureLimits.SecondEndMaximum | Typed equivalent |
| models/hws.py | HwsDeviceParameters.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| models/hws.py | HwsDeviceParameters.limits | HotWaterSnapshot.TemperatureLimits | Typed equivalent |
| models/hws.py | HwsDeviceParameters.permission | HotWaterSnapshot.Permission | Typed equivalent |
| models/hws.py | HwsDeviceParameters.weekly_mode | HotWaterSnapshot.SupportsWeeklyMode | Typed equivalent |
| models/hws.py | HwsDeviceParameters.pv_mode | HotWaterSnapshot.SupportsPhotovoltaicMode | Typed equivalent |
| models/hws.py | HwsDeviceParameters.holiday_mode | HotWaterSnapshot.SupportsHolidayMode | Typed equivalent |
| models/hws.py | HwsInhibitTimeSettings.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| models/hws.py | HwsInhibitTimeSettings.inhibit_time_available | HotWaterInterval.InhibitAvailable | Typed equivalent |
| models/hws.py | HwsInhibitTimeSettings.start_time | HotWaterInterval.StartTime | Typed equivalent |
| models/hws.py | HwsInhibitTimeSettings.end_time | HotWaterInterval.EndTime | Typed equivalent |
| models/hws.py | HwsSet1Settings.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| models/hws.py | HwsSet1Settings.start_time | HotWaterInterval.StartTime | Typed equivalent |
| models/hws.py | HwsSet1Settings.end_time | HotWaterInterval.EndTime | Typed equivalent |
| models/hws.py | HwsSet2Settings.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| models/hws.py | HwsSet2Settings.set2_available | HotWaterInterval.SecondPeriodAvailable | Typed equivalent |
| models/hws.py | HwsSet2Settings.start_time | HotWaterInterval.StartTime | Typed equivalent |
| models/hws.py | HwsSet2Settings.end_time | HotWaterInterval.EndTime | Typed equivalent |
| models/hws.py | HwsSet2Settings.start_temperature | HotWaterInterval.StartTemperature | Typed equivalent |
| models/hws.py | HwsSet2Settings.end_temperature | HotWaterInterval.EndTemperature | Typed equivalent |
| models/hws.py | HwsDailySettings.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| models/hws.py | HwsDailySettings.weekday | HotWaterDay.Weekday | Typed equivalent |
| models/hws.py | HwsDailySettings.mode | HotWaterDay.Mode | Typed equivalent |
| models/hws.py | HwsDailySettings.inhibit_time | HotWaterDay.InhibitTime | Typed equivalent |
| models/hws.py | HwsDailySettings.set1 | HotWaterDay.FirstPeriod | Typed equivalent |
| models/hws.py | HwsDailySettings.set2 | HotWaterDay.SecondPeriod | Typed equivalent |
| models/hws.py | HwsParameters.has_changed | DeviceObservation<T>.HasChanged / Update result | Explicit application-owned tracking |
| models/hws.py | HwsParameters.hpu_operation_status | HotWaterParameters.Power | Typed equivalent |
| models/hws.py | HwsParameters.operation_mode | HotWaterParameters.Mode | Typed equivalent |
| models/hws.py | HwsParameters.boost_mode | HotWaterParameters.Boost | Typed equivalent |
| models/hws.py | HwsParameters.tank_temperature | HotWaterParameters.TankTemperature | Typed equivalent |
| models/hws.py | HwsParameters.outdoor_temperature | HotWaterParameters.OutdoorTemperature | Typed equivalent |
| models/hws.py | HwsParameters.weekly_settings | HotWaterParameters.WeeklySettings | Typed equivalent |
| models/hws.py | HwsConsumption.tank_consumption | HotWaterEnergyHistoryEntry.Consumption | Typed equivalent |
| models/hws.py | HwsConsumption.tank_cost | HotWaterEnergyHistoryEntry.Cost | Typed equivalent |
| models/hws.py | HwsConsumption.data_time | HotWaterEnergyHistoryEntry.DateTime | Typed equivalent |
| models/hws.py | HwsConsumption.outdoor_temp | HotWaterEnergyHistoryEntry.OutdoorTemperature | Typed equivalent |
| models/hws.py | HwsConsumption.tank_temp | HotWaterEnergyHistoryEntry.TankTemperature | Typed equivalent |
| models/hws.py | HwsConsumption.total_consumption | HotWaterEnergyHistoryEntry.ReportedTotal | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerZone.zone_id | AcZone.Id | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerZone.zone_name | AcZone.Name | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerZone.zone_on_off | AcZone.Power | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.is_empty | TimerPattern.IsEmpty | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.start_time | AcTimerPattern.StartTime | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.power | AcTimerPattern.Power | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.mode | AcTimerPattern.Mode | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.mode_raw | AcTimerPattern.RawMode | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.temperature | AcTimerPattern.Temperature | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.fan_speed | AcTimerPattern.Fan | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.fan_auto_mode | AcTimerPattern.FanAutoMode | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.air_swing_ud | AcTimerPattern.VerticalSwing | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.air_swing_lr | AcTimerPattern.HorizontalSwing | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.eco_mode | AcTimerPattern.Eco | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.eco_navi | AcTimerPattern.EcoNavi | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.nanoe | AcTimerPattern.Nanoe | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.iauto | AcTimerPattern.IAuto | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.zones | AcTimerPattern.Zones | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerPattern.raw | Attribute-defined typed model + unknown-field round trips using ComfortCloudJson | Typed adaptation; no public raw JSON |
| models/weeklytimer.py | WeeklyTimerDay.weekday | WeeklyTimerDay<T>.Weekday | Typed equivalent |
| models/weeklytimer.py | WeeklyTimerDay.patterns | WeeklyTimerDay<T>.Patterns | Typed equivalent |
| models/weeklytimer.py | WeeklyTimer.device_guid | AcSchedule.DeviceGuid | Typed equivalent |
| models/weeklytimer.py | WeeklyTimer.enabled | AcSchedule.Enabled | Typed equivalent |
| models/weeklytimer.py | WeeklyTimer.permission | AcSchedule.Permission | Typed equivalent |
| models/weeklytimer.py | WeeklyTimer.can_edit | AcSchedule.CanEdit | Typed equivalent |
| models/weeklytimer.py | WeeklyTimer.temperature_unit | AcSchedule.TemperatureUnit | Typed equivalent |
| models/weeklytimer.py | WeeklyTimer.mode_availability | AcSchedule.ModeAvailability | Typed equivalent |
| models/weeklytimer.py | WeeklyTimer.days | AcSchedule.Days | Typed equivalent |
| models/weeklytimer.py | WeeklyTimer.raw | Attribute-defined typed model + unknown-field round trips using ComfortCloudJson | Typed adaptation; no public raw JSON |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.is_empty | TimerPattern.IsEmpty | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.start_time | AquareaTimerPattern.StartTime | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.mode | AquareaTimerPattern.Mode | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.mode_raw | AquareaTimerPattern.RawMode | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.tank_on_off | AquareaTimerPattern.TankPower | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.tank_temperature | AquareaTimerPattern.TankHeat | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerPattern.raw | Attribute-defined typed model + unknown-field round trips using ComfortCloudJson | Typed adaptation; no public raw JSON |
| models/aquareaweeklytimer.py | AquareaWeeklyTimerDay.patterns | WeeklyTimerDay<AquareaTimerPattern>.Patterns | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer.gwid | AquareaSchedule.GatewayId | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer._weekly_timer | AquareaSchedule.Timer | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer.enabled | AquareaSchedule.Enabled | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer.zone_status | AquareaSchedule.Limits.Zones | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer.tank_status | AquareaSchedule.TankLimits | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer.days | AquareaSchedule.Timer.Days | Typed equivalent |
| models/aquareaweeklytimer.py | AquareaWeeklyTimer.raw | Attribute-defined typed model + unknown-field round trips using ComfortCloudJson | Typed adaptation; no public raw JSON |
| changerequestbuilder.py | ChangeRequestBuilder.has_changes | AcCommandBuilder.HasChanges | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.eco_mode | AcCommandBuilder.Build().Eco | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.target_temperature | AcCommandBuilder.Build().TargetTemperature | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.fan_speed | AcCommandBuilder.Build().Fan | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.hvac_mode | AcCommandBuilder.Build().Mode | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.horizontal_swing | AcCommandBuilder.Build().HorizontalSwing and FanAutoMode | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.vertical_swing | AcCommandBuilder.Build().VerticalSwing and FanAutoMode | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.nanoe_mode | AcCommandBuilder.Build().Nanoe | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.eco_navi_mode | AcCommandBuilder.Build().EcoNavi | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.eco_function_mode | AcCommandBuilder.Build().EcoFunction | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.inside_cleaning_mode | AcCommandBuilder.Build().InsideCleaning | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.power_mode | AcCommandBuilder.Build().Power | Typed/application-owned equivalent |
| changerequestbuilder.py | ChangeRequestBuilder.iautox_mode | AcCommandBuilder.Build().IAutoX | Typed/application-owned equivalent |
| panasonicsession.py | PanasonicSession.authentication | CreateAuthorizationRequest / AuthenticateWithPasswordAsync / CompletePasswordMfaAsync | Typed/application-owned equivalent |
| panasonicsession.py | PanasonicSession.app_version | ComfortCloudClient.AppVersion | Typed/application-owned equivalent |
| panasonicsettings.py | PanasonicSettings.version | AppVersionObservation.Version / ComfortCloudClient.AppVersion | Typed/application-owned equivalent |
| panasonicsettings.py | PanasonicSettings.is_version_expired | AppVersionDiscovery.GetAsync two-day expiry | Typed/application-owned equivalent |
| panasonicsettings.py | PanasonicSettings.is_access_token_valid | CurrentSession.ExpiresAt, checked by EnsureSessionAsync | Typed/application-owned equivalent |
| panasonicsettings.py | PanasonicSettings.has_refresh_token | CurrentSession.RefreshToken presence | Typed/application-owned equivalent |
| panasonicsettings.py | PanasonicSettings.access_token | CurrentSession.AccessToken | Typed/application-owned equivalent |
| panasonicsettings.py | PanasonicSettings.refresh_token | CurrentSession.RefreshToken | Typed/application-owned equivalent |
| panasonicsettings.py | PanasonicSettings.scope | CurrentSession.Scope | Typed/application-owned equivalent |
| panasonicsettings.py | PanasonicSettings.clientId | CurrentSession.ClientId | Typed/application-owned equivalent |
| apiclient/__init__.py | ApiClient.unknown_devices | GetDevicesAsync(DeviceFamily.Unknown) | Typed/application-owned equivalent |
| apiclient/__init__.py | ApiClient.has_unknown_devices | GetDevicesAsync(Unknown) result.Any() | Typed/application-owned equivalent |
| apiclient/__init__.py | ApiClient.aquarea_devices | GetDevicesAsync(DeviceFamily.Aquarea) | Typed/application-owned equivalent |
| apiclient/__init__.py | ApiClient.has_aquarea_devices | GetDevicesAsync(Aquarea) result.Any() | Typed/application-owned equivalent |
| apiclient/__init__.py | ApiClient.hws_devices | GetDevicesAsync(DeviceFamily.HotWater) | Typed/application-owned equivalent |
| apiclient/__init__.py | ApiClient.has_hws_devices | GetDevicesAsync(HotWater) result.Any() | Typed/application-owned equivalent |
| apiclient/__init__.py | ApiClient.has_devices | GetDevicesAsync() result.Any() | Typed/application-owned equivalent |
| apiclient/_protocol.py | ApiClientCore.unknown_devices | GetDevicesAsync(DeviceFamily.Unknown) | Typed/application-owned equivalent |
| apiclient/_protocol.py | ApiClientCore.has_unknown_devices | GetDevicesAsync(Unknown) result.Any() | Typed/application-owned equivalent |

## Wire-field mapping

The JSON inventory supplies one row per field and is exercised by the test suite. Its field groups are:

| Upstream class | C# model | Wire fields |
| --- | --- | --- |
| AquareaConsumption | EnergyHistoryEntry | coolConsumption, coolCost, dataTime, heatConsumption, heatCost, outdoorTemp, tankConsumption, tankCost |
| AquareaDevice | AquareaSnapshot | a2wName, operation, ownerFlg, status |
| AquareaDeviceParameters | AquareaStatus | bivalent, bivalentActual, controlBox, coolMode, deiceStatus, direction, externalHeater, faultStatus, forceDHW, forceHeater, holidayTimer, modelSeriesSelection, multiOdConnection, operationMode, operationStatus, outdoorNow, powerful, pumpDuty, quietMode, serviceType, specialStatus, standAlone, tankStatus, uncontrollableTaw1Flg, waterPressure, zoneStatus |
| AquareaTankStatus | AquareaTank | heatMax, heatMin, heatSet, operationStatus, temperatureNow |
| AquareaWeeklyTimer | AquareaSchedule | status, weeklyTimer |
| AquareaWeeklyTimerPattern | AquareaTimerPattern | mode, startTime, tankOnOff, tankTemperature, zone1Cool, zone1Heat, zone1OnOff, zone2Cool, zone2Heat, zone2OnOff |
| AquareaZoneStatus | AquareaZone | comfortCool, comfortHeat, coolMax, coolMin, coolSet, ecoCool, ecoHeat, heatMax, heatMin, heatSet, operationStatus, temperatureNow, zoneId, zoneName |
| HwsConsumption | HotWaterEnergyHistoryEntry | consumption, cost, dataTime, outdoorTemperature, tankTemperature |
| HwsDailySettings | HotWaterDay | inhibitTime, mode, set1, set2, weekday |
| HwsDevice | HotWaterSnapshot | parameters |
| HwsDeviceParameters | HotWaterSnapshot | holidayMode, permission, pvMode, temperatureSettingLimits, weeklyMode |
| HwsInhibitTimeSettings | HotWaterInterval | endTime, inhibitTimeAvailable, startTime |
| HwsParameters | HotWaterParameters | boostMode, hpuOperationStatus, operationMode, outdoorTemperature, tankTemperature, weeklySettingInfo |
| HwsSet1Settings | HotWaterInterval | endTime, startTime |
| HwsSet2Settings | HotWaterInterval | endTemperature, endTime, set2Available, startTemperature, startTime |
| HwsTemperatureSettingLimits | HotWaterTemperatureLimits | set1EndTemperature, set2EndTemperatureMax, set2EndTemperatureMin, set2StartTemperatureMax, set2StartTemperatureMin |
| PanasonicDevice | AcSnapshot | parameters, timestamp |
| PanasonicDeviceEnergy | AcEnergyHistoryEntry | consumption, coolConsumptionRate, heatConsumptionRate |
| PanasonicDeviceFeatures | AcSnapshot | airSwingLR, autoMode, autoSwingUD, clothesDrying, coolMode, deviceNanoe, dryMode, ecoFunction, ecoNavi, fanDirectionMode, fanMode, fanSpeedMode, fireplace, heatMode, iAutoX, insideCleaning, modeAvlList, nanoe, nanoeStandAlone, offTimer, pairedFlg, permission, powerConsumptionFlg, powerfulMode, quietMode, summerHouse, temperatureUnit |
| PanasonicDeviceInfo | DeviceInfo | autoMode, coordinableFlg, deviceGuid, deviceHashGuid, deviceModuleNumber, deviceName, deviceType, groupName, modeAvlList, modelVersion, nanoeStandAlone, permission, powerConsumptionFlg, summerHouse, temperatureUnit |
| PanasonicDeviceParameters | AcParameters | airDirection, airQuality, airSwingLR, airSwingUD, ecoFunctionData, ecoMode, ecoNavi, fanAutoMode, fanSpeed, fireplace, iAuto, insideCleaning, insideTemperature, lastSettingMode, nanoe, operate, operationMode, outTemperature, temperatureSet, zoneParameters |
| PanasonicDeviceZone | AcZone | zoneId, zoneLevel, zoneName, zoneOnOff, zoneSpill, zoneTemperature |
| WeeklyTimer | AcSchedule | avlFlg, modeAvlList, permission, temperatureUnit, weeklyTimerList |
| WeeklyTimerDay | WeeklyTimerDay`1 | patternList |
| WeeklyTimerPattern | AcTimerPattern | airSwingLR, airSwingUD, ecoMode, ecoNavi, fanAutoMode, fanSpeed, iAuto, mode, nanoe, onOff, startTime, temperature, zoneParameters |
| WeeklyTimerZone | AcZone | zoneId, zoneName, zoneOnOff |