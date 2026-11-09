import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import type { CardUpdate } from "@notrelix/work-management-core";
import { en } from "../../card-popup.copy";
import {
  CardPopupProvider,
  type CardPopupModel,
} from "../../card-popup-context";
import { CommentsColumn } from "../comments-column";

const THREAD = {
  id: "c-1",
  cardId: "item-1",
  author: {
    id: "cm-u-1",
    userId: "u-1",
    name: "Ada",
    initials: "A",
    color: "",
  },
  body: "Cần xem lại",
  mentionUserIds: [],
  attachmentIds: [],
  createdAt: "2026-11-01T09:00:00Z",
  version: 1,
  reactions: [],
} as unknown as CardUpdate;

function show(options: {
  viewerUserId: string;
  canEditFields: boolean;
  resolved?: boolean;
}) {
  const onSetUpdateResolved = vi.fn();
  const update = { ...THREAD, resolved: options.resolved ?? false };
  const model = {
    mode: "view",
    copy: en,
    now: new Date("2026-11-02T09:00:00Z"),
    timeZone: "UTC",
    readOnly: options.canEditFields ? null : { reason: "viewer" },
    capabilities: { canEditFields: options.canEditFields, canComment: true },
    card: { id: "item-1", workspaceId: "ws-1", members: [] },
    board: { members: [] },
    data: {
      viewerUserId: options.viewerUserId,
      updates: [update],
      updatesLoading: false,
      activity: [],
      activityLoading: false,
      people: { people: [] },
    },
    callbacks: {
      onSetUpdateResolved,
      onUpdateUpdate: vi.fn(),
      onDeleteUpdate: vi.fn(),
      onToggleReaction: vi.fn(),
      onCreateUpdate: vi.fn(),
    },
    track: vi.fn(),
  } as unknown as CardPopupModel;
  renderPureUi(
    <I18nProvider catalogs={[]} initialLocale="en">
      <CardPopupProvider value={model}>
        <CommentsColumn />
      </CardPopupProvider>
    </I18nProvider>,
  );
  return { onSetUpdateResolved };
}

describe("comment thread resolve and reopen (UC-COL-A07, A08)", () => {
  it("lets the thread starter resolve their own thread", () => {
    const { onSetUpdateResolved } = show({
      viewerUserId: "u-1",
      canEditFields: false,
    });

    fireEvent.click(screen.getByRole("button", { name: "Resolve" }));

    expect(onSetUpdateResolved).toHaveBeenCalledWith("c-1", true);
  });

  it("lets an editor resolve a thread they did not start", () => {
    const { onSetUpdateResolved } = show({
      viewerUserId: "u-2",
      canEditFields: true,
    });

    fireEvent.click(screen.getByRole("button", { name: "Resolve" }));

    expect(onSetUpdateResolved).toHaveBeenCalledWith("c-1", true);
  });

  it("gives a viewer who did not start the thread no resolve control", () => {
    show({ viewerUserId: "u-2", canEditFields: false });

    expect(screen.queryByRole("button", { name: "Resolve" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Reopen" })).toBeNull();
  });

  it("offers Reopen on a resolved thread and reopens it", () => {
    const { onSetUpdateResolved } = show({
      viewerUserId: "u-1",
      canEditFields: true,
      resolved: true,
    });

    expect(screen.getByText(/Resolved$/)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Reopen" }));

    expect(onSetUpdateResolved).toHaveBeenCalledWith("c-1", false);
  });
});
