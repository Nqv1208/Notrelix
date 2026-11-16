---
document_id: SPEC-CHAT-SCALE-ARCHITECTURE
document_type: feature-spec
status: draft
owner: chat
applies_to:
  - chat
  - chat-media
  - chat-calls
  - chat-realtime
  - deployment
  - operations
evidence:
  - docs/architecture/system-overview.md
  - docs/architecture/capability-extraction-strategy.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/decisions/SYS-ADR-001-chat-bounded-context.md
  - docs/decisions/SYS-ADR-002-chat-rich-media-and-calls.md
  - docs/workstreams/specs/chat-foundation.md
  - docs/workstreams/specs/chat-media-and-calls.md
  - docker-compose.prod.yml
review_on:
  - operational-scaling-change
  - deployment-topology-change
  - call-infrastructure-change
  - chat-realtime-change
  - database-ownership-change
---

# Chat at scale: infrastructure and domain plan

This plan covers the infrastructure and the domain of Chat (messages, media, voice messages, calls) for a system with millions of users. It builds no feature. It says what each part must guarantee, how it grows, and the measured trigger that moves it to the next stage.

Today's shape is one backend process, one PostgreSQL, one Redis, one RabbitMQ and nginx in `docker-compose.prod.yml`. That shape stays correct until the triggers in section 12 fire. SYS-002 (the modular monolith is the default) and the extraction strategy still apply: a part leaves the monolith only when its readiness criteria are met, never because the user count grew.

## 1. Capacity model

The numbers below are design targets, not measurements. Every target is re-checked against production metrics before a stage begins.

| Quantity                       | Stage 2 target (1 M)    | Stage 3 target (10 M)      | Assumption                                                                |
| ------------------------------ | ----------------------- | -------------------------- | ------------------------------------------------------------------------- |
| Registered users               | 1,000,000               | 10,000,000                 |                                                                           |
| Daily active users             | 300,000                 | 3,000,000                  | 30 % of registered                                                        |
| Peak concurrent connections    | 100,000                 | 1,000,000                  | about a third of DAU online at peak, one socket per tab after tab sharing |
| Messages per day               | 15 M                    | 150 M                      | 50 per DAU                                                                |
| Message writes per second      | 175 average, 1,000 peak | 1,750 average, 10,000 peak | peak is 6× the average                                                    |
| Realtime frames out per second | 50,000 peak             | 500,000 peak               | average 50 online readers per message                                     |
| Stored messages after 3 years  | 16 B                    | 160 B                      | no retention purge; retention lowers it                                   |
| Message bytes after 3 years    | 8 TB                    | 80 TB                      | about 500 bytes per row and index                                         |
| Attachment bytes per day       | 1.5 TB                  | 15 TB                      | 10 % of messages carry a file, median 1 MB                                |
| Concurrent call participants   | 10,000                  | 100,000                    | 3 % of concurrent connections                                             |
| SFU egress                     | 10 Gbit/s               | 100 Gbit/s                 | about 1 Mbit/s down per participant at an adaptive layer                  |

Four facts follow from the table.

1. Writes are modest. One PostgreSQL primary handles stage 2 writes. Stage 3 needs the message store split by space.
2. Fan-out is the load, not writes. Realtime delivery is about 50 times the write rate and must scale on its own tier.
3. History is the size problem. Older messages are read rarely and must move off the hot store.
4. Media bandwidth is the cost. Calls and files cost far more in egress than in compute.

## 2. Principles

1. **One owner per fact.** Chat owns spaces, memberships, messages, threads, read state and calls. Collaboration owns attachment rows and file policy. Platform owns the gateway, storage and media infrastructure. Scale never moves ownership.
2. **Planes are separate runtimes.** The API, the realtime gateway, the workers and the media plane deploy, scale and fail separately, even while they share one codebase.
3. **Stateless compute.** API and worker processes keep no session state. The gateway keeps only socket state, which a client can rebuild by resuming.
4. **Partition by space.** The space id is the unit of ordering, of locking, of fan-out and of sharding. No operation needs two spaces in one transaction.
5. **Retries and duplicates are normal.** Every write is idempotent by a client key. Every consumer tolerates a repeat and an out-of-order delivery.
6. **Degrade, do not fall.** When a dependency is slow, the system drops presence first, then typing, then live delivery (clients poll), and keeps sending and reading messages last.
7. **Measure before moving.** Each stage change has a written trigger, a load test and a rollback.

## 3. Runtime topology (target at stage 2 and later)

```
Clients (web, mobile)
  │  HTTPS, WSS, WebRTC (UDP 50000-60000, TURN 443)
  ▼
Edge: CDN (static, public media variants), WAF, L7 load balancer per region
  │
  ├── API tier             stateless .NET, autoscaled on CPU and p95 latency
  ├── Realtime gateway     .NET WebSocket nodes, autoscaled on connections
  ├── Worker tier          outbox relay, consumers, thumbnails, jobs
  └── Media plane          SFU cluster per region + TURN
        │
Data plane
  ├── PostgreSQL           primary + synchronous standby + read replicas, then sharded by space
  ├── Redis                presence, rate limits, routing, SFU room state (separate instances)
  ├── Event log            partitioned by space id (RabbitMQ streams or Kafka-compatible)
  ├── Object storage       S3-compatible, private, signed URLs
  └── Search index         OpenSearch, fed by events
```

All tiers run in at least two availability zones. Kubernetes replaces Docker Compose from stage 2: the same images, with deployment manifests, horizontal autoscaling, pod disruption budgets and rolling updates. Compose stays for development and single-host staging.

## 4. Data plane

### 4.1 Message store

- **Identity.** Message ids stay UUIDv7, so ids sort by time and index pages stay warm. Order within a space is the server sequence, not the id.
- **Sequence.** The per-space counter (`chat.spaces.last_sequence`) stays. The row lock serializes writers of one space only. A space that gets more than about 200 writes per second is a hot space (section 4.4).
- **Hot store.** PostgreSQL `chat.messages` uses declarative partitioning by hash of `space_id` (64 partitions to start). Partitions keep indexes small and make vacuum and reindex local. The key index is `(space_id, sequence desc)`.
- **Cold history.** Messages older than a set age (12 months to start) move by a background job into monthly range partitions on cheaper storage, still in PostgreSQL. A read of old history pages through the same API; it is slower, not different.
- **Sharding (stage 3).** When one primary passes its limits (section 12), the message tables move to N PostgreSQL shards. The shard is chosen by a directory table `space_id → shard`, not by a formula, so a hot space can move alone. Citus is one option; an application-level directory is the other; the choice is ADR-level work done before stage 3.
- **Wide-column store.** ScyllaDB or Cassandra is not planned. Discord moved to them at trillions of messages ([Discord engineering](https://discord.com/blog/how-discord-stores-trillions-of-messages)), well above the stage 3 target. It becomes a question only if sharded PostgreSQL cannot hold stage 3 latency in a load test.

### 4.2 Everything else in PostgreSQL

Spaces, memberships, read state, threads, pins, reactions, calls and stickers stay in schema `chat` with RLS by workspace. Read state is the highest-write row after messages. It updates in place, is coalesced on the client (one write per second at most) and on the server (last write wins inside a short window).

- **Replicas.** Read replicas serve history, search filters and lists that tolerate a lag of a second. Reads that follow the reader's own write go to the primary.
- **Connections.** PgBouncer in transaction mode sits in front of every PostgreSQL. EF Core keeps its pool per process; the bouncer caps the database at a fixed number of server connections.
- **Backups and DR.** Continuous WAL archiving to object storage, point-in-time recovery, a restore test every month. Targets: RPO 1 minute, RTO 1 hour for the primary region.

### 4.3 Object storage and media

- Files go into a private S3-compatible bucket. The API never streams large bytes in production: upload uses a short-lived signed PUT (or multipart upload for large files), then a commit call that runs the file policy and records the row. Download uses a signed GET of 5 minutes issued after the access check.
- Image dimensions and audio duration are read from the first bytes at commit (as built today), never from the client.
- A thumbnail worker writes resized variants (`320`, `1280` px, WebP) and a poster frame for video. Variants of a file have the same access as the file.
- The CDN caches only signed responses with a cache key that excludes the signature, and only for variants. Originals of private spaces are never cached at the edge.
- Malware scanning runs on commit for types that can carry code. A file is readable only after the scan passes or the type is exempt.
- Lifecycle rules move originals older than 90 days to an infrequent-access class. A soft-deleted file is purged by the retention job.

### 4.4 Hot spaces

A space with thousands of members and high traffic (an all-company channel) gets these limits, each configurable:

- Slow mode (already built) and a per-space write rate limit in Redis.
- Fan-out by topic, not by member: the gateway subscribes sockets to the space topic, so one write is one publish whatever the member count.
- Read receipts and typing are sampled: typing shows "several people are typing" above 5 typers; seen-by lists are not sent live above 50 members.
- If one space alone needs more than one shard can give, it gets its own shard through the directory.

### 4.5 Search

OpenSearch is fed from the outbox events (`chat.message.created|updated|deleted`), indexed by workspace with the space id and the reader set as filter fields. Access is checked twice: the index filters by the spaces the caller can read, and the API re-checks each hit before returning it. PostgreSQL full-text search (today's path) stays until search latency or index size passes its trigger.

## 5. Events and delivery

- **Outbox.** Every durable change writes its facts in the same transaction (as today). A relay worker publishes them; at least once, ordered per space.
- **Event log.** Stage 1 keeps RabbitMQ with consistent-hash exchanges keyed by space id. From stage 2 the chat stream moves to a partitioned log (RabbitMQ streams or a Kafka-compatible broker) with the space id as the partition key, so order per space holds across consumers and a consumer can replay.
- **Consumers.** Each consumer stores the last version it applied per stream key and drops older or equal versions. Poison messages go to a dead-letter queue with an alert, never block the partition.
- **Versioning.** Event payload contracts are versioned (`chat.attachment.v1`). A new field is additive; a breaking change is a new version published beside the old one until every consumer moves.

## 6. Realtime gateway

- **Shape.** Gateway nodes hold WebSockets only. They authenticate with a short-lived ticket from the API, authorize every subscription through Chat's query, and never write business state.
- **Routing.** The gateway subscribes to the event log by partition and keeps an in-memory map `topic → sockets`. Stage 1 uses Redis pub/sub between nodes; stage 2 uses consumer groups on the event log, one group per gateway node, filtered by the topics the node holds.
- **Limits.** The current protocol limits stay: frames of at most 4096 bytes, 50 subscriptions per connection, 20 frames per second per connection. A node holds 50,000 sockets as a design ceiling (load-tested before use); 1 M connections need about 25 nodes with headroom.
- **Resume.** A client that reconnects sends its last stream version per topic. The gateway replays from the event log if the gap is small, else tells the client to refetch over REST. Reconnects are jittered so a node restart does not cause a thundering herd.
- **Presence.** Presence lives in Redis with a TTL heartbeat (30 s), sharded by workspace, and is fanned out only to people who look at the person (a DM partner, an open member list), never to the whole workspace.
- **Backpressure.** Each socket has a bounded send queue. A slow socket loses typing and presence first; if message frames still back up, the socket is closed with a resume hint.

## 7. Calls (media plane)

### 7.1 Choice

The proposal stays LiveKit (open-source SFU, Apache 2.0) self-hosted, because it fits the domain (rooms, tokens, webhooks), runs in Kubernetes and keeps media inside our infrastructure. LiveKit Cloud is the fallback when running media ourselves costs more than it saves; the domain does not change, only the gateway configuration.

### 7.2 Topology

- One SFU cluster per region. Each cluster has its own Redis for room state and uses LiveKit's region-aware, load-aware node selector ([LiveKit distributed setup](https://docs.livekit.io/transport/self-hosting/distributed.md)).
- Self-hosted regions do not relay media to each other ([LiveKit community](https://community.livekit.io/t/do-i-need-a-global-redis-instance-for-multi-region-livekit-deployments/233)). A call is therefore pinned to one region at creation: the region of the caller's workspace. Participants far away pay the latency; this is accepted until cross-region relay is needed.
- SFU nodes use host networking (UDP ports), one pod per node, and scale on CPU at about 60 to 70 % (operator guidance, verify by load test).
- TURN runs on 443/TLS and 3478/UDP with its own public addresses, so people behind strict firewalls connect. TURN credentials are short-lived and issued with the join token.

### 7.3 Control path (our backend)

- Chat owns `Call` and `CallParticipant`. The SFU never decides who may join.
- Join: the API checks space membership and call state, then issues a LiveKit token valid for 60 seconds with the room name `call-{callId}` and the participant identity = user id. Grants are minimal (publish audio and video, subscribe; screen share only when allowed).
- Webhooks from the SFU (participant joined or left, room finished) are verified by signature and update the call. They are idempotent by event id.
- Jobs end ringing calls after 45 seconds (Missed), end Active calls whose room is empty for 60 seconds, and reconcile calls with the SFU room list every minute so a lost webhook cannot leave a call open.
- Rule: one active call per person and per space, enforced by a partial unique index, not only in code.

### 7.4 Limits and quality

- 1:1 and small groups: simulcast with three layers, adaptive stream, dynacast.
- Group calls: 50 participants to start, 25 video publishers at once; above that, audio-only listeners.
- Recording and transcription are out of scope. If needed later they use LiveKit egress workers in their own pool and require a separate privacy decision (consent, retention).

## 8. Domain model

Aggregates and their consistency boundary. A transaction touches one aggregate, except the send path which also updates the author's read state in the same space.

| Aggregate            | Owns                                                                            | Key invariants                                                        | Partition key |
| -------------------- | ------------------------------------------------------------------------------- | --------------------------------------------------------------------- | ------------- |
| Space                | name, kind, archive and delete state, last sequence, retention, slow mode       | sequence gap-free and monotonic; archived space takes no writes       | space id      |
| Membership           | role, read sequence, mention count, notification level, mute                    | read sequence only grows and never passes the space's last sequence   | space id      |
| Message              | content, kind (`text`, `system`, `call`, `sticker`), author, version, tombstone | edit and delete keep the sequence; idempotent by `clientMessageId`    | space id      |
| Thread               | root, reply counters, followers                                                 | reply count is the thread sequence                                    | space id      |
| Reaction             | emoji per person per message                                                    | one per person per emoji                                              | space id      |
| Call                 | state (Ringing, Active, Ended), end reason, room, timestamps                    | one active call per space and per person; Ended is final              | space id      |
| CallParticipant      | joined, left, media flags                                                       | joins only while the call is Ringing or Active                        | space id      |
| StickerPack, Sticker | owned by Notrelix or the workspace                                              | a sticker message keeps the sticker id even after the pack is removed | workspace id  |
| Block                | blocker, blocked                                                                | a block stops new DMs both ways                                       | workspace id  |

- Cross-context facts reach Chat through ports (`IChatMessageAttachments`, `IChatNotificationIntents`, Governance permission checks). No foreign key decides ownership.
- Every command takes the workspace from the path and is checked against the caller's membership, then RLS checks the workspace again in the database.

## 9. Security, tenancy and abuse

- Tenancy: RLS by workspace on every Chat table (as today). At sharding, the shard directory is keyed by space, and each shard keeps the same policies.
- Rate limits in Redis per person, per space and per workspace for sends, uploads, call starts and joins; burst and sustained windows.
- Abuse: blocked file types and sniffed content (as today), malware scan, reporting and moderation delete, audit of moderation actions.
- Secrets (SFU keys, TURN secrets, storage keys) live in the secret store, rotate on a schedule and are never in images.
- Transport encryption everywhere (TLS, DTLS-SRTP). End-to-end encryption is out of scope: it conflicts with search, moderation and retention and needs its own ADR.

## 10. Reliability and SLOs

| Path                                       | SLO                                      |
| ------------------------------------------ | ---------------------------------------- |
| Send message, API accepted                 | 99.95 % monthly; p95 under 200 ms        |
| Message delivered live to an online reader | p95 under 500 ms end to end              |
| History page                               | p95 under 300 ms (hot), under 1 s (cold) |
| Call join to first media                   | p95 under 2 s; join success 99 %         |
| Upload commit of a 10 MB file              | p95 under 3 s after the bytes arrive     |

- Error budgets gate releases: a spent budget stops feature rollout for that path.
- Degradation order (principle 6) is implemented as feature switches the operator can flip.
- Game days each quarter: kill a gateway node, fail over the PostgreSQL primary, cut one SFU node, block the event log for five minutes.

## 11. Observability

- Metrics: message write rate and latency, fan-out lag (event created to frame sent), sockets per node, resume rate, queue depth and consumer lag per partition, PostgreSQL replication lag and lock waits on `spaces`, call setup time, ICE failure rate, TURN share, SFU CPU and packet loss.
- Traces across API → outbox → event log → gateway with the message id as an attribute.
- Logs never carry message content or file names; they carry ids.
- Load tests (k6 or similar) for every stage: 2× the stage's peak send rate, the connection count and a reconnect storm; call tests with synthetic publishers.

## 12. Stages and triggers

| Stage   | Users            | Shape                                                                                                                                                                                     | Move to the next stage when                                                                                         |
| ------- | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| 0 (now) | up to about 10 k | one host, Compose, one of each store                                                                                                                                                      | CPU over 60 % at peak for a week, or one host failure is unacceptable                                               |
| 1       | up to 100 k      | 2+ API and gateway instances behind the load balancer, managed PostgreSQL with a standby, Redis with a replica, object storage with signed URLs, LiveKit single cluster + TURN, PgBouncer | sockets over 30 k, write p95 over 150 ms, or replication lag over 1 s                                               |
| 2       | up to 1 M        | Kubernetes in 2+ zones, separate API, gateway and worker deployments, read replicas, hash-partitioned messages, partitioned event log, OpenSearch, CDN variants, autoscaled SFU           | primary storage over 4 TB, write peak over 60 % of the measured primary limit, or one region's latency unacceptable |
| 3       | 10 M             | message store sharded by space directory, cold history tier, multi-region (a workspace has a home region; calls pinned to it), regional SFU clusters                                      | measured, not planned                                                                                               |

Each stage begins with an ADR that records the measured trigger, the load test result and the rollback.

## 13. Delivery of this plan (no feature work)

1. Stage 1 readiness inside the monolith: signed-URL upload and download, PgBouncer, separate gateway and worker processes from the same image, health and readiness probes, metrics and traces listed in section 11.
2. Partitioning readiness: every Chat query filters by space id; an architecture test fails any Chat query on `messages` without it.
3. Load-test harness and the stage 1 baseline.
4. Call infrastructure in Compose for development (LiveKit + TURN), the `IChatCallMediaGateway` port with a fake, webhooks and reconciliation jobs.
5. Kubernetes manifests and the stage 2 ADR, written when the stage 1 trigger fires.

## 14. Decisions the owner must make

1. SFU: self-hosted LiveKit (proposed) or LiveKit Cloud.
2. Hosting: which cloud or provider, and which first region.
3. Retention of call history and of old messages (affects the cold tier size).
4. Malware scanning provider.
5. GIF provider, or Notrelix stickers only.
6. Whether recording or end-to-end encryption is ever in scope (each changes the design and needs its own ADR).
