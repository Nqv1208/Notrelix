# Notrelix Frontend

**Enterprise Workspace Giao diện khách hàng của Notrelix**

Dự án frontend của Notrelix được xây dựng bằng **Next.js 16 (App Router)** và **TypeScript**, tuân thủ nguyên lý **Feature-Sliced Design (FSD)** để đảm bảo khả năng mở rộng quy mô doanh nghiệp và dễ dàng bảo trì.

---

## 1. Stack Công nghệ Cốt lõi

*   **Framework:** Next.js 16 (App Router) & React 19
*   **Quản lý State:** TanStack Query v5 (Server State) & Zustand (Global Client UI State)
*   **Styling & Design System:** Tailwind CSS v4 & shadcn/ui + Base UI
*   **Xử lý Form:** React Hook Form & Zod
*   **Quản lý Gói & Runner:** Bun

---

## 2. Kiến trúc Thư mục Chuẩn (FSD-Adjacent)

Hệ thống được tổ chức thành 4 phân tầng có ranh giới rõ ràng:

```txt
frontend/
├── app/                  # Routing, Layouts & Screen Composition (không chứa business logic)
│   ├── (app)/            # Public marketing / Landing routes
│   ├── (auth)/           # Trang đăng nhập, đăng ký (/sign-in, /sign-up)
│   ├── (dashboard)/      # Shell xác thực chung (/home)
│   ├── (workspace)/      # Ngữ cảnh workspace (/[workspaceId])
│   └── invite/[token]/   # Route chấp nhận lời mời (public)
│
├── features/             # Các lát cắt tính năng (Vertical Bounded Slices)
│   ├── auth/             # Xác thực & Phiên làm việc
│   ├── account/          # Thông tin cá nhân & Thiết lập tài khoản
│   ├── workspace/        # switcher, thành viên, cài đặt workspace, lời mời
│   ├── work-management/  # Boards, danh sách, cards, tùy chỉnh trường, checklists
│   ├── docs/             # Tài liệu dạng block (Notion-like editor), trang con
│   ├── collaboration/    # Bình luận, nhắc tên, cảm xúc, tệp đính kèm
│   └── notifications/    # Hộp thông báo, đếm chưa đọc, realtime stream
│
├── components/           # UI chung, hoàn toàn không biết nghiệp vụ (Generic UI Only)
│   ├── ui/               # Primitives của Design System (nút, input, dialog)
│   ├── layout/           # Shell, thanh bên, thanh đầu trang dùng chung
│   └── feedback/         # Trạng thái trống (empty), tải (loading), lỗi (error)
│
└── lib/                  # Hạ tầng kỹ thuật của Frontend (Technical Infrastructure)
    ├── api/              # Axios client với xử lý Token Refresh Lock chống bão request
    ├── query/            # Centralized QueryClient & Query Key Factory
    ├── routes/           # Centralized Route Registry (Không hardcode URL)
    └── permissions/      # Trình đánh giá quyền hạn trên UI (useCan hook)
```

Để biết thêm chi tiết về ranh giới import giữa các tầng, quy tắc định tuyến (Routing vs View) và quy định thiết kế, vui lòng đọc kỹ **[ARCHITECTURE.md](./ARCHITECTURE.md)**.

---

## 3. Khởi động Nhanh

### Yêu cầu hệ thống
*   Đã cài đặt **Bun** (v1.x trở lên)

### Cài đặt dependencies
```bash
bun install
```

### Chạy môi trường phát triển (Development)
```bash
bun run dev
```
Mở [http://localhost:3000](http://localhost:3000) trên trình duyệt của bạn.

### Cấu hình biến môi trường
Tạo file `.env.local` ở thư mục root của `frontend/`:
```env
NEXT_PUBLIC_API_URL=/api/v1
```
*Lưu ý: Để tránh vấn đề CORS và đồng bộ token, trình duyệt luôn gọi same-origin thông qua proxy rewrite của Next.js.*

---

## 4. Quy trình Đảm bảo Chất lượng (Quality Gates)

Trước khi gửi Pull Request hoặc tích hợp code mới, hãy đảm bảo các lệnh kiểm tra chất lượng sau đây đều vượt qua thành công:

```bash
# 1. Kiểm tra kiểu dữ liệu (Type Check)
bun run type-check

# 2. Kiểm tra định dạng code (Linter)
bun run lint

# 3. Chạy toàn bộ kiểm thử (Unit/Integration Tests)
bun run test

# 4. Chạy kiểm tra chất lượng tổng hợp (Quality Gate)
bun run quality

# 5. Build thử nghiệm sản phẩm
bun run build
```

---

## 5. Quy tắc Phát triển cho Nhà phát triển & AI Agents

1.  **Chân lý nghiệp vụ ở Feature:** Không viết API call, DTO mapper, hay business rules trực tiếp trong các trang của `app/`. Hãy đưa vào thư mục `features/` tương ứng.
2.  **Ranh giới của Components/ui:** Các component trong `components/ui` tuyệt đối không được phép import bất kỳ thứ gì từ `features/`. Chúng phải là các primitive thuần túy.
3.  **Không hardcode URL:** Luôn luôn sử dụng `routes` registry được định nghĩa tại `lib/routes/routes.ts` để sinh liên kết điều hướng.
4.  **Xác thực quyền hạn:** Sử dụng hook `useCan` trong `lib/permissions/use-can.ts` thay vì kiểm tra trực tiếp thuộc tính `role` của người dùng trên UI.
