export interface PlatformUser {
  id: string;
  email: string;
  name: string;
  avatarUrl: string | null;
}

export interface PlatformAuthContext {
  user: PlatformUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isReady: boolean;
}
