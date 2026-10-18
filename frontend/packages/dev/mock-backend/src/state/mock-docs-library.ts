/**
 * The workspace documents of the 2026-10 mockups beside the invoice process
 * page: the ERP specification, conversion plan and on-call handbook of the
 * Công nghệ department, and the Kế toán bank reconciliation guide. Times are UTC
 * instants of the Hanoi clock the mockups use (Monday 12/10/2026 10:40).
 *
 * Pure data: the world seed inserts it in place of generic pages.
 */
import { INVOICE_FOLDER_ID } from "./mock-docs-invoice";
import { ERP_PEOPLE } from "./mock-erp-board";
import { mockIds } from "./mock-ids";

export interface LibraryDoc {
  readonly id: string;
  readonly title: string;
  readonly spaceId: string;
  readonly parentId?: string;
  readonly createdAt: string;
  readonly updatedAt: string;
  /** Who made the last edit (the latest history entry). */
  readonly editorId: string;
  /** Title, then blocks; a `## ` prefix marks a section heading. */
  readonly blocks: readonly string[];
}

export const LIBRARY_DOCS: readonly LibraryDoc[] = [
  {
    id: "mock-doc-product-spec",
    title: "Đặc tả triển khai ERP",
    spaceId: mockIds.spaces.engineering,
    createdAt: "2026-09-18T03:00:00.000Z",
    updatedAt: "2026-09-30T03:10:00.000Z",
    editorId: mockIds.users.owner,
    blocks: [
      "Đặc tả triển khai ERP",
      "Tài liệu này mô tả phạm vi, các phân hệ và mốc triển khai ERP của Minh An Group trong quý 4/2026.",
      "## Phạm vi",
      "ERP thay phần mềm bán hàng và sổ sách đang dùng, gồm các phân hệ bán hàng, kho, công nợ và báo cáo. Các phòng ban dùng chung một danh mục khách hàng và tài khoản kế toán.",
      "## Các mốc chính",
      "Chuyển dữ liệu khách hàng xong trước 14/10, kiểm thử công nợ với số liệu tháng 9, đào tạo người dùng đợt 1, vận hành song song hai hệ thống rồi chạy chính thức từ 01/11/2026.",
      "## Phân quyền",
      "Mỗi phòng ban chỉ thấy dữ liệu của mình. Kế toán xem và sửa phân hệ công nợ; các phòng khác xem báo cáo tổng hợp.",
      "## Rủi ro",
      "Dữ liệu khách hàng trùng mã số thuế, lệch công nợ đầu kỳ và thời gian đào tạo người dùng là ba rủi ro đang được theo dõi.",
      "## Liên quan",
      "Kế hoạch chuyển đổi dữ liệu mô tả quy tắc gộp bản ghi trùng chi tiết.",
    ],
  },
  {
    id: "mock-doc-data-migration-plan",
    title: "Kế hoạch chuyển đổi dữ liệu",
    spaceId: mockIds.spaces.engineering,
    createdAt: "2026-09-23T08:10:00.000Z",
    updatedAt: "2026-10-11T08:30:00.000Z",
    editorId: ERP_PEOPLE.MQ.id,
    blocks: [
      "Kế hoạch chuyển đổi dữ liệu",
      "Quy tắc chuyển 48.200 hồ sơ khách hàng từ phần mềm bán hàng cũ sang ERP.",
      "## Quy tắc gộp bản ghi trùng",
      "Giữ nguyên mã khách hàng. Hai bản ghi cùng mã số thuế được gộp, lấy thông tin liên hệ mới nhất.",
      "## Kiểm tra sau khi chuyển",
      "Số khách hàng trên ERP khớp danh sách đã gộp, lệch không quá 0,1%. Công nợ đầu kỳ của 200 khách lớn nhất khớp sổ Kế toán.",
    ],
  },
  {
    id: "mock-doc-bank-reconciliation",
    title: "Hướng dẫn đối chiếu ngân hàng",
    spaceId: mockIds.spaces.accounting,
    parentId: INVOICE_FOLDER_ID,
    createdAt: "2026-09-02T04:00:00.000Z",
    updatedAt: "2026-10-09T08:15:00.000Z",
    editorId: mockIds.users.commenter,
    blocks: [
      "Hướng dẫn đối chiếu ngân hàng",
      "Các bước đối chiếu sao kê ngân hàng với sổ quỹ vào cuối mỗi tháng.",
      "## Chuẩn bị",
      "Tải sao kê của tất cả tài khoản trong tháng và khoá sổ quỹ đến hết ngày cuối tháng.",
      "## Đối chiếu",
      "Khớp từng khoản thu chi theo ngày và số tiền. Khoản chưa khớp được ghi vào bảng chênh lệch để xử lý trong 2 ngày làm việc.",
    ],
  },
  {
    id: "mock-doc-oncall-handbook",
    title: "Sổ tay trực hệ thống",
    spaceId: mockIds.spaces.engineering,
    createdAt: "2026-08-20T03:00:00.000Z",
    updatedAt: "2026-10-03T02:00:00.000Z",
    editorId: ERP_PEOPLE.VT.id,
    blocks: [
      "Sổ tay trực hệ thống",
      "Lịch trực và các bước xử lý sự cố của phòng Công nghệ.",
      "## Lịch trực",
      "Mỗi tuần một kỹ sư trực chính và một kỹ sư dự phòng; đổi ca báo trước 24 giờ.",
      "## Xử lý sự cố",
      "Ghi nhận sự cố, xác định mức ảnh hưởng, báo các phòng bị ảnh hưởng và cập nhật tiến độ mỗi 30 phút.",
    ],
  },
];

/** Titles of the primary workspace's other generic Boards, in order. */
export const SECONDARY_BOARD_TITLES: readonly string[] = [
  "Sự kiện khách hàng Q4",
];
