import { useEffect } from 'react';

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

export function useAuthFailureListener(onFailure: () => void) {
  useEffect(() => {
    const handleFailure = () => {
      onFailure();
    };

    if (typeof window !== 'undefined') {
      window.addEventListener('auth:failure', handleFailure);
    }

    return () => {
      if (typeof window !== 'undefined') {
        window.removeEventListener('auth:failure', handleFailure);
      }
    };
  }, [onFailure]);
}

export function emitAuthFailure() {
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent('auth:failure'));
  }
}
