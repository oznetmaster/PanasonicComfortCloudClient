# Public releases

Public GitHub tags and NuGet versions use three parts, for example `1.0.0` / `v1.0.0`. `Directory.Build.props` owns the version. Update README, CHANGELOG, RELEASE-NOTES and `release-notes/vVERSION.md`; keep validation limitations and upstream provenance accurate.

## Validation and publication

- Run the full Debug and Release solution builds and both offline test targets. Debug promotes warnings to errors; Release does not. Run the shared documentation task for strict XML and Pages validation, and verify EditorConfig formatting/style.
- Run workstation and external processor validation appropriate to the change. The library remains Crestron-independent; its processor host belongs to `CrestronLibraryTests`. Preserve credentials, use the shared processor lease, remove transferred private inputs and the temporary host, and verify original configuration before release.
- Push the exact release source and require successful `ci.yml` and `documentation.yml`. Hosted CI includes Windows/net472, Linux/net10.0 and the Windows console. Pages deploy only from main.
- The repository variable `RELEASE_REQUIRED_CHECKS` includes `Processor tests / PanasonicComfortCloudClient` (GitHub Actions app 15368). Until an automated processor check is wired to this repository, use the manual publishing workflow with `skip_hardware_checks=true` and a factual evidence statement: full source SHA, completion time, pass/failure counts, frozen host hash, cleanup and private receipt location. This records reuse of completed independent tests; it does not create an automated hardware pass.
- Normally leave `skip_desktop_checks=false` so the workflow verifies completed hosted CI. The shared gate also supports explicitly recorded workstation evidence and recent ancestor CI reuse under its existing restrictions. Do not describe a skipped or unavailable test as passed.
- Dispatch `.github/workflows/dotnet-publish.yml` for the intended version. Publication is serialized within this repository while other repositories can release independently. The workflow restores lock files, checks version collisions, runs strict XML/packaging through the pinned shared component, publishes the Windows console and retains the exact release assets before authentication.
- NuGet uses Trusted Publishing: owner `oznetmaster`, repository `PanasonicComfortCloudClient`, workflow `dotnet-publish.yml`, no environment, exact package restriction `PanasonicComfortCloudClient`. No long-lived key is stored in the repository. The initial policy allows new packages and versions because the first release creates this package.
- NuGet publication precedes the GitHub release. GitHub receives the same `.nupkg`, a framework-dependent Windows console ZIP and SHA256SUMS. Verify both public services, the console download and the Pages site before reporting completion.

If publication fails, inspect the retained run artifact and the external publication state first. Do not rebuild or replace bytes for a version that NuGet has accepted. If NuGet succeeds but GitHub release creation fails, recover the GitHub release using the retained assets and original source commit; do not push the NuGet package again. Do not move a published tag or silently overwrite an existing release asset.

Normal cloning, IDE builds and local tests do not publish, deploy or contact a processor. Explicit shared build tasks download the immutable component in `build/component.json`; maintainers should review and advance that pin deliberately.
