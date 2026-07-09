import type { UserProfile, UserPreferences, SecuritySettings } from '../types/account';

export interface AccountApiClient {
  get<T>(url: string): Promise<T>;
  post<T>(url: string, body: unknown): Promise<T>;
  patch<T>(url: string, body: unknown): Promise<T>;
}

export interface AccountEndpoints {
  auth: {
    profile: string;
  };
  users: {
    updateProfile: string;
    // PENDING BACKEND
    preferences?: string;
    security?: string;
  };
}

export function createAccountService(
  api: AccountApiClient,
  endpoints: AccountEndpoints,
  options?: {
    mockMode?: boolean;
  },
) {
  const mockMode = options?.mockMode === true;

  return {
    async getProfile(): Promise<UserProfile> {
      return api.get<UserProfile>(endpoints.auth.profile);
    },

    async updateProfile(profile: Partial<UserProfile>): Promise<UserProfile> {
      return api.patch<UserProfile>(endpoints.users.updateProfile, profile);
    },

    async getPreferences(): Promise<UserPreferences> {
      if (!endpoints.users.preferences) {
        if (mockMode) {
          return {
            userId: 'me',
            theme: 'system',
            colorTheme: 'zinc',
            sidebarCollapsed: false,
            defaultView: 'board',
          };
        }
        throw new Error('Backend contract missing for users.preferences');
      }
      return api.get<UserPreferences>(endpoints.users.preferences);
    },

    async updatePreferences(prefs: Partial<UserPreferences>): Promise<UserPreferences> {
      if (!endpoints.users.preferences) {
        if (mockMode) {
          return {
            userId: 'me',
            theme: 'system',
            colorTheme: 'zinc',
            sidebarCollapsed: false,
            defaultView: 'board',
            ...prefs,
          };
        }
        throw new Error('Backend contract missing for users.preferences');
      }
      return api.patch<UserPreferences>(endpoints.users.preferences, prefs);
    },

    async getSecuritySettings(): Promise<SecuritySettings> {
      if (!endpoints.users.security) {
        if (mockMode) {
          return {
            userId: 'me',
            twoFactorEnabled: false,
            lastPasswordChange: new Date().toISOString(),
            activeSessions: 1,
          };
        }
        throw new Error('Backend contract missing for users.security');
      }
      return api.get<SecuritySettings>(endpoints.users.security);
    },
  };
}
