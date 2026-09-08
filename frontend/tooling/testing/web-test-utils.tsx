import { render, type RenderOptions } from "@testing-library/react";
import { type ReactElement } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { installPureUiNetworkGuard } from "./src/pure-ui-network-guard";

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
    },
  });
}

export function renderWithProviders(
  ui: ReactElement,
  options?: Omit<RenderOptions, "wrapper">,
) {
  const queryClient = createTestQueryClient();

  function Wrapper({ children }: { children: React.ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  }

  return render(ui, { wrapper: Wrapper, ...options });
}

export function renderPureUi(
  ui: ReactElement,
  options?: Omit<RenderOptions, "wrapper">,
) {
  const guard = installPureUiNetworkGuard();
  const portalHost = document.createElement("div");
  portalHost.setAttribute("id", "notrelix-pure-ui-portal-root");
  document.body.appendChild(portalHost);

  let result: ReturnType<typeof render>;
  try {
    result = render(ui, options);
  } catch (error) {
    portalHost.remove();
    guard.restore();
    throw error;
  }

  const originalUnmount = result.unmount;
  const cleanupPureUi = () => {
    originalUnmount();
    portalHost.remove();
    guard.restore();
  };

  return {
    ...result,
    pureUiPortalHost: portalHost,
    unmount: cleanupPureUi,
  };
}

export {
  render,
  screen,
  fireEvent,
  waitFor,
  act,
} from "@testing-library/react";
