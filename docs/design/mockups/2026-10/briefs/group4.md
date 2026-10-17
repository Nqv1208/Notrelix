# Nhóm 4 — Tự động hoá, báo cáo, thanh toán, tích hợp (2026-10-12)

Read with: `review-decisions.md` (world facts, people, editing rules — all apply), `group4-rules.md` (product rules digest; cite rule IDs in small captions only where they explain a visible behaviour), and for mobile `mobile-brief.md`.
Every number, name and date below is canonical for this batch. Do not invent other counts; if you need an extra fact, keep it small and list it in your report under "Assumed".

## Global decisions for this batch

- Language: Vietnamese, plain and short. Status words for items: Cần làm, Đang làm, Chờ duyệt, Hoàn thành.
- "Not entitled" ≠ "not authorized" (billing §100–101). Upgrade prompts appear only for entitlement gaps, never for permission gaps. A permission gap names who can help ("Nhờ quản trị workspace…"), an entitlement gap names who manages billing.
- Never show secrets, tokens, webhook secrets, raw card numbers, stack traces or raw provider payloads. A secret field shows "Đã lưu · tạo 02/09/2026" + "Tạo lại", never the value.
- Every live status has a durable source: show "Cập nhật lúc …" and a "Tải lại" affordance where a status could be stale. Never an endless spinner.
- Every number states unit, period and freshness (ANA-027): e.g. "Cập nhật 10:38 · số liệu đến 10:37 (GMT+7)".
- Unreadable = NotFound (GOV-025): a widget/report that covers resources the viewer cannot read excludes them and says only "Không tính 1 board bạn không có quyền xem" — never names the board.
- Automation runs "với quyền của" one named person (this is the mockups' chosen model; the docs leave it open — keep it visible on every rule and every run). If that person loses access, the rule pauses; it never silently switches principal.
- Billing is Account-scoped (BIL-007): the commercial subscription belongs to the company account "Tập đoàn Minh An" and covers its two workspaces (Minh An Group 86 thành viên, Minh An Đà Nẵng 18 thành viên). Workspace settings show this workspace's entitlements/usage and link to account billing for people allowed to manage it. Billing admin is separately authorized (BIL-028).
- Downgrade/past-due/over-limit never delete work (BIL-011/012, INT-030): they block new creation, make excess read-only or pause premium features, and say exactly what.

## Shared data

### People (as in review-decisions.md)
Hà (HA #4E8574, Quản trị workspace, current user). Lê Hoàng Phúc (HP #B4500B, Quản trị Phòng Công nghệ). Trần Minh Quân (MQ). Phan Ngọc Anh (NA #7C4DDB). Vũ Thanh (VT #0E7490). Bùi Thanh Tâm (TT #475569). Đỗ Khánh Linh (KL #9A6A12, Quản trị Phòng Kế toán). Hồ Quang Minh (HM #57534E, left Phòng Công nghệ 08/10). Trịnh Quốc Hưng (Owner, Tổng giám đốc). Copy avatar colours for MQ etc. from Final-Board.dc.html.

### ERP board "Triển khai ERP Q4" (Phòng Công nghệ) — for dashboards
16 items. Status: Cần làm 6 · Đang làm 3 · Chờ duyệt 2 · Hoàn thành 5 → 5/16 = 31% hoàn thành. Quá hạn 1 (ERP-142, Quá hạn 2 ngày). Priority (all 16): Khẩn 1 · Cao 5 · Trung bình 7 · Thấp 3. Go-live 01/11/2026.
Open items by assignee (11): Trần Minh Quân 2 (ERP-142, ERP-138; 1 quá hạn) · Phan Ngọc Anh 2 (ERP-151, ERP-140) · Vũ Thanh 2 (ERP-146, ERP-150) · Lê Hoàng Phúc 1 (ERP-144) · Bùi Thanh Tâm 1 (ERP-147) · Đỗ Khánh Linh 1 (ERP-149) · Nguyễn Thu Hà 1 (ERP-152) · Chưa giao 1 (ERP-153).
Open items by Phân hệ: Kế toán 5 (151, 147, 142, 144, 140) · Kho 2 (150, 146) · Mua hàng 2 (149, 153) · Chưa đặt 2 (152, 138).
Completed per week (Thứ Hai–Chủ nhật): 21–27/09: 2 (ERP-133 "Dựng môi trường thử nghiệm ERP", ERP-134 "Khảo sát quy trình công nợ hiện tại") · 28/09–04/10: 2 (ERP-129, ERP-131) · 05–11/10: 1 (ERP-136) · 12/10 đến nay: 0. Average last 3 weeks 1,7 việc/tuần; 11 việc còn lại trong 3 tuần tới go-live needs ~3,7 việc/tuần → the dashboard flags "Có nguy cơ trễ go-live" (as an observation with its basis, not a prediction engine).
Due in the next 7 days (13–19/10): ERP-138 14/10, ERP-140 15/10, ERP-146 16/10, ERP-151 18/10.

### Dashboards Hà can see
1. "Tổng quan triển khai ERP Q4" — Phòng Công nghệ, created by Lê Hoàng Phúc, shared with Phòng Công nghệ + team Ban giám đốc; Hà can edit (Có thể sửa). Source: board Triển khai ERP Q4. Main dashboard.
2. "Đóng sổ tháng 10" — Phòng Kế toán, by Đỗ Khánh Linh, Hà can view.
3. "Báo cáo tuần Ban giám đốc" — Ban giám đốc, by Trịnh Quốc Hưng, shared with Hà; sources 6 boards across departments, 1 of which Hà cannot read → widgets show "Không tính 1 board bạn không có quyền xem".
Freshness: "Cập nhật 10:38 · số liệu đến 10:37 (GMT+7)". Time basis: tuần bắt đầu Thứ Hai, múi giờ (GMT+7) Hà Nội — shown once in the dashboard header info.

### Automation — board "Triển khai ERP Q4" (fixes Final-Settings: there are 3 running rules + 1 paused)
Running (Đang chạy 3):
- R1 "Gửi duyệt khi chuyển sang Chờ duyệt" — Khi Trạng thái chuyển sang Chờ duyệt → thì Giao Nguyễn Thu Hà duyệt · Gửi thông báo cho người duyệt. Chạy với quyền của Lê Hoàng Phúc. 3 lần trong 7 ngày; lần cuối 10:28 hôm nay (ERP-138), thành công. Phiên bản 3, sửa lần cuối 02/10 bởi Phúc.
- R2 "Nhắc việc quá hạn" — Khi (lịch) 09:00 hằng ngày, múi giờ (GMT+7) Hà Nội → nếu Hạn chót đã qua và Trạng thái không phải Hoàn thành → thì Nhắc người phụ trách. Bỏ lỡ giờ chạy: chạy bù một lần. Quyền của Phúc. 7 lần trong 7 ngày; lần cuối 09:00 hôm nay, thành công (nhắc Trần Minh Quân về ERP-142).
- R4 "Khẩn → báo Trưởng phòng" — Khi Mức ưu tiên đổi thành Khẩn → thì Gửi thông báo cho Lê Hoàng Phúc · Đăng tin vào kênh Microsoft Teams "ERP – Điều hành". Quyền của Phúc. 1 lần trong 7 ngày: Thứ Bảy 10/10 18:05 on ERP-142 — **lỗi một phần**: bước 1 thành công, bước 2 thất bại "Kết nối Microsoft Teams cần uỷ quyền lại" (token hết hạn 10/10 02:00). After reconnect, "Chạy lại bước lỗi" re-runs only step 2 (AUT-014/018); step 1 is not repeated.
Paused (Tạm dừng 1):
- R3 "Gắn nhóm Dữ liệu cho việc kho" — Khi Có việc mới → nếu Phân hệ ERP là Kho → thì Chuyển vào nhóm Dữ liệu. Quyền của Hồ Quang Minh, who left Phòng Công nghệ → tạm dừng từ 08/10; lần cuối chạy 07/10 thành công. Fix: Đổi người chạy rồi bật lại.
Draft example (for the editor): "Báo kế toán khi việc Kế toán hoàn thành" — Khi Trạng thái chuyển sang Hoàn thành → nếu Phân hệ ERP là Kế toán → thì Gửi thông báo cho team Nhóm Kế toán tổng hợp. Draft = can be incomplete; enabling validates server-side (AUT-002).
Invalid example: a copy of R4 whose step 1 writes today into date field "Ngày bàn giao", archived 02/10 by Trần Minh Quân (as in Final-Settings) → "Quy tắc không bật được: trường Ngày bàn giao đã được lưu trữ" with Khôi phục trường / Chọn trường khác (AUT-034).
Run log (Nhật ký chạy, newest first, 9 rows): 10:28 R1 ERP-138 Thành công · 09:00 hôm nay R2 (1 việc khớp: ERP-142) Thành công · Chủ nhật 11/10 09:00 R2 (ERP-142) Thành công · Thứ Bảy 10/10 18:05 R4 ERP-142 Lỗi một phần · 10/10 09:00 R2 Bỏ qua · không khớp (ERP-142 đến hạn 10/10, chưa quá hạn) · 09/10 15:12 R1 ERP-140 Thành công · 09/10 09:00 R2 Bỏ qua · không khớp · 08/10 11:20 R3 Bỏ qua · không chạy vì người chạy mất quyền · 08/10 09:00 R2 Bỏ qua · không khớp. Tabs: Tất cả 9 · Thành công 4 · Lỗi 1 · Bỏ qua 4. "Bỏ qua · không khớp điều kiện" is not an error (AUT-011).
Plan usage: lượt chạy tự động tháng 10 của workspace: 3.880 (account total 4.210 / 25.000).

### Integrations — workspace Minh An Group
Connected:
- Microsoft Teams — phạm vi Workspace, kết nối bởi Lê Hoàng Phúc 14/08/2026. Trạng thái kết nối: **Cần uỷ quyền lại** (hết hạn 10/10 02:00). Đồng bộ: tạm dừng chờ uỷ quyền, 1 thao tác đang chờ (bước 2 của R4). Used by: quy tắc R4 · thông báo kênh cho 2 board.
- Microsoft OneDrive & SharePoint — phạm vi Workspace, by Hà 20/08. Đang hoạt động; đồng bộ ổn định; 214 tệp đính kèm liên kết; cập nhật 10:31.
- Email vào board — phạm vi board "Hỗ trợ nội bộ" (resource-scoped), địa chỉ hotro-noibo@in.notrelix.vn, 38 email → việc trong 30 ngày, lần cuối 09:52.
- Webhook gửi đi "Trạng thái việc sang ERP cũ" — phạm vi board Triển khai ERP Q4, đích https://erp-cu.minhan.vn/hooks/notrelix, sự kiện: Trạng thái thay đổi. Đang lỗi: 6 lần gửi gần nhất trả về 503 từ 09:40; đang thử lại có giãn cách, lần thử tới 10:45; 6 sự kiện đang chờ, không mất. Secret: "Đã lưu · tạo 02/09/2026 · Tạo lại".
Catalog (not connected): Slack, Google Drive, Google Calendar, GitHub, Zalo OA, Power BI (gói Enterprise — entitlement gap example).
Connect flow example: Google Drive — choose scope (Workspace / chỉ một board), provider consent, then Notrelix permission check (INT-004): Hà is workspace Admin so allowed at Workspace scope.
Disconnect example (Microsoft OneDrive & SharePoint): states what remains (214 liên kết tệp giữ lại dạng liên kết, không xoá tệp ở OneDrive; quy tắc dùng kết nối: 0), what stops, and that copies in OneDrive are managed by the provider (INT-017/034).

### Billing — account "Tập đoàn Minh An"
- Gói Business, thanh toán theo năm, kỳ 01/01/2026 – 31/12/2026, tự gia hạn 01/01/2027. Đang hoạt động.
- Price: Business 159.000 đ / người / tháng (trả theo năm). Pro 89.000 đ. Miễn phí 0 đ (tối đa 10 người). Enterprise: Liên hệ.
- Chỗ (seats): mua 110, dùng 104 (Minh An Group 86 · Minh An Đà Nẵng 18). Khách không tính chỗ, tối đa 50 khách (đang có 2 ở Minh An Group).
- Lưu trữ: 312 GB / 1 TB dùng chung. Lượt chạy tự động: 4.210 / 25.000 tháng 10 (Minh An Group 3.880 · Minh An Đà Nẵng 330). Tích hợp: Không giới hạn. Báo cáo nâng cao: Có.
- Enterprise-only examples: Power BI connector, SCIM, lưu nhật ký kiểm toán 7 năm.
- Billing managers: Trịnh Quốc Hưng (Owner) and Nguyễn Thu Hà (được giao Quản lý thanh toán 03/2026). Lê Hoàng Phúc is not → view-only state.
- Payment: chuyển khoản theo hoá đơn (default for annual) + thẻ Visa •••• 4821 hết hạn 08/2028 for add-on seats. Hoá đơn điện tử VAT gửi tới ketoan@minhan.vn; MST 0101234567; Công ty CP Tập đoàn Minh An, 12 Láng Hạ, Ba Đình, Hà Nội.
- Invoices: HĐ-2026-0001 01/01/2026 Gói Business 100 chỗ × 12 tháng = 190.800.000 đ, Đã thanh toán · HĐ-2026-0047 05/06/2026 Thêm 6 chỗ (06–12/2026, tính theo ngày) Đã thanh toán · HĐ-2026-0112 05/09/2026 Thêm 4 chỗ (09–12/2026) Đã thanh toán. Amounts excl./incl. VAT 8% shown on detail only.
- Past-due scenario (state only): HĐ-2026-0131 thêm 4 chỗ ngày 28/09, hạn 05/10 chưa thanh toán (thẻ bị từ chối) → "Quá hạn thanh toán", gia hạn xử lý đến 19/10; meanwhile cannot add new seats; nothing else changes; after 19/10 new members cannot be added until paid; data never removed. (In this state seats bought = 110 still counts the 4.)
- Downgrade-to-Pro impact (state only): Pro = 5.000 lượt chạy/tháng, không có báo cáo nâng cao, không có webhook gửi đi, lưu trữ 250 GB → effective at end of period 31/12/2026; 3 dashboards become chỉ xem; webhook "Trạng thái việc sang ERP cũ" tạm dừng; storage 312 GB > 250 GB → không tải tệp mới cho tới khi dưới mức; nothing deleted.
- Seat limit reached scenario: inviting 7 more people when 6 seats free → "Còn 6 chỗ. Mua thêm chỗ hoặc bớt 1 người trong lời mời."

## Screens to build (one agent per file)

| File | Shell/template to copy | Artboard | Notes |
|---|---|---|---|
| Final-Automation.dc.html | Final-Settings.dc.html (board settings shell, nav item Tự động hoá active, count 3) | 1440×900 | rule editor + run log |
| Final-Reports.dc.html | Final-Space.dc.html (main shell + canonical sidebar; add nav item "Báo cáo" after "Tài liệu", active) | 1440×900 | dashboards |
| Final-Billing.dc.html | Final-Teams.dc.html (workspace settings shell; nav "Gói và thanh toán" active; add nav "Tích hợp" after "Chia sẻ và bảo mật") | 1440×1000 | plan, usage, invoices |
| Final-Integrations.dc.html | Final-Teams.dc.html (same; nav "Tích hợp" active) | 1440×1000 | connections |
| Final-M-Reports.dc.html | Final-M-Account.dc.html / Final-M-Home.dc.html mobile shell | 390×844 | read dashboards |
| Final-M-Automation.dc.html | same mobile shell | 390×844 | rules list, run log, failed run |

Links: Settings nav "Tự động hoá" → Final-Automation.dc.html; workspace nav "Gói và thanh toán" → Final-Billing.dc.html, "Tích hợp" → Final-Integrations.dc.html, other workspace nav items keep the template's targets; sidebar "Báo cáo" → Final-Reports.dc.html; ERP-142/ERP-138 → Final-Detail.dc.html; other items → Final-Board.dc.html; failed Teams step → Final-Integrations.dc.html; "Nâng cấp"/"Xem gói" → Final-Billing.dc.html. Mobile: back chevrons to Final-M-Home.dc.html / Final-M-Space.dc.html; ERP-142 → Final-M-Detail.dc.html.
