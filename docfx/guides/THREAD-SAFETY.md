# Thread safety and Mono audit

Audited on **9 November 2026** in the maintained C# 14, net10.0/net472 library. The audit covers library-owned shared state, compound operations, cancellation, disposal, publication and the Framework transport. Mutable request/response DTOs and command builders remain application-owned; callers must synchronize their own edits and keep submitted models stable until an operation completes.

## Synchronization contract

| Area | Protection and ownership |
| --- | --- |
| Client requests, pacing, cookies, pending password attempts | One asynchronous operation gate per client; all accesses to compound private request state remain within that gate. |
| Schedule activation and explicit pending-policy acceptance | The entire read/edit/write or document/status/accept sequence holds the same client gate. Concurrent operations on this client cannot switch accounts or overwrite the intervening state. Other applications and clients can still change cloud state. |
| Session and app-version getters | Immutable objects/strings published with `Volatile.Write` and read with `Volatile.Read`. No collection or mutable DTO is exposed through the credential snapshot. |
| Authentication context consumption | `Interlocked.Exchange` admits exactly one consumer. Invalid/foreign legacy contexts do not consume a valid attempt. |
| Buffered AC preferences | Serialized asynchronous calls plus a short Monitor-protected commit. Dispose clears the buffer under the same commit lock, preventing merge/clear races and post-disposal buffer resurrection. |
| App-version cache | Serialized lookups, acquire/release reference publication, and lifecycle cancellation for both owned and injected HTTP clients. |
| Device observations | Updates and individual getters use a Monitor. `Current` is an independent coherent typed copy. Refresh includes tracker publication within the originating client's request gate. |
| Energy tracking | Chronological validation and the complete update run under one Monitor; getters use that Monitor too. Nullable doubles and timestamps cannot tear on 32-bit Mono. Separate property reads may observe different completed updates. |
| Session-change notifications | FIFO notification records are queued before releasing the operation gate and delivered serially afterward. Concurrent callers wait for delivery. Reentrant session operations queue their own notification without waiting for the active callback. Subscriber failures are isolated; separate clients/processes still need coordinated storage ownership. |
| JSON options, protocol constants and static delegates | Fully constructed static readonly instances; serializer options are not mutated after publication. |

The shared gate counts **waiting callers as well as holders**. Disposal closes admission and state publication before requesting cancellation. Cancellation callbacks run outside its Monitor, so reentrant disposal does not deadlock. Synchronization primitives and owned HTTP resources are released exactly once, after admitted work exits and cancellation callbacks return. Injected HTTP clients remain application-owned. A late response that ignores cancellation cannot publish a new session/cache after disposal.

Disposal does not synchronously wait for a remote response and cannot retract a command already delivered to Panasonic. Retained credential snapshots remain readable; disposal does not log out or erase application storage.

## Findings corrected

- Preference merging read `_pending` repeatedly while `Dispose` could clear it outside the request semaphore. The merge now uses one local reference and shares its commit lock with disposal.
- Version discovery did not cancel queued/in-flight work on an injected HTTP client and could publish a cache entry after disposal. Client-driven version selection also contacted discovery before checking the client's lifecycle. Both paths now use lifecycle admission and cancellation.
- Client disposal released owned HTTP resources while active work could still use them. Cleanup is now deferred until the final admitted operation and cancellation callback have exited; queued work is canceled rather than dispatched.
- State publication previously relied on transport cancellation being honored. Explicit cancellation checks and stop-aware commits reject late responses as well.
- Schedule activation and pending-policy acceptance released the client gate between their reads and writes, allowing intervening requests or a changed account. Private core methods retain one gate across each sequence without nested semaphore acquisition.
- Tracker updates and multiword getters lacked synchronization. Their validation, updates and reads now use Monitor synchronization. Refresh publishes before releasing the client gate, preserving ordering for requests through that client.

## Why these primitives on Mono

[Mono's memory-model documentation](https://www.mono-project.com/docs/advanced/runtime/docs/atomics-memory-model/) specifies acquire/release semantics for `Volatile`, sequentially consistent atomic operations for `Interlocked`, and the limits of ordinary multiword loads on 32-bit systems. The design relies on those contracts and BCL synchronization, not workstation-specific processor ordering.

The three shared state locks deliberately use Monitor-backed `object` instances on both targets. A narrowly scoped `IDE0330` style suppression prevents the modern-target formatter from replacing them with `System.Threading.Lock`, which is absent from net472. No compiler or correctness warning is suppressed. Modern guard and timeout-wait APIs remain at call sites, with Framework compatibility shims.

## Validation

The 22 focused concurrency cases use fake credentials and controlled HTTP bodies/callbacks. They cover active/queued disposal, late refresh/cache responses, injected-client ownership, one-use contexts, preference merging, callback reentrancy and failure, permit conservation, account isolation, tracker publication and bounded contention. A separate compatibility case checks generic/non-generic timeout waits without canceling the underlying task. Runtime output records the actual Mono version and pointer size.

These cases run within the complete workstation suites and the same merged/patched assembly installed temporarily on the processor. Exact counts, runtime evidence and cleanup status are recorded in [validation status](VALIDATION.md). Stress cases supplement controlled interleavings and the ownership/locking review; they do not constitute a proof of every possible scheduling order. No physical HVAC changes are required for this audit.
