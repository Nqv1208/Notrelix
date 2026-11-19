import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor, within } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { ChatMessageRow } from "../web/components/chat-message-row";
import { chatCatalog } from "../web/components/chat.catalog";
import { ChatWorkspace } from "../web/components/chat-workspace";
import { en } from "../web/components/chat.copy";
import { createChatHooks } from "../web/query/create-chat-hooks";
import { TASK_TITLE_MAX, taskDraftFromMessage } from "../core/model/task-draft";
import type { ChatMessage } from "../core/types/chat";

const WORKSPACE = mockIds.workspaces.primary;

function message(content: string | null): ChatMessage {
  return {
    id: "m-1",
    spaceId: "space-1",
    sequence: 1,
    kind: "user",
    authorId: "user-other",
    content,
    clientMessageId: null,
    replyToId: null,
    edited: false,
    deleted: false,
    createdAt: "2026-11-10T09:00:00.000Z",
    editedAt: null,
    deletedAt: null,
    version: 1,
  };
}

describe("taskDraftFromMessage (UC-CHT-I08)", () => {
  const base = { link: null, linkLabel: "Source: #general", boardId: null };

  it("takes the first non-empty line as the title and keeps the whole text as the snapshot", () => {
    const draft = taskDraftFromMessage({
      ...base,
      text: "\n  Prepare the Q3 report  \nwith the new totals",
    });

    expect(draft.title).toBe("Prepare the Q3 report");
    expect(draft.descriptionMd).toBe(
      "Prepare the Q3 report  \nwith the new totals",
    );
  });

  it("cuts a long first line at the title limit with an ellipsis", () => {
    const draft = taskDraftFromMessage({
      ...base,
      text: "x".repeat(TASK_TITLE_MAX + 40),
    });

    expect(Array.from(draft.title)).toHaveLength(TASK_TITLE_MAX);
    expect(draft.title.endsWith("…")).toBe(true);
  });

  it("ends the description with a link back to the message, and the board the channel is linked to", () => {
    const draft = taskDraftFromMessage({
      text: "Fix the login bug",
      link: "https://app.example/workspaces/w/chat/c?m=7",
      linkLabel: "Source: #general",
      boardId: "board-1",
    });

    expect(draft.descriptionMd).toBe(
      "Fix the login bug\n\n[Source: #general](https://app.example/workspaces/w/chat/c?m=7)",
    );
    expect(draft.boardId).toBe("board-1");
  });
});

describe("Create task in the message menu (UC-CHT-I08)", () => {
  function row(
    content: string | null,
    onCreateTask?: (message: ChatMessage) => void,
  ) {
    return (
      <ChatMessageRow
        message={message(content)}
        author={{ name: "Minh Quân" }}
        continued={false}
        isOwn={false}
        locale="en"
        timeZone="UTC"
        copy={en}
        onCreateTask={onCreateTask}
      />
    );
  }

  it("is offered only with a handler and a message that has text, and hands the message over", async () => {
    const without = render(row("Fix the login bug"));
    expect(screen.queryByRole("button", { name: "More actions" })).toBeNull();
    without.unmount();

    const onCreateTask = vi.fn();
    const empty = render(row(null, onCreateTask));
    expect(screen.queryByRole("button", { name: "More actions" })).toBeNull();
    empty.unmount();

    render(row("Fix the login bug", onCreateTask));
    fireEvent.keyDown(screen.getByRole("button", { name: "More actions" }), {
      key: "Enter",
    });
    fireEvent.click(
      await screen.findByRole("menuitem", { name: "Create task" }),
    );
    expect(onCreateTask).toHaveBeenCalledWith(
      expect.objectContaining({ id: "m-1", content: "Fix the login bug" }),
    );
  });
});

describe("ChatWorkspace create task (UC-CHT-I08)", () => {
  beforeAll(() => {
    Element.prototype.scrollIntoView ??= () => undefined;
  });

  async function open(linkBoard: boolean) {
    const store = new MockStore({ ...defaultConfig, persona: "owner" });
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: (input, init) => createMockFetch(store)(input, init),
    });
    const deps = { api: client.api, endpoints: client.endpoints };
    const hooks = createChatHooks(deps);
    const general = (await hooks.service.listChannels(WORKSPACE)).find(
      (channel) => channel.name === "general",
    )!;
    if (linkBoard) {
      await hooks.service.linkChannel(WORKSPACE, general.id, {
        kind: "work-management.board",
        resourceId: mockIds.boards.roadmap,
        expectedVersion: general.version,
      });
    }
    const onCreateTask = vi.fn();
    render(
      <QueryClientProvider
        client={
          new QueryClient({ defaultOptions: { queries: { retry: false } } })
        }
      >
        <I18nProvider catalogs={[chatCatalog]} initialLocale="en">
          <ChatWorkspace
            deps={deps}
            workspaceId={WORKSPACE}
            viewerId={store.getCurrentUser().id}
            viewerRole="owner"
            channelId={general.id}
            onOpenChannel={vi.fn()}
            people={new Map<string, string>()}
            buildMessageLink={({ spaceId, sequence }) =>
              `https://app.example/chat/${spaceId}?m=${sequence}`
            }
            onCreateTask={onCreateTask}
          />
        </I18nProvider>
      </QueryClientProvider>,
    );
    await waitFor(() =>
      expect(document.querySelector("[data-message-id]")).not.toBeNull(),
    );
    return { onCreateTask, general };
  }

  async function createTaskFromFirstMessage() {
    const target = document.querySelector("[data-message-id]") as HTMLElement;
    fireEvent.keyDown(
      within(target).getByRole("button", { name: "More actions" }),
      { key: "Enter" },
    );
    fireEvent.click(
      await screen.findByRole("menuitem", { name: "Create task" }),
    );
  }

  it("hands the host a draft with the linked Board and a link back to the message", async () => {
    const { onCreateTask, general } = await open(true);
    await waitFor(() => expect(onCreateTask).not.toHaveBeenCalled());
    await createTaskFromFirstMessage();

    const draft = onCreateTask.mock.calls[0]![0] as {
      title: string;
      descriptionMd: string;
      boardId: string | null;
    };
    expect(draft.boardId).toBe(mockIds.boards.roadmap);
    expect(draft.title.length).toBeGreaterThan(0);
    expect(draft.descriptionMd).toContain(
      `](https://app.example/chat/${general.id}?m=`,
    );
    expect(draft.descriptionMd).toContain("Source: # general");
  });

  it("hands over no Board when the channel has no link, so the dialog asks for one", async () => {
    const { onCreateTask } = await open(false);
    await createTaskFromFirstMessage();

    expect(onCreateTask).toHaveBeenCalledTimes(1);
    expect(
      (onCreateTask.mock.calls[0]![0] as { boardId: unknown }).boardId,
    ).toBeNull();
  });
});
