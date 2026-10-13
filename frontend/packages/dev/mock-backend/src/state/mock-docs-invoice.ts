/**
 * Seed data of the Accounting document "Nhập hoá đơn đầu vào" (the 2026-10
 * Docs mockup): a folder page with one page that mentions ERP board items,
 * embeds a task table, lists a checklist, and carries two comments and the
 * edit history of three days. Times are UTC instants of the Hanoi clock; the
 * mockup clock is Monday 12/10/2026 10:40.
 *
 * Pure data: `MockStore.seedInvoiceDoc` inserts it.
 */
import { erpItemId } from "./mock-erp-board";
import { mockIds } from "./mock-ids";

export const INVOICE_FOLDER_ID = "mock-doc-process-folder";
export const INVOICE_DOC_ID = "mock-doc-invoice-process";
/** An item of a private board the viewer cannot read (reads as not found). */
export const INVOICE_PRIVATE_ITEM_ID = "mock-erp-item-private-hr";

const task = (key: string) => `[[task:${erpItemId(key)}]]`;

export interface InvoiceBlock {
  readonly type: string;
  readonly properties: Record<string, unknown>;
}

export const INVOICE_BLOCKS: readonly InvoiceBlock[] = [
  { type: "heading_2", properties: { text: "Phạm vi áp dụng" } },
  {
    type: "paragraph",
    properties: {
      text: `Quy trình áp dụng cho mọi hoá đơn điện tử đầu vào của Minh An Group từ ngày 01/11/2026, khi phân hệ công nợ của ERP chính thức vận hành. Việc kiểm thử phân hệ đang theo dõi tại ${task("ERP-144")}.`,
    },
  },
  { type: "heading_2", properties: { text: "Các bước xử lý" } },
  {
    type: "numbered_list",
    properties: {
      text: "Hộp thư hoá đơn tự động chuyển hoá đơn vào hàng chờ của nhóm Công nợ.",
    },
  },
  {
    type: "numbered_list",
    properties: {
      text: "Kế toán viên đối chiếu với đơn đặt hàng và phiếu nhập kho trong tối đa 2 ngày làm việc.",
    },
  },
  {
    type: "numbered_list",
    properties: {
      text: "Hoá đơn sai lệch quá 0,5% chuyển cho trưởng nhóm duyệt.",
    },
  },
  {
    type: "numbered_list",
    properties: {
      text: `Hạch toán vào ERP và gắn mã chi phí theo phòng ban, theo phân quyền ở ${task("ERP-151")}.`,
    },
  },
  { type: "heading_2", properties: { text: "Chuẩn bị trước khi áp dụng" } },
  {
    type: "todo",
    properties: {
      text: "Chốt danh mục tài khoản dùng chung",
      checked: true,
      linkedTaskId: erpItemId("ERP-129"),
    },
  },
  {
    type: "todo",
    properties: {
      text: "Đào tạo nhóm Công nợ về đối chiếu tự động",
      checked: false,
    },
  },
  {
    type: "todo",
    properties: {
      text: "Thông báo 42 nhà cung cấp về hộp thư hoá đơn mới",
      checked: false,
    },
  },
  { type: "heading_2", properties: { text: "Việc liên quan" } },
  {
    type: "board_reference",
    properties: {
      linkedBoardId: mockIds.boards.erp,
      title: "Triển khai ERP Q4",
      caption: "Phân hệ Kế toán · chưa xong",
      items: [
        erpItemId("ERP-142"),
        erpItemId("ERP-144"),
        erpItemId("ERP-151"),
        erpItemId("ERP-149"),
        INVOICE_PRIVATE_ITEM_ID,
      ],
    },
  },
  {
    type: "callout",
    properties: {
      icon: "info",
      text: "Hoá đơn từ 20 triệu đồng phải có chứng từ thanh toán không dùng tiền mặt mới được khấu trừ thuế GTGT.",
    },
  },
];

/** Comments, oldest first; the leading "> " line is the anchored text. */
export const INVOICE_COMMENTS: ReadonlyArray<{
  readonly userId: string;
  readonly body: string;
  readonly at: string;
}> = [
  {
    userId: mockIds.users.member,
    body: "> trong tối đa 2 ngày làm việc\nCuối tháng lượng hoá đơn gấp ba, đề xuất 3 ngày cho tuần cuối tháng.",
    at: "2026-10-11T03:10:00.000Z",
  },
  {
    userId: mockIds.users.admin,
    body: "Đồng ý, mình sẽ thêm ngoại lệ. Anh Nam rà lại quy định thuế giúp nhé.",
    at: "2026-10-11T03:42:00.000Z",
  },
];

/** History, newest first. */
export const INVOICE_HISTORY: ReadonlyArray<{
  readonly userId: string;
  readonly action: string;
  readonly detail: string;
  readonly at: string;
}> = [
  {
    userId: mockIds.users.admin,
    action: "edited_page",
    detail: "Đổi trạng thái Đang soạn → Chờ duyệt",
    at: "2026-10-11T09:40:00.000Z",
  },
  {
    userId: mockIds.users.admin,
    action: "edited_page",
    detail: "Sửa bước 3 · thêm 2 dòng danh sách kiểm",
    at: "2026-10-11T08:05:00.000Z",
  },
  {
    userId: mockIds.users.member,
    action: "edited_page",
    detail: "Thêm lưu ý thuế GTGT cho hoá đơn từ 20 triệu đồng",
    at: "2026-10-10T08:42:00.000Z",
  },
  {
    userId: mockIds.users.admin,
    action: "edited_page",
    detail: "Thêm mục Việc liên quan",
    at: "2026-10-10T04:20:00.000Z",
  },
  {
    userId: mockIds.users.admin,
    action: "edited_page",
    detail: "Khôi phục từ phiên bản 05/10 14:20",
    at: "2026-10-08T10:05:00.000Z",
  },
  {
    userId: mockIds.users.member,
    action: "edited_page",
    detail: "Viết lại mục 2 · bỏ bước duyệt hai cấp",
    at: "2026-10-08T02:40:00.000Z",
  },
  {
    userId: mockIds.users.admin,
    action: "created_page",
    detail: "Tạo trang",
    at: "2026-10-05T07:20:00.000Z",
  },
];
