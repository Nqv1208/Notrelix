---
title: "Application Vertical Slices"
document_class: handbook
normative: true
owner: backend-application
maturity: FROZEN
conformance: CANONICAL
applies_to: backend/application
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Application Vertical Slices

Application owns use-case orchestration. The canonical new-code layout is **bounded-context → module → command/query → use case**.

## BE-APP-101 — Canonical module-first layout

```text
Features/{BoundedContext}/
  {Module}/
    Commands/
      {UseCase}/
        {UseCase}Command.cs or consolidated use-case file
        {UseCase}CommandValidator.cs
    Queries/
      {UseCase}/
        {UseCase}Query.cs
        {UseCase}QueryValidator.cs
    DTOs/
    ReadModels/
    Mapping/
    Permissions/
    Cache/
    Services/          # only narrow module-owned orchestration
  Common/              # context-wide only when truly shared across modules
  Abstractions/        # context ports/read services when justified
```

Do not introduce legacy inversions such as `Features/{Context}/Commands/{Module}/{UseCase}` for new code.

## BE-APP-102 — Command declares execution semantics

A command uses the existing request marker/contracts matching its behavior, for example:

- transactional when it writes durable state;
- workspace/account/global/resource scope marker as appropriate;
- authorization requirement;
- idempotency for retryable/external duplicate-prone operations;
- expected-version for concurrency-sensitive updates;
- entitlement/feature gate;
- realtime/post-commit semantics where supported.

Do not add a marker mechanically; each marker is an execution contract consumed by pipeline behavior.

## BE-APP-103 — Query is still a protected use case

Workspace/account queries filter the tenant in the database/read model and apply permission/visibility. Return DTO/read models, not Domain entities. Use no-tracking read behavior unless tracking is explicitly required.

Unsafe pattern:

```text
find Board by BoardId only
cache as board:{BoardId}
```

Safe scope includes workspace/account identity as defined by the query contract.

## Handler responsibilities

Allowed:

- load aggregate through owning-context port;
- load cross-aggregate/external facts through explicit ports;
- invoke Domain behavior;
- add/update through owning Application persistence abstraction;
- return approved result/DTO;
- enqueue approved post-commit intent through pipeline/mechanism.

Forbidden:

- direct `SaveChangesAsync` in ordinary handlers when commit is pipeline-owned;
- manual transaction start;
- `DateTimeOffset.UtcNow`/HTTP context access instead of Application abstractions;
- external email/provider calls as durable side effects before commit;
- direct write through another context's persistence abstraction;
- business invariant implemented only in handler.

## BE-APP-104 — Narrow services must justify existence

Before creating `...Service`, answer:

```text
Why is this not the use-case handler?
Why is this not Domain logic?
Why is this not Infrastructure?
Which module owns it?
Which concrete use cases reuse it?
```

Generic `BoardService`, `UserService`, `AppService`, `Helper` are rejected because they hide ownership.

## Cross-context

Reads may use explicit read services/projections/snapshots. Writes default to durable integration event/consumer or an explicitly designed process manager. One handler mutating three contexts is not “orchestration”; it is ownership erosion unless a reviewed transactional workflow explicitly requires it.
