/**
 * Search context — CONTRACT-BLOCKED (CTR-GAP-SEARCH).
 *
 * No authoritative global search contract exists in @notrelix/contracts or OpenAPI.
 * Per MFB-FZ-02 and 01-FREEZE-SPEC.md §FZ-S06:
 * The mock backend must not invent speculative producer contracts.
 * This handler module is disabled and excluded from the operation registry.
 */

// Explicitly empty array — no speculative operations registered
export const searchOperations = [];
