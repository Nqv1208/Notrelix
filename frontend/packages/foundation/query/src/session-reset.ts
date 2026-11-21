const callbacks = new Set<() => void>();

/**
 * Registers a callback that drops user content kept in memory outside the
 * query cache (unsent drafts, picked files). It runs on sign-out and when
 * another account signs in. Returns the unregister function.
 */
export function onSessionReset(callback: () => void): () => void {
  callbacks.add(callback);
  return () => callbacks.delete(callback);
}

export function runSessionReset(): void {
  for (const callback of callbacks) callback();
}
