---
title: "Contract Boundaries"
document_class: handbook
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: system
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Contract Boundaries

Notrelix uses explicit contracts wherever independently changing components communicate.

## Contract classes

- REST/OpenAPI requests, responses, errors, pagination and concurrency/idempotency metadata;
- realtime logical events/channels/payloads;
- domain events (internal business facts);
- integration events (durable cross-boundary facts);
- generated frontend TypeScript contracts;
- persistence schema when deployed data is consumed across versions.

## SYS-CON-001 — Producer and consumer are explicit

Every public/cross-boundary contract names the producer and known consumer classes. A contract with no owner is not stable enough to freeze.

## SYS-CON-002 — Implementation classes are not transport contracts

Do not serialize a Domain aggregate or EF entity as an API/event contract simply because fields currently match. Transport contracts evolve under compatibility/versioning rules.

## SYS-CON-003 — Generated output is changed through source

Generated frontend contracts are not hand-edited. Change the source artifact/generator, regenerate, review diff, and run drift checks.

## Change impact

For any contract change answer:

1. backward compatible for which consumer versions?
2. additive, semantic, or breaking?
3. rollout order producer vs consumer?
4. cached/persisted payload compatibility?
5. event replay/backlog implications?
6. codegen and documentation update?
