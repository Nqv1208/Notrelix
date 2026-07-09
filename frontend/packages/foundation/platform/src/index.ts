/**
 * @notrelix/platform — Platform services and utilities
 * 
 * Provides auth, permissions, workspace context, navigation, and configuration.
 * May use React for providers and hooks.
 */

// Permissions
export { permissions, permissionValues, type Permission } from './permissions'
export { hasPermission, type UserRole, type PermissionResourceContext } from './permissions'
export { useCan, PermissionProvider } from './permissions'
export { PermissionContext } from './permissions'
export { PermissionGuard } from './permissions'

// Configuration
export {
  createMockModeChecker,
  isMockModeEnabled,
  type MockFeature,
  type MockModeConfig,
} from './config'

// Routes
export { routes } from './routes'

// Navigation
export {
  NavigationProvider,
  useNavigation,
  useNavigate,
  useSearchParams,
  usePathname,
  useLink,
  type NavigateOptions,
  type NavigationAdapter,
  type LinkComponentProps,
  type LinkComponent,
  type NavigationConfig,
} from './navigation'

// Auth
export * from './auth'

// Workspace (placeholder)
export type { } from './workspace'
