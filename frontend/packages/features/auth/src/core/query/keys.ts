/**
 * @notrelix/feature-auth — Auth query keys.
 *
 * Type A: CRUD server state.
 */

export const authQueryKeys = {
  all: ["auth"] as const,
  profile: ["auth", "profile"] as const,
} as const;
