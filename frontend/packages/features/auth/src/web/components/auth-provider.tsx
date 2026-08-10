import { createContext, useContext } from "react";
import type { ReactNode } from "react";
import type { User } from "../../core/types/auth";
import { createUseAuthUser } from "../hooks/use-auth-user";
import type { AuthApiClient, AuthEndpoints } from "../../core/api/auth.service";

export interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isReady: boolean;
  sessionGeneration: string | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}

export function useCurrentUser() {
  return useAuth().user;
}

interface AuthProviderProps {
  children: ReactNode;
}

interface CreateAuthProviderDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createAuthProvider({ api, endpoints }: CreateAuthProviderDeps) {
  const useAuthUser = createUseAuthUser({ api, endpoints });

  return function AuthProvider({ children }: AuthProviderProps) {
    const { user, isAuthenticated, isLoading, isReady } = useAuthUser();

    const contextValue: AuthContextType = {
      user: user || null,
      isAuthenticated,
      isLoading,
      isReady,
      sessionGeneration: user?.id ? `user:${user.id}` : null,
    };

    return (
      <AuthContext.Provider value={contextValue}>
        {children}
      </AuthContext.Provider>
    );
  };
}
