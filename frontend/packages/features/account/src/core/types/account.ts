export interface UserProfile {
  id: string;
  email: string;
  name: string;
  avatarUrl: string | null;
  timezone: string;
  locale: string;
  createdAt: string;
}

export interface UserPreferences {
  userId: string;
  theme: "light" | "dark" | "system";
  colorTheme: string;
  sidebarCollapsed: boolean;
  defaultView: string;
}

export interface SecuritySettings {
  userId: string;
  twoFactorEnabled: boolean;
  lastPasswordChange: string;
  activeSessions: number;
}
