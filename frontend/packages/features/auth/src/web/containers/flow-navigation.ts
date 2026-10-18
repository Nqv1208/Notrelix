/** What the sign-in containers need from the host's router. */
export interface AuthFlowNavigation {
  navigate: (options: {
    to: string;
    replace?: boolean;
    search?: Record<string, string>;
  }) => void;
}
