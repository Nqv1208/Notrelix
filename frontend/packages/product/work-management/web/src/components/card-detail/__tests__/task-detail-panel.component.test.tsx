import { beforeAll, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { renderPureUi, screen, waitFor, within } from "@notrelix/testing";
import type {
  BoardAccess,
  CardDetail,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import {
  WorkManagementTestScope,
  type WorkManagementServices,
} from "@notrelix/work-management-state";
import {
  erpBoardScenario,
  erpCommenterAccess,
  erpItemId,
  erpOwnerAccess,
} from "@notrelix/work-management-testing";
import { itemSheetCatalog } from "../item-sheet.catalog";
import { TaskDetailPanel } from "../task-detail-panel";

beforeAll(() => {
  // A desktop window: the panel renders the non-modal sheet.
  window.matchMedia ??= ((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    addListener: () => undefined,
    removeListener: () => undefined,
    dispatchEvent: () => false,
  })) as typeof window.matchMedia;
  Element.prototype.scrollIntoView = () => undefined;
});

function detailOf(board: FullBoardResponse, id: string): CardDetail {
  const card = board.groups
    .flatMap((group) => group.cards)
    .find((candidate) => candidate.id === id)!;
  return {
    ...card,
    boardTitle: board.board.title,
    watchers: [],
    isWatched: false,
    updates: [],
    files: [],
    activity: [],
  };
}

function renderPanel(access: BoardAccess | Promise<BoardAccess>) {
  const scenario = erpBoardScenario();
  const services = {
    boards: { getFullBoard: vi.fn(async () => scenario) },
    access: { getBoardAccess: vi.fn(() => Promise.resolve(access)) },
    cards: {
      getCard: vi.fn(async (id: string) => detailOf(scenario, id)),
      getCardFiles: vi.fn(async () => []),
      getCardActivity: vi.fn(async () => []),
    },
    comments: { getCardUpdates: vi.fn(async () => []) },
  } as unknown as WorkManagementServices;
  return renderPureUi(
    <WorkManagementTestScope services={services}>
      <I18nProvider catalogs={[itemSheetCatalog]} initialLocale="vi">
        <TaskDetailPanel
          board={scenario.board}
          cardId={erpItemId("ERP-142")}
          open
          onClose={vi.fn()}
        />
      </I18nProvider>
    </WorkManagementTestScope>,
  );
}

const viewer = (): BoardAccess => ({ ...erpOwnerAccess(), myLevel: "viewer" });

describe("Table view item sheet capabilities", () => {
  it("is read-only for a viewer: no title edit, no item actions, a disabled composer", async () => {
    renderPanel(viewer());
    const dialog = await screen.findByRole("dialog", {
      name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    });
    await waitFor(() =>
      expect(
        within(dialog)
          .getByRole("button", { name: /^Trạng thái: / })
          .getAttribute("aria-disabled"),
      ).toBe("true"),
    );
    expect(
      within(dialog).queryByRole("textbox", { name: "Sửa tên công việc" }),
    ).toBeNull();
    expect(within(dialog).queryByRole("button", { name: "Thêm" })).toBeNull();
    const composer = within(dialog).getByRole("textbox", {
      name: /bình luận/i,
    }) as HTMLTextAreaElement;
    expect(composer.disabled).toBe(true);
  });

  it("keeps only the composer for a commenter", async () => {
    renderPanel(erpCommenterAccess());
    const dialog = await screen.findByRole("dialog", {
      name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    });
    await waitFor(() =>
      expect(
        within(dialog).getByRole("textbox", { name: /bình luận/i }),
      ).toBeTruthy(),
    );
    expect(
      within(dialog).queryByRole("textbox", { name: "Sửa tên công việc" }),
    ).toBeNull();
  });

  it("never offers edits while the access summary is still loading", async () => {
    renderPanel(new Promise<BoardAccess>(() => undefined));
    const dialog = await screen.findByRole("dialog", {
      name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    });
    expect(
      within(dialog).queryByRole("textbox", { name: "Sửa tên công việc" }),
    ).toBeNull();
    expect(within(dialog).queryByRole("button", { name: "Thêm" })).toBeNull();
  });

  it("lets an editor edit", async () => {
    renderPanel({ ...erpOwnerAccess(), myLevel: "editor" });
    const dialog = await screen.findByRole("dialog", {
      name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    });
    await waitFor(() =>
      expect(
        within(dialog).getByRole("textbox", { name: "Sửa tên công việc" }),
      ).toBeTruthy(),
    );
  });
});
