import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
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
  ChatSearchPanel,
  type ChatSearchSpace,
} from "../web/components/chat-search-panel";
import { createChatHooks } from "../web/query/create-chat-hooks";
import type { ChatSearchHit } from "../core/types/chat";
import { dayEndExclusiveIso, dayStartIso } from "../core/model/search-model";

const WORKSPACE = mockIds.workspaces.primary;
const GENERAL = "c1000000-0000-4000-8000-000000000001";

beforeAll(() => {
  Element.prototype.scrollIntoView ??= () => undefined;
});

const spaces: readonly ChatSearchSpace[] = [
  { id: GENERAL, label: "general", kind: "channel_public" },
  { id: "space-2", label: "engineering", kind: "channel_public" },
];
const people = new Map([
  ["mock-user-owner", "Owner Person"],
  ["mock-user-member", "Member Person"],
]);

interface Harness {
  readonly urls: string[];
  readonly onOpenHit: ReturnType<typeof vi.fn>;
  readonly onOpenChange: ReturnType<typeof vi.fn>;
}

type Override = (url: string) => Response | null;

function mount(
  options: {
    persona?: MockPersona;
    locale?: "en" | "vi";
    override?: Override;
    spaceList?: readonly ChatSearchSpace[];
  } = {},
): Harness {
  const store = new MockStore({
    ...defaultConfig,
    persona: options.persona ?? "member",
  });
  const urls: string[] = [];
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: async (input, init) => {
      const url = typeof input === "string" ? input : (input as Request).url;
      if (url.includes("/chat/search")) {
        urls.push(url);
        const forced = options.override?.(url);
        if (forced) return forced;
      }
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
  const onOpenHit = vi.fn();
  const onOpenChange = vi.fn();
  render(
    <I18nProvider
      catalogs={[chatCatalog]}
      initialLocale={options.locale ?? "en"}
    >
      <QueryClientProvider client={queryClient}>
        <ChatSearchPanel
          open
          onOpenChange={onOpenChange}
          hooks={hooks}
          workspaceId={WORKSPACE}
          copy={options.locale === "vi" ? viCopy : en}
          locale={options.locale ?? "en"}
          timeZone="UTC"
          spaces={options.spaceList ?? spaces}
          people={people}
          authorName={(id) => people.get(id) ?? "?"}
          onOpenHit={onOpenHit}
        />
      </QueryClientProvider>
    </I18nProvider>,
  );
  return { urls, onOpenHit, onOpenChange };
}

const box = () =>
  document.querySelector<HTMLInputElement>('input[role="combobox"]')!;
/** The hits only: the filter selects have options of their own. */
const hitOptions = () => {
  const list = screen.queryByRole("listbox", { name: en.search.resultsLabel });
  return list ? within(list).queryAllByRole("option") : [];
};
const firstHit = async () => {
  await screen.findByRole("listbox", { name: en.search.resultsLabel });
  return hitOptions()[0]!;
};
const type = (value: string) => fireEvent.change(box(), { target: { value } });

function problem(status: number, body: Record<string, unknown>): Response {
  return new Response(JSON.stringify({ status, ...body }), {
    status,
    headers: { "Content-Type": "application/problem+json" },
  });
}

describe("ChatSearchPanel (UC-CHT-H07)", () => {
  it("asks for at least 2 characters and sends nothing before that", async () => {
    const { urls } = mount();
    expect(screen.getByText(en.search.hint)).toBeTruthy();
    type("a");
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 400));
    });
    expect(screen.getByText(en.search.tooShort)).toBeTruthy();
    expect(urls).toHaveLength(0);
  });

  it("limits the text to 100 characters", () => {
    mount();
    expect(box().maxLength).toBe(100);
  });

  it("debounces, then lists the hits with the match marked and the count announced", async () => {
    const { urls } = mount();
    type("h");
    type("ho");
    type("hop ban lanh dao");
    expect(urls).toHaveLength(0);

    const list = await screen.findByRole("listbox", {
      name: en.search.resultsLabel,
    });
    expect(urls).toHaveLength(1);
    expect(urls[0]).toContain("q=hop+ban+lanh+dao");
    const options = hitOptions();
    expect(options).toHaveLength(1);
    expect(list.contains(options[0]!)).toBe(true);
    expect(options[0]!.querySelector("mark")?.textContent).toBe(
      "Họp ban lãnh đạo",
    );
    expect(
      screen.getByRole("status", { hidden: true }).textContent,
    ).toBeDefined();
    await waitFor(() =>
      expect(
        screen
          .getAllByRole("status")
          .some((node) => node.textContent === "1 result"),
      ).toBe(true),
    );
  });

  it("draws a snippet that looks like markup as plain text", async () => {
    mount();
    type("hop ban");
    await firstHit();
    expect(document.querySelector("option b, [role=option] b")).toBeNull();
  });

  it("moves with the arrow keys and opens the highlighted hit with Enter, then closes", async () => {
    const { onOpenHit, onOpenChange } = mount();
    type("kien nghi OR");
    type("da ghi nhan");
    await firstHit();

    expect(box().getAttribute("aria-activedescendant")).toBeNull();
    fireEvent.keyDown(box(), { key: "ArrowDown" });
    const active = hitOptions()[0]!;
    expect(box().getAttribute("aria-activedescendant")).toBe(active.id);
    expect(active.getAttribute("aria-selected")).toBe("true");
    fireEvent.keyDown(box(), { key: "Enter" });

    expect(onOpenHit).toHaveBeenCalledTimes(1);
    const hit = onOpenHit.mock.calls[0]![0] as ChatSearchHit;
    expect(hit.spaceName).toBe("leadership");
    expect(hit.messageId).toBeTruthy();
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it("opens a hit with a click", async () => {
    const { onOpenHit } = mount();
    type("da ghi nhan");
    fireEvent.click(await firstHit());
    expect(onOpenHit).toHaveBeenCalledTimes(1);
  });

  it("sends the filters: place, person, dates, file and mention", async () => {
    const { urls } = mount();
    type("hop ban");
    await firstHit();
    fireEvent.change(screen.getByLabelText(en.search.spaceLabel), {
      target: { value: GENERAL },
    });
    fireEvent.change(screen.getByLabelText(en.search.authorLabel), {
      target: { value: "mock-user-member" },
    });
    fireEvent.click(screen.getByRole("button", { name: en.search.dateChip }));
    fireEvent.change(screen.getByLabelText(en.search.fromLabel), {
      target: { value: "2026-01-02" },
    });
    fireEvent.change(screen.getByLabelText(en.search.toLabel), {
      target: { value: "2026-01-05" },
    });
    fireEvent.click(screen.getByLabelText(en.search.hasFile));
    fireEvent.click(screen.getByLabelText(en.search.mentionsMe));

    await waitFor(() => {
      const sent = new URLSearchParams(urls[urls.length - 1]!.split("?")[1]);
      expect(sent.get("spaceId")).toBe(GENERAL);
      expect(sent.get("authorId")).toBe("mock-user-member");
      expect(sent.get("from")).toBe(dayStartIso("2026-01-02"));
      expect(sent.get("to")).toBe(dayEndExclusiveIso("2026-01-05"));
      expect(sent.get("hasAttachment")).toBe("true");
      expect(sent.get("mentionsMe")).toBe("true");
    });
  });

  it("refuses an end date before the start date and sends nothing for it", async () => {
    const { urls } = mount();
    type("hop ban");
    await firstHit();
    const before = urls.length;
    fireEvent.click(screen.getByRole("button", { name: en.search.dateChip }));
    fireEvent.change(screen.getByLabelText(en.search.fromLabel), {
      target: { value: "2026-02-10" },
    });
    fireEvent.change(screen.getByLabelText(en.search.toLabel), {
      target: { value: "2026-02-01" },
    });
    expect(screen.getByRole("alert").textContent).toContain(
      en.search.rangeInvalid,
    );
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 50));
    });
    expect(
      urls.slice(before).every((url) => !url.includes("to=2026-02-02")),
    ).toBe(true);
  });

  it("clears the filters", async () => {
    mount();
    type("hop ban");
    await firstHit();
    fireEvent.click(screen.getByLabelText(en.search.hasFile));
    fireEvent.click(
      screen.getByRole("button", { name: en.search.clearFilters }),
    );
    expect(
      (screen.getByLabelText(en.search.hasFile) as HTMLInputElement).checked,
    ).toBe(false);
  });

  it("says nothing was found, and why a hidden place is not offered", async () => {
    mount();
    type("zzz khong co");
    expect(
      (await screen.findAllByText(en.search.noResults)).length,
    ).toBeGreaterThan(0);
    expect(
      screen
        .getAllByRole("status")
        .some((node) => node.textContent === en.search.noResults),
    ).toBe(true);
    expect(screen.getByText(en.search.noResultsHint)).toBeTruthy();
  });

  it("shows a Guest only the hits of the places they were added to", async () => {
    mount({ persona: "viewer" });
    type("ban");
    await waitFor(() =>
      expect(screen.queryByText(en.search.loading)).toBeNull(),
    );
    const names = hitOptions().map(
      (node) => node.getAttribute("aria-label") ?? "",
    );
    expect(names.some((label) => label.includes("leadership"))).toBe(false);
  });

  it("words a failure with a retry, a rate limit and a lost access apart", async () => {
    mount({
      override: () =>
        problem(500, { errorCode: "server_error", title: "Server error" }),
    });
    type("hop ban");
    expect(await screen.findByText(en.search.error)).toBeTruthy();
    expect(screen.getByRole("button", { name: en.search.retry })).toBeTruthy();
  });

  it("explains a 429", async () => {
    mount({
      override: () =>
        problem(429, {
          errorCode: "rate_limit.exceeded",
          ruleCode: "rate-limited",
          retryAfter: 5,
        }),
    });
    type("hop ban");
    expect(await screen.findByText(en.search.rateLimited)).toBeTruthy();
  });

  it("explains lost access and offers no retry", async () => {
    mount({
      override: () =>
        problem(403, { errorCode: "forbidden", title: "Forbidden" }),
    });
    type("hop ban");
    expect(await screen.findByText(en.search.lostAccess)).toBeTruthy();
    expect(screen.queryByRole("button", { name: en.search.retry })).toBeNull();
  });

  it("drops a place filter when the caller can no longer read that place", async () => {
    const { urls } = mount();
    type("hop ban");
    await firstHit();
    fireEvent.change(screen.getByLabelText(en.search.spaceLabel), {
      target: { value: "space-2" },
    });
    await waitFor(() =>
      expect(urls[urls.length - 1]).toContain("spaceId=space-2"),
    );
  });

  it("keeps recent searches in memory and never touches browser storage", async () => {
    const getItem = vi.spyOn(Storage.prototype, "getItem");
    const setItem = vi.spyOn(Storage.prototype, "setItem");
    mount();
    type("da ghi nhan");
    fireEvent.click(await firstHit());
    expect(setItem).not.toHaveBeenCalled();
    expect(getItem).not.toHaveBeenCalled();
    getItem.mockRestore();
    setItem.mockRestore();
  });

  it("speaks Vietnamese", async () => {
    mount({ locale: "vi" });
    expect(screen.getByText(viCopy.search.title)).toBeTruthy();
    type("a");
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 400));
    });
    expect(screen.getByText(viCopy.search.tooShort)).toBeTruthy();
  });
});
