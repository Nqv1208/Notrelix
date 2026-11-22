import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { itemPageCopy } from "../../../item-page/item-page.copy";
import {
  WmUiPortsProvider,
  type MentionSuggestionsView,
  type WmUiPorts,
} from "../../../shared/wm-ui-ports";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { CommentsColumn } from "../comments-column";

const copy = itemPageCopy("en").composer;
const BINH = { userId: "u-binh", name: "Bình Trần", avatarUrl: null };

function show(view: MentionSuggestionsView) {
  const calls: (string | null)[] = [];
  const onCreateUpdate = vi.fn();
  const ports: WmUiPorts = {
    useCardImageUrl: () => null,
    useRelationTarget: () => ({
      groups: [],
      isLoading: false,
      error: null,
      refetch: () => undefined,
    }),
    useMentionSuggestions: (workspaceId, itemId, query) => {
      calls.push(`${workspaceId}/${itemId}/${query}`);
      return view;
    },
  };
  const model = {
    mode: "view",
    copy: en,
    readOnly: null,
    now: new Date("2026-11-08T10:00:00.000Z"),
    timeZone: "UTC",
    capabilities: { canEditFields: true, canComment: true, frozen: false },
    // The card's own people and the board's directory hold "Ân": the picker must never offer them.
    card: {
      id: "item-1",
      workspaceId: "ws-1",
      title: "Card",
      members: [{ userId: "u-an", name: "Ân Nguyễn", initials: "ÂN" }],
      watchers: [],
    },
    data: {
      updates: [],
      updatesLoading: false,
      updatesError: false,
      activity: [],
      activityLoading: false,
      activityFetching: false,
      files: [],
      filesLoading: false,
      viewerUserId: "u-me",
      people: {
        people: [
          { userId: "u-an", name: "Ân Nguyễn", onBoard: true, openCount: 0 },
        ],
        canInvite: false,
      },
    },
    callbacks: {
      onCreateUpdate,
      onRefreshUpdates: vi.fn(),
      onDeleteUpdate: vi.fn(),
      onUpdateUpdate: vi.fn(),
      onToggleReaction: vi.fn(),
      onRefreshActivity: vi.fn(),
    },
  } as unknown as CardPopupModel;
  renderPureUi(
    <I18nProvider catalogs={[]} initialLocale="en">
      <WmUiPortsProvider ports={ports}>
        <CardPopupProvider value={model}>
          <CommentsColumn />
        </CardPopupProvider>
      </WmUiPortsProvider>
    </I18nProvider>,
  );
  fireEvent.click(
    screen.getByRole("button", {
      name: en.related.comments.composerPlaceholder,
    }),
  );
  const field = screen.getByRole("textbox", {
    name: en.related.comments.composerLabel,
  }) as HTMLTextAreaElement;
  const type = (text: string) =>
    fireEvent.change(field, {
      target: { value: text, selectionStart: text.length },
    });
  return { calls, onCreateUpdate, field, type };
}

describe("comment @mention list (server read)", () => {
  it("offers only the people the server returned and asks for this item", () => {
    const { calls, type } = show({
      people: [BINH],
      status: "success",
      retry: vi.fn(),
    });
    expect(calls.at(-1)).toBe("ws-1/item-1/null");
    type("Hi @b");
    expect(calls.at(-1)).toBe("ws-1/item-1/b");
    // The typed "b" is highlighted inside the name, so read the row's text rather than its split name.
    expect(
      screen.getAllByRole("option").map((option) => option.textContent),
    ).toEqual(["BTBình Trần"]);
    expect(document.body.textContent).not.toContain("Ân Nguyễn");
  });

  it("says it is loading", () => {
    const { type } = show({ people: [], status: "loading", retry: vi.fn() });
    type("@");
    expect(screen.getByRole("status").textContent).toBe(copy.mentionLoading);
    expect(screen.queryAllByRole("option")).toHaveLength(0);
  });

  it("says there is nobody to mention (a Guest gets the same answer)", () => {
    const { type } = show({ people: [], status: "success", retry: vi.fn() });
    type("@zz");
    expect(screen.getByRole("status").textContent).toBe(copy.mentionEmpty);
  });

  it("says the read failed and retries it", () => {
    const retry = vi.fn();
    const { type } = show({ people: [], status: "error", retry });
    type("@");
    expect(screen.getByRole("alert").textContent).toContain(copy.mentionError);
    fireEvent.click(screen.getByRole("button", { name: copy.mentionRetry }));
    expect(retry).toHaveBeenCalledTimes(1);
  });

  it("records the mention of the picked person when the comment is sent", () => {
    const { type, field, onCreateUpdate } = show({
      people: [BINH],
      status: "success",
      retry: vi.fn(),
    });
    type("Hi @b");
    fireEvent.keyDown(field, { key: "Enter" });
    expect(field.value).toBe("Hi @Bình Trần ");
    fireEvent.keyDown(field, { key: "Enter", metaKey: true });
    expect(onCreateUpdate).toHaveBeenCalledWith(
      expect.objectContaining({
        cardId: "item-1",
        mentionUserIds: ["u-binh"],
      }),
      expect.anything(),
    );
  });

  it("leaves Enter to the field when there is nobody to pick", () => {
    const { type, field } = show({
      people: [],
      status: "success",
      retry: vi.fn(),
    });
    type("@zz");
    const notPrevented = fireEvent.keyDown(field, { key: "Enter" });
    expect(notPrevented).toBe(true);
  });
});
