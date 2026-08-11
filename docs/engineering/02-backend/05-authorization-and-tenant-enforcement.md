---
title: "Authorization and Tenant Enforcement"
document_class: handbook
normative: true
owner: backend-security
maturity: FROZEN
conformance: CANONICAL
applies_to: backend/application
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Authorization and Tenant Enforcement

## BE-AUTH-101 — Security requirement is use-case-owned

Commands/queries declare the approved security/resource requirement; Application policy resolves actor/resource permission. Endpoints do not invent a parallel permission model.

## BE-AUTH-102 — Resource authorization and tenant verification are linked

A caller-provided `WorkspaceId`/resource ID is not trusted merely because it is in the route. Resource resolution verifies the actual scope and permission before use-case behavior.

## Request context

Application handlers use the approved request-context abstraction for current user/account/workspace facts. Direct HTTP context is forbidden. Tenant-runtime/persistence/RLS infrastructure may use lower-level tenant context where required by mechanism ownership.

## Permissioned caching

### BE-AUTH-CACHE-101 — Permission-sensitive cache keys include a real permission version

A permissioned cache identity must include sufficient scope and actor identity (account/workspace/user as applicable) plus a real permission-version stamp. Placeholder values such as `default`/`unknown` are invalid because they can serve stale authorization state.

Permission version may derive from the latest relevant membership/role/resource-policy updates as implemented by the approved provider.

## Owner/Admin/Guest semantics

Do not collapse role names into equivalent power unless Governance semantics say so. Guest restrictions, ownership transfer/last-owner safety and permission changes belong to product/governance rules and are tested as explicit use cases.

## RLS

RLS is defense in depth after Application authorization. The same tenant identity used for authorization must be carried into persistence/consumer transactions so a correctly authorized request does not execute under an unrelated database scope.
