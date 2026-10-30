/**
 * @notrelix/wm-state — Work Management state management
 *
 * React Query hooks, mutations, and API clients for work management.
 * Depends on @notrelix/work-management-core for types and schemas.
 */

// API clients
export * from "./api/board.api";
export * from "./api/board-api-mappers";
export * from "./api/board-access.api";
export * from "./api/board-settings.api";
export * from "./api/workspace-automations.api";
export * from "./api/checklist.api";
export * from "./api/field.api";
export * from "./api/field-option.api";
export * from "./api/group.api";
export * from "./api/item.api";
export * from "./api/item-comments.api";
export * from "./api/item-extras.api";
export * from "./api/label.api";
export * from "./api/template.api";
export * from "./api/list.api";
export * from "./api/saved-filter.api";
export * from "./api/form.api";
export * from "./services";

// Query hooks
export * from "./queries";

// Mutation hooks
export * from "./mutations";

// State hooks
export * from "./hooks";

export * from "./api/board-preference.api";

// Cache helpers
export * from "./cache";

// Isolated state scope for component tests and stories
export * from "./testing/work-management-test-scope";
