/**
 * Account appearance copy (WX-FND-002/006), vi and en, registered as the
 * `account-appearance` catalog (`account-appearance.catalog.ts`). It owns the
 * theme mode, accent and language labels that the web host used to keep in
 * its `appearance` catalog. The pure surface takes a `copy` prop;
 * `useAccountAppearanceCopy` resolves it for the active locale in the
 * container.
 */
export const en = {
  title: "Appearance",
  description: "Customize how Notrelix looks on this device.",
  mode: {
    legend: "Mode",
    light: {
      label: "Light",
      description: "Light surfaces for bright environments",
    },
    dark: {
      label: "Dark",
      description: "Dark surfaces that are easy on the eyes in low light",
    },
    system: {
      label: "System",
      description: "Follows your device setting",
    },
    highContrast: {
      label: "High contrast",
      description: "Maximum legibility for text and borders",
    },
  },
  accent: {
    legend: "Accent color",
    blue: { label: "Notrelix Blue", description: "The default brand color" },
    coral: { label: "Coral", description: "Warm orange" },
    ember: { label: "Ember", description: "Deep rose red" },
    sage: { label: "Sage", description: "Muted green" },
    amber: {
      label: "Amber",
      description: "Warm yellow with dark text on fills",
    },
  },
  language: {
    legend: "Language",
    vi: "Tiếng Việt",
    en: "English",
  },
  preview: {
    title: "Preview",
    modeName: {
      light: "Light",
      dark: "Dark",
      system: "System",
      highContrast: "High contrast",
    },
    tabs: { table: "Table", kanban: "Kanban", calendar: "Calendar" },
    cardOne: "Move customer data from the old system to the ERP",
    cardTwo: "Set up single sign-on (SSO) for the whole company",
    doing: "In progress",
    review: "In review",
    overdue: "2 days overdue",
    dateTwo: "14/10",
    addItem: "Add item",
    openBoard: "See the whole board",
    changes: "Changes with the accent:",
    changesTo: " primary buttons, links, the selected item. ",
    stays: "Stays the same:",
    staysTo: " status colors, department colors, overdue warnings.",
  },
  companyDefault:
    "Workspace admins set the default accent for {{workspace}}: {{accent}}. Your choice takes priority.",
  sidebar: {
    legend: "Sidebar",
    label: "Collapsed by default",
    help: "Start with the sidebar collapsed.",
    loading: "Loading your sidebar preference…",
    loadError: "We couldn't load your sidebar preference.",
    retry: "Try again",
    saving: "Saving…",
    saved: "Saved.",
    saveError: "We couldn't save this preference. Try again.",
  },
};

export type AccountAppearanceCopy = typeof en;

export const vi: AccountAppearanceCopy = {
  title: "Giao diện",
  description: "Tùy chỉnh cách Notrelix hiển thị trên thiết bị này.",
  mode: {
    legend: "Chế độ hiển thị",
    light: {
      label: "Sáng",
      description: "Nền sáng cho môi trường nhiều ánh sáng",
    },
    dark: {
      label: "Tối",
      description: "Nền tối, dịu mắt khi thiếu sáng",
    },
    system: {
      label: "Theo hệ thống",
      description: "Tự động theo cài đặt của thiết bị",
    },
    highContrast: {
      label: "Tương phản cao",
      description: "Chữ và viền rõ nét tối đa",
    },
  },
  accent: {
    legend: "Màu nhấn",
    blue: { label: "Xanh Notrelix", description: "Màu thương hiệu mặc định" },
    coral: { label: "San hô", description: "Cam ấm" },
    ember: { label: "Than hồng", description: "Đỏ hồng đậm" },
    sage: { label: "Xô thơm", description: "Xanh lục dịu" },
    amber: {
      label: "Hổ phách",
      description: "Vàng ấm, chữ tối trên nền màu",
    },
  },
  language: {
    legend: "Ngôn ngữ",
    vi: "Tiếng Việt",
    en: "English",
  },
  preview: {
    title: "Xem trước",
    modeName: {
      light: "Sáng",
      dark: "Tối",
      system: "Theo hệ thống",
      highContrast: "Tương phản cao",
    },
    tabs: { table: "Bảng", kanban: "Kanban", calendar: "Lịch" },
    cardOne: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    cardTwo: "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
    doing: "Đang làm",
    review: "Chờ duyệt",
    overdue: "Quá hạn 2 ngày",
    dateTwo: "14/10",
    addItem: "Thêm việc",
    openBoard: "Xem cả board",
    changes: "Đổi theo màu nhấn:",
    changesTo: " nút chính, liên kết, mục đang chọn. ",
    stays: "Giữ nguyên:",
    staysTo: " màu trạng thái, màu phòng ban, cảnh báo quá hạn.",
  },
  companyDefault:
    "Quản trị workspace đặt màu mặc định cho {{workspace}}: {{accent}}. Lựa chọn của bạn được ưu tiên.",
  sidebar: {
    legend: "Thanh bên",
    label: "Thu gọn theo mặc định",
    help: "Bắt đầu với thanh bên đã thu gọn.",
    loading: "Đang tải tùy chọn thanh bên của bạn…",
    loadError: "Không thể tải tùy chọn thanh bên của bạn.",
    retry: "Thử lại",
    saving: "Đang lưu…",
    saved: "Đã lưu.",
    saveError: "Không thể lưu tùy chọn này. Hãy thử lại.",
  },
};

export function accountAppearanceCopy(
  locale: "vi" | "en",
): AccountAppearanceCopy {
  return locale === "vi" ? vi : en;
}
