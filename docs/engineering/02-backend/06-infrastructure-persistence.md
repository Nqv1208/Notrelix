---
title: "Infrastructure and Persistence"
document_class: handbook
normative: true
owner: backend-infrastructure
maturity: FROZEN
conformance: CANONICAL
applies_to: backend/infrastructure
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Infrastructure and Persistence

Infrastructure adapts Application/Domain contracts to EF Core/PostgreSQL, Redis, storage/email/security/provider/search implementations.

## BE-INF-101 — Mapping adapts to Domain, not vice versa

Do not add public setters, weak constructors or provider types to Domain solely to make EF mapping easier. Use configuration/backing fields/converters/owned types as appropriate while preserving Domain invariants.

## DbContext ownership

Application consumes context-specific abstractions/ports. Concrete `ApplicationDbContext` and provider-specific query/transaction behavior remain Infrastructure implementation.

## BE-INF-102 — Durable invariants need race-safe database protection

Where uniqueness/referential/concurrency integrity must survive races, combine friendly Application precheck with database unique/FK/check/concurrency constraint. Precheck alone is not protection.

## Concurrency

Map Domain aggregate `Version` or approved concurrency token as an optimistic concurrency contract. Database conflict is translated to the Application/API conflict model; do not overwrite silently.

## Cache

Cache keys include tenant/resource/permission scope required by semantics. Cache is a projection and never the only source of authorization truth. Cache failure should degrade according to the port contract rather than corrupting business state.

## Providers

Provider SDK DTOs/exceptions stay inside Infrastructure. Translate to stable Application contracts/errors. External retries/timeouts belong to provider/runtime policy; Domain never sees SDK clients.

## Search/projections

Search indexes/read projections include tenant scope and stable source identities. Eventual consistency is explicit; use source database/application authorization when search freshness cannot safely make a security decision.
