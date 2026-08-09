// @notrelix/dependency-rules — Import boundary enforcement
// The closed-world architecture manifest is the executable source of truth;
// the exact package boundary table is generated from it
// (see docs/client/architecture/package-boundaries.generated.md).
export {
  ARCHITECTURE_MANIFEST,
  ARCHITECTURE_POLICY_BY_PACKAGE,
  validateArchitectureManifest,
} from "./architecture-manifest";
export type {
  ArchitectureLayer,
  ArchitecturePackagePolicy,
  FreezeScope,
  ManifestViolation,
  ManifestViolationCode,
} from "./architecture-manifest";
export { ALLOWED_IMPORTS } from "./allowed-imports";
export { FORBIDDEN_IMPORTS } from "./forbidden-imports";
