/**
 * Compatibility derivation of the legacy ALLOWED_IMPORTS map from the
 * closed-world architecture manifest. The manifest is the source of truth;
 * this file exists only for consumers still referencing the old shape.
 */
import { ARCHITECTURE_MANIFEST } from './architecture-manifest';

export const ALLOWED_IMPORTS = Object.fromEntries(
  ARCHITECTURE_MANIFEST.map(({ packageName, allowedInternalImports }) => [
    packageName,
    [...allowedInternalImports],
  ]),
) satisfies Record<string, string[]>;
