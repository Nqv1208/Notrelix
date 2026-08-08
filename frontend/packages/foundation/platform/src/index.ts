/**
 * @notrelix/platform — Technical ports and environment-independent abstractions.
 *
 * Frozen port categories: clock/time source, storage/key-value capability,
 * ID/random source (where already required), and environment-independent
 * technical abstractions. Runtime packages implement platform-specific
 * behavior. No browser/native globals and no React surface here.
 */

// Ports (implemented by runtime packages)
export type { ClockPort } from './ports';
export type { KeyValueStorage } from './ports';

// Environment-independent permission evaluation
export { permissions, permissionValues, type Permission } from './permissions/permissions';
export { hasPermission, type UserRole, type PermissionResourceContext } from './permissions/ability';

// Environment-independent configuration
export {
  createMockModeChecker,
  isMockModeEnabled,
  type MockFeature,
  type MockModeConfig,
} from './config';

// Environment-independent form error mapping
export { applyServerValidationErrors } from './forms';
