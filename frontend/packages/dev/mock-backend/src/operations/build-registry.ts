/**
 * Assembles the global operation registry from all context handler modules.
 *
 * Each context registers its own operations independently.
 * The registry enforces the closed-world rule on dispatch.
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Context layout
 */

import { MockOperationRegistry } from "./operation-registry";
import { identityOperations } from "../contexts/identity/identity.handlers";
import { workspaceOperations } from "../contexts/workspace/workspace.handlers";
import { accountOperations } from "../contexts/account/account.handlers";
import { notificationsOperations } from "../contexts/notifications/notifications.handlers";
import { workManagementOperations } from "../contexts/work-management";
import { documentsOperations } from "../contexts/documents";
import { searchOperations } from "../contexts/search/search.handlers";

export function buildOperationRegistry(): MockOperationRegistry {
  const registry = new MockOperationRegistry();

  registry.registerMany([
    // Core identity
    ...identityOperations,
    // Workspace (includes CONTRACT-BLOCKED legacy views/members)
    ...workspaceOperations,
    // Account (all CONTRACT-BLOCKED)
    ...accountOperations,
    // Notifications
    ...notificationsOperations,
    // Work Management (boards, lists, cards)
    ...workManagementOperations,
    // Documents (pages)
    ...documentsOperations,
    // Search
    ...searchOperations,
  ]);

  return registry;
}
