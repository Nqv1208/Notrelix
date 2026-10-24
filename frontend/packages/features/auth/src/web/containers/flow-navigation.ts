/** What the sign-in containers need from the host's router. */
export interface AuthFlowNavigation {
  navigate: (options: {
    to: string;
    replace?: boolean;
    search?: Record<string, string>;
  }) => void;
  /**
   * Sends the browser to the backend's provider sign-in (a path under the API
   * base). The host owns how: a full page navigation, or the mock world's own
   * round trip.
   */
  startProviderSignIn: (path: string) => void;
}
