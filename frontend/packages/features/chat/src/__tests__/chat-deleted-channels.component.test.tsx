import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
  type MockPersona,
} from "@notrelix/dev-mock-backend";
import { chatCatalog } from "../web/components/chat.catalog";
import { en, vi as viCopy } from "../web/components/chat.copy";
import {
  ChatDeletedChannels,
  deletedAgo,
} from "../web/components/chat-deleted-channels";
import { createChatHooks } from "../web/query/create-chat-hooks";

const WORKSPACE = mockIds.workspaces.primary;
const GENERAL = "c1000000-0000-4000-8000-000000000001";
const people = new Map([["mock-user-owner", "Owner Person"]]);

/** A deleted copy of the seeded #general under another id and name, deleted `daysAgo` days ago. */
function deleteCopy(
  store: MockStore,
  id: string,
  name: string,
  daysAgo: number,
): void {
  const base = store.chat.spaces.get(GENERAL)!;
  store.chat.spaces.set(id, {
    ...base,
    id,
    name,
    deletedAt: store.getClock().offsetDays(-daysAgo),
    deletedBy: "mock-user-owner",
  });
}

type Override = (url: string, init?: RequestInit) => Response | null;

function problem(status: number, errorCode: string): Response {
  return new Response(JSON.stringify({ status, errorCode }), {
    status,
    headers: { "Content-Type": "application/problem+json" },
  });
}

function mount(
  options: {
    persona?: MockPersona;
    locale?: "en" | "vi";
    seed?: (store: MockStore) => void;
    override?: Override;
  } = {},
) {
  const store = new MockStore({
    ...defaultConfig,
    persona: options.persona ?? "owner",
  });
  // The default world already holds a deleted channel (old-notes); these tests set up their own.
  for (const space of [...store.chat.spaces.values()]) {
    if (space.deletedAt !== null) store.chat.spaces.delete(space.id);
  }
  options.seed?.(store);
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: async (input, init) => {
      const url = typeof input === "string" ? input : (input as Request).url;
      const forced = options.override?.(url, init);
      if (forced) return forced;
      return createMockFetch(store)(input, init);
    },
  });
  const hooks = createChatHooks({
    api: client.api,
    endpoints: client.endpoints,
  });
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const onRestored = vi.fn();
  const locale = options.locale ?? "en";
  render(
    <I18nProvider catalogs={[chatCatalog]} initialLocale={locale}>
      <QueryClientProvider client={queryClient}>
        <ChatDeletedChannels
          hooks={hooks}
          workspaceId={WORKSPACE}
          copy={locale === "vi" ? viCopy : en}
          nameOf={(id) => people.get(id) ?? "?"}
          locale={locale}
          timeZone="UTC"
          onRestored={onRestored}
        />
      </QueryClientProvider>
    </I18nProvider>,
  );
  return { store, onRestored };
}

const SPACE_A = "c1000000-0000-4000-8000-0000000000a1";
const SPACE_B = "c1000000-0000-4000-8000-0000000000b2";

describe("ChatDeletedChannels (UC-CHT-A08)", () => {
  it("lists the deletable channels newest delete first, with who deleted and the restore deadline", async () => {
    mount({
      seed: (store) => {
        deleteCopy(store, SPACE_A, "old-campaign", 20);
        deleteCopy(store, SPACE_B, "fresh-campaign", 2);
      },
    });
    const names = await screen.findAllByText(/-campaign$/);
    expect(names.map((node) => node.textContent)).toEqual([
      "fresh-campaign",
      "old-campaign",
    ]);
    expect(screen.getAllByText(/Deleted by Owner Person/)).toHaveLength(2);
    expect(screen.getAllByText(/Restorable until/)).toHaveLength(2);
    expect(screen.getByText("2 channels can be restored")).toBeTruthy();
  });

  it("leaves out a channel deleted more than 30 days ago", async () => {
    mount({
      seed: (store) => {
        deleteCopy(store, SPACE_A, "gone-for-good", 31);
        deleteCopy(store, SPACE_B, "still-here", 29);
      },
    });
    await screen.findByText("still-here");
    expect(screen.queryByText("gone-for-good")).toBeNull();
  });

  it("restores a channel: the row leaves the list and the server copy is live again", async () => {
    const { store, onRestored } = mount({
      seed: (s) => deleteCopy(s, SPACE_A, "old-campaign", 3),
    });
    fireEvent.click(
      await screen.findByRole("button", { name: "Restore #old-campaign" }),
    );
    await waitFor(() => expect(onRestored).toHaveBeenCalledTimes(1));
    expect(store.chat.spaces.get(SPACE_A)?.deletedAt).toBeNull();
    await screen.findByText(en.deleted.emptyTitle);
  });

  it("shows a name-taken answer on its own row and keeps the channel deleted", async () => {
    const { store } = mount({
      seed: (s) => deleteCopy(s, SPACE_A, "general", 3),
    });
    fireEvent.click(
      await screen.findByRole("button", { name: "Restore #general" }),
    );
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toBe(
      "Another channel is already named general. Rename that channel, then restore this one.",
    );
    expect(store.chat.spaces.get(SPACE_A)?.deletedAt).not.toBeNull();
    // The row stays and can be tried again.
    expect(
      screen.getByRole<HTMLButtonElement>("button", {
        name: "Restore #general",
      }).disabled,
    ).toBe(false);
  });

  it("answers a restore past the window on the row", async () => {
    mount({
      seed: (s) => deleteCopy(s, SPACE_A, "old-campaign", 3),
      override: (url, init) =>
        url.endsWith(`/chat/channels/${SPACE_A}/restore`) &&
        init?.method === "POST"
          ? problem(409, "restore-window-elapsed")
          : null,
    });
    fireEvent.click(
      await screen.findByRole("button", { name: "Restore #old-campaign" }),
    );
    expect((await screen.findByRole("alert")).textContent).toBe(
      en.deleted.windowElapsed,
    );
  });

  it("shows the empty state when nothing can be restored", async () => {
    mount();
    await screen.findByText(en.deleted.emptyTitle);
    expect(screen.getByText(en.deleted.emptyBody)).toBeTruthy();
  });

  it("shows the loading state before the list arrives", async () => {
    mount({
      override: (url) =>
        url.endsWith("/chat/channels/deleted")
          ? (new Promise<Response>(() => undefined) as unknown as Response)
          : null,
    });
    expect(
      await screen.findByRole("status", { name: en.deleted.loading }),
    ).toBeTruthy();
  });

  it("shows a retryable error when the list cannot load", async () => {
    let failing = true;
    mount({
      override: (url) =>
        failing && url.endsWith("/chat/channels/deleted")
          ? new Response("{}", { status: 500 })
          : null,
    });
    await screen.findByText(en.deleted.error);
    failing = false;
    fireEvent.click(screen.getByRole("button", { name: en.deleted.retry }));
    await screen.findByText(en.deleted.emptyTitle);
  });

  it("lists nothing for a person who could not have deleted the channel", async () => {
    mount({
      persona: "member",
      seed: (s) => deleteCopy(s, SPACE_A, "old-campaign", 3),
    });
    await screen.findByText(en.deleted.emptyTitle);
    expect(screen.queryByText("old-campaign")).toBeNull();
  });

  it("reads in Vietnamese", async () => {
    mount({
      locale: "vi",
      seed: (s) => deleteCopy(s, SPACE_A, "old-campaign", 3),
    });
    expect(
      await screen.findByRole("button", { name: "Khôi phục #old-campaign" }),
    ).toBeTruthy();
    expect(screen.getByText(/Xoá bởi Owner Person/)).toBeTruthy();
  });
});

describe("deletedAgo", () => {
  const now = Date.parse("2026-11-19T12:00:00Z");
  it("counts whole days from a day on, hours and minutes before", () => {
    expect(deletedAgo("2026-11-16T12:00:00Z", now, "en")).toBe("3 days ago");
    expect(deletedAgo("2026-11-19T07:00:00Z", now, "en")).toBe("5 hours ago");
    expect(deletedAgo("2026-11-19T11:30:00Z", now, "en")).toBe(
      "30 minutes ago",
    );
  });
});
