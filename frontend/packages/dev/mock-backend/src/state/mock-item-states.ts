/**
 * Items that show the Detail page in the states the States mockup
 * (docs/design/mockups/2026-10/src/Final-States.dc.html) lists: a new item
 * with nothing on it (ERP-154), an archived one (ERP-118) and one with very
 * long content (ERP-160). They sit on the ERP board but stay out of its
 * reads (`unlisted`), so the lanes, counts and views of the board mockups
 * keep their numbers. Pure data; `MockStore.seedErpBoard` inserts it.
 */
import type { ErpGroupKey } from "./mock-erp-board";
import { ERP_PEOPLE } from "./mock-erp-board";

export interface ErpStateItem {
  readonly key: string;
  readonly group: ErpGroupKey;
  readonly title: string;
  readonly description?: string;
  readonly status: "todo" | "doing" | "review" | "done";
  readonly priority: "urgent" | "high" | "medium" | "low";
  readonly assignee: "VT" | "MQ" | "HA" | null;
  /** Instant the item was created. */
  readonly createdAt: string;
  readonly archived?: { readonly at: string; readonly byUserId: string };
  readonly children?: number;
}

const LONG_DESCRIPTION = [
  "Rà soát toàn bộ danh mục vật tư, hàng hoá, mã kho và đơn vị tính của 14 chi nhánh trước khi nhập số dư đầu kỳ vào ERP. Mỗi chi nhánh đang giữ một bảng tính riêng, đặt tên khác nhau và không thống nhất đơn vị: có nơi tính theo thùng, nơi tính theo cái, nơi tính theo kg.",
  "Mục tiêu là một danh mục dùng chung, mỗi mặt hàng một mã duy nhất, mỗi mã một đơn vị tính gốc và bảng quy đổi sang các đơn vị còn lại. Mã kho theo quy ước mới: hai chữ cái của chi nhánh, hai số của khu vực, ba số của vị trí.",
  "Các bước thực hiện: thu thập bảng tính của từng chi nhánh, gộp vào một tệp trung gian, đánh dấu mã trùng và mã thiếu đơn vị, gửi lại chi nhánh xác nhận, rồi mới nạp vào ERP. Không nạp khi chi nhánh chưa xác nhận.",
  "Lưu ý với kho lạnh: hàng có hạn dùng phải kèm cột hạn dùng và số lô. Kho Hải Phòng và kho Đà Nẵng có thêm hàng ký gửi, cần tách riêng khỏi tồn kho của công ty.",
  "Kế toán kho đối chiếu số lượng và giá trị từng mã với sổ cái trước khi chốt. Chênh lệch trên 0,5% giá trị mỗi chi nhánh phải có giải trình bằng văn bản.",
  "Sau khi nạp xong, in biên bản kiểm kê từng chi nhánh, trưởng chi nhánh ký xác nhận, quét và đính kèm vào việc này.",
].join("\n\n");

export const ERP_STATE_ITEMS: readonly ErpStateItem[] = [
  {
    key: "ERP-154",
    group: "test",
    title: "Kiểm tra quyền xem báo cáo thuế",
    status: "todo",
    priority: "medium",
    assignee: null,
    createdAt: "2026-10-12T03:39:00.000Z",
  },
  {
    key: "ERP-118",
    group: "prep",
    title: "Khảo sát nhà cung cấp phần mềm kho",
    description: "Không còn cần vì đã chọn phân hệ kho của ERP.",
    status: "todo",
    priority: "low",
    assignee: "VT",
    createdAt: "2026-09-14T02:00:00.000Z",
    archived: { at: "2026-10-08T03:00:00.000Z", byUserId: ERP_PEOPLE.HA.id },
  },
  {
    key: "ERP-160",
    group: "training",
    title:
      "Rà soát và chuẩn hoá toàn bộ danh mục vật tư, hàng hoá, mã kho và đơn vị tính của 14 chi nhánh trước khi nhập số dư đầu kỳ vào ERP",
    description: LONG_DESCRIPTION,
    status: "doing",
    priority: "high",
    assignee: "MQ",
    createdAt: "2026-10-01T02:00:00.000Z",
    children: 48,
  },
];

const BRANCHES = [
  "Hà Nội",
  "Hải Phòng",
  "Đà Nẵng",
  "Huế",
  "Nha Trang",
  "Quy Nhơn",
  "Buôn Ma Thuột",
  "Đà Lạt",
  "TP. Hồ Chí Minh",
  "Biên Hoà",
  "Bình Dương",
  "Vũng Tàu",
  "Cần Thơ",
  "Long An",
];

/** The 48 child items of ERP-160: 12 done (Hà Nội first), the rest open. */
export function erpLongChildTitles(): string[] {
  const titles: string[] = [];
  for (const branch of BRANCHES) {
    titles.push(`Chi nhánh ${branch}`);
  }
  let n = 0;
  while (titles.length < 48) {
    const branch = BRANCHES[n % BRANCHES.length];
    titles.push(
      `Đối chiếu mã kho chi nhánh ${branch} lần ${Math.floor(n / BRANCHES.length) + 2}`,
    );
    n += 1;
  }
  return titles;
}
