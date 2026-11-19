import { act, type ReactNode } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Board } from "@notrelix/work-management-core";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const state = vi.hoisted(() => ({
  locale: "vi" as "vi" | "en",
  inviteGuest: vi.fn(),
  onInviteGuest: null as null | ((email: string) => Promise<boolean>),
  toastError: vi.fn(),
}));

vi.mock("sonner", () => ({
  toast: { error: state.toastError, success: vi.fn() },
}));
vi.mock("@notrelix/i18n", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/i18n")>()),
  useLocale: () => ({ locale: state.locale, setLocale: vi.fn(), locales: [] }),
}));
vi.mock("@notrelix/features-auth", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/features-auth")>()),
  useCurrentUser: () => ({ id: "me" }),
}));
vi.mock("@notrelix/runtime-web", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/runtime-web")>()),
  useAppRuntime: () => ({ api: { api: {}, endpoints: {} } }),
}));
vi.mock("@notrelix/features-workspace/web", async (importOriginal) => ({
  ...(await importOriginal<
    typeof import("@notrelix/features-workspace/web")
  >()),
  createUseWorkspaceSettings: () => () => ({ data: undefined }),
  createUseWorkspaceMembers: () => () => ({ data: [] }),
}));
vi.mock("@notrelix/work-management-state", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/work-management-state")>()),
  useBoardAccess: () => ({ access: { sources: [], myLevel: "manager" } }),
}));
vi.mock("@notrelix/work-management-web", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/work-management-web")>()),
  // The surface is covered by its own interaction tests; here it only hands the host's callback over.
  BoardShareDialogSurface: (props: {
    onInviteGuest?: (email: string) => Promise<boolean>;
  }) => {
    state.onInviteGuest = props.onInviteGuest ?? null;
    return null;
  },
}));
vi.mock("@/providers/workspace-provider", () => ({
  useWorkspaceContext: () => ({ workspace: { name: "Notrelix" } }),
}));
vi.mock("../board-placement-bridge", () => ({
  useBoardPlacements: () => ({ data: [] }),
}));
vi.mock("../use-workspace-spaces", () => ({
  useSpaceHooks: () => ({ useSpaceTree: () => ({ data: [] }) }),
}));
vi.mock("../use-board-sharing", () => ({
  useBoardSharing: () => ({
    data: {
      canManage: true,
      entriesDisclosed: true,
      entries: [],
      version: 1,
      visibility: "Workspace",
      peopleCount: 0,
      guestCount: 0,
      log: [],
      privateImpact: null,
      spaceName: null,
      spacePeople: null,
    },
    loading: false,
    failed: false,
    reload: vi.fn(),
    pendingInvitations: [],
    readPendingInvitations: () => [],
    inviteGuest: state.inviteGuest,
  }),
}));

import { BoardShareDialog } from "../board-share-dialog";

const BOARD = {
  id: "board-1",
  title: "ERP",
  visibility: "workspace",
  isArchived: false,
  members: [],
} as unknown as Board;

let root: Root | null = null;
let host: HTMLDivElement | null = null;

function refusal(errorCode: string) {
  return {
    kind: "forbidden",
    status: 403,
    details: { errorCode, detail: "Forbidden access." },
  };
}

async function inviteRefusedWith(errorCode: string, locale: "vi" | "en") {
  state.locale = locale;
  state.inviteGuest.mockRejectedValue(refusal(errorCode));
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  await act(async () => {
    root!.render(
      <BoardShareDialog
        workspaceId="ws-1"
        boardId="board-1"
        board={BOARD}
        open
        onClose={vi.fn()}
      />,
    );
  });
  expect(state.onInviteGuest).not.toBeNull();
  let sent = true;
  await act(async () => {
    sent = await state.onInviteGuest!("khach@doitac-erp.vn");
  });
  expect(sent).toBe(false);
  const [title, options] = state.toastError.mock.calls[0]!;
  return { title: title as string, description: options.description as string };
}

beforeEach(() => {
  state.toastError.mockReset();
  state.onInviteGuest = null;
});

afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
  host = null;
});

describe("BoardShareDialog guest invitation refused", () => {
  it("UC-WM-A19 tells the inviter guest invites are off, in Vietnamese", async () => {
    const toast = await inviteRefusedWith("workspace.guest_invites_off", "vi");
    expect(toast.description).toBe(
      "Workspace này đang tắt lời mời khách. Hãy nhờ Chủ sở hữu bật lại.",
    );
  });

  it("UC-WM-A19 tells the inviter guest invites are off, in English", async () => {
    const toast = await inviteRefusedWith("workspace.guest_invites_off", "en");
    expect(toast.description).toBe(
      "Guest invites are off in this workspace. Ask the Owner to turn them on.",
    );
  });

  it("UC-WM-A19 shows no such message for a different refusal", async () => {
    const toast = await inviteRefusedWith("auth.forbidden", "en");
    expect(toast.description).not.toContain("Guest invites are off");
    expect(toast.description).toBe("");
  });
});
