import type { GettingStartedCardProps } from "../web/onboarding/getting-started-card";
import { onboardingCopy } from "../web/onboarding/onboarding.copy";
import type { OnboardingJoinSurfaceProps } from "../web/onboarding/onboarding-join-surface";
import {
  emptyDraft,
  type OnboardingCatalog,
  type OnboardingDraftData,
  type OnboardingSummary,
} from "../web/onboarding/onboarding-models";
import type { OnboardingSurfaceProps } from "../web/onboarding/onboarding-surface";

/**
 * Mockup world of the onboarding pages (Final-Onboarding), for stories and
 * tests only: Công ty CP Thực phẩm Sao Mai setting up its workspace, and
 * Trang joining Minh An Group. Deterministic: no clock, no random.
 */
const noop = () => undefined;
const copy = onboardingCopy("vi");

export const SAO_MAI_CATALOG: OnboardingCatalog = {
  industry: "food",
  extras: [
    { key: "mua-hang", name: "Mua hàng" },
    { key: "kho-van", name: "Kho vận" },
    { key: "kiem-soat-chat-luong", name: "Kiểm soát chất lượng" },
  ],
  boardTemplates: [
    {
      id: "project",
      stages: ["Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành"],
      fields: ["Người làm", "Hạn", "Ưu tiên"],
      sample: [],
      bars: [],
    },
    {
      id: "approval",
      stages: ["Gửi", "Trưởng phòng", "Kế toán", "Xong"],
      fields: ["Số tiền", "Người duyệt", "Hạn duyệt", "Chứng từ"],
      sample: [],
      bars: [],
    },
    {
      id: "hiring",
      stages: ["Ứng tuyển", "Phỏng vấn", "Đề nghị", "Nhận việc"],
      fields: [],
      sample: [],
      bars: [100, 96, 40, 20],
    },
    {
      id: "support",
      stages: [],
      fields: ["Khách hàng", "Kênh", "Mức độ"],
      sample: [
        ["Đại lý Phú Mỹ", "Zalo", "Gấp"],
        ["Siêu thị An Bình", "Email", "Thường"],
        ["Chị Hạnh", "Điện thoại", "Thường"],
      ],
      bars: [],
    },
    { id: "blank", stages: [], fields: ["Tên"], sample: [], bars: [] },
  ],
};

export function saoMaiDraft(
  patch: Partial<OnboardingDraftData> = {},
): OnboardingDraftData {
  return {
    workspace: {
      name: "Công ty CP Thực phẩm Sao Mai",
      slug: "saomai",
      size: "l",
      locale: "vi",
    },
    departments: [
      { key: "kinh-doanh", name: "Kinh doanh", color: "coral" },
      { key: "ke-toan", name: "Kế toán", color: "gold" },
      { key: "san-xuat", name: "Sản xuất", color: "blue" },
      { key: "nhan-su", name: "Nhân sự", color: "sage" },
    ],
    invites: [
      {
        id: "row-1",
        email: "hoang.minh.khoa@saomaifood.vn",
        departmentKey: "san-xuat",
        role: "Admin",
      },
      {
        id: "row-2",
        email: "tran.thi.lan@saomaifood.vn",
        departmentKey: "ke-toan",
        role: "Member",
      },
      {
        id: "row-3",
        email: "vo.duc.long@saomaifood.vn",
        departmentKey: "kinh-doanh",
        role: "Member",
      },
      { id: "row-4", email: "", departmentKey: "kinh-doanh", role: "Member" },
    ],
    board: {
      templateId: "project",
      name: "Ra mắt nước chấm Sao Mai",
      departmentKey: "san-xuat",
    },
    ...patch,
  };
}

export const SAO_MAI_SUMMARY: OnboardingSummary = {
  companyName: "Công ty CP Thực phẩm Sao Mai",
  address: "saomai.notrelix.vn",
  departments: [
    { name: "Kinh doanh", color: "coral" },
    { name: "Kế toán", color: "gold" },
    { name: "Sản xuất", color: "blue" },
    { name: "Nhân sự", color: "sage" },
  ],
  invitesSent: 3,
  board: {
    name: "Ra mắt nước chấm Sao Mai",
    templateId: "project",
    departmentName: "Sản xuất",
  },
};

export function onboardingScenario(
  overrides: Partial<OnboardingSurfaceProps> = {},
): OnboardingSurfaceProps {
  return {
    copy,
    step: "workspace",
    draft: saoMaiDraft(),
    email: "ha.nguyen@saomaifood.vn",
    ownerName: "Nguyễn Thu Hà",
    address: { kind: "free" },
    catalog: SAO_MAI_CATALOG,
    summary: null,
    today: "Thứ Hai, 12 tháng 10",
    onDraft: noop,
    onUseSuggestion: noop,
    onNext: noop,
    onBack: noop,
    onSkip: noop,
    onExit: noop,
    onEnterHome: noop,
    ...overrides,
  };
}

export function emptyOnboardingDraft(): OnboardingDraftData {
  return emptyDraft();
}

export function joinScenario(
  overrides: Partial<OnboardingJoinSurfaceProps> = {},
): OnboardingJoinSurfaceProps {
  return {
    copy,
    email: "trang.ly@minhan.vn",
    org: { name: "Minh An Group", initials: "MA" },
    inviterName: "Nguyễn Thu Hà",
    spaces: [
      {
        id: "cong-nghe",
        name: "Công nghệ",
        color: "blue",
        open: false,
        member: true,
        boards: [
          { id: "b1", title: "Triển khai ERP Q4", level: "editor" },
          { id: "b2", title: "Hạ tầng & bảo mật", level: "editor" },
          { id: "b3", title: "Hỗ trợ nội bộ", level: "editor" },
        ],
      },
      {
        id: "marketing",
        name: "Marketing",
        color: "coral",
        open: true,
        member: false,
        boards: [],
      },
    ],
    displayName: "Lý Thu Trang",
    initials: "LT",
    mode: "light",
    accent: "sage",
    companyAccent: "blue",
    notifyEmail: "digest",
    onDisplayName: noop,
    onMode: noop,
    onAccent: noop,
    onNotifyEmail: noop,
    onStart: noop,
    onLater: noop,
    ...overrides,
  };
}

export function gettingStartedScenario(
  overrides: Partial<GettingStartedCardProps> = {},
): GettingStartedCardProps {
  return {
    copy,
    name: "Sao Mai",
    items: [
      {
        key: "first-item",
        title: "Tạo việc đầu tiên",
        note: "Trong board Ra mắt nước chấm Sao Mai",
        done: false,
        href: "/workspaces/ws/boards/b1",
      },
      {
        key: "invite-more",
        title: "Mời thêm người",
        note: "3 người đang chờ nhận lời",
        done: false,
        href: "/workspaces/ws/members",
      },
      {
        key: "mobile",
        title: "Tải ứng dụng di động",
        note: "iOS và Android, đăng nhập bằng cùng tài khoản",
        done: false,
        href: null,
      },
    ],
    onToggle: noop,
    onHide: noop,
    ...overrides,
  };
}
