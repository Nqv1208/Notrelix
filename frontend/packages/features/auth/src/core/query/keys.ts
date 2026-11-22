/**
 * @notrelix/feature-auth — Auth query keys.
 *
 * Type A: CRUD server state.
 */

export const authQueryKeys = {
  all: ["auth"] as const,
  profile: ["auth", "profile"] as const,
  /** Per person: another person's answer is never reachable under this key. */
  bootstrapAccount: (userId: string) =>
    ["auth", "bootstrap-account", userId] as const,
} as const;
