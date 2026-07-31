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

type SessionEventListener = () => void;

class SessionEventBus {
  private listeners = new Set<SessionEventListener>();

  subscribe(listener: SessionEventListener): () => void {
    this.listeners.add(listener);
    return () => {
      this.listeners.delete(listener);
    };
  }

  emitExpired(): void {
    for (const listener of this.listeners) {
      try {
        listener();
      } catch (err) {
        console.error('Error in SessionEventBus listener:', err);
      }
    }
  }
}

export const sessionEventBus = new SessionEventBus();

export function useAuthFailureListener(onFailure: () => void) {
  useEffect(() => {
    return sessionEventBus.subscribe(onFailure);
  }, [onFailure]);
}

export function emitAuthFailure() {
  sessionEventBus.emitExpired();
}
