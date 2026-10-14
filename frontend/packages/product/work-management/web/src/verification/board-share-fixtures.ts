/**
 * Deterministic fixtures of the Board share dialog: the data of Final-Share
 * (the "Triển khai ERP Q4" Board in Phòng Công nghệ) for every state it
 * declares, and what the mock world can fill today. No clocks, no randomness,
 * no network.
 */
import { vi } from "../components/board-share/board-share.copy";
import type { BoardShareDialogSurfaceProps } from "../components/board-share/board-share-dialog-surface";
import type {
  BoardShareView,
  ShareImpactPerson,
  ShareLogEntry,
  SharePersonView,
  ShareRowView,
} from "../components/board-share/board-share-models";

export type BoardShareScenario = Omit<
  BoardShareDialogSurfaceProps,
  "onClose" | "onCopyLink" | "onChangeLevel" | "onRemove" | "onChangeGeneral"
>;

const person = (
  id: string,
  name: string,
  initials: string,
  tone: SharePersonView["tone"],
): SharePersonView => ({ id, name, initials, tone });

const HP = person("u-phuc", "Lê Hoàng Phúc", "HP", "ember");
const HA = person("u-ha", "Nguyễn Thu Hà", "HA", "sage");
const KL = person("u-linh", "Đỗ Khánh Linh", "KL", "gold");
const MT = person("u-tuan", "Đặng Minh Tuấn", "MT", "blue");

function row(
  over: Partial<ShareRowView> & Pick<ShareRowView, "id" | "name">,
): ShareRowView {
  return {
    kind: "person",
    person: null,
    isYou: false,
    detail: null,
    level: null,
    fixed: false,
    badge: null,
    expires: null,
    ...over,
  };
}

export const MOCKUP_ROWS: ShareRowView[] = [
  row({
    id: "u-phuc",
    name: HP.name,
    person: HP,
    detail: "Chủ board · Quản trị Phòng Công nghệ",
    level: "manage",
    fixed: true,
  }),
  row({
    id: "t-kt",
    kind: "team",
    name: "Nhóm Kế toán tổng hợp",
    detail: "Nhóm workspace · 6 người · ngoài phòng Công nghệ",
    level: "view",
  }),
  row({
    id: "u-linh",
    name: KL.name,
    person: KL,
    detail: "Phòng Kế toán · ngoài phòng Công nghệ",
    level: "comment",
    expires: "31/12/2026",
  }),
  row({
    id: "u-tuan",
    name: MT.name,
    shortName: "Tuấn",
    person: MT,
    detail: "tuan@doitac-erp.vn · khách, chỉ thấy board này",
    level: "edit",
    badge: "outside",
    expires: "31/01/2027",
  }),
  row({
    id: "u-ha",
    name: HA.name,
    person: HA,
    isYou: true,
    detail: "Quản trị Phòng Công nghệ",
    level: "manage",
    fixed: true,
  }),
];

const VIEW: BoardShareView = {
  boardTitle: "Triển khai ERP Q4",
  space: { name: "Phòng Công nghệ", initials: "CN", markTone: "blue" },
  workspaceName: "Minh An Group",
  general: {
    kind: "space",
    name: "Phòng Công nghệ",
    count: 24,
    mark: { initials: "CN", tone: "blue" },
  },
  rows: MOCKUP_ROWS,
  policy: "off",
  canManage: true,
};

const IMPACT_PEOPLE: ShareImpactPerson[] = [
  { ...person("u-quan", "Trần Minh Quân", "MQ", "blue"), items: 2, overdue: 1 },
  { ...person("u-anh", "Phan Ngọc Anh", "NA", "coral"), items: 1, overdue: 0 },
  { ...person("u-thanh", "Vũ Thanh", "VT", "gold"), items: 1, overdue: 0 },
];

const LOG: ShareLogEntry[] = [
  {
    id: "l1",
    actor: HA,
    text: "**Nguyễn Thu Hà** thêm **Đặng Minh Tuấn** (khách, Có thể sửa, hết quyền 31/01/2027)",
    when: "08/10 · 14:12",
    note: null,
  },
  {
    id: "l2",
    actor: HP,
    text: "**Lê Hoàng Phúc** đổi **Nhóm Kế toán tổng hợp**: Có thể sửa → Có thể xem",
    when: "02/10",
    note: null,
  },
  {
    id: "l3",
    actor: null,
    text: "**Hệ thống:** quyền của **Nguyễn Văn Bình** hết hạn",
    when: "30/09",
    note: "gia hạn phải thêm lại quyền",
  },
  {
    id: "l4",
    actor: null,
    text: "**Quản trị workspace** tắt liên kết công khai",
    when: "15/09",
    note: null,
  },
  {
    id: "l5",
    actor: HP,
    text: "**Lê Hoàng Phúc** tạo board",
    when: "01/09",
    note: null,
  },
];

function base(): BoardShareScenario {
  return { copy: vi, open: true, view: VIEW, log: null };
}

/** Final-Share `chia-se`. */
export function shareDefaultScenario(): BoardShareScenario {
  return { ...base(), log: LOG, initialView: "share" };
}

/** Final-Share `doi-quyen`: the permission menu of a guest. */
export function shareLevelScenario(): BoardShareScenario {
  return { ...shareDefaultScenario(), initialLevelMenu: "u-tuan" };
}

/** Final-Share `rieng-tu`: switching to Private lists who loses access. */
export function sharePrivateScenario(): BoardShareScenario {
  return {
    ...base(),
    initialView: "private",
    impact: {
      lose: 22,
      people: IMPACT_PEOPLE,
      keepText:
        "**Lê Hoàng Phúc, Nguyễn Thu Hà** (đã được thêm riêng), **Nhóm Kế toán tổng hợp**, **Đỗ Khánh Linh** và khách **Đặng Minh Tuấn**.",
    },
  };
}

/** Final-Share `nhat-ky`: the permission history. */
export function shareLogScenario(): BoardShareScenario {
  return { ...shareDefaultScenario(), initialView: "log" };
}

/** What the mock world fills: members and the access read, no levels or history. */
export function shareProducedOnlyScenario(): BoardShareScenario {
  return {
    ...base(),
    view: {
      ...VIEW,
      rows: MOCKUP_ROWS.map((entry) =>
        entry.fixed ? entry : { ...entry, level: null, expires: null },
      ),
    },
    log: null,
  };
}

/** A Board at workspace level: no department line, "through the workspace". */
export function shareWorkspaceScenario(): BoardShareScenario {
  return {
    ...base(),
    view: {
      ...VIEW,
      space: null,
      general: { kind: "workspace" },
      rows: MOCKUP_ROWS.slice(0, 1),
      policy: "unknown",
    },
  };
}

/** A Private Board nobody else was added to. */
export function shareEmptyScenario(): BoardShareScenario {
  return {
    ...base(),
    view: { ...VIEW, general: { kind: "private" }, rows: [], policy: "on" },
  };
}
