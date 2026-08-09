/**
 * @notrelix/query — canonical TanStack Query key scopes.
 *
 * Every server-state query key in the Notrelix client starts with exactly one
 * of three roots: `global`, `account`, or `workspace`. There is no registry:
 * these helpers are direct tuple construction, and `assertNotrelixQueryKey`
 * exists for test/tooling validation.
 */

export function globalQueryKey<const R extends string, const S extends readonly unknown[]>(
  resource: R,
  ...segments: S
): readonly ['global', R, ...S] {
  return ['global', resource, ...segments] as readonly ['global', R, ...S];
}

export function accountQueryKey<const R extends string, const S extends readonly unknown[]>(
  resource: R,
  ...segments: S
): readonly ['account', R, ...S] {
  return ['account', resource, ...segments] as readonly ['account', R, ...S];
}

export function workspaceQueryKey<
  const W extends string,
  const R extends string,
  const S extends readonly unknown[],
>(
  workspaceId: W,
  resource: R,
  ...segments: S
): readonly ['workspace', W, R, ...S] {
  return ['workspace', workspaceId, resource, ...segments] as readonly [
    'workspace',
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
 * - a workspace root with an empty or missing workspace ID.
 *
 * This is test/tooling validation, not a runtime global key registry.
 */
export function assertNotrelixQueryKey(key: readonly unknown[]): void {
  if (key.length === 0) {
    throw new Error('Notrelix query key must not be empty');
  }

  const root = key[0];
  if (root !== 'global' && root !== 'account' && root !== 'workspace') {
    throw new Error(`invalid Notrelix query key root: ${String(root)}`);
  }

  if (root === 'workspace') {
    if (key.length < 3) {
      throw new Error('workspace-scoped query key requires a resource segment');
    }
    const workspaceId = key[1];
    if (typeof workspaceId !== 'string' || workspaceId.length === 0) {
      throw new Error('workspace-scoped query key requires a non-empty workspace ID');
    }
    return;
  }

  if (key.length < 2) {
    throw new Error(`query key root "${root}" requires a resource segment`);
  }
}
