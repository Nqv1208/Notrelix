# Realtime Protocol & Transport Specification

> **State Machine Transport, Heartbeat, Deduplication & Sequence Gap Recovery**

---

## 1. Protocol Architecture

Realtime messages are parsed via `parseRealtimeMessage` into discriminated union types:
- **Control Messages:** `{ kind: 'control', message: Ping | Pong | Subscribed | Resumed | SubscriptionError }`
- **Domain Envelopes:** `{ kind: 'domain', envelope: RealtimeEnvelope<TPayload> }`

---

## 2. Transport State Machine

`RealtimeClient` in `@notrelix/realtime` operates as an explicit State Machine:
- **Connection States:** `idle` | `connecting` | `connected` | `reconnecting` | `offline` | `closed` | `failed`.
- **Heartbeat Timeout:** Emits `ping` frames periodically; triggers disconnect and reconnect upon exceeding `maximumMissedPongs`.
- **Deduplication:** Bounded LRU cache keyed by `eventId` with TTL eviction.
- **Sequence Gap Detection:** Emits `RealtimeSequenceGap` when received sequence exceeds expected sequence by > 1, triggering module recovery refetch.
