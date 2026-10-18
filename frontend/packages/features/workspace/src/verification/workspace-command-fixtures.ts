/**
 * Deterministic fixtures and scenarios for the command palette
 * (Final-Command), at the mockup's data. No clocks, no randomness, no network.
 */
import type {
  PaletteBoardEntry,
  PaletteDocEntry,
  PalettePerson,
  PaletteRecentEntry,
  PaletteWorkEntry,
  PaletteWorkspace,
} from "../web/command/command-models";
import type { WorkspaceCommandPaletteSurfaceProps } from "../web/command/command-palette-surface";
import {
  workspaceCommandCopy,
  type WorkspaceCommandCopy,
} from "../web/command/workspace-command.copy";

export function commandCopyFixture(
  locale: "en" | "vi" = "vi",
): WorkspaceCommandCopy {
  return workspaceCommandCopy(locale);
}

const CN = { name: "Công nghệ", markTone: "blue" } as const;
const KT = { name: "Kế toán", markTone: "gold" } as const;

export const commandWorkspaceFixture: PaletteWorkspace = {
  name: "Minh An Group",
  markTone: "blue",
  others: ["Minh An Đà Nẵng", "Tân Lộc Logistics"],
};

/** The Items of Final-Command `tim-kiem`. */
export const commandWorkFixture: readonly PaletteWorkEntry[] = [
  {
    id: "item-kt-88",
    title: "Đối chiếu sao kê ngân hàng tháng 9",
    key: "KT-88",
    boardName: "Đóng sổ tháng 10",
    department: KT,
    mine: true,
    status: { bucket: "todo", label: "Cần làm" },
    due: { tone: "today", label: "Hôm nay 17:00" },
    destination: {
      section: "board",
      boardId: "board-dong-so",
      item: "item-kt-88",
    },
  },
  {
    id: "item-kt-91",
    title: "Đối chiếu công nợ 42 nhà cung cấp",
    key: "KT-91",
    boardName: "Công nợ nhà cung cấp",
    department: KT,
    assignee: "Lý Thu Trang",
    status: { bucket: "doing", label: "Đang làm" },
    due: { tone: "later", label: "20/10" },
    destination: {
      section: "board",
      boardId: "board-cong-no",
      item: "item-kt-91",
    },
  },
  {
    id: "item-erp-142",
    title: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    key: "ERP-142",
    department: CN,
    matchNote: "khớp trong việc con: “Đối chiếu công nợ đầu kỳ”",
    status: { bucket: "doing", label: "Đang làm" },
    due: { tone: "overdue", label: "Quá hạn" },
    destination: {
      section: "board",
      boardId: "board-erp",
      item: "item-erp-142",
    },
  },
];

export const commandDocsFixture: readonly PaletteDocEntry[] = [
  {
    id: "doc-doi-chieu",
    title: "Hướng dẫn đối chiếu ngân hàng",
    department: KT,
    location: "Quy trình",
    editedBy: "Đỗ Khánh Linh",
    editedLabel: "3 ngày trước",
  },
  {
    id: "doc-hoa-don",
    title: "Quy trình nhập hoá đơn đầu vào",
    department: KT,
    matchNote:
      "khớp trong nội dung: “…đối chiếu với đơn đặt hàng và phiếu nhập kho…”",
  },
];

export const commandBoardsFixture: readonly PaletteBoardEntry[] = [
  {
    id: "board-dong-so",
    name: "Đóng sổ tháng 10",
    department: KT,
    matchNote: "có 4 việc khớp “đối chiếu”",
  },
];

export const commandPeopleFixture: readonly PalettePerson[] = [
  {
    id: "user-ha",
    name: "Nguyễn Thu Hà",
    identity: "sage",
    roleLabel: "Quản trị workspace",
  },
  {
    id: "user-trang",
    name: "Lý Thu Trang",
    identity: "coral",
    roleLabel: "Thành viên",
  },
];

export const commandRecentsFixture: readonly PaletteRecentEntry[] = [
  {
    kind: "board",
    id: "board-erp",
    name: "Triển khai ERP Q4",
    department: CN,
    whenLabel: "20 phút trước",
  },
  {
    kind: "work",
    id: "item-erp-138",
    name: "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
    key: "ERP-138",
    statusLabel: "Chờ duyệt",
    whenLabel: "12 phút trước",
    destination: {
      section: "board",
      boardId: "board-erp",
      item: "item-erp-138",
    },
  },
  {
    kind: "doc",
    id: "doc-hoa-don",
    name: "Quy trình nhập hoá đơn đầu vào",
    whenLabel: "hôm qua",
  },
];

type Scenario = Omit<
  WorkspaceCommandPaletteSurfaceProps,
  "onOpenChange" | "onSelect"
>;

function base(copy: WorkspaceCommandCopy): Scenario {
  return {
    copy,
    open: true,
    workspace: commandWorkspaceFixture,
    work: commandWorkFixture,
    docs: commandDocsFixture,
    boards: commandBoardsFixture,
    people: commandPeopleFixture,
    recents: commandRecentsFixture,
    appearance: "light",
    canCreateWorkspace: false,
    canCreateDoc: true,
    canSwitchWorkspace: true,
  };
}

/** `tim-kiem`: "đối chiếu" over items, Docs, Boards and commands. */
export function commandSearchScenario(locale: "en" | "vi" = "vi"): Scenario {
  return { ...base(commandCopyFixture(locale)), defaultQuery: "đối chiếu" };
}

/** `chua-go`: nothing typed. */
export function commandRecentScenario(locale: "en" | "vi" = "vi"): Scenario {
  return base(commandCopyFixture(locale));
}

/** `lenh`: ">chuyển", the five commands the design lists. */
export function commandCommandScenario(locale: "en" | "vi" = "vi"): Scenario {
  return {
    ...base(commandCopyFixture(locale)),
    defaultQuery: ">chuyển",
    commandIds: [
      "set-status",
      "set-assignee",
      "move-board",
      "home",
      "switch-workspace",
    ],
  };
}

/** `khong-thay`: nothing matches. */
export function commandNoneScenario(locale: "en" | "vi" = "vi"): Scenario {
  return {
    ...base(commandCopyFixture(locale)),
    defaultQuery: "hợp đồng bảo trì thang máy",
  };
}

/** `dang-chay`: a status change waiting for the server. */
export function commandRunningScenario(locale: "en" | "vi" = "vi"): Scenario {
  return {
    ...base(commandCopyFixture(locale)),
    running: {
      typed: "Chuyển ERP-142 sang Chờ duyệt",
      title: "Đang chuyển ERP-142 sang Chờ duyệt…",
      detail:
        "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP · Đang làm → Chờ duyệt",
    },
  };
}

/** Only what the member's lists hold: no matches, no Items, no extra text. */
export function commandProducedOnlyScenario(
  locale: "en" | "vi" = "vi",
): Scenario {
  return {
    ...base(commandCopyFixture(locale)),
    work: [],
    docs: [{ id: "doc-a", title: "Kế hoạch quý" }],
    boards: [{ id: "board-a", name: "Kế hoạch tháng 10" }],
    people: [],
    recents: [],
    defaultQuery: "kế",
  };
}

/** Long names, many matches, dark-theme labels. */
export function commandEdgeDataScenario(locale: "en" | "vi" = "vi"): Scenario {
  const longTitle =
    "Kế hoạch ra mắt sản phẩm toàn cầu — giai đoạn hai (bản nháp cuối cùng, đã duyệt bởi ban giám đốc)";
  return {
    ...base(commandCopyFixture(locale)),
    appearance: "dark",
    work: Array.from({ length: 9 }, (_, index) => ({
      id: `item-long-${index}`,
      title: `${longTitle} ${index + 1}`,
      key: `KH-${100 + index}`,
      boardName: "Kế hoạch toàn cầu",
      department: CN,
      mine: true,
      due: { tone: "later" as const, label: `${10 + index}/11` },
      destination: { section: "board" as const, boardId: "board-kh" },
    })),
    defaultQuery: "kế hoạch",
  };
}
