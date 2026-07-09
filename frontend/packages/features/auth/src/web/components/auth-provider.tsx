'use client';

import { createContext, useContext, ReactNode } from 'react';
import { useAuthFailureListener, type PlatformUser } from '@notrelix/platform';
import { createUseAuthUser } from '../hooks/use-auth-user';
import type { AuthApiClient, AuthEndpoints } from '../../core/api/auth.service';

export interface AuthContextType {
  user: PlatformUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isReady: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

export function useCurrentUser() {
  return useAuth().user;
}

interface AuthProviderProps {
  children: ReactNode;
  onAuthFailure: (currentPath: string) => void;
}

interface CreateAuthProviderDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createAuthProvider({ api, endpoints }: CreateAuthProviderDeps) {
  const useAuthUser = createUseAuthUser({ api, endpoints });

  return function AuthProvider({ children, onAuthFailure }: AuthProviderProps) {
    const { user, isAuthenticated, isLoading, isReady } = useAuthUser();

    // Listen to the global auth:failure event and call the onAuthFailure callback
    useAuthFailureListener(() => {
      const currentPath = typeof window !== 'undefined' ? window.location.pathname + window.location.search : '';
      onAuthFailure(currentPath);
    });

    const contextValue: AuthContextType = {
      user: user || null,
      isAuthenticated,
      isLoading,
      isReady,
    };

    return (
      <AuthContext.Provider value={contextValue}>
        {children}
      </AuthContext.Provider>
    );
  };
}
