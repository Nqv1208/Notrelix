import { createContext, useContext, type ReactNode } from 'react';

export interface NavigateOptions {
  to: string;
  replace?: boolean;
  search?: Record<string, string | undefined>;
}

export interface NavigationAdapter {
  navigate: (options: NavigateOptions) => void;
  getSearchParams: () => URLSearchParams;
  getPathname: () => string;
}

export interface LinkComponentProps {
  to: string;
  children: ReactNode;
  className?: string;
  replace?: boolean;
}

export type LinkComponent = React.ComponentType<LinkComponentProps>;

export interface NavigationConfig {
  adapter: NavigationAdapter;
  Link: LinkComponent;
}

const NavigationContext = createContext<NavigationConfig | null>(null);

export function NavigationProvider({
  config,
  children,
}: {
  config: NavigationConfig;
  children: ReactNode;
}) {
  return (
    <NavigationContext.Provider value={config}>
      {children}
    </NavigationContext.Provider>
  );
}

export function useNavigation(): NavigationConfig {
  const config = useContext(NavigationContext);
  if (!config) {
    throw new Error('useNavigation must be used within a NavigationProvider');
  }
  return config;
}

export function useNavigate() {
  const { adapter } = useNavigation();
  return adapter.navigate;
}

export function useSearchParams() {
  const { adapter } = useNavigation();
  return adapter.getSearchParams();
}

export function usePathname() {
  const { adapter } = useNavigation();
  return adapter.getPathname();
}

export function useLink() {
  const { Link } = useNavigation();
  return Link;
}
