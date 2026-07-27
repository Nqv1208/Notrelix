import { type ReactNode, useEffect, useState, useMemo, createContext, useContext } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'sonner';
import { createAuthProvider } from '@notrelix/features-auth';
import { endpoints } from '@notrelix/contracts';
import { createQueryClient } from '@notrelix/query';
import { AppRuntimeProvider, type AppRuntime } from '@notrelix/runtime-web';
import { GlobalErrorBoundary } from '../components/global-error-boundary';
import { SessionLifecycle } from './session-lifecycle';
import { RealtimeLifecycle } from './realtime-lifecycle';
import { router } from '../router';

type Theme = 'light' | 'dark' | 'system';

interface ThemeContextType {
  theme: Theme;
  setTheme: (theme: Theme) => void;
}

const ThemeContext = createContext<ThemeContextType>({
  theme: 'system',
  setTheme: () => {},
});

export function useTheme() {
  return useContext(ThemeContext);
}

function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>('system');

  useEffect(() => {
    const stored = localStorage.getItem('theme') as Theme | null;
    if (stored) setThemeState(stored);
  }, []);

  useEffect(() => {
    const root = document.documentElement;
    root.classList.remove('light', 'dark');

    if (theme === 'system') {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      root.classList.add(prefersDark ? 'dark' : 'light');
    } else {
      root.classList.add(theme);
    }

    localStorage.setItem('theme', theme);
  }, [theme]);

  const setTheme = (newTheme: Theme) => {
    setThemeState(newTheme);
  };

  return (
    <ThemeContext.Provider value={{ theme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function AppProviders({
  runtime,
  children,
}: {
  runtime: AppRuntime;
  children: ReactNode;
}) {
  const [queryClient] = useState(() => createQueryClient());

  const FeatureAuthProvider = useMemo(
    () => createAuthProvider({ api: runtime.api.api, endpoints }),
    [runtime.api]
  );

  return (
    <GlobalErrorBoundary>
      <AppRuntimeProvider runtime={runtime}>
        <QueryClientProvider client={queryClient}>
          <SessionLifecycle>
            <FeatureAuthProvider
              onAuthFailure={(currentPath) => {
                router.navigate({
                  to: '/sign-in',
                  search: { redirect: currentPath },
                });
              }}
            >
              <RealtimeLifecycle>
                <ThemeProvider>
                  {children}
                  <Toaster />
                </ThemeProvider>
              </RealtimeLifecycle>
            </FeatureAuthProvider>
          </SessionLifecycle>
        </QueryClientProvider>
      </AppRuntimeProvider>
    </GlobalErrorBoundary>
  );
}
