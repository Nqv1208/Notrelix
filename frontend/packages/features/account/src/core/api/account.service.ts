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

export function createAccountService(api: AccountApiClient, endpoints: AccountEndpoints) {
  return {
    async getProfile(): Promise<UserProfile> {
      return api.get<UserProfile>(endpoints.auth.profile);
    },

    async updateProfile(profile: Partial<UserProfile>): Promise<UserProfile> {
      return api.patch<UserProfile>(endpoints.users.updateProfile, profile);
    },

    async getPreferences(): Promise<UserPreferences> {
      if (!endpoints.users.preferences) {
        // PENDING BACKEND: fallback stub
        return {
          userId: 'me',
          theme: 'system',
          colorTheme: 'zinc',
          sidebarCollapsed: false,
          defaultView: 'board',
        };
      }
      return api.get<UserPreferences>(endpoints.users.preferences);
    },

    async updatePreferences(prefs: Partial<UserPreferences>): Promise<UserPreferences> {
      if (!endpoints.users.preferences) {
        // PENDING BACKEND: fallback stub
        return {
          userId: 'me',
          theme: 'system',
          colorTheme: 'zinc',
          sidebarCollapsed: false,
          defaultView: 'board',
          ...prefs,
        };
      }
      return api.patch<UserPreferences>(endpoints.users.preferences, prefs);
    },

    async getSecuritySettings(): Promise<SecuritySettings> {
      if (!endpoints.users.security) {
        // PENDING BACKEND: fallback stub
        return {
          userId: 'me',
          twoFactorEnabled: false,
          lastPasswordChange: new Date().toISOString(),
          activeSessions: 1,
        };
      }
      return api.get<SecuritySettings>(endpoints.users.security);
    },
  };
}
