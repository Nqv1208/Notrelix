/**
 * @notrelix/feature-account — Account query keys.
 *
 * Type A: CRUD server state.
 */

export const accountQueryKeys = {
  all: ["account"] as const,
  profile: ["account", "profile"] as const,
  preferences: ["account", "preferences"] as const,
  security: ["account", "security"] as const,
} as const;
