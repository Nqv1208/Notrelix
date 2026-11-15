import type { ReactNode } from "react";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { chatCatalog } from "../web/components/chat.catalog";
import { ChatWorkspace } from "../web/components/chat-workspace";

const toastError = vi.hoisted(() => vi.fn());
vi.mock("sonner", () => ({ toast: { error: toastError } }));

const WORKSPACE = mockIds.workspaces.primary;

describe("Mark all as read in the channel column", () => {
  it("shows the toast on a failed call, keeps the unread dots and the button, and clears them on the retry", async () => {
    const store = new MockStore({ ...defaultConfig });
    const mockFetch = createMockFetch(store);
    let failNext = true;
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: (input, init) => {
        const url = typeof input === "string" ? input : (input as Request).url;
        if (url.endsWith("/chat/me/read-all") && failNext) {
          failNext = false;
          return Promise.resolve(
            new Response(JSON.stringify({ status: 500 }), {
              status: 500,
              headers: { "Content-Type": "application/problem+json" },
            }),
          );
        }
        return mockFetch(input, init);
      },
    });
    const viewerId = store.getCurrentUser().id;
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>
        <I18nProvider catalogs={[chatCatalog]} initialLocale="en">
          {children}
        </I18nProvider>
      </QueryClientProvider>
    );
    render(
      <ChatWorkspace
        deps={{ api: client.api, endpoints: client.endpoints }}
        workspaceId={WORKSPACE}
        viewerId={viewerId}
        viewerRole="owner"
        channelId={null}
        conversationId={null}
        onOpenConversation={vi.fn()}
        onOpenChannel={vi.fn()}
        people={new Map()}
      />,
      { wrapper },
    );
    const button = await screen.findByRole("button", {
      name: "Mark all as read",
    });
    const dots = screen.getAllByTestId("chat-unread-dot").length;
    expect(dots).toBeGreaterThan(0);

    fireEvent.click(button);
    await waitFor(() =>
      expect(toastError).toHaveBeenCalledWith(
        "Could not mark everything as read. Try again.",
        expect.anything(),
      ),
    );
    expect(screen.getAllByTestId("chat-unread-dot")).toHaveLength(dots);
    const again = screen.getByRole("button", { name: "Mark all as read" });
    expect((again as HTMLButtonElement).disabled).toBe(false);

    fireEvent.click(again);
    await waitFor(() =>
      expect(screen.queryAllByTestId("chat-unread-dot")).toHaveLength(0),
    );
    expect(toastError).toHaveBeenCalledTimes(1);
  });
});
