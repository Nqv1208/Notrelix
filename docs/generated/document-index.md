---
document_id: DOC-GEN-DOCUMENT-INDEX
document_type: generated
status: generated
owner: documentation-governance
applies_to:
  - repository-documentation-inventory
  - documentation-discovery
  - documentation-governance-evidence
evidence:
  - scripts/docs/generate-document-index.mjs
  - scripts/docs/check-metadata.mjs
  - docs/governance/documentation-authority.md
  - docs/governance/documentation-lifecycle.md
review_on:
  - canonical-document-added
  - canonical-document-removed
  - document-metadata-change
  - documentation-index-generator-change
---

# Notrelix Documentation Index

<!-- GENERATED FILE — DO NOT EDIT. -->
<!-- Producer: scripts/docs/generate-document-index.mjs -->
<!-- Source: canonical document frontmatter -->
<!-- Regenerate: node scripts/docs/generate-document-index.mjs -->
<!-- Check drift: node scripts/docs/generate-document-index.mjs --check -->

> This file is generated discovery evidence.
> It does not replace the canonical authority described by each source document.

Document count: 85

## Documents by type

| Value | Count |
|:---|---:|
| `architecture` | 22 |
| `architecture-decision` | 10 |
| `architecture-policy` | 1 |
| `decision-registry` | 3 |
| `delivery-handbook` | 1 |
| `delivery-policy` | 6 |
| `generated` | 3 |
| `governance` | 5 |
| `index-router` | 1 |
| `infrastructure-standard` | 3 |
| `operations` | 2 |
| `operations-standard` | 4 |
| `product-context` | 12 |
| `product-experience` | 1 |
| `quality-standard` | 4 |
| `template` | 6 |
| `testing-strategy` | 1 |

## Documents by status

| Value | Count |
|:---|---:|
| `Accepted` | 9 |
| `active` | 72 |
| `generated` | 3 |
| `Superseded` | 1 |

## Document inventory

| Document ID | Type | Status | Owner | Applies to | Source |
|:---|:---|:---|:---|:---|:---|
| `ADR-001` | `architecture-decision` | `Accepted` | `backend-architecture` | `backend`, `backend-application`, `application-pipeline` | [`backend/docs/decisions/ADR-001-pipeline-boundary.md`](../../backend/docs/decisions/ADR-001-pipeline-boundary.md) |
| `ADR-002` | `architecture-decision` | `Accepted` | `backend-architecture` | `backend`, `backend-security`, `backend-tenancy`, `backend-rls`, `backend-application`, `backend-infrastructure` | [`backend/docs/decisions/ADR-002-rls-bootstrap-connection-lifecycle.md`](../../backend/docs/decisions/ADR-002-rls-bootstrap-connection-lifecycle.md) |
| `ADR-003` | `architecture-decision` | `Superseded` | `backend-architecture` | `backend`, `backend-api`, `backend-security`, `browser-authentication`, `csrf` | [`backend/docs/decisions/ADR-003-csrf-protection.md`](../../backend/docs/decisions/ADR-003-csrf-protection.md) |
| `ADR-004` | `architecture-decision` | `Accepted` | `backend-architecture` | `backend`, `backend-api`, `backend-application`, `backend-security`, `rate-limiting`, `abuse-protection` | [`backend/docs/decisions/ADR-004-rate-limiting-architecture.md`](../../backend/docs/decisions/ADR-004-rate-limiting-architecture.md) |
| `ADR-005` | `architecture-decision` | `Accepted` | `backend-architecture` | `backend`, `backend-api`, `backend-security`, `browser-authentication`, `csrf` | [`backend/docs/decisions/ADR-005-csrf-cross-origin-bootstrap.md`](../../backend/docs/decisions/ADR-005-csrf-cross-origin-bootstrap.md) |
| `BE-API-CONTRACTS` | `architecture` | `active` | `backend-architecture` | `backend/src/Notrelix.API`, `backend/tests/Notrelix.API.Tests`, `backend/tests/Notrelix.Integration.Tests` | [`backend/docs/architecture/api-and-contracts.md`](../../backend/docs/architecture/api-and-contracts.md) |
| `BE-APPLICATION-MODEL` | `architecture` | `active` | `backend-architecture` | `backend/src/Notrelix.Application`, `backend/tests/Notrelix.Application.Tests`, `backend/tests/Notrelix.Integration.Tests` | [`backend/docs/architecture/application-model.md`](../../backend/docs/architecture/application-model.md) |
| `BE-BACKEND-OVERVIEW` | `architecture` | `active` | `backend-architecture` | `backend`, `backend-production-projects`, `backend-project-boundaries` | [`backend/docs/architecture/backend-overview.md`](../../backend/docs/architecture/backend-overview.md) |
| `BE-CONFIGURATION-RUNTIME` | `operations` | `active` | `backend-runtime-operations` | `backend`, `backend-runtime`, `backend-configuration`, `backend-secrets`, `backend-startup`, `backend-dependencies`, `backend-local-development` | [`backend/docs/operations/configuration-and-runtime.md`](../../backend/docs/operations/configuration-and-runtime.md) |
| `BE-DECISIONS-INDEX` | `decision-registry` | `active` | `backend-architecture` | `backend`, `backend-architecture-decisions` | [`backend/docs/decisions/README.md`](../../backend/docs/decisions/README.md) |
| `BE-DOMAIN-MODELING` | `architecture` | `active` | `backend-architecture` | `backend/src/Notrelix.Domain`, `backend/tests/Notrelix.Domain.Tests` | [`backend/docs/architecture/domain-modeling.md`](../../backend/docs/architecture/domain-modeling.md) |
| `BE-GEN-PROJECT-MAP` | `generated` | `generated` | `backend-architecture` | `backend-project-inventory`, `backend-project-references`, `backend-test-project-relationships` | [`backend/docs/generated/project-map.md`](../../backend/docs/generated/project-map.md) |
| `BE-INFRASTRUCTURE-DATA` | `architecture` | `active` | `backend-architecture` | `backend/src/Notrelix.Infrastructure`, `backend/tests/Notrelix.Infrastructure.Tests`, `backend/tests/Notrelix.Integration.Tests` | [`backend/docs/architecture/infrastructure-and-data.md`](../../backend/docs/architecture/infrastructure-and-data.md) |
| `BE-MIGRATIONS-DATA-CHANGE` | `operations` | `active` | `backend-data-operations` | `backend-persistence`, `backend-migrations`, `backend-data-change`, `backend-backfills`, `backend-rls`, `backend-indexes`, `backend-data-repair` | [`backend/docs/operations/migrations-and-data-change.md`](../../backend/docs/operations/migrations-and-data-change.md) |
| `BE-PLATFORM-MESSAGING` | `architecture` | `active` | `backend-architecture` | `backend/src/Notrelix.Platform`, `backend/tests/Notrelix.Platform.Tests`, `backend/tests/Notrelix.Integration.Tests` | [`backend/docs/architecture/platform-and-messaging.md`](../../backend/docs/architecture/platform-and-messaging.md) |
| `BE-SECURITY-TENANCY-AUTHORIZATION` | `architecture` | `active` | `backend-security-architecture` | `backend`, `authentication`, `authorization`, `tenancy`, `rls`, `privileged-operations`, `security-sensitive-cache`, `background-execution`, `realtime`, `provider-boundaries` | [`backend/docs/architecture/security-tenancy-authorization.md`](../../backend/docs/architecture/security-tenancy-authorization.md) |
| `BE-TESTING-QUALITY-GATES` | `architecture` | `active` | `backend-quality-architecture` | `backend-tests`, `backend-ci`, `backend-quality-gates`, `backend-architecture-tests`, `backend-contract-tests` | [`backend/docs/architecture/testing-and-quality-gates.md`](../../backend/docs/architecture/testing-and-quality-gates.md) |
| `DECISIONS-INDEX` | `decision-registry` | `active` | `architecture` | `repository`, `system-architecture`, `backend`, `frontend`, `architecture-decisions` | [`docs/decisions/README.md`](../decisions/README.md) |
| `DEL-CHANGE-CLASSIFICATION` | `delivery-policy` | `active` | `engineering-delivery` | `repository`, `backend`, `frontend`, `contracts`, `data`, `infrastructure`, `documentation` | [`docs/delivery/change-classification.md`](../delivery/change-classification.md) |
| `DEL-CONTRACT-FIRST` | `delivery-policy` | `active` | `engineering-delivery` | `repository`, `backend`, `frontend`, `api`, `events`, `realtime`, `generated-contracts`, `public-packages`, `integrations` | [`docs/delivery/contract-first-delivery.md`](../delivery/contract-first-delivery.md) |
| `DEL-DEFINITION-OF-DONE` | `delivery-policy` | `active` | `engineering-delivery` | `repository`, `backend`, `frontend`, `documentation`, `ci` | [`docs/delivery/definition-of-done.md`](../delivery/definition-of-done.md) |
| `DEL-LOCAL-DEVELOPMENT` | `delivery-handbook` | `active` | `engineering-delivery` | `repository`, `local-development`, `onboarding`, `backend`, `frontend`, `docker`, `developer-tooling` | [`docs/delivery/local-development.md`](../delivery/local-development.md) |
| `DEL-MIGRATION` | `delivery-policy` | `active` | `engineering-delivery` | `repository`, `backend`, `database`, `persisted-contracts`, `data-backfills`, `ownership-migrations`, `projections`, `message-backlogs` | [`docs/delivery/migration-policy.md`](../delivery/migration-policy.md) |
| `DEL-RELEASE-ROLLOUT` | `delivery-policy` | `active` | `engineering-delivery` | `repository`, `backend`, `frontend`, `mobile`, `workers`, `infrastructure`, `feature-flags`, `releases` | [`docs/delivery/release-and-rollout.md`](../delivery/release-and-rollout.md) |
| `DEL-TEAM-OWNERSHIP` | `delivery-policy` | `active` | `engineering-delivery` | `repository`, `product-contexts`, `backend`, `frontend`, `platform`, `quality`, `delivery`, `operations`, `infrastructure` | [`docs/delivery/team-ownership.md`](../delivery/team-ownership.md) |
| `DOC-AUTHORITY` | `governance` | `active` | `documentation-governance` | `repository` | [`docs/governance/documentation-authority.md`](../governance/documentation-authority.md) |
| `DOC-DECISION-EXCEPTION` | `governance` | `active` | `documentation-governance` | `repository` | [`docs/governance/decision-and-exception-policy.md`](../governance/decision-and-exception-policy.md) |
| `DOC-GEN-RULE-INDEX` | `generated` | `generated` | `documentation-governance` | `repository-rule-inventory`, `architecture-rule-discovery`, `documentation-governance-evidence` | [`docs/generated/rule-index.md`](./rule-index.md) |
| `DOC-LIFECYCLE` | `governance` | `active` | `documentation-governance` | `repository` | [`docs/governance/documentation-lifecycle.md`](../governance/documentation-lifecycle.md) |
| `DOC-QUALITY-GATES` | `governance` | `active` | `documentation-governance` | `repository` | [`docs/governance/documentation-quality-gates.md`](../governance/documentation-quality-gates.md) |
| `DOC-TOPIC-AUTHORITY` | `governance` | `active` | `documentation-governance` | `repository` | [`docs/governance/topic-authority-map.md`](../governance/topic-authority-map.md) |
| `FE-ADR-001` | `architecture-decision` | `Accepted` | `frontend-architecture` | `frontend-hosts`, `frontend-framework-split`, `frontend-web`, `frontend-mobile`, `frontend-marketing` | [`frontend/docs/decisions/FE-ADR-001-framework-split.md`](../../frontend/docs/decisions/FE-ADR-001-framework-split.md) |
| `FE-ADR-002` | `architecture-decision` | `Accepted` | `frontend-architecture` | `frontend-package-management`, `frontend-workspace`, `frontend-lockfile`, `frontend-ci-install`, `frontend-toolchain` | [`frontend/docs/decisions/FE-ADR-002-package-manager.md`](../../frontend/docs/decisions/FE-ADR-002-package-manager.md) |
| `FE-ADR-003` | `architecture-decision` | `Accepted` | `frontend-architecture` | `frontend-package-exports`, `frontend-public-package-api`, `frontend-cross-package-imports`, `frontend-deep-import-policy`, `frontend-package-encapsulation` | [`frontend/docs/decisions/FE-ADR-003-package-exports.md`](../../frontend/docs/decisions/FE-ADR-003-package-exports.md) |
| `FE-ADR-004` | `architecture-decision` | `Accepted` | `frontend-architecture` | `frontend-framework-boundaries`, `frontend-nextjs-boundary`, `frontend-reusable-packages`, `frontend-web`, `frontend-mobile`, `frontend-marketing` | [`frontend/docs/decisions/FE-ADR-004-no-next-in-packages.md`](../../frontend/docs/decisions/FE-ADR-004-no-next-in-packages.md) |
| `FE-ADR-005` | `architecture-decision` | `Accepted` | `frontend-architecture` | `frontend-authentication`, `frontend-session-management`, `frontend-cookie-auth`, `frontend-auth-refresh`, `frontend-session-expiry`, `frontend-auth-navigation`, `frontend-csrf-client-contract` | [`frontend/docs/decisions/FE-ADR-005-auth-session-model.md`](../../frontend/docs/decisions/FE-ADR-005-auth-session-model.md) |
| `FE-ARCH-API-CONTRACTS` | `architecture` | `active` | `frontend-platform` | `frontend-api`, `frontend-contracts`, `frontend-codegen`, `frontend-api-client`, `frontend-error-contracts`, `frontend-auth-transport`, `frontend-idempotency` | [`frontend/docs/architecture/api-and-contracts.md`](../../frontend/docs/architecture/api-and-contracts.md) |
| `FE-ARCH-CHANGE-POLICY` | `architecture-policy` | `active` | `frontend-platform` | `frontend-architecture-changes`, `frontend-package-changes`, `frontend-host-changes`, `frontend-contract-foundation`, `frontend-state-foundation`, `frontend-realtime-foundation`, `frontend-ui-foundation`, `frontend-testing-foundation` | [`frontend/docs/architecture/architecture-change-policy.md`](../../frontend/docs/architecture/architecture-change-policy.md) |
| `FE-ARCH-DEPENDENCY-BOUNDARIES` | `architecture` | `active` | `frontend-platform` | `frontend-package-graph`, `frontend-import-boundaries`, `frontend-public-exports`, `frontend-mobile-purity`, `frontend-generated-architecture-evidence` | [`frontend/docs/architecture/dependency-boundaries.md`](../../frontend/docs/architecture/dependency-boundaries.md) |
| `FE-ARCH-FRONTEND-OVERVIEW` | `architecture` | `active` | `frontend-platform` | `frontend`, `frontend-workspace`, `frontend-hosts`, `frontend-package-architecture`, `frontend-product-client-boundary` | [`frontend/docs/architecture/frontend-overview.md`](../../frontend/docs/architecture/frontend-overview.md) |
| `FE-ARCH-HOSTS-COMPOSITION-ROUTING` | `architecture` | `active` | `frontend-platform` | `frontend-hosts`, `web-composition`, `mobile-composition`, `marketing-composition`, `frontend-routing`, `frontend-session-bootstrap`, `frontend-service-lifecycle` | [`frontend/docs/architecture/hosts-composition-routing.md`](../../frontend/docs/architecture/hosts-composition-routing.md) |
| `FE-ARCH-REALTIME` | `architecture` | `active` | `frontend-platform` | `frontend-realtime`, `realtime-transport`, `realtime-recovery`, `realtime-subscriptions`, `realtime-product-reconciliation`, `realtime-workspace-lifecycle` | [`frontend/docs/architecture/realtime.md`](../../frontend/docs/architecture/realtime.md) |
| `FE-ARCH-STATE-QUERY-MUTATIONS` | `architecture` | `active` | `frontend-platform` | `frontend-server-state`, `frontend-query`, `frontend-cache`, `frontend-mutations`, `frontend-optimistic-updates`, `frontend-scope-transitions`, `frontend-client-state` | [`frontend/docs/architecture/state-query-mutations.md`](../../frontend/docs/architecture/state-query-mutations.md) |
| `FE-ARCH-TESTING-QUALITY-GATES` | `architecture` | `active` | `frontend-platform` | `frontend-testing`, `frontend-quality-gates`, `frontend-ci`, `frontend-architecture-gates`, `frontend-codegen-gates`, `frontend-mobile-verification`, `frontend-ui-verification`, `frontend-e2e` | [`frontend/docs/architecture/testing-and-quality-gates.md`](../../frontend/docs/architecture/testing-and-quality-gates.md) |
| `FE-ARCH-UI-DESIGN-SYSTEM` | `architecture` | `active` | `frontend-platform` | `frontend-ui`, `design-tokens`, `web-ui`, `mobile-ui`, `frontend-theme`, `frontend-accessibility`, `frontend-motion`, `frontend-storybook` | [`frontend/docs/architecture/ui-and-design-system.md`](../../frontend/docs/architecture/ui-and-design-system.md) |
| `FE-DECISIONS-INDEX` | `decision-registry` | `active` | `frontend-architecture` | `frontend-decisions`, `frontend-architecture-history`, `frontend-adr-governance` | [`frontend/docs/decisions/README.md`](../../frontend/docs/decisions/README.md) |
| `FE-GEN-PACKAGE-BOUNDARIES` | `generated` | `generated` | `frontend-architecture` | `frontend-package-graph`, `frontend-import-boundaries`, `frontend-architecture-evidence` | [`frontend/docs/generated/package-boundaries.md`](../../frontend/docs/generated/package-boundaries.md) |
| `INFRA-CONTAINERIZATION-LOCAL-SERVICES` | `infrastructure-standard` | `active` | `infrastructure` | `container-builds`, `container-images`, `docker-compose`, `local-services`, `local-networking`, `local-volumes`, `development-tooling`, `packaging-ci` | [`docs/infrastructure/containerization-and-local-services.md`](../infrastructure/containerization-and-local-services.md) |
| `INFRA-DEPLOYMENT-RUNTIME` | `infrastructure-standard` | `active` | `infrastructure` | `deployment`, `runtime`, `processes`, `networking`, `persistence`, `cache`, `messaging`, `object-storage`, `external-providers`, `promotion` | [`docs/infrastructure/deployment-runtime.md`](../infrastructure/deployment-runtime.md) |
| `INFRA-ENVIRONMENT-MODEL` | `infrastructure-standard` | `active` | `infrastructure` | `local`, `ci`, `staging`, `production`, `configuration`, `secrets`, `feature-flags`, `environment-data` | [`docs/infrastructure/environment-model.md`](../infrastructure/environment-model.md) |
| `OPS-INCIDENT-READINESS` | `operations-standard` | `active` | `operations` | `runtime`, `incidents`, `production`, `security-events`, `data-integrity-events`, `release-failures`, `dependency-failures` | [`docs/operations/incident-readiness.md`](../operations/incident-readiness.md) |
| `OPS-OBSERVABILITY` | `operations-standard` | `active` | `operations` | `runtime`, `api`, `background-processing`, `messaging`, `realtime`, `database`, `integrations`, `frontend`, `mobile` | [`docs/operations/observability.md`](../operations/observability.md) |
| `OPS-RECOVERY-DATA-SAFETY` | `operations-standard` | `active` | `operations` | `production-data`, `database`, `migrations`, `background-processing`, `messaging`, `integrations`, `object-storage`, `projections`, `recovery` | [`docs/operations/recovery-and-data-safety.md`](../operations/recovery-and-data-safety.md) |
| `OPS-SERVICE-DEGRADATION` | `operations-standard` | `active` | `operations` | `runtime`, `api`, `database`, `cache`, `messaging`, `realtime`, `object-storage`, `integrations`, `frontend`, `mobile` | [`docs/operations/service-degradation.md`](../operations/service-degradation.md) |
| `PROD-ACCOUNTS` | `product-context` | `active` | `accounts` | `accounts`, `account-administration`, `enterprise-administration` | [`docs/product/accounts.md`](../product/accounts.md) |
| `PROD-ANALYTICS` | `product-context` | `active` | `analytics` | `analytics`, `reporting`, `dashboards`, `widgets`, `metrics`, `snapshots`, `projections` | [`docs/product/analytics.md`](../product/analytics.md) |
| `PROD-AUTOMATION` | `product-context` | `active` | `automation` | `automation`, `rules`, `triggers`, `conditions`, `actions`, `executions`, `scheduling`, `automation-templates` | [`docs/product/automation.md`](../product/automation.md) |
| `PROD-BILLING` | `product-context` | `active` | `billing` | `billing`, `plans`, `subscriptions`, `entitlements`, `usage`, `invoices`, `payment-methods`, `billing-customers` | [`docs/product/billing.md`](../product/billing.md) |
| `PROD-COLLABORATION` | `product-context` | `active` | `collaboration` | `collaboration`, `comments`, `mentions`, `reactions`, `attachments`, `presence`, `read-state`, `watchers`, `notifications`, `user-activity` | [`docs/product/collaboration.md`](../product/collaboration.md) |
| `PROD-DOCUMENTS` | `product-context` | `active` | `documents` | `documents`, `pages`, `blocks`, `document-hierarchy`, `resource-links`, `document-versions`, `document-templates` | [`docs/product/documents.md`](../product/documents.md) |
| `PROD-EXPERIENCE` | `product-experience` | `active` | `product-design` | `authenticated-product`, `marketing`, `web`, `mobile`, `product-copy`, `accessibility` | [`docs/product/product-experience.md`](../product/product-experience.md) |
| `PROD-GOVERNANCE` | `product-context` | `active` | `governance` | `governance`, `authorization`, `permissions`, `policies`, `roles`, `sharing`, `share-links`, `security-audit` | [`docs/product/governance.md`](../product/governance.md) |
| `PROD-IDENTITY` | `product-context` | `active` | `identity` | `identity`, `authentication`, `sessions`, `credentials`, `mfa`, `oauth`, `api-tokens`, `user-security` | [`docs/product/identity.md`](../product/identity.md) |
| `PROD-INDEX` | `index-router` | `active` | `product` | `product`, `repository` | [`docs/product/README.md`](../product/README.md) |
| `PROD-INTEGRATIONS` | `product-context` | `active` | `integrations` | `integrations`, `provider-connections`, `provider-sync`, `webhooks`, `calendar-integrations`, `provider-mappings`, `external-side-effects` | [`docs/product/integrations.md`](../product/integrations.md) |
| `PROD-MODEL` | `product-context` | `active` | `product` | `product`, `repository`, `all-bounded-contexts` | [`docs/product/product-model.md`](../product/product-model.md) |
| `PROD-WORK-MANAGEMENT` | `product-context` | `active` | `work-management` | `work-management`, `boards`, `fields`, `items`, `groups`, `views`, `forms`, `relations`, `formulas`, `rollups`, `approvals`, `checklists`, `workload` | [`docs/product/work-management.md`](../product/work-management.md) |
| `PROD-WORKSPACES` | `product-context` | `active` | `workspaces` | `workspaces`, `workspace-membership`, `workspace-invitations`, `spaces`, `teams`, `workspace-scope` | [`docs/product/workspaces.md`](../product/workspaces.md) |
| `QLT-ACCESSIBILITY` | `quality-standard` | `active` | `product-accessibility` | `web`, `mobile`, `marketing`, `authenticated-product`, `ui-components`, `product-workflows` | [`docs/quality/accessibility-standard.md`](../quality/accessibility-standard.md) |
| `QLT-ENGINEERING` | `quality-standard` | `active` | `engineering-quality` | `repository`, `backend`, `frontend`, `documentation`, `ci` | [`docs/quality/engineering-quality-standard.md`](../quality/engineering-quality-standard.md) |
| `QLT-PERFORMANCE` | `quality-standard` | `active` | `engineering-quality` | `repository`, `backend`, `frontend`, `api`, `data`, `messaging`, `realtime`, `analytics`, `integrations` | [`docs/quality/performance-and-scalability.md`](../quality/performance-and-scalability.md) |
| `QLT-SECURITY` | `quality-standard` | `active` | `engineering-security` | `repository`, `backend`, `frontend`, `api`, `background-processing`, `integrations`, `ci` | [`docs/quality/security-quality-standard.md`](../quality/security-quality-standard.md) |
| `QLT-TESTING` | `testing-strategy` | `active` | `engineering-quality` | `repository`, `backend`, `frontend`, `ci` | [`docs/quality/testing-strategy.md`](../quality/testing-strategy.md) |
| `SYS-BOUNDED-CONTEXT-MAP` | `architecture` | `active` | `system-architecture` | `product`, `backend`, `frontend`, `cross-context-contracts` | [`docs/architecture/bounded-context-map.md`](../architecture/bounded-context-map.md) |
| `SYS-CAPABILITY-EXTRACTION` | `architecture` | `active` | `system-architecture` | `repository`, `backend`, `frontend`, `bounded-contexts`, `deployment`, `data-ownership`, `public-contracts`, `operations` | [`docs/architecture/capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |
| `SYS-CONTRACT-BOUNDARIES` | `architecture` | `active` | `system-architecture` | `repository`, `backend`, `frontend`, `public-contracts`, `integration-contracts`, `realtime-contracts`, `generated-contracts` | [`docs/architecture/contract-boundaries.md`](../architecture/contract-boundaries.md) |
| `SYS-DATA-OWNERSHIP-CONSISTENCY` | `architecture` | `active` | `system-architecture` | `repository`, `backend`, `frontend`, `data`, `cross-context-workflows`, `projections`, `caching`, `asynchronous-processing` | [`docs/architecture/data-ownership-and-consistency.md`](../architecture/data-ownership-and-consistency.md) |
| `SYS-EVENTS-REALTIME-DELIVERY` | `architecture` | `active` | `system-architecture` | `repository`, `backend`, `frontend`, `domain-events`, `integration-events`, `messaging`, `realtime`, `activity`, `audit` | [`docs/architecture/events-realtime-and-delivery-boundary.md`](../architecture/events-realtime-and-delivery-boundary.md) |
| `SYS-OVERVIEW` | `architecture` | `active` | `system-architecture` | `repository`, `backend`, `frontend`, `public-contracts`, `runtime` | [`docs/architecture/system-overview.md`](../architecture/system-overview.md) |
| `TEMPLATE-ADR` | `template` | `active` | `documentation-governance` | `docs/decisions`, `backend/docs/decisions`, `frontend/docs/decisions` | [`docs/templates/adr-template.md`](../templates/adr-template.md) |
| `TEMPLATE-ARCHITECTURE-CHANGE` | `template` | `active` | `documentation-governance` | `repository`, `system-architecture`, `backend-architecture`, `frontend-architecture`, `cross-boundary-changes` | [`docs/templates/architecture-change-template.md`](../templates/architecture-change-template.md) |
| `TEMPLATE-FEATURE-SPEC` | `template` | `active` | `documentation-governance` | `product-features`, `backend-features`, `frontend-features`, `cross-stack-features` | [`docs/templates/feature-spec-template.md`](../templates/feature-spec-template.md) |
| `TEMPLATE-INCIDENT-REPORT` | `template` | `active` | `documentation-governance` | `incidents`, `production`, `security`, `data-integrity`, `availability`, `provider-failures`, `release-incidents` | [`docs/templates/incident-template.md`](../templates/incident-template.md) |
| `TEMPLATE-MIGRATION-PLAN` | `template` | `active` | `documentation-governance` | `schema-migrations`, `data-migrations`, `contract-migrations`, `ownership-migrations`, `backfills`, `persisted-identities`, `async-backlogs` | [`docs/templates/migration-plan-template.md`](../templates/migration-plan-template.md) |
| `TEMPLATE-PR-CHECKLIST` | `template` | `active` | `documentation-governance` | `pull-requests`, `change-reviews`, `repository`, `backend`, `frontend`, `documentation`, `infrastructure` | [`docs/templates/pr-checklist.md`](../templates/pr-checklist.md) |

## Generation contract

This index is derived from canonical document frontmatter.

To change a row:

```text
edit the canonical source document metadata
→ run check-metadata.mjs
→ regenerate this index
```

Do not edit this generated table manually.
