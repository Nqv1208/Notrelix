/**
 * @notrelix/query — TanStack Query configuration and optimistic mutation engine
 *
 * Shared QueryClient configuration, optimistic command pattern,
 * and query utilities for the entire application.
 *
 * NOTE: Domain-specific query keys live in their respective feature packages:
 *   workManagement: @notrelix/work-management-core
 *   workspace: @notrelix/features-workspace
 *   auth: @notrelix/features-auth
 */

export { createQueryClient } from './query-client';
export {
  executeOptimisticCommand,
  defineOptimisticUpdate,
  type ExecuteOptimisticCommandOptions,
  type ExecuteOptimisticCommandOptions as OptimisticCommandOptions,
  type OptimisticSnapshot,
  type OptimisticUpdate,
} from './optimistic-command';
