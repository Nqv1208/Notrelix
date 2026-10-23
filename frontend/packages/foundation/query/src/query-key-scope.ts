/**
 * @notrelix/query — canonical TanStack Query key scopes.
 *
 * Every server-state query key in the Notrelix client starts with exactly one
 * of three roots: `global`, `account`, or `workspace`. There is no registry:
 * these helpers are direct tuple construction, and `assertNotrelixQueryKey`
 * exists for test/tooling validation.
 */

export function globalQueryKey<
  const R extends string,
  const S extends readonly unknown[],
>(resource: R, ...segments: S): readonly ["global", R, ...S] {
  return ["global", resource, ...segments] as readonly ["global", R, ...S];
}

/**
 * Account-scoped key: `["account", accountId, resource, ...segments]`.
 *
 * Until the backend exposes a distinct Account identity, the client defines
 * Account as the authenticated principal (`User.id`). The ID is mandatory so
 * Account A data can never be addressed by Account B.
 */
export function accountQueryKey<
  const A extends string,
  const R extends string,
  const S extends readonly unknown[],
>(accountId: A, resource: R, ...segments: S): readonly ["account", A, R, ...S] {
  assertAccountId(accountId);
  return ["account", accountId, resource, ...segments] as readonly [
    "account",
    A,
    R,
    ...S,
  ];
}

export function assertAccountId(
  accountId: unknown,
): asserts accountId is string {
  if (typeof accountId !== "string" || accountId.length === 0) {
    throw new Error("account-scoped query key requires a non-empty account ID");
  }
}

export function workspaceQueryKey<
  const W extends string,
  const R extends string,
  const S extends readonly unknown[],
>(
  workspaceId: W,
  resource: R,
  ...segments: S
): readonly ["workspace", W, R, ...S] {
  return ["workspace", workspaceId, resource, ...segments] as readonly [
    "workspace",
    W,
    R,
    ...S,
  ];
}

/**
 * Validates a query key shape. Rejects:
 *
 * - empty keys;
 * - a root other than `global|account|workspace`;
 * - a missing resource;
 * - a workspace root with an empty or missing workspace ID;
 * - an account root with an empty or missing account ID or resource.
 *
 * This is test/tooling validation, not a runtime global key registry.
 */
export function assertNotrelixQueryKey(key: readonly unknown[]): void {
  if (key.length === 0) {
    throw new Error("Notrelix query key must not be empty");
  }

  const root = key[0];
  if (root !== "global" && root !== "account" && root !== "workspace") {
    throw new Error(`invalid Notrelix query key root: ${String(root)}`);
  }

  if (root === "workspace") {
    if (key.length < 3) {
      throw new Error("workspace-scoped query key requires a resource segment");
    }
    const workspaceId = key[1];
    if (typeof workspaceId !== "string" || workspaceId.length === 0) {
      throw new Error(
        "workspace-scoped query key requires a non-empty workspace ID",
      );
    }
    return;
  }

  if (root === "account") {
    const accountId = key[1];
    if (typeof accountId !== "string" || accountId.length === 0) {
      throw new Error(
        "account-scoped query key requires a non-empty account ID",
      );
    }
    if (key.length < 3) {
      throw new Error("account-scoped query key requires a resource segment");
    }
    return;
  }

  if (key.length < 2) {
    throw new Error(`query key root "${root}" requires a resource segment`);
  }
}
