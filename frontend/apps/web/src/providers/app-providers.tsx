import { type ReactNode, useEffect, useState, createContext, useContext } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'sonner';
import { createAuthProvider } from '@notrelix/features-auth';
import { endpoints } from '@notrelix/contracts';
import { createQueryClient } from '@notrelix/query';
import { AppRuntimeProvider, type AppRuntime } from '@notrelix/runtime-web';
import { GlobalErrorBoundary } from '../components/global-error-boundary';
import { router } from '../router';

const queryClient = createQueryClient();

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
  const FeatureAuthProvider = createAuthProvider({ api: runtime.api.api, endpoints });

  return (
    <GlobalErrorBoundary>
      <AppRuntimeProvider runtime={runtime}>
        <QueryClientProvider client={queryClient}>
          <FeatureAuthProvider
            onAuthFailure={(currentPath) => {
              router.navigate({
                to: '/sign-in',
                search: { redirect: currentPath },
              });
            }}
          >
            <ThemeProvider>
              {children}
              <Toaster />
            </ThemeProvider>
          </FeatureAuthProvider>
        </QueryClientProvider>
      </AppRuntimeProvider>
    </GlobalErrorBoundary>
  );
}
