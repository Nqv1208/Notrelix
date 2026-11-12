import { useState, type ReactNode } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
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
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { ChatComposer } from "../web/components/chat-composer";
import { ReactionPicker } from "../web/components/chat-reactions";
import { resetRecentEmoji } from "../web/components/chat-emoji-recent";
import { createChatHooks } from "../web/query/create-chat-hooks";
import { en, vi as viCopy } from "../web/components/chat.copy";

afterEach(() => resetRecentEmoji());

function Harness({
  initial = "",
  copy = en,
  onSend = () => {},
  mentions,
}: {
  initial?: string;
  copy?: typeof en;
  onSend?: (text: string) => void;
  mentions?: Parameters<typeof ChatComposer>[0]["mentions"];
}) {
  const [value, setValue] = useState(initial);
  return (
    <ChatComposer
      channelName="general"
      value={value}
      disabledReason={null}
      copy={copy}
      onChange={setValue}
      onSend={onSend}
      mentions={mentions}
    />
  );
}

const box = () =>
  screen.getByRole(
    document.querySelector("textarea[role=combobox]") ? "combobox" : "textbox",
    { name: "Message #general" },
  ) as HTMLTextAreaElement;

function openPicker() {
  fireEvent.click(screen.getByRole("button", { name: "Add emoji" }));
  return screen.getByTestId("chat-emoji-picker");
}

describe("emoji picker in the composer", () => {
  it("opens with a labelled search box that has the focus, and inserts at the caret keeping the caret", async () => {
    render(<Harness initial="hello world" />);
    const textarea = box();
    textarea.focus();
    textarea.setSelectionRange(5, 5);
    const picker = openPicker();
    const search = within(picker).getByRole("searchbox", {
      name: "Search emoji",
    });
    await waitFor(() => expect(document.activeElement).toBe(search));
    fireEvent.click(within(picker).getByRole("button", { name: "grinning" }));
    await waitFor(() => expect(box().value).toBe("hello😀 world"));
    expect(box().selectionStart).toBe(7);
    // The popover closed and the focus went back to the composer.
    await waitFor(() => expect(document.activeElement).toBe(box()));
    expect(screen.queryByTestId("chat-emoji-picker")).toBeNull();
  });

  it("finds emoji by English and Vietnamese words and shows the empty state", () => {
    render(<Harness />);
    const picker = openPicker();
    const search = within(picker).getByRole("searchbox");
    fireEvent.change(search, { target: { value: "rocket" } });
    expect(within(picker).getByRole("button", { name: "rocket" })).toBeTruthy();
    fireEvent.change(search, { target: { value: "cảm ơn" } });
    expect(within(picker).getByRole("button", { name: "pray" })).toBeTruthy();
    fireEvent.change(search, { target: { value: "zzzzqx" } });
    expect(
      within(picker).getByText("No emoji found for this search."),
    ).toBeTruthy();
  });

  it("switches category", () => {
    render(<Harness />);
    const picker = openPicker();
    expect(within(picker).queryByRole("button", { name: "pizza" })).toBeNull();
    fireEvent.click(
      within(picker).getByRole("button", { name: "Food and drink" }),
    );
    expect(within(picker).getByRole("button", { name: "pizza" })).toBeTruthy();
    expect(
      within(picker)
        .getByRole("button", { name: "Food and drink" })
        .getAttribute("aria-pressed"),
    ).toBe("true");
  });

  it("keeps a recent row of what was used, newest first, in memory only", async () => {
    render(<Harness />);
    let picker = openPicker();
    expect(within(picker).queryByTestId("chat-emoji-recent")).toBeNull();
    fireEvent.click(within(picker).getByRole("button", { name: "smile" }));
    await waitFor(() =>
      expect(screen.queryByTestId("chat-emoji-picker")).toBeNull(),
    );
    picker = openPicker();
    const recent = within(picker).getByTestId("chat-emoji-recent");
    expect(
      within(recent)
        .getAllByRole("button")
        .map((b) => b.textContent),
    ).toEqual(["😄"]);
    expect(within(picker).getByText("Recent")).toBeTruthy();
    expect(window.localStorage.length).toBe(0);
  });

  it("moves through the grid with the arrow keys and picks with Enter on the focused cell", async () => {
    render(<Harness />);
    const picker = openPicker();
    const search = within(picker).getByRole("searchbox");
    fireEvent.keyDown(search, { key: "ArrowDown" });
    const grid = within(picker).getByTestId("chat-emoji-grid");
    const cells = within(grid).getAllByRole("button");
    await waitFor(() => expect(document.activeElement).toBe(cells[0]));
    fireEvent.keyDown(cells[0]!, { key: "ArrowRight" });
    await waitFor(() => expect(document.activeElement).toBe(cells[1]));
    fireEvent.keyDown(cells[1]!, { key: "ArrowDown" });
    await waitFor(() => expect(document.activeElement).toBe(cells[9]));
    expect(cells[9]!.tabIndex).toBe(0);
    expect(cells[0]!.tabIndex).toBe(-1);
    fireEvent.keyDown(cells[9]!, { key: "ArrowUp" });
    await waitFor(() => expect(document.activeElement).toBe(cells[1]));
    fireEvent.keyDown(cells[1]!, { key: "ArrowUp" });
    await waitFor(() => expect(document.activeElement).toBe(search));
  });

  it("closes on Escape and returns the focus to the composer", async () => {
    render(<Harness initial="abc" />);
    openPicker();
    fireEvent.keyDown(screen.getByRole("searchbox"), { key: "Escape" });
    await waitFor(() =>
      expect(screen.queryByTestId("chat-emoji-picker")).toBeNull(),
    );
    await waitFor(() => expect(document.activeElement).toBe(box()));
    expect(box().value).toBe("abc");
  });

  it("is named in Vietnamese", () => {
    render(<Harness copy={viCopy} />);
    fireEvent.click(
      screen.getByRole("button", { name: "Thêm biểu tượng cảm xúc" }),
    );
    const picker = screen.getByTestId("chat-emoji-picker");
    expect(
      within(picker).getByRole("searchbox", {
        name: "Tìm biểu tượng cảm xúc",
      }),
    ).toBeTruthy();
    expect(
      within(picker).getByRole("button", { name: "Mặt cười" }),
    ).toBeTruthy();
  });
});

describe(":shortcode autocomplete", () => {
  function type(text: string) {
    const textarea = box();
    fireEvent.change(textarea, { target: { value: text } });
    textarea.setSelectionRange(text.length, text.length);
    fireEvent.select(textarea);
  }

  it("suggests while typing and inserts on Enter or Tab without sending", async () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    type("hi :smi");
    const list = screen.getByRole("listbox", { name: "Emoji suggestions" });
    expect(within(list).getAllByRole("option")[0]!.textContent).toContain(
      ":smile:",
    );
    fireEvent.keyDown(box(), { key: "Enter" });
    await waitFor(() => expect(box().value).toBe("hi 😄"));
    expect(onSend).not.toHaveBeenCalled();
    expect(screen.queryByRole("listbox")).toBeNull();

    type("x :roc");
    fireEvent.keyDown(box(), { key: "Tab" });
    await waitFor(() => expect(box().value).toBe("x 🚀"));
  });

  it("moves with the arrows, picks by click, and closes on Escape until the text changes", async () => {
    render(<Harness />);
    type(":smi");
    fireEvent.keyDown(box(), { key: "ArrowDown" });
    const options = screen.getAllByRole("option");
    expect(options[1]!.getAttribute("aria-selected")).toBe("true");
    fireEvent.keyDown(box(), { key: "Escape" });
    expect(screen.queryByRole("listbox")).toBeNull();
    type(":smil");
    fireEvent.click(screen.getAllByRole("option")[0]!);
    await waitFor(() => expect(box().value).toBe("😄"));
  });

  it("does not open for one character or inside a word", () => {
    render(<Harness />);
    type(":s");
    expect(screen.queryByRole("listbox")).toBeNull();
    type("a:smile");
    expect(screen.queryByRole("listbox")).toBeNull();
  });

  it("leaves the @ mention list alone: the two triggers are exclusive", () => {
    const mentions = {
      selfId: "me",
      mentionables: {
        canBroadcast: false,
        memberCount: 2,
        people: [{ userId: "u-1", inSpace: true }],
      },
      status: "ready" as const,
      people: new Map([["u-1", "Smith"]]),
      channels: [],
      draftKey: "d",
    } as unknown as Parameters<typeof ChatComposer>[0]["mentions"];
    render(<Harness mentions={mentions} />);
    type("@smi");
    const list = screen.getByRole("listbox", { name: en.mentions.listLabel });
    expect(within(list).getByText("Smith")).toBeTruthy();
    expect(
      screen.queryByRole("listbox", { name: "Emoji suggestions" }),
    ).toBeNull();
    type("@Smith :smi");
    expect(
      screen.getByRole("listbox", { name: "Emoji suggestions" }),
    ).toBeTruthy();
  });
});

describe("reaction picker with the full emoji picker", () => {
  it("keeps the quick row on top and opens the full picker from the more button", () => {
    const onPick = vi.fn();
    render(<ReactionPicker copy={en} reacted={["🚀"]} onPick={onPick} />);
    fireEvent.click(screen.getByRole("button", { name: "React" }));
    const quick = screen.getByRole("group", { name: "Reactions" });
    expect(within(quick).getAllByRole("button")).toHaveLength(6);
    fireEvent.click(screen.getByRole("button", { name: "More emoji" }));
    const picker = screen.getByTestId("chat-emoji-picker");
    fireEvent.change(within(picker).getByRole("searchbox"), {
      target: { value: "rocket" },
    });
    const rocket = within(picker).getByRole("button", { name: "rocket" });
    expect(rocket.getAttribute("aria-pressed")).toBe("true");
  });

  it("reports a pick from the full picker and closes", () => {
    const onPick = vi.fn();
    render(<ReactionPicker copy={en} reacted={[]} onPick={onPick} />);
    fireEvent.click(screen.getByRole("button", { name: "React" }));
    fireEvent.click(screen.getByRole("button", { name: "More emoji" }));
    const picker = screen.getByTestId("chat-emoji-picker");
    fireEvent.change(within(picker).getByRole("searchbox"), {
      target: { value: "tada" },
    });
    fireEvent.click(within(picker).getByRole("button", { name: "tada" }));
    expect(onPick).toHaveBeenCalledWith("🎉");
    expect(screen.queryByTestId("chat-emoji-picker")).toBeNull();
  });

  it("can go back to the quick row", () => {
    render(<ReactionPicker copy={en} reacted={[]} onPick={() => {}} />);
    fireEvent.click(screen.getByRole("button", { name: "React" }));
    fireEvent.click(screen.getByRole("button", { name: "More emoji" }));
    fireEvent.click(
      screen.getByRole("button", { name: "Back to quick reactions" }),
    );
    expect(screen.getByRole("group", { name: "Reactions" })).toBeTruthy();
  });
});

describe("a reaction chosen in the full picker, against the mock", () => {
  const WORKSPACE = mockIds.workspaces.primary;

  function setup() {
    const store = new MockStore({ ...defaultConfig });
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: createMockFetch(store),
    });
    const hooks = createChatHooks({
      api: client.api,
      endpoints: client.endpoints,
    });
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    return { hooks, queryClient };
  }

  function Reacting({
    hooks,
    messageId,
    done,
  }: {
    hooks: ReturnType<typeof setup>["hooks"];
    messageId: string;
    done: (status: "success" | "error") => void;
  }) {
    const react = hooks.useReaction(WORKSPACE);
    return (
      <ReactionPicker
        copy={en}
        reacted={[]}
        onPick={(kind) =>
          react.mutate(
            { messageId, kind, add: true },
            { onSuccess: () => done("success"), onError: () => done("error") },
          )
        }
      />
    );
  }

  async function run(emojiName: string) {
    const { hooks, queryClient } = setup();
    const channels = await hooks.service.listChannels(WORKSPACE);
    const channel = channels.find((c) => c.name === "general")!;
    const page = await hooks.service.listMessages(
      WORKSPACE,
      channel.id,
      {},
      50,
    );
    const target = page.items.find((m) => m.kind === "user" && !m.deleted)!;
    const done = vi.fn();
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
    render(<Reacting hooks={hooks} messageId={target.id} done={done} />, {
      wrapper,
    });
    fireEvent.click(screen.getByRole("button", { name: "React" }));
    fireEvent.click(screen.getByRole("button", { name: "More emoji" }));
    const picker = screen.getByTestId("chat-emoji-picker");
    fireEvent.change(within(picker).getByRole("searchbox"), {
      target: { value: emojiName },
    });
    await act(async () => {
      fireEvent.click(within(picker).getByRole("button", { name: emojiName }));
    });
    await waitFor(() => expect(done).toHaveBeenCalled());
    return done.mock.calls[0]![0] as "success" | "error";
  }

  it("is stored when the emoji is one of the six the server accepts", async () => {
    expect(await run("tada")).toBe("success");
  });

  it("is refused by the server for any other emoji (the palette is the server's rule)", async () => {
    expect(await run("fire")).toBe("error");
  });
});
