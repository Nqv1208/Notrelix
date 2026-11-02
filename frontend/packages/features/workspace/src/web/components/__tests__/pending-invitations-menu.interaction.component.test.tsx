import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { I18nProvider } from "@notrelix/i18n";
import { AccountScopeProvider } from "@notrelix/query";
import { endpoints } from "@notrelix/contracts";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { PendingInvitationsMenu } from "../pending-invitations-menu";

const row = (id: string, workspaceName: string, role = "Member") => ({
  id,
  workspaceName,
  workspaceSlug: workspaceName.toLowerCase(),
  inviterName: "Ha Le",
  role,
  expiresAt: "2026-12-01T00:00:00Z",
});

function mount(post: (url: string) => Promise<unknown>) {
  const get = vi.fn(() => Promise.resolve([row("i1", "Acme")]));
  const api = { get, post: vi.fn(post) } as unknown as WorkspaceApiClient;
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <I18nProvider catalogs={[]} initialLocale="en">
        <AccountScopeProvider accountId="acc-1">
          {children}
        </AccountScopeProvider>
      </I18nProvider>
    </QueryClientProvider>
  );
  render(
    <PendingInvitationsMenu
      api={api}
      endpoints={endpoints as never}
      onAccepted={vi.fn()}
    />,
    { wrapper },
  );
  return { api, get };
}

async function open() {
  fireEvent.click(
    screen.getByRole("button", { name: "Pending workspace invitations" }),
  );
  await screen.findByText("Acme");
}

const accept = () =>
  screen.getByRole("button", { name: /^Accept the invitation to Acme/ });
const decline = () =>
  screen.getByRole("button", { name: /^Decline the invitation to Acme/ });

describe("PendingInvitationsMenu answers", () => {
  it("FUI[workspace.pending-invitations:seat-refused] tells the person why a refused accept was refused and keeps the invitation", async () => {
    const reason = "The member limit of this account's plan has been reached.";
    mount(() =>
      Promise.reject(Object.assign(new Error(reason), { status: 400 })),
    );
    await open();

    fireEvent.click(accept());

    expect((await screen.findByRole("alert")).textContent).toContain(reason);
    expect(screen.getByText("Acme")).toBeTruthy();
  });

  it("FUI[workspace.pending-invitations:gone] says an answered invitation is gone and reads the list again", async () => {
    const gone = Object.assign(new Error("not found"), { status: 404 });
    const { get } = mount(() => Promise.reject(gone));
    await open();
    const reads = get.mock.calls.length;

    fireEvent.click(decline());

    expect((await screen.findByRole("alert")).textContent).toBe(
      "That invitation is no longer available.",
    );
    await waitFor(() => expect(get.mock.calls.length).toBeGreaterThan(reads));
  });

  it("FUI[workspace.pending-invitations:pending] holds both buttons while an answer is on its way", async () => {
    mount(() => new Promise(() => undefined));
    await open();

    fireEvent.click(decline());

    await waitFor(() =>
      expect((accept() as HTMLButtonElement).disabled).toBe(true),
    );
    expect((decline() as HTMLButtonElement).disabled).toBe(true);
  });
});
