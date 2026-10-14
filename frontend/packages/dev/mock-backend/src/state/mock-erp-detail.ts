/**
 * Descriptions and comments of the ERP board's items, as the Detail mockup
 * (docs/design/mockups/2026-10/src/Final-Detail.dc.html) shows ERP-142. Pure
 * data; `MockStore.seedErpBoard` inserts it. Times are UTC instants of the
 * Hanoi clock the mockups use (Monday 12/10/2026 10:40).
 */
import { ERP_COMMENTER, ERP_PEOPLE } from "./mock-erp-board";

/** Item description (markdown) by item key; items without one stay empty. */
export const ERP_DESCRIPTIONS: Readonly<Record<string, string>> = {
  "ERP-142": [
    "Chuyển 48.200 hồ sơ khách hàng từ phần mềm bán hàng cũ. Giữ nguyên mã khách hàng, gộp bản ghi trùng theo mã số thuế, đối chiếu công nợ đầu kỳ với phòng Kế toán trước khi chốt. Quy tắc gộp chi tiết ở Kế hoạch chuyển đổi dữ liệu.",
    "Xong khi:",
    [
      "- Số khách hàng trên ERP khớp danh sách đã gộp, lệch không quá 0,1%.",
      "- Công nợ đầu kỳ của 200 khách lớn nhất khớp sổ Kế toán.",
      "- Chị Linh (Kế toán) ký xác nhận trên biên bản đối chiếu.",
    ].join("\n"),
  ].join("\n\n"),
  "ERP-144":
    "Chạy thử phân hệ công nợ với số liệu thực tháng 9: phát sinh, đối trừ và báo cáo tuổi nợ. Ghi lại mọi chênh lệch so với sổ Kế toán để xử lý trước khi chốt.",
  "ERP-146":
    "Đồng bộ số tồn giữa kho Bình Dương và kho Long An theo giờ, báo khi hai bên lệch quá 1% số lượng.",
  "ERP-138":
    "Thiết lập đăng nhập một lần qua Microsoft Entra cho toàn công ty, giữ lại tài khoản quản trị cục bộ để dự phòng.",
  "ERP-151":
    "Phân quyền truy cập phân hệ Kế toán theo phòng ban: Kế toán xem và sửa, các phòng khác chỉ xem báo cáo.",
  "ERP-149":
    "Viết hướng dẫn từng bước nhập hoá đơn đầu vào trên ERP, kèm ảnh chụp màn hình, cho nhân viên Kế toán.",
};

export interface ErpMockComment {
  readonly itemKey: string;
  readonly userId: string;
  readonly contentMd: string;
  readonly createdAt: string;
}

/** Comments in time order; the Detail mockup shows the three of ERP-142. */
export const ERP_COMMENTS: readonly ErpMockComment[] = [
  {
    itemKey: "ERP-138",
    userId: ERP_PEOPLE.MQ.id,
    contentMd:
      "Em chọn Microsoft Entra ID vì công ty đang dùng Microsoft 365, không phải mua thêm hệ thống đăng nhập.",
    createdAt: "2026-10-09T03:20:00.000Z",
  },
  {
    itemKey: "ERP-138",
    userId: ERP_PEOPLE.VT.id,
    contentMd:
      "Bên hạ tầng đã mở cổng đồng bộ danh bạ, em thử với phòng Kế toán trước.",
    createdAt: "2026-10-10T08:05:00.000Z",
  },
  {
    itemKey: "ERP-138",
    userId: ERP_PEOPLE.HA.id,
    contentMd:
      "Giữ lại tài khoản quản trị cục bộ để dự phòng, đừng tắt hẳn mật khẩu trong tuần đầu.",
    createdAt: "2026-10-11T02:30:00.000Z",
  },
  {
    itemKey: "ERP-138",
    userId: ERP_PEOPLE.MQ.id,
    contentMd:
      "Đã thử với 5 tài khoản phòng Kế toán, đăng nhập ổn. Chị duyệt giúp trước thứ Tư để kịp thông báo toàn công ty nhé.",
    createdAt: "2026-10-12T03:28:00.000Z",
  },
  {
    itemKey: "ERP-142",
    userId: ERP_COMMENTER.id,
    contentMd:
      "Bên mình chốt xong số dư công nợ đầu kỳ rồi, sáng mai mình gửi file để đối chiếu nhé @Trần Minh Quân.",
    createdAt: "2026-10-11T09:40:00.000Z",
  },
  {
    itemKey: "ERP-142",
    userId: ERP_PEOPLE.MQ.id,
    contentMd:
      "Đã gộp xong bản ghi trùng. Nhận được file của chị Linh rồi, em đối chiếu xong trong hôm nay, dự kiến nhập chính thức thứ Tư.",
    createdAt: "2026-10-12T03:05:00.000Z",
  },
  {
    itemKey: "ERP-144",
    userId: ERP_PEOPLE.HP.id,
    contentMd:
      "Số liệu tháng 9 đã nạp xong, mình chạy đối trừ công nợ chiều nay.",
    createdAt: "2026-10-12T02:15:00.000Z",
  },
];
