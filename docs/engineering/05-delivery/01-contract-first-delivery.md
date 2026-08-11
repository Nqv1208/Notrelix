---
title: "Contract-First Delivery"
document_class: handbook
normative: true
owner: engineering-delivery
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Contract-First Delivery

## DLV-CONTRACT-101 — Cross-boundary behavior starts with an explicit contract

For REST, realtime, integration events or cross-package public APIs, establish producer semantics, consumer needs, version/compatibility, authorization/tenant scope, error behavior and rollout order before independently implementing both sides.

## Sequence

1. confirm product semantics and owner;
2. define/change transport/event/public contract;
3. assess backward/forward compatibility;
4. implement producer while preserving old consumers where required;
5. generate/publish checked artifacts;
6. update consumers/adapters;
7. run contract/integration/drift gates;
8. remove compatibility path only after consumer migration evidence.

## DLV-CONTRACT-102 — Internal implementation is not a contract

Do not couple frontend to database naming, consumers to CLR event class names alone, or one package to another package's internal folder. Contracts expose stable semantic identity.

## Independent delivery

If producer/consumer deploy independently, additive evolution is preferred. A breaking coordinated switch needs an explicit deployment sequence and rollback/forward-recovery plan. “Both changes are in the same PR” does not by itself prove production atomicity.
