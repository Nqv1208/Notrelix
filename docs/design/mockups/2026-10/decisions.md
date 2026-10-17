# Mockup review — shared decisions (2026-10-12)

These decisions apply to every Final-*.dc.html file. Apply them only where your file contains the affected content.

## World facts (canonical)

- Today: Monday 12/10/2026, clock 10:40. Dates in board views: "18/10" style (dd/mm), never "18 thg 10".
- Current user: Nguyễn Thu Hà (HA, #4E8574). She is **workspace Admin** (that is why she sees the Private departments KT/NS/GĐ in the sidebar and could create the Pháp chế department). Wherever her user block subtitle reads "Trưởng nhóm triển khai", change it to "Quản trị workspace".
- Workspace Admin does NOT reach a Private board unless added to it explicitly; only the workspace Owner always passes (GOV-025/GOV-028). Hà is explicitly added to Ngân sách CNTT 2027 and to the ERP board's own list.
- People spellings: "Phan Ngọc Anh" (NA, #7C4DDB) — replace every "Phạm Ngọc Anh". "Vũ Thanh" (VT, #0E7490) — replace every "Vũ Đức Thắng". Bùi Thanh Tâm keeps initials TT but uses colour #475569 (Lý Thu Trang keeps #BE185D).
- ERP-138 title everywhere: "Thiết lập đăng nhập một lần (SSO) cho toàn công ty". Sent for Hà's approval 12 minutes ago (10:28); "Cập nhật 12 phút trước". Approval due Thứ Tư 14/10.
- ERP-142 "Chuyển dữ liệu khách hàng sang ERP", assignee Trần Minh Quân, overdue; Hà follows it. Full page = Final-Detail.dc.html. Other items have no own page: link them to Final-Board.dc.html (the Kanban with the side sheet).
- ERP board lanes (Kanban order is canonical: Cần làm, Đang làm, Chờ duyệt, Hoàn thành): Cần làm 6 (ERP-147, 149, 150, 151, 152, 153), Đang làm 3, Chờ duyệt 2, Hoàn thành 5. Where a lane shows fewer cards than its count, add a "Xem thêm N việc" button at the lane bottom.
- Inbox counts: "Cần bạn phản hồi 2" = 1 approval (ERP-138) + 1 mention. Mentions count is 1 everywhere (Home chip "1 nhắc tên bạn", Inbox tab "Nhắc tên 1").
- Phòng Công nghệ (CN #1E6FD0) has 24 people with access = 2 quản trị (Phúc, Hà) · 19 thành viên · 3 người xem (team Ban giám đốc), plus 1 pending invitation that is not counted. Team Nhóm Kỹ sư ERP has 4 people (Quân + 3), counted once. Members list shows 11 distinct people (Phúc, Hà, Quân, VT, TT, 3 other ERP engineers, 3 BGĐ) so the expander reads "Hiện 13 người khác". Avatar stacks: 3 avatars + "+21" or 4 + "+20".
- "Khách" (guest) means the workspace role guest only (GOV-030). The ERP board "Triển khai ERP Q4" has exactly 1 guest: Đặng Minh Tuấn (tuan@doitac-erp.vn, ngoài công ty, Có thể sửa, hết quyền 31/01/2027). Access chip: "Phòng Công nghệ · có 1 khách"; stack aria-label: "32 người vào được board: 24 người Phòng Công nghệ, 6 người Nhóm Kế toán tổng hợp, Đỗ Khánh Linh và 1 khách". Any "khách ở board" list shows only Tuấn.
- Đỗ Khánh Linh is NOT a guest: she is a workspace Member, Quản trị Phòng Kế toán (author of the Kế toán page in Final-Docs). She was added explicitly to the ERP board by Lê Hoàng Phúc with Có thể bình luận, expiring 31/12/2026 (any explicit entry may expire, GOV-031). Her label: "Phòng Kế toán · quyền bình luận"; in Chia sẻ: "Phòng Kế toán · ngoài phòng Công nghệ · hết quyền 31/12/2026", no Khách badge. Her own sidebar shows Kế toán (+ public Marketing) and a group "Được chia sẻ với bạn" with the ERP board.
- "Tân Lộc Logistics (khách)" in the system rail is Hà's workspace-level guest membership elsewhere and stays. Workspace guests in Minh An Group: Tuấn and Lâm Gia Bảo (Final-Teams "2 khách đang có").
- Switching the ERP board to Riêng tư: 22 people of the department lose access (24 − Phúc − Hà, who are added explicitly); of them 3 have unfinished items on THIS board: Trần Minh Quân "2 việc chưa xong, 1 quá hạn", Phan Ngọc Anh "1 việc chưa xong", Vũ Thanh "1 việc chưa xong".
- Department colours: CN #1E6FD0, KT #9A6A12, MK #C2410C, NS #4E8574, GĐ #C8143F, PC #5B5BD6. Do not use initials "KT" for a person.

## Product rules now in docs/product (cite in captions when useful)

- GOV-025: unreadable = NotFound, identical to non-existent. Space roles (even Space Admin) never reach a Private board.
- GOV-030: a Guest is capped at Editor (Manager disabled with the reason).
- GOV-031: an explicit entry may expire; managers see expired entries; renewing is explicit.
- GOV-032: workspace Owner/Admin can turn share links off; "copy link" is only a locator.
- WSP-024/026: Space members are existing workspace members and Teams only. Outsiders come in by workspace invitation (Owner/Admin; may pre-assign Spaces/Teams, applied on acceptance) or as a guest of one board (board Manager). A pending workspace invitation can show in a Space list as "Chờ nhận lời mời vào workspace" with Gửi lại / Thu hồi.
- WM-039: links are waits-on/blocks/related; waits-on is acyclic. WM-040: only people who can view the board can be assigned; losing access later does not unassign. WM-041: progress of an item with children = done children / children, read-only.
- COL-030: mentioning someone who cannot view the item notifies nobody and grants nothing.
- Last Space Admin cannot be removed or downgraded (WSP-024). Managing Teams = workspace Owner/Admin (GOV-028).

## Editing rules for the .dc.html files

- Keep the `<x-dc>` / `<helmet>` / `{{hole}}` / `<sc-if value=...>` / `<sc-for list=... as=...>` / `class Component extends DCLogic { renderVals() }` structure intact. Never put `<sc-for>` inside `<table>`.
- New tweak states: extend the existing `data-props` option list for `state` (keep the existing values and default), derive booleans in `renderVals()`, and gate markup with `<sc-if value="{{flag}}">`. Do not change the artboard width/height.
- Visual language: reuse the file's existing classes and CSS tokens; no side-stripe borders (border-left/right > 1px as accent), no gradient text, no new fonts.
- Vietnamese copy, plain and short. No numbered section eyebrows.
- Links between screens are plain `<a href="Final-X.dc.html">`. Leave `href="#"` only where no target screen exists.
- One sidebar everywhere (except Final-Share's dimmed background and Final-States): the Final-Space workspace sidebar + Notrelix rail with the `nav` tweak (an / hien-de / hien-day), plus "Thiết lập workspace" (→ Final-Teams) above Hà's user block. Its CSS is the scoped block "/* canonical workspace sidebar … */" appended to each file (tokens pinned inside `.side`). Docs folds its document tree under "Tài liệu" (Kế toán › Quy trình › current page). Linh's variant (Board/Detail `chi-xem`): Phòng ban Kế toán + Marketing, group "Được chia sẻ với bạn" with the ERP board.
- Accessibility quick wins: icon-only controls get `aria-label`; clickable things are `<a>` or `<button>`; menus use role="menu"/"menuitem" on buttons; dialogs role="dialog" aria-modal="true" aria-labelledby.

## Addendum (group 2 data)

- ERP-142 canonical full title: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP" (as in Board/Table/Detail). Short labels in tight chips may truncate by CSS, never by rewording. Same for every item: use the Board/Table title verbatim (ERP-146 "Đồng bộ tồn kho giữa kho Bình Dương và kho Long An", ERP-151 "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán", ERP-129 "Chốt danh mục tài khoản kế toán dùng chung", ERP-150 "Thiết lập cảnh báo khi đồng bộ tồn kho thất bại", ERP-147 "Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng").
- ERP-142: start 01/10, due 10/10 (overdue 2 days, "Quá hạn 2 ngày" — never "Trễ"), Phân hệ "Kế toán", Ước lượng "40 giờ", progress 3/5 = 60%.
- ERP-142 links (WM-039), canonical in Detail and drawn in Timeline: Đang chờ ERP-129 (Hoàn thành 30/09); Đang chặn ERP-146 (start 13/10) and ERP-147 (due 21/10); Liên quan ERP-151. Timeline finish-to-start arrows: 129→142, 142→146, 142→147, 146→150, 147→ĐT-KT (starts 22/10)→SONG→MỐC Go-live, so one unbroken critical path 129→142→147→ĐT-KT→SONG→Go-live.
- Go-live: 01/11/2026 everywhere (Settings "02/11" is wrong).
- Undated items: only ERP-153 has no date ("1 việc chưa có ngày").
- Phases without an ERP key (UAT, Đào tạo phòng Kế toán, Chạy song song, Go-live) carry a "Giai đoạn"/"Mốc" chip instead of a key.
- Status words everywhere: Cần làm, Đang làm, Chờ duyệt, Hoàn thành. Legends say "Hoàn thành", not "Xong".
- Board totals: 16 items. Priority distribution: Khẩn 1, Cao 5, Trung bình 7, Thấp 3.
- Đỗ Khánh Linh has Có thể bình luận on the ERP board (explicit entry, not a guest): she can attach files only inside a comment, and she may be assigned (WM-040) but cannot tick a sub-item done.

## Việc của tôi (My Work) — canonical data (2026-10-12)

"Việc của tôi 7" = Hà's 7 open items across boards she can see (GOV-029: only items she can open):

| Key | Title | Board (dept) | Why it's Hà's | Status | Due | Priority |
|---|---|---|---|---|---|---|
| ERP-142 | Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP | Triển khai ERP Q4 (CN) | Đang theo dõi (giao cho Trần Minh Quân) | Đang làm | 10/10 · Quá hạn 2 ngày | Khẩn |
| ERP-144 | Kiểm thử phân hệ công nợ với số liệu tháng 9 | Triển khai ERP Q4 (CN) | Đang theo dõi (giao cho Lê Hoàng Phúc) | Đang làm | Hôm nay | Cao |
| KT-88 | Đối chiếu sao kê ngân hàng tháng 9 | Đóng sổ tháng 10 (KT) | Giao cho tôi | Cần làm | Hôm nay 17:00 | Trung bình |
| ERP-138 | Thiết lập đăng nhập một lần (SSO) cho toàn công ty | Triển khai ERP Q4 (CN) | Cần tôi duyệt (gửi bởi Trần Minh Quân 10:28) | Chờ duyệt | Thứ Tư 14/10 | Cao |
| MK-21 | Góp ý nội dung trang giới thiệu sản phẩm mới | Ra mắt sản phẩm Q4 (MK) | Giao cho tôi | Cần làm | Thứ Sáu 16/10 | Trung bình |
| KT-95 | Xác nhận số dư tài khoản ngân hàng trên ERP tháng 10 | Đóng sổ tháng 10 (KT) | Giao cho tôi | Cần làm | Thứ Năm 22/10 | Trung bình |
| ERP-152 | Kế hoạch hỗ trợ người dùng tuần đầu vận hành | Triển khai ERP Q4 (CN) | Giao cho tôi | Cần làm | Thứ Sáu 30/10 | Thấp |

Tabs/filters: Tất cả 7 · Giao cho tôi 4 · Cần tôi duyệt 1 · Đang theo dõi 2 · Đã xong. Groups by due: Quá hạn 1 · Hôm nay 2 · Tuần này 2 · Sau đó 2 (matches Home: "Sau đó · 2 việc đến hạn từ tuần sau"). Done this week (Đã xong): KT-86 "Khoá sổ phụ ngân hàng tháng 9" (hoàn thành 10:05 hôm nay), ERP-136 "Chốt danh sách người dùng đợt 1" (hoàn thành Thứ Sáu 09/10), ERP-129 is NOT Hà's.
Department colours: KT #9A6A12, MK #C2410C, CN #1E6FD0. The view never shows items on boards Hà cannot open; Private boards she was added to are included.

## Nhóm 3 — Đăng nhập, onboarding, cài đặt tài khoản (2026-10-12)

Source of truth for account appearance = the shipped code (frontend/packages/features/account/src/web/appearance/account-appearance.copy.ts):
- Chế độ (radio): Sáng · Tối · Theo hệ thống · Tương phản cao.
- Màu nhấn (radio, 5 điểm màu của logo): Xanh Notrelix (mặc định, #1666CC) · San hô (#B5461D) · Than hồng (đỏ hồng đậm) · Xô thơm (#2D6A59) · Hổ phách. Status colours and department colours never change with the accent.
- Ngôn ngữ: Tiếng Việt · English. Thanh bên: "Thu gọn theo mặc định" switch.
- Account sections in code: Hồ sơ, Giao diện, Bảo mật, Thông báo (+ the mockup adds "Workspace của bạn").
- Workspace admins may set a default accent for the company (from the Themes board); the user's choice overrides it.

Identity/auth facts for mockups:
- Sign-in: email first; if the email domain belongs to a workspace with SSO (minhan.vn → Microsoft Entra), continue with SSO; otherwise password. Social sign-in: Google, Microsoft. 2-step verification code (6 digits, authenticator app) when enabled. Errors never reveal whether an email exists ("Email hoặc mật khẩu chưa đúng"); rate limit after 5 tries ("Thử lại sau 60 giây").
- Invitations (WSP-006/009/026): a pending invitation grants nothing until accepted; acceptance is retry-safe; an expired/revoked invitation shows a calm dead-end with "Nhờ người mời gửi lại". Example invite: Đặng Minh Tuấn (tuan@doitac-erp.vn) invited by Nguyễn Thu Hà as a guest of board "Triển khai ERP Q4" (Có thể sửa, hết quyền 31/01/2027) — after accepting he sees only that board (GOV-009).
- Logo gradient (red → blue, 5 points) is allowed only on the logo, sign-in screens and marketing — never in the work area.
