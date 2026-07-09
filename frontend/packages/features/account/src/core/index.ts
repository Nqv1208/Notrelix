/**
 * @notrelix/feature-account — Account core types.
 *
 * Framework-neutral: no React, no DOM.
 */

export type {
  UserProfile,
  UserPreferences,
  SecuritySettings,
} from './types/account';

export { createAccountService, type AccountApiClient, type AccountEndpoints } from './api/account.service';
export * from './query';
