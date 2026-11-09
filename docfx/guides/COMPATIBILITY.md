# .NET Framework compatibility packages

The client uses modern .NET APIs and C# 14 on both targets. Compatibility dependencies are selected from actual source and assembly references, rather than copied wholesale from another project. Versions below record this project's 9 November 2026 audit; check availability and compatibility before reuse.

| Package | Provides | Decision in this solution |
| --- | --- | --- |
| Hafner.Compatibility.MetaPackage 1.9.0 | Broad bundle of compatibility attributes | Replaced with the three families below. |
| Hafner.Compatibility.CallerInformationAttributes.G2 1.0.0 | CallerArgumentExpressionAttribute | Retained for modern guard methods. |
| Hafner.Compatibility.NullableReferenceTypeAttributes.G1 1.0.3 | Includes NotNullAttribute | Retained for nullable flow contracts. |
| Hafner.Compatibility.NullableReferenceTypeAttributes.G3 1.0.3 | NullableAttribute and NullableContextAttribute | Retained for the compiled nullable metadata. |
| RequiredMemberAttribute 1.0.0 | Compiler support for required members | Build-only; private package assets. |
| IsExternalInit 1.0.3 | Compiler support for init accessors | Build-only; private package assets. |
| Microsoft.NETFramework.ReferenceAssemblies 1.0.3 | Framework reference assemblies for portable builds | Build-only; private package assets. |
| System.Memory 4.6.3 | Span, Memory and related APIs | Direct library use; retained. |
| System.Threading.Tasks.Extensions 4.6.3 | ValueTask and async infrastructure | Direct library use; retained. |
| Microsoft.Bcl.AsyncInterfaces 10.0.12 | Async interface support | Required transitively by JSON dependencies; explicit reference removed. |
| System.Runtime.CompilerServices.Unsafe 6.1.2 | Runtime support for memory operations | Required transitively; explicit reference removed. |
| Microsoft.Bcl.Memory 10.0.12 | Includes System.Index and System.Range | Not required by the library. Retained only in net472 tests, which compile range/index expressions and the linked console parser. |
| Microsoft.Bcl.HashCode 6.0.0 | System.HashCode | Not used; removed. |

The broad Hafner metapackage also supplies ConstantExpected, DynamicallyAccessedMembers, Experimental, FeatureGuard and the G2 nullable-flow attribute family. These are available when a future project needs them, but none is referenced by this client's compiled library. Retaining the three used families preserves their existing assembly identities.

Private build packages can inject compiler support into the assembly without becoming runtime package dependencies. Conversely, removing a redundant direct reference does not remove a runtime dependency still needed transitively. Inspect the restored dependency graph and package nuspec, not just the PackageReference list.

## Validation when changing dependencies

1. Restore lock files and build the complete solution in Debug and Release. A library-only build misses dependencies used by tests or linked console source.
2. Run offline suites on both frameworks and inspect the net472 assembly references and package dependency groups.
3. Rebuild the external processor host from the changed source, freeze its exact package and rerun the portable suite on Mono. The host has its own dependencies; its merged dependency closure is distinct from the public library package.
4. Run bounded read-only hardware checks using preserved credentials, then clear private transferred inputs and remove the temporary host.

The library's C# 14 extension members implement missing modern guard, hashing, hexadecimal, timeout-wait and memory-stream APIs. These are source shims, not a reason to add every available compatibility package. The Framework bodyless-GET transport handler is a separate protocol shim. See [validation](VALIDATION.md) and [thread safety](THREAD-SAFETY.md).
