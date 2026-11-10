# Shared build tasks

`Invoke.ps1` downloads the immutable shared Cake Frosting component identified by `component.json`, then executes `recipe.json`. Product/IDE builds do not download Cake. Local tasks do not publish or deploy anything.

```powershell
./build/Invoke.ps1 -Task xml-documentation
$env:BUILD_FRAMEWORK = 'net10.0'
./build/Invoke.ps1 -Task unit-tests
./build/Invoke.ps1 -Task documentation
./build/Invoke.ps1 -Task package
```

Use Windows for the full solution, console and `net472` execution. `net10.0` library tests are portable. Every recipe task first applies the shared strict source XML policy to both frameworks in Debug and Release. Pages use the shared default/modern DocFX layout, search and API/property-value checks.


## Public library publication

The common Cake task `release-assets` runs the configured package and app preparation. See `build/release.json` and the [shared publication protocol](https://github.com/oznetmaster/BuildAutomation/blob/38d25666f55186e6ddbb73d256ac26524c1054bd/ReleasePublishing/README.md). Publication runs in a separate job from retained, hash-verified artifacts. Keep the original run when retrying; use Re-run failed jobs so existing NuGet payloads can be verified against the same bytes.

If the publisher itself needs correction, dispatch this workflow from the default branch with the original `artifact_run_id` and exact `source_commit`. The recovery job verifies that successful build and publishes its frozen assets without rebuilding. Publication uses no GitHub environment binding, matching the shared standard and its package-scoped NuGet policy. No environment or deployment restriction setup is required.
