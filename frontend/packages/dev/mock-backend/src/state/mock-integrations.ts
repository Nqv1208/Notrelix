/**
 * What the Integrations pages of the 2026-10 Integrations mockup need (gap
 * `CTR-GAP-INTEGRATIONS`, see docs/superpowers/be-blockers-2026-10.md, "BE
 * requirement from FE: Integrations"): the connections of Minh An Group, the
 * catalog, three connection details (OneDrive, Teams, a webhook) and the
 * actions on them. Free-text lines (subtitles, activity, notes) are composed
 * here in Vietnamese the way the mockup shows them; time labels are Hanoi time.
 */
export type Tone = "ok" | "warn" | "off";
export interface Status {
  readonly tone: Tone;
  readonly label: string;
  readonly note?: string;
}
export interface Line {
  readonly t: string;
  readonly sub?: string;
}
export interface Detail {
  readonly key: string;
  readonly mono: string;
  readonly name: string;
  readonly subtitle: string;
  readonly kind: "storage" | "chat" | "webhook";
  readonly actions: {
    reauthorize: boolean;
    disconnect: boolean;
    test: boolean;
    pause: boolean;
    paused: boolean;
  };
  readonly health: { conn: Status; sync: Status };
  readonly overview: readonly { k: string; v: string; sub?: string }[];
  readonly activity: readonly { t: string; text: string; ref: string | null }[];
  readonly callout: {
    title: string;
    body: string;
    hint: string | null;
    cta: string | null;
  } | null;
  readonly pending: {
    title: string;
    sub: string;
    note: string;
    boardId: string;
  } | null;
  readonly who: string | null;
  readonly usedIn: readonly { title: string; sub: string }[];
  readonly perms: readonly Line[];
  readonly permsTitleKey: "granted" | "grantedWebhook";
  readonly nots: readonly Line[];
  readonly permNote: string | null;
  readonly notNote: string | null;
  readonly deliveries: {
    retryAt: string;
    waiting: readonly {
      t: string;
      ref: string;
      result: string;
      next: string;
    }[];
    sent: readonly { t: string; ref: string; result: string }[];
  } | null;
  readonly config: {
    url: string;
    event: string;
    scope: string;
    secret: string;
  } | null;
  readonly dedupe: readonly string[];
  readonly disconnect: {
    intro: string;
    stops: readonly string[];
    keeps: readonly string[];
    note: string;
    foot: string;
  } | null;
}

const OD_ACTIVITY = [
  {
    t: "10:31",
    text: "Phan Ngọc Anh đính kèm “Đối chiếu công nợ T9.xlsx”",
    ref: "ERP-151",
  },
  {
    t: "10:12",
    text: "Trần Minh Quân đính kèm “Mapping khách hàng v3.xlsx”",
    ref: "ERP-142",
  },
  {
    t: "09:20",
    text: "Tệp được đổi tên ở OneDrive thành “Kế hoạch kiểm thử v2.docx”; liên kết trên việc tự cập nhật",
    ref: "ERP-144",
  },
  {
    t: "Thứ Bảy 10/10",
    text: "Tệp “Sơ đồ kho cũ.pdf” đã bị xoá ở OneDrive; liên kết trên việc hiện “Không còn tệp”, việc giữ nguyên",
    ref: "ERP-146",
  },
  {
    t: "09/10 14:05",
    text: "Vũ Thanh đính kèm “Quy trình nhập kho.pdf”",
    ref: "ERP-150",
  },
] as const;

const base = {
  callout: null,
  pending: null,
  who: null,
  usedIn: [],
  nots: [],
  permNote: null,
  notNote: null,
  deliveries: null,
  config: null,
  dedupe: [],
  disconnect: null,
  permsTitleKey: "granted",
} as const;

export class MockIntegrations {
  private teamsFixed = false;
  private oneDriveGone = false;
  private paused = false;
  private googleDrive = false;
  private secretRotated = false;
  private contactRequests = 0;

  reset(): void {
    this.teamsFixed = false;
    this.oneDriveGone = false;
    this.paused = false;
    this.googleDrive = false;
    this.secretRotated = false;
    this.contactRequests = 0;
  }

  reauthorize(): void {
    this.teamsFixed = true;
  }
  disconnect(key: string): boolean {
    if (key === "onedrive") this.oneDriveGone = true;
    return key === "onedrive";
  }
  setPaused(value: boolean): void {
    this.paused = value;
  }
  connectGoogleDrive(): void {
    this.googleDrive = true;
  }
  rotateSecret(): void {
    this.secretRotated = true;
  }
  contact(): number {
    this.contactRequests += 1;
    return this.contactRequests;
  }

  rows(viewer: string) {
    const teams: Status[] = this.teamsFixed
      ? [
          { tone: "ok", label: "Đang hoạt động" },
          { tone: "ok", label: "Ổn định" },
        ]
      : [
          { tone: "warn", label: "Cần uỷ quyền lại" },
          { tone: "off", label: "Tạm dừng" },
        ];
    const rows = [
      {
        key: "teams",
        mono: "MT",
        name: "Microsoft Teams",
        sub: "Kết nối bởi Lê Hoàng Phúc · 14/08/2026",
        scope: { kind: "workspace", name: "" },
        conn: teams[0],
        sync: teams[1],
        last: this.teamsFixed ? "10:42" : "Thứ Bảy 10/10 18:05",
        lastSub: this.teamsFixed
          ? "Đã gửi 1 tin đang giữ"
          : "1 tin chưa gửi, đang giữ",
      },
      {
        key: "webhook",
        mono: "WH",
        name: "Trạng thái việc sang ERP cũ",
        sub: "Webhook gửi đi · tạo bởi Lê Hoàng Phúc · 02/09/2026",
        scope: { kind: "board", name: "Triển khai ERP Q4" },
        conn: { tone: "ok", label: "Đang hoạt động" },
        sync: this.paused
          ? { tone: "off", label: "Tạm dừng" }
          : { tone: "warn", label: "Đang thử lại" },
        last: "10:28",
        lastSub: "503 · thử lại lúc 10:45",
      },
      ...(this.oneDriveGone
        ? []
        : [
            {
              key: "onedrive",
              mono: "OD",
              name: "Microsoft OneDrive & SharePoint",
              sub: "Kết nối bởi Nguyễn Thu Hà · 20/08/2026",
              scope: { kind: "workspace", name: "" },
              conn: { tone: "ok", label: "Đang hoạt động" },
              sync: { tone: "ok", label: "Ổn định" },
              last: "10:31",
              lastSub: "214 liên kết tệp",
            },
          ]),
      {
        key: "email",
        mono: "EM",
        name: "Email vào board",
        sub: "hotro-noibo@in.notrelix.vn · kết nối bởi Vũ Thanh · 25/08/2026",
        scope: { kind: "board", name: "Hỗ trợ nội bộ" },
        conn: { tone: "ok", label: "Đang hoạt động" },
        sync: { tone: "ok", label: "Ổn định" },
        last: "09:52",
        lastSub: "38 email thành việc trong 30 ngày",
      },
      ...(this.googleDrive
        ? [
            {
              key: "google-drive",
              mono: "GD",
              name: "Google Drive",
              sub: `Kết nối bởi ${viewer} · 12/10/2026`,
              scope: { kind: "workspace", name: "" },
              conn: { tone: "ok", label: "Đang hoạt động" },
              sync: { tone: "ok", label: "Ổn định" },
              last: "10:40",
              lastSub: "Chưa có liên kết tệp",
            },
          ]
        : []),
    ];
    return rows;
  }

  detail(key: string): Detail | null {
    if (key === "onedrive" && !this.oneDriveGone) {
      return {
        ...base,
        key,
        mono: "OD",
        name: "Microsoft OneDrive & SharePoint",
        kind: "storage",
        subtitle:
          "Đính kèm tệp OneDrive và SharePoint vào việc và trang · phạm vi Workspace",
        actions: {
          reauthorize: true,
          disconnect: true,
          test: false,
          pause: false,
          paused: false,
        },
        health: {
          conn: { tone: "ok", label: "Đang hoạt động" },
          sync: {
            tone: "ok",
            label: "Ổn định",
            note: "214 liên kết tệp · cập nhật 10:31",
          },
        },
        overview: [
          {
            k: "Phạm vi",
            v: "Workspace Minh An Group",
            sub: "Mọi board trong workspace đính kèm được tệp qua kết nối này",
          },
          { k: "Kết nối bởi", v: "Nguyễn Thu Hà · 20/08/2026" },
          {
            k: "Tài khoản Microsoft",
            v: "thuha.nguyen@minhan.vn",
            sub: "Tổ chức Microsoft minhan.vn",
          },
          {
            k: "Dùng ở",
            v: "Tệp đính kèm trên 9 board",
            sub: "214 liên kết tệp · quy tắc tự động dùng kết nối này: 0",
          },
        ],
        activity: OD_ACTIVITY.map((a) => ({ ...a })),
        perms: [
          {
            t: "Đọc tệp được chọn",
            sub: "Chỉ tệp người dùng chọn khi đính kèm",
          },
          { t: "Tạo liên kết xem", sub: "Để mở tệp ngay từ việc hoặc trang" },
        ],
        nots: [
          { t: "Đọc cả OneDrive hay SharePoint" },
          { t: "Sửa, xoá hay chia sẻ tệp" },
        ],
        permNote:
          "Notrelix chỉ xin quyền tối thiểu. Tính năng mới cần thêm quyền sẽ hỏi bạn đồng ý lại trước (INT-035).",
        notNote: "Người mở tệp vẫn cần quyền xem tệp đó ở Microsoft.",
        disconnect: {
          intro:
            "Kết nối này dùng chung cho cả workspace Minh An Group. Muốn dùng lại sau này phải kết nối và uỷ quyền từ đầu.",
          stops: [
            "Đính kèm tệp mới từ OneDrive và SharePoint",
            "Cập nhật tên và trạng thái của tệp đã liên kết",
            "Quyền Notrelix đã nhận ở Microsoft bị thu hồi",
          ],
          keeps: [
            "214 liên kết tệp trên 9 board, giữ lại dạng liên kết",
            "Không xoá tệp nào ở OneDrive",
            "Quy tắc tự động dùng kết nối này: 0",
          ],
          note: "Tệp và liên kết xem đã tạo ở OneDrive do Microsoft quản lý. Ngắt kết nối không xoá chúng; nếu cần, xoá trực tiếp ở OneDrive (INT-034).",
          foot: "Không việc, trang hay tệp nào bị xoá (INT-017)",
        },
      };
    }
    if (key === "teams") {
      const fixed = this.teamsFixed;
      return {
        ...base,
        key,
        mono: "MT",
        name: "Microsoft Teams",
        kind: "chat",
        subtitle:
          "Đăng tin từ quy tắc và thông báo board vào kênh Teams · phạm vi Workspace · kết nối bởi Lê Hoàng Phúc 14/08/2026",
        actions: {
          reauthorize: false,
          disconnect: true,
          test: false,
          pause: false,
          paused: false,
        },
        health: fixed
          ? {
              conn: { tone: "ok", label: "Đang hoạt động" },
              sync: { tone: "ok", label: "Ổn định" },
            }
          : {
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
        overview: [],
        callout: fixed
          ? null
          : {
              title:
                "Uỷ quyền đã hết hạn 10/10 02:00. Tin nhắn mới tạm giữ, không mất.",
              body: "Kết nối vẫn còn: kênh đã chọn, cấu hình và lịch sử giữ nguyên. Uỷ quyền lại xong là gửi tiếp được (INT-006).",
              hint: "Dùng đúng tài khoản Microsoft đã kết nối (phuc.le@minhan.vn). Nếu dùng tài khoản khác, Notrelix sẽ hỏi lại trước khi đổi.",
              cta: "Uỷ quyền lại với Microsoft",
            },
        pending: fixed
          ? null
          : {
              title: "Đăng tin vào kênh “ERP – Điều hành”",
              sub: "Bước 2 của quy tắc “Khẩn → báo Trưởng phòng” · ERP-142 · chờ từ Thứ Bảy 10/10 18:05",
              boardId: "mock-board-erp",
              note: "Sau khi uỷ quyền lại, mở lần chạy và bấm “Chạy lại bước lỗi”. Bước 1 đã gửi thông báo cho Lê Hoàng Phúc nên không gửi lại.",
            },
        who: "Người đã kết nối (<b>Lê Hoàng Phúc</b>) hoặc quản trị workspace. Bạn là quản trị workspace nên làm được.",
        usedIn: [
          {
            title: "Khẩn → báo Trưởng phòng",
            sub: "Quy tắc · board Triển khai ERP Q4",
          },
          {
            title: "Thông báo kênh cho 2 board",
            sub: "Triển khai ERP Q4 · Hạ tầng & bảo mật",
          },
        ],
        activity: fixed
          ? [
              {
                t: "12/10 10:42",
                text: "Uỷ quyền lại xong. Đã gửi tin đang giữ.",
                ref: null,
              },
            ]
          : [
              {
                t: "10/10 18:05",
                text: "Không đăng được tin của quy tắc “Khẩn → báo Trưởng phòng”: cần uỷ quyền lại",
                ref: "ERP-142",
              },
              {
                t: "10/10 02:00",
                text: "Microsoft báo uỷ quyền hết hạn. Đã báo Lê Hoàng Phúc và quản trị workspace trong Hộp thư.",
                ref: null,
              },
              {
                t: "09/10 16:20",
                text: "Đăng tin việc hoàn thành vào kênh “ERP – Điều hành”",
                ref: "ERP-136",
              },
            ],
        perms: [
          { t: "Đăng tin vào kênh được chọn" },
          { t: "Xem tên nhóm và kênh để chọn kênh" },
        ],
        disconnect: {
          intro:
            "Kết nối này dùng chung cho cả workspace Minh An Group. Muốn dùng lại sau này phải kết nối và uỷ quyền từ đầu.",
          stops: [
            "Đăng tin vào kênh Teams",
            "Quyền Notrelix đã nhận ở Microsoft bị thu hồi",
          ],
          keeps: [
            "Quy tắc và cấu hình, chuyển sang trạng thái cần kết nối lại",
            "Không kênh hay tin nào ở Teams bị xoá",
          ],
          note: "Tin đã đăng ở Teams do Microsoft quản lý. Ngắt kết nối không xoá chúng.",
          foot: "Không việc, trang hay tệp nào bị xoá (INT-017)",
        },
      };
    }
    if (key === "webhook") {
      const paused = this.paused;
      return {
        ...base,
        key,
        mono: "WH",
        name: "Trạng thái việc sang ERP cũ",
        kind: "webhook",
        subtitle:
          "Webhook gửi đi · phạm vi board Triển khai ERP Q4 · tạo bởi Lê Hoàng Phúc 02/09/2026",
        actions: {
          reauthorize: false,
          disconnect: false,
          test: true,
          pause: true,
          paused,
        },
        health: {
          conn: { tone: "ok", label: "Đang hoạt động" },
          sync: paused
            ? { tone: "off", label: "Tạm dừng", note: "1 sự kiện đang giữ" }
            : {
                tone: "warn",
                label: "Đang thử lại",
                note: "1 sự kiện đang chờ · lần thử tới 10:45",
              },
        },
        overview: [],
        callout: {
          title: "Hệ thống nhận đang trả về 503 từ 10:28.",
          body: "Notrelix thử lại với giãn cách tăng dần, tối đa 8 lần cho mỗi sự kiện. 1 sự kiện đang chờ, không mất. Lỗi nằm ở phía erp-cu.minhan.vn; nhờ đội vận hành ERP cũ kiểm tra.",
          hint: null,
          cta: null,
        },
        deliveries: {
          retryAt: "10:45",
          waiting: [
            {
              t: "10:28",
              ref: "ERP-138",
              result: "503 · lần 3/8",
              next: "10:45",
            },
          ],
          sent: [
            { t: "09/10 16:20", ref: "ERP-136", result: "200 · đã nhận" },
            { t: "09/10 15:12", ref: "ERP-140", result: "200 · đã nhận" },
          ],
        },
        config: {
          url: "https://erp-cu.minhan.vn/hooks/notrelix",
          event: "Trạng thái thay đổi",
          scope: "Board Triển khai ERP Q4",
          secret: this.secretRotated
            ? "Đã lưu · tạo lại 12/10/2026"
            : "Đã lưu · tạo 02/09/2026",
        },
        dedupe: [
          "Mỗi sự kiện được gửi kèm mã định danh để hệ thống nhận bỏ qua bản trùng.",
          "Một lần thử lại không phải thay đổi mới trên việc; nhật ký của việc không ghi thêm gì (INT-010).",
        ],
        activity: [],
        perms: [],
      };
    }
    return null;
  }
}

const BASE_CATALOG = [
  {
    key: "slack",
    mono: "SL",
    name: "Slack",
    desc: "Gửi thông báo của board vào kênh Slack",
    enterprise: false,
  },
  {
    key: "google-drive",
    mono: "GD",
    name: "Google Drive",
    desc: "Đính kèm tệp Google Drive vào việc và trang",
    enterprise: false,
  },
  {
    key: "google-calendar",
    mono: "GC",
    name: "Google Calendar",
    desc: "Hiện hạn chót của việc trên Google Calendar",
    enterprise: false,
  },
  {
    key: "github",
    mono: "GH",
    name: "GitHub",
    desc: "Gắn commit và pull request với việc",
    enterprise: false,
  },
  {
    key: "zalo-oa",
    mono: "ZL",
    name: "Zalo OA",
    desc: "Gửi thông báo qua Zalo Official Account",
    enterprise: false,
  },
  {
    key: "power-bi",
    mono: "PB",
    name: "Power BI",
    desc: "Đưa số liệu board sang Power BI",
    enterprise: true,
  },
] as const;

export const INTEGRATION_CATALOG = BASE_CATALOG.map((item) => ({
  ...item,
  provider: item.name.split(" ")[0] ?? item.name,
  will:
    item.key === "google-drive"
      ? [
          "Đọc tệp người dùng chọn khi đính kèm",
          "Tạo liên kết xem cho tệp đã đính kèm",
          "Biết khi tệp đổi tên hoặc bị xoá để cập nhật liên kết",
        ]
      : [item.desc],
  wont:
    item.key === "google-drive"
      ? ["Đọc cả Google Drive", "Sửa, xoá hay chia sẻ tệp"]
      : [
          `Đọc dữ liệu ngoài phạm vi đã chọn`,
          `Sửa hay xoá dữ liệu ở ${item.name}`,
        ],
  gate: item.enterprise
    ? {
        can: [
          "Đưa số liệu của board được chọn sang Power BI để dựng báo cáo riêng",
          "Làm mới số liệu theo lịch",
        ],
        alsoHas: [
          "Tự động cấp và thu hồi tài khoản qua SCIM",
          "Lưu nhật ký kiểm toán 7 năm",
        ],
      }
    : null,
}));
