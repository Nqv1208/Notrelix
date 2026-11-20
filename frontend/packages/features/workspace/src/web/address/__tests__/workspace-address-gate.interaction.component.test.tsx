import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { I18nProvider } from "@notrelix/i18n";
import { endpoints } from "@notrelix/contracts";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { createWorkspaceAddressService } from "../../../core/api/workspace-address.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { WorkspaceAddressGate } from "../workspace-address-gate";

const ACCOUNT = "3f2a9c1e-7b64-4d0a-9e51-0c8f6a2b7d10";

const refusal = (status: number) =>
  Object.assign(new Error("Workspace not found."), { status });

function mount(get: (url: string, options?: unknown) => Promise<unknown>) {
  const api = { get: vi.fn(get) } as unknown as WorkspaceApiClient & {
    get: ReturnType<typeof vi.fn>;
  };
  const onOpen = vi.fn();
  const onExit = vi.fn();
  const queryClient = new QueryClient();
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <I18nProvider catalogs={[]} initialLocale="en">
        {children}
      </I18nProvider>
    </QueryClientProvider>
  );
  const view = render(
    <WorkspaceAddressGate
      service={createWorkspaceAddressService(api, endpoints)}
      accountId={ACCOUNT}
      slug="acme-team"
      onOpen={onOpen}
      exitHref="/home"
      onExit={onExit}
    />,
    { wrapper },
  );
  return { api, onOpen, onExit, view };
}

const REFUSED_TITLE = "You don't have access to this workspace";

describe("WorkspaceAddressGate", () => {
  it("FUI[workspace.address:loading] shows the opening state while the address resolves, and goes nowhere yet", () => {
    const { onOpen } = mount(() => new Promise(() => {}));
    expect(
      screen.getByRole("status", { name: "Opening workspace" }),
    ).toBeTruthy();
    expect(onOpen).not.toHaveBeenCalled();
  });

  it("FUI[workspace.address:resolved] asks the anonymous resolve route once, quietly, then opens the workspace by its id", async () => {
    const { api, onOpen } = mount(() =>
      Promise.resolve({ id: "ws-42", name: "Acme", slug: "acme-team" }),
    );
    await waitFor(() => expect(onOpen).toHaveBeenCalledWith("ws-42"));
    expect(onOpen).toHaveBeenCalledTimes(1);
    expect(api.get).toHaveBeenCalledTimes(1);
    expect(api.get).toHaveBeenCalledWith(
      `/accounts/${ACCOUNT}/resolve?slug=acme-team`,
      { skipGlobalErrorToast: true },
    );
    // Still the loading state while the redirect runs: no flash of a refusal.
    expect(screen.queryByText(REFUSED_TITLE)).toBeNull();
  });

  it("FUI[workspace.address:refused] shows the refusal and never opens anything for an unknown address (400)", async () => {
    const { onOpen } = mount(() => Promise.reject(refusal(400)));
    expect(await screen.findByText(REFUSED_TITLE)).toBeTruthy();
    expect(onOpen).not.toHaveBeenCalled();
  });

  it("FUI[workspace.address:no-enumeration] gives a deleted address and a made-up one the same screen, whatever status the refusal carries", async () => {
    const screens: string[] = [];
    for (const status of [400, 404]) {
      const { view } = mount(() => Promise.reject(refusal(status)));
      await screen.findByText(REFUSED_TITLE);
      screens.push(view.container.innerHTML);
      view.unmount();
    }
    expect(screens[0]).toBe(screens[1]);
  });

  it("FUI[workspace.address:refused-exit] the refusal offers a way out to the account's workspaces", async () => {
    const { onExit } = mount(() => Promise.reject(refusal(400)));
    await screen.findByText(REFUSED_TITLE);
    fireEvent.click(screen.getByRole("link", { name: "Go to my workspaces" }));
    expect(onExit).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.address:error] a failed lookup (429, 500, network) is the error state, not a refusal, and Try again asks again", async () => {
    for (const failure of [refusal(429), refusal(500), new TypeError("net")]) {
      let calls = 0;
      const { api, onOpen, view } = mount(() => {
        calls += 1;
        return calls === 1
          ? Promise.reject(failure)
          : Promise.resolve({ id: "ws-42" });
      });
      expect(
        await screen.findByText("This workspace couldn't load"),
      ).toBeTruthy();
      expect(screen.queryByText(REFUSED_TITLE)).toBeNull();
      fireEvent.click(screen.getByRole("button", { name: "Try again" }));
      await waitFor(() => expect(onOpen).toHaveBeenCalledWith("ws-42"));
      expect(api.get).toHaveBeenCalledTimes(2);
      view.unmount();
    }
  });
});
