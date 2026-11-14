import type { ReactNode } from "react";
import { render, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { createChatService } from "../core/api/chat.service";
import { createChatHooks } from "../web/query/create-chat-hooks";
import { MessageLinkPreviews } from "../web/components/chat-link-previews";
import { en } from "../web/components/chat.copy";

const WORKSPACE = mockIds.workspaces.primary;
const INVOICE = "mock-doc-invoice-process";

function setup(persona: "owner" | "member" = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const deps = { api: client.api, endpoints: client.endpoints };
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return {
    store,
    hooks: createChatHooks(deps),
    service: createChatService(deps.api, deps.endpoints),
    wrapper,
  };
}

describe("MessageLinkPreviews (UC-CHT-G05)", () => {
  it("shows nothing for a message without a product link", () => {
    const s = setup();
    const { container } = render(
      <MessageLinkPreviews
        content="no links here"
        workspaceId={WORKSPACE}
        hooks={s.hooks}
        copy={en}
      />,
      { wrapper: s.wrapper },
    );
    expect(container.querySelector("[data-link-preview]")).toBeNull();
  });

  it("shows a loading chip, then the title of a Page the reader can view", async () => {
    const s = setup("owner");
    const title = s.store.getPage(INVOICE)!.title;
    render(
      <MessageLinkPreviews
        content={`see /workspaces/${WORKSPACE}/docs/${INVOICE}`}
        workspaceId={WORKSPACE}
        hooks={s.hooks}
        copy={en}
      />,
      { wrapper: s.wrapper },
    );

    expect(screen.getByRole("status")).toBeTruthy();
    const chip = await screen.findByText(title);
    expect(chip.closest("a")?.getAttribute("data-link-preview")).toBe(
      "resolved",
    );
    expect(chip.closest("a")?.getAttribute("href")).toBe(
      `/workspaces/${WORKSPACE}/docs/${INVOICE}`,
    );
  });

  it("shows a generic chip with no title when the resource cannot be viewed", async () => {
    const s = setup("owner");
    const title = s.store.getPage(INVOICE)!.title;
    s.store.getPage(INVOICE)!.trashedAt = s.store.getClock().isoNow();
    const { container } = render(
      <MessageLinkPreviews
        content={`/workspaces/${WORKSPACE}/docs/${INVOICE}`}
        workspaceId={WORKSPACE}
        hooks={s.hooks}
        copy={en}
      />,
      { wrapper: s.wrapper },
    );

    await waitFor(() =>
      expect(
        container.querySelector('[data-link-preview="no-access"]'),
      ).not.toBeNull(),
    );
    expect(screen.queryByText(title)).toBeNull();
  });
});
