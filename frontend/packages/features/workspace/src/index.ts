/**
 * @notrelix/features-workspace — Workspace feature package.
 *
 * Core types, services, query keys, and hooks.
 */

// Core exports (API, types, query hooks/keys)
export * from './core';
export { workspaceQueryKeys } from './core/query/keys';

// Web exports (Mutation hooks)
export * from './web';
export { workspaceRealtimeAdapter } from './realtime-adapter';
