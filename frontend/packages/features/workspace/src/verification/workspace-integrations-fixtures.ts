import { integrationsCopy } from "../web/integrations/integrations.copy";
import type {
  IntegrationCatalogDto,
  IntegrationDetailDto,
  IntegrationRowDto,
  IntegrationsLinks,
  IntegrationsListDto,
} from "../web/integrations/integrations-models";

/**
 * Mockup world of the Integrations pages (Final-Integrations), for stories and
 * tests only: Minh An Group on Monday 12/10/2026 10:40 (GMT+7). Deterministic.
 */
export const INTEGRATIONS_FIXTURE_COPY = integrationsCopy("vi");
const link = { href: "#", onNavigate: () => undefined };
export const INTEGRATIONS_FIXTURE_LINKS: IntegrationsLinks = {
  list: link,
  detail: () => link,
  billing: link,
  automation: () => link,
};

const ok = { tone: "ok", label: "Đang hoạt động" } as const;
const stable = { tone: "ok", label: "Ổn định" } as const;
const ROWS: readonly IntegrationRowDto[] = [
  {
    key: "teams",
    mono: "MT",
    name: "Microsoft Teams",
    sub: "Kết nối bởi Lê Hoàng Phúc · 14/08/2026",
    scope: { kind: "workspace", name: "" },
    conn: { tone: "warn", label: "Cần uỷ quyền lại" },
    sync: { tone: "off", label: "Tạm dừng" },
    last: "Thứ Bảy 10/10 18:05",
    lastSub: "1 tin chưa gửi, đang giữ",
  },
  {
    key: "onedrive",
    mono: "OD",
    name: "Microsoft OneDrive & SharePoint",
    sub: "Kết nối bởi Nguyễn Thu Hà · 20/08/2026",
    scope: { kind: "workspace", name: "" },
    conn: ok,
    sync: stable,
    last: "10:31",
    lastSub: "214 liên kết tệp",
  },
  {
    key: "email",
    mono: "EM",
    name: "Email vào board",
    sub: "hotro-noibo@in.notrelix.vn · kết nối bởi Vũ Thanh · 25/08/2026",
    scope: { kind: "board", name: "Hỗ trợ nội bộ" },
    conn: ok,
    sync: stable,
    last: "09:52",
    lastSub: "38 email thành việc trong 30 ngày",
  },
];

export const CATALOG_FIXTURE: readonly IntegrationCatalogDto[] = [
  {
    key: "google-drive",
    mono: "GD",
    name: "Google Drive",
    provider: "Google",
    desc: "Đính kèm tệp Google Drive vào việc và trang",
    enterprise: false,
    will: [
      "Đọc tệp người dùng chọn khi đính kèm",
      "Tạo liên kết xem cho tệp đã đính kèm",
    ],
    wont: ["Đọc cả Google Drive", "Sửa, xoá hay chia sẻ tệp"],
    gate: null,
  },
  {
    key: "power-bi",
    mono: "PB",
    name: "Power BI",
    provider: "Power",
    desc: "Đưa số liệu board sang Power BI",
    enterprise: true,
    will: [],
    wont: [],
    gate: {
      can: [
        "Đưa số liệu của board được chọn sang Power BI để dựng báo cáo riêng",
      ],
      alsoHas: ["Tự động cấp và thu hồi tài khoản qua SCIM"],
    },
  },
];

export function listFixture(canManage = true): IntegrationsListDto {
  return {
    serverTime: "2026-10-12T03:40:00.000Z",
    updatedAt: "10:40",
    canManage,
    workspaceName: "Minh An Group",
    admins: ["Nguyễn Thu Hà", "Trịnh Quốc Hưng"],
    rows: ROWS,
    catalog: CATALOG_FIXTURE,
    plan: {
      name: "business",
      account: "Tập đoàn Minh An",
      workspaces: ["Minh An Group", "Minh An Đà Nẵng"],
    },
  };
}

const empty = {
  overview: [],
  activity: [],
  callout: null,
  pending: null,
  who: null,
  usedIn: [],
  perms: [],
  nots: [],
  permNote: null,
  notNote: null,
  deliveries: null,
  config: null,
  dedupe: [],
  disconnect: null,
  updatedAt: "10:40",
} as const;

export const TEAMS_DETAIL: IntegrationDetailDto = {
  ...empty,
  key: "teams",
  mono: "MT",
  name: "Microsoft Teams",
  kind: "chat",
  subtitle:
    "Đăng tin từ quy tắc và thông báo board vào kênh Teams · phạm vi Workspace",
  actions: {
    reauthorize: false,
    disconnect: true,
    test: false,
    pause: false,
    paused: false,
  },
  health: {
    conn: {
      tone: "warn",
      label: "Cần uỷ quyền lại",
      note: "hết hạn 10/10 02:00",
    },
    sync: {
      tone: "off",
      label: "Tạm dừng",
      note: "chờ uỷ quyền · 1 thao tác đang chờ",
    },
  },
  callout: {
    title: "Uỷ quyền đã hết hạn 10/10 02:00. Tin nhắn mới tạm giữ, không mất.",
    body: "Kết nối vẫn còn: kênh đã chọn, cấu hình và lịch sử giữ nguyên.",
    hint: null,
    cta: "Uỷ quyền lại với Microsoft",
  },
  pending: {
    title: "Đăng tin vào kênh “ERP – Điều hành”",
    sub: "Bước 2 của quy tắc",
    note: "Sau khi uỷ quyền lại, mở lần chạy.",
    boardId: "board-erp",
  },
  perms: [{ t: "Đăng tin vào kênh được chọn" }],
  disconnect: {
    intro: "Kết nối này dùng chung cho cả workspace.",
    stops: ["Đăng tin vào kênh Teams"],
    keeps: ["Quy tắc và cấu hình"],
    note: "Tin đã đăng ở Teams do Microsoft quản lý.",
    foot: "Không việc, trang hay tệp nào bị xoá (INT-017)",
  },
};

export const WEBHOOK_DETAIL: IntegrationDetailDto = {
  ...empty,
  key: "webhook",
  mono: "WH",
  name: "Trạng thái việc sang ERP cũ",
  kind: "webhook",
  subtitle: "Webhook gửi đi · phạm vi board Triển khai ERP Q4",
  actions: {
    reauthorize: false,
    disconnect: false,
    test: true,
    pause: true,
    paused: false,
  },
  health: {
    conn: ok,
    sync: {
      tone: "warn",
      label: "Đang thử lại",
      note: "1 sự kiện đang chờ · lần thử tới 10:45",
    },
  },
  callout: {
    title: "Hệ thống nhận đang trả về 503 từ 10:28.",
    body: "Notrelix thử lại với giãn cách tăng dần.",
    hint: null,
    cta: null,
  },
  deliveries: {
    retryAt: "10:45",
    waiting: [
      { t: "10:28", ref: "ERP-138", result: "503 · lần 3/8", next: "10:45" },
    ],
    sent: [{ t: "09/10 16:20", ref: "ERP-136", result: "200 · đã nhận" }],
  },
  config: {
    url: "https://erp-cu.minhan.vn/hooks/notrelix",
    event: "Trạng thái thay đổi",
    scope: "Board Triển khai ERP Q4",
    secret: "Đã lưu · tạo 02/09/2026",
  },
  dedupe: [
    "Mỗi sự kiện được gửi kèm mã định danh để hệ thống nhận bỏ qua bản trùng.",
  ],
};
