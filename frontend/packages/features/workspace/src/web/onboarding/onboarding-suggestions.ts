import type {
  BoardTemplateInfo,
  CompanySize,
  OnboardingCatalog,
} from "./onboarding-models";

/**
 * The suggestions the setup starts from: the departments a company of a size
 * usually has, the extras its line of business adds, and what each board
 * template starts with. They are starting points the person edits, not product
 * facts the server owns (the server only stores what the person confirms, as
 * `CreateSpace` and `Boards.Create`), so they live with the page that offers
 * them. Pure: a size, a name and a language in, the catalogue out.
 */
export type SuggestionLocale = "vi" | "en";

export interface DepartmentSuggestion {
  readonly key: string;
  readonly name: string;
}

export interface OnboardingSuggestions extends OnboardingCatalog {
  readonly size: CompanySize;
  readonly departments: readonly DepartmentSuggestion[];
}

type Names = Readonly<Record<SuggestionLocale, string>>;
type Named = { readonly key: string; readonly name: Names };

const KINH_DOANH: Named = {
  key: "kinh-doanh",
  name: { vi: "Kinh doanh", en: "Sales" },
};
const KE_TOAN: Named = {
  key: "ke-toan",
  name: { vi: "Kế toán", en: "Accounting" },
};
const VAN_HANH: Named = {
  key: "van-hanh",
  name: { vi: "Vận hành", en: "Operations" },
};
const SAN_XUAT: Named = {
  key: "san-xuat",
  name: { vi: "Sản xuất", en: "Production" },
};
const NHAN_SU: Named = {
  key: "nhan-su",
  name: { vi: "Nhân sự", en: "People" },
};
const CONG_NGHE: Named = {
  key: "cong-nghe",
  name: { vi: "Công nghệ", en: "Technology" },
};

/** The departments by size; "more than 500" starts from the 201 to 1000 list. */
const BY_SIZE: Readonly<Record<Exclude<CompanySize, "xxl">, readonly Named[]>> =
  {
    s: [KINH_DOANH, VAN_HANH],
    m: [KINH_DOANH, KE_TOAN, VAN_HANH],
    l: [KINH_DOANH, KE_TOAN, SAN_XUAT, NHAN_SU],
    xl: [KINH_DOANH, KE_TOAN, SAN_XUAT, NHAN_SU, CONG_NGHE],
  };

const FOOD_EXTRAS: readonly Named[] = [
  { key: "mua-hang", name: { vi: "Mua hàng", en: "Purchasing" } },
  { key: "kho-van", name: { vi: "Kho vận", en: "Warehouse" } },
  {
    key: "kiem-soat-chat-luong",
    name: { vi: "Kiểm soát chất lượng", en: "Quality control" },
  },
];

const GENERAL_EXTRAS: readonly Named[] = [
  { key: "marketing", name: { vi: "Marketing", en: "Marketing" } },
  {
    key: "cham-soc-khach-hang",
    name: { vi: "Chăm sóc khách hàng", en: "Customer care" },
  },
];

/** The line of business is read from the company name. */
const FOOD_NAME = /thực phẩm|food|nước chấm/i;

const TEMPLATES: Readonly<
  Record<SuggestionLocale, readonly BoardTemplateInfo[]>
> = {
  vi: [
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
  en: [
    {
      id: "project",
      stages: ["To do", "In progress", "In review", "Done"],
      fields: ["Assignee", "Due", "Priority"],
      sample: [],
      bars: [],
    },
    {
      id: "approval",
      stages: ["Submitted", "Department head", "Accounting", "Done"],
      fields: ["Amount", "Approver", "Approval due", "Documents"],
      sample: [],
      bars: [],
    },
    {
      id: "hiring",
      stages: ["Applied", "Interview", "Offer", "Hired"],
      fields: [],
      sample: [],
      bars: [100, 96, 40, 20],
    },
    {
      id: "support",
      stages: [],
      fields: ["Customer", "Channel", "Urgency"],
      sample: [
        ["Phu My Distributor", "Zalo", "Urgent"],
        ["An Binh Supermarket", "Email", "Normal"],
        ["Ms. Hanh", "Phone", "Normal"],
      ],
      bars: [],
    },
    { id: "blank", stages: [], fields: ["Name"], sample: [], bars: [] },
  ],
};

const named = (list: readonly Named[], locale: SuggestionLocale) =>
  list.map((entry) => ({ key: entry.key, name: entry.name[locale] }));

export function onboardingSuggestions(
  size: CompanySize,
  companyName: string,
  locale: SuggestionLocale,
): OnboardingSuggestions {
  const food = FOOD_NAME.test(companyName);
  return {
    size,
    industry: food ? "food" : null,
    departments: named(BY_SIZE[size === "xxl" ? "xl" : size], locale),
    extras: named(food ? FOOD_EXTRAS : GENERAL_EXTRAS, locale),
    boardTemplates: TEMPLATES[locale],
  };
}
