/**
 * @notrelix/feature-governance — Governance core types.
 *
 * Framework-neutral: no React, no DOM.
 */

export type {
  GovernanceRole,
  GovernancePermission,
  AuditLogEntry,
} from './types/governance';

export { governanceQueryKeys } from './query/keys';

export {
  createUseRoles,
  createUseCreateRole,
  createUseAuditLogs,
} from './query/hooks/use-governance';

export {
  createGovernanceService,
  type GovernanceApiClient,
  type GovernanceEndpoints,
} from './api/governance.service';
