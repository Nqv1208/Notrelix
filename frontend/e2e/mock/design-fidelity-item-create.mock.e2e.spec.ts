import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The create popup against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), artboard
 * `Kanban-CardPopup-Create`, frame 1 ("Thẻ mới, còn trống"). Numbers are read from the artboard rendered in Chromium,
 * relative to the popup: 1120 x 772, 16 px radius, header 56, the item column 720 and the comments column 400 (the
 * comments column is off until the item exists: a 12 px rhythm, the dashed composer, the empty note), the footer 61 with
 * "Tạo tiếp thẻ khác" and Huỷ / Tạo thẻ. Opened from "Thêm công việc" of the toolbar, the lane is chosen for the person.
 *
 * Frame 2 (the filled draft) and the states frames (`Kanban-CardPopup-Create-States`: creating, a lost connection, created
 * in place, closing a draft) are measured text by text below; a scripted create ("notrelix-mock-create" in the browser's
 * storage, read by the mock) holds or fails the request. Kept deviations: the artboard's custom field is "Hạng mục = Đào
 * tạo", the mock board's own field stands for it; in the created frame the comments column and everything under the quick
 * row are the open card's own popup, measured with the card popup specs; the 320 px sheet is `Kanban-CardPopup-M-Create`.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";

// The filled draft is built by hand, and the first run compiles the app: give it room.
test.describe.configure({ timeout: 90_000 });

const near = (actual: number, want: number, what: string) =>
  expect
    .soft(Math.abs(actual - want), `${what}: ${actual.toFixed(1)} vs ${want}`)
    .toBeLessThanOrEqual(1);

async function rel(el: Locator, origin: Locator) {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

async function expectRel(
  el: Locator,
  origin: Locator,
  want: Partial<{ x: number; y: number; w: number; h: number }>,
  what: string,
) {
  const actual = await rel(el, origin);
  for (const k of ["x", "y", "w", "h"] as const)
    if (want[k] !== undefined) near(actual[k], want[k]!, `${what}.${k}`);
}

/** The filled draft of frame 2, built the way a person would. */
async function fillFrame2(page: Page, pop: Locator) {
  await pop
    .getByRole("textbox")
    .first()
    .fill("Chốt lịch đào tạo kế toán trưởng");
  // Labels: Đào tạo and Tài liệu.
  await pop.getByRole("button", { name: "Nhãn" }).first().click();
  await page
    .locator("[data-radix-popper-content-wrapper]")
    .last()
    .getByRole("checkbox")
    .nth(2)
    .click();
  await page
    .locator("[data-radix-popper-content-wrapper]")
    .last()
    .getByRole("checkbox")
    .nth(4)
    .click();
  await page.keyboard.press("Escape");
  // Assignee: Lê Hoàng Phúc (the row's plus).
  await pop.getByRole("button", { name: "Người làm" }).first().click();
  const phuc = page.getByText("Lê Hoàng Phúc").first();
  await phuc
    .locator("xpath=ancestor::*[.//button][1]")
    .locator("button")
    .last()
    .click()
    .catch(async () => {
      await phuc.click();
    });
  await page.keyboard.press("Escape");
  // Dates: 15/10 to 20/10.
  await pop.getByRole("button", { name: "Ngày" }).first().click();
  const dates = page.getByRole("checkbox");
  const due = page.locator("input[value$='/2026']").last();
  const start = page
    .locator("input[value$='/2026'], input[placeholder='dd/mm/yyyy']")
    .first();
  await dates.nth((await dates.count()) - 2).check();
  await due.fill("20/10/2026");
  await start.fill("15/10/2026");
  await page.getByRole("button", { name: "Lưu", exact: true }).click();
  // Priority: Cao.
  await pop.getByRole("button", { name: "Thêm", exact: true }).first().click();
  await page.getByRole("option", { name: "Ưu tiên", exact: true }).click();
  await page.waitForTimeout(500);
  await expect(page.getByText("Khẩn", { exact: true }).last()).toBeVisible();
  await page
    .getByText("Cao", { exact: true })
    .and(page.locator(":not(.sr-only)"))
    .last()
    .click();
  // Description.
  await pop.getByText("Thêm mô tả…").first().click();
  await page.keyboard.insertText(
    "Làm việc với kế toán trưởng Minh An Group để chọn hai buổi đào tạo phân hệ công nợ trước ngày chạy thử.\n\n- Buổi 1: nhập chứng từ và đối chiếu\n- Buổi 2: báo cáo công nợ\n\nGửi lịch cho các phòng ban liên quan.",
  );
  await page.getByRole("button", { name: "Lưu", exact: true }).click();
  await page.waitForTimeout(500);
  // A custom field (the board's own "Phân hệ ERP" stands for the artboard's "Hạng mục").
  await pop.getByRole("button", { name: "Thêm trường" }).click();
  await page.getByText("Phân hệ ERP").first().click();
  await page.waitForTimeout(300);
  await pop.getByRole("button", { name: "Chọn…" }).click();
  await page.getByText("Kế toán", { exact: true }).last().click();
  // A queued file and a queued link.
  await page.getByRole("button", { name: "Thêm đính kèm" }).click();
  await page
    .locator("input[type=file]")
    .first()
    .setInputFiles({
      name: "Lich-dao-tao-ke-toan.xlsx",
      mimeType: "application/vnd.ms-excel",
      buffer: Buffer.alloc(1153434),
    });
  await page.waitForTimeout(400);
  await page
    .getByPlaceholder(/Dán liên kết/)
    .fill("https://minhan.example/lich-phong-dao-tao");
  await page.getByLabel("Tên hiển thị").fill("Lịch phòng đào tạo tháng 10");
  await page.getByRole("button", { name: "Gắn", exact: true }).click();
  await page.waitForTimeout(500);
}

const CREATE_SCRIPT = "notrelix-mock-create";

async function openFilled(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(BOARD);
  await page
    .getByRole("button", { name: "Thêm công việc", exact: true })
    .first()
    .click();
  const pop = page.getByRole("dialog").first();
  await expect(pop.getByText("Thêm vào cột")).toBeVisible();
  await page.waitForTimeout(900);
  await fillFrame2(page, pop);
  await page.mouse.move(2, 2);
  await page.waitForTimeout(300);
  return pop;
}

/** Every text node of the popup: [text, x, y, w, h] relative to it. */
function textsOf(pop: Locator) {
  return pop.evaluate((root) => {
    const o = root.getBoundingClientRect();
    const res: [string, number, number, number, number][] = [];
    const w = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    let n: Node | null;
    while ((n = w.nextNode())) {
      const t = (n.textContent ?? "").trim();
      if (!t) continue;
      const r = document.createRange();
      r.selectNodeContents(n);
      const q = r.getBoundingClientRect();
      res.push([t, q.x - o.x, q.y - o.y, q.width, q.height]);
    }
    return res;
  });
}

// Text nodes of the artboard's state frames, rendered in Chromium: [text, nth, x, y, w, h] relative to the popup.
const FRAME_0: [string, number, number, number, number, number][] = [
  ["Thêm vào cột", 0, 16, 19.4, 84.3, 16],
  ["Cần làm", 0, 143.3, 18.3, 57.2, 18],
  ["Thêm", 0, 94, 119.6, 35.8, 16],
  ["Nhãn", 0, 182.8, 119.6, 33.4, 16],
  ["Ngày", 0, 269.2, 119.6, 33.8, 16],
  ["Checklist", 0, 356, 119.6, 60.3, 16],
  ["Người làm", 0, 469.2, 119.6, 64.2, 16],
  ["Người làm", 1, 60, 165, 59.8, 15],
  ["HP", 0, 71.3, 199.1, 13.4, 13],
  ["Nhãn", 1, 135.8, 165, 31.1, 15],
  ["Đào tạo", 0, 147.8, 195.6, 53, 16],
  ["Tài liệu", 0, 230.8, 195.6, 45.7, 16],
  ["Ngày", 1, 304.5, 165, 31.5, 15],
  ["15/10 – 20/10", 0, 337.5, 196.2, 91.2, 18],
  ["Còn 8 ngày", 0, 435.7, 197.9, 70, 16],
  ["Ưu tiên", 0, 556.7, 165, 41.7, 15],
  ["Cao", 0, 592.7, 196.2, 28, 18],
  ["Mô tả", 0, 60, 250.1, 42.7, 19],
  [
    "Làm việc với kế toán trưởng Minh An Group để chọn hai buổi đào tạo phân hệ công nợ trước ngày chạy thử.",
    0,
    72,
    291,
    580.4,
    39.7,
  ],
  ["Buổi 1: nhập chứng từ và đối chiếu", 0, 92, 340.4, 228.8, 18],
  ["Buổi 2: báo cáo công nợ", 0, 92, 364.1, 165.2, 18],
  ["Gửi lịch cho các phòng ban liên quan.", 0, 72, 387.8, 253.1, 18],
  ["Trường tuỳ chỉnh", 0, 60, 448.6, 124.7, 19],
  ["Đính kèm", 0, 60, 540.6, 68.3, 19],
  ["XLSX", 0, 88.3, 596.5, 31.3, 14],
  ["Lich-dao-tao-ke-toan.xlsx", 0, 152, 585.1, 196.6, 18],
  ["1,1 MB · tải lên sau khi thẻ được tạo", 0, 152, 606.4, 208.3, 16],
  ["Lịch phòng đào tạo tháng 10", 0, 152, 645.1, 199.2, 18],
  ["minhan.example · đính kèm khi tạo", 0, 152, 666.4, 207.4, 16],
  ["Liên kết và phụ thuộc", 0, 60, 721.3, 150.5, 18],
  ["· Chưa có gì", 0, 218.5, 720.7, 77, 18],
  ["Bình luận và hoạt động", 0, 771, 76.1, 171.9, 19],
  ["Hiện chi tiết", 0, 1015.5, 77.9, 73.5, 16],
  ["Viết bình luận…", 0, 756, 124.8, 102.5, 18],
  ["Bình luận mở sau khi tạo thẻ", 0, 822.5, 253, 196, 18],
  [
    "Tạo thẻ xong, bạn viết bình luận, nhắc @ người làm và xem hoạt động ngay tại đây.",
    0,
    753.9,
    280.3,
    333.2,
    37.6,
  ],
  [
    "Đang tạo thẻ, chỉ gửi một lần. Bấm lại cũng không tạo thẻ thứ hai.",
    0,
    43,
    733.9,
    397,
    16,
  ],
  ["Huỷ", 0, 918.8, 732.2, 25.4, 18],
  ["Đang tạo…", 0, 1007.1, 732.2, 73.9, 18],
];
const FRAME_1: [string, number, number, number, number, number][] = [
  ["Thêm vào cột", 0, 16, 19.4, 84.3, 16],
  ["Cần làm", 0, 143.3, 18.3, 57.2, 18],
  ["Chưa tạo được thẻ", 0, 72, 80, 125.8, 18],
  [
    "Mất kết nối trước khi máy chủ trả lời. Mọi thứ bạn nhập vẫn còn. Thử lại dùng cùng mã yêu cầu nên không bao giờ tạo hai thẻ.",
    0,
    72,
    99.6,
    584.4,
    37.6,
  ],
  ["Thử lại", 0, 85, 154.3, 43.2, 16],
  ["Giữ nháp và đóng", 0, 162.2, 154.3, 111.7, 16],
  ["Thêm", 0, 94, 254.3, 35.8, 16],
  ["Nhãn", 0, 182.8, 254.3, 33.4, 16],
  ["Ngày", 0, 269.2, 254.3, 33.8, 16],
  ["Checklist", 0, 356, 254.3, 60.3, 16],
  ["Người làm", 0, 469.2, 254.3, 64.2, 16],
  ["Người làm", 1, 60, 299.7, 59.8, 15],
  ["HP", 0, 71.3, 333.8, 13.4, 13],
  ["Nhãn", 1, 135.8, 299.7, 31.1, 15],
  ["Đào tạo", 0, 147.8, 330.3, 53, 16],
  ["Tài liệu", 0, 230.8, 330.3, 45.7, 16],
  ["Ngày", 1, 342.5, 299.7, 31.5, 15],
  ["15/10 – 20/10", 0, 375.5, 330.9, 91.2, 18],
  ["Còn 8 ngày", 0, 473.7, 332.6, 70, 16],
  ["Ưu tiên", 0, 594.7, 299.7, 41.7, 15],
  ["Cao", 0, 630.7, 330.9, 28, 18],
  ["Mô tả", 0, 60, 384.8, 42.7, 19],
  ["Sửa", 0, 661.4, 386.6, 23.6, 16],
  [
    "Làm việc với kế toán trưởng Minh An Group để chọn hai buổi đào tạo phân hệ công nợ trước ngày chạy thử.",
    0,
    72,
    425.7,
    580.4,
    39.7,
  ],
  ["Buổi 1: nhập chứng từ và đối chiếu", 0, 92, 475.1, 228.8, 18],
  ["Buổi 2: báo cáo công nợ", 0, 92, 498.8, 165.2, 18],
  ["Gửi lịch cho các phòng ban liên quan.", 0, 72, 522.4, 253.1, 18],
  ["Trường tuỳ chỉnh", 0, 60, 583.3, 124.7, 19],
  ["Trường hiển thị trên thẻ", 0, 542.8, 585.1, 142.2, 16],
  ["Thêm trường", 0, 79, 656.7, 82.4, 16],
  ["Đính kèm", 0, 60, 705.3, 68.3, 19],
  ["Thêm", 1, 650.6, 707.1, 34.4, 16],
  ["XLSX", 0, 88.3, 761.2, 31.3, 14],
  ["Lich-dao-tao-ke-toan.xlsx", 0, 152, 749.8, 196.6, 18],
  ["1,1 MB · tải lên sau khi thẻ được tạo", 0, 152, 771.1, 208.3, 16],
  ["Lịch phòng đào tạo tháng 10", 0, 152, 809.8, 199.2, 18],
  ["minhan.example · đính kèm khi tạo", 0, 152, 831.1, 207.4, 16],
  ["Liên kết và phụ thuộc", 0, 60, 886, 150.5, 18],
  ["· Chưa có gì", 0, 218.5, 885.3, 77, 18],
  ["Thêm liên kết", 0, 599.8, 886.7, 86.2, 16],
  ["Bình luận và hoạt động", 0, 771, 76.1, 171.9, 19],
  ["Hiện chi tiết", 0, 1015.5, 77.9, 73.5, 16],
  ["Viết bình luận…", 0, 756, 124.8, 102.5, 18],
  ["Bình luận mở sau khi tạo thẻ", 0, 822.5, 253, 196, 18],
  [
    "Tạo thẻ xong, bạn viết bình luận, nhắc @ người làm và xem hoạt động ngay tại đây.",
    0,
    753.9,
    280.3,
    333.2,
    37.6,
  ],
  ["Tạo tiếp thẻ khác", 0, 51, 732.2, 116.3, 18],
  ["· giữ cột, nhãn, người làm", 0, 176.3, 733.9, 151.2, 16],
  ["Huỷ", 0, 963.5, 732.2, 25.4, 18],
  ["Tạo thẻ", 0, 1028.8, 732.2, 52.2, 18],
];
const FRAME_2: [string, number, number, number, number, number][] = [
  ["Cần làm", 0, 45, 18.3, 57.2, 18],
  ["ERP-153", 0, 141.2, 19.8, 50.4, 15],
  ["Đã tạo ERP-153 trong Cần làm", 0, 72, 80, 208.2, 18],
  [
    "Popup ở lại trên thẻ vừa tạo. Bình luận và hoạt động đã mở.",
    0,
    72,
    99.6,
    387.4,
    18,
  ],
  ["Thêm", 0, 94, 194.7, 35.8, 16],
  ["Nhãn", 0, 182.8, 194.7, 33.4, 16],
  ["Ngày", 0, 269.2, 194.7, 33.8, 16],
  ["Checklist", 0, 356, 194.7, 60.3, 16],
  ["Người làm", 0, 469.2, 194.7, 64.2, 16],
  ["Người làm", 1, 60, 240.1, 59.8, 15],
  ["HP", 0, 71.3, 274.2, 13.4, 13],
  ["Nhãn", 1, 135.8, 240.1, 31.1, 15],
  ["Đào tạo", 0, 147.8, 270.7, 53, 16],
  ["Tài liệu", 0, 230.8, 270.7, 45.7, 16],
  ["Ngày", 1, 342.5, 240.1, 31.5, 15],
  ["15/10 – 20/10", 0, 375.5, 271.3, 91.2, 18],
  ["Còn 8 ngày", 0, 473.7, 273.1, 70, 16],
  ["Ưu tiên", 0, 594.7, 240.1, 41.7, 15],
  ["Cao", 0, 630.7, 271.3, 28, 18],
  ["Mô tả", 0, 60, 325.3, 42.7, 19],
  ["Sửa", 0, 661.4, 327.1, 23.6, 16],
  [
    "Làm việc với kế toán trưởng Minh An Group để chọn hai buổi đào tạo phân hệ công nợ trước ngày chạy thử.",
    0,
    72,
    366.1,
    580.4,
    39.7,
  ],
  ["Buổi 1: nhập chứng từ và đối chiếu", 0, 92, 415.5, 228.8, 18],
  ["Buổi 2: báo cáo công nợ", 0, 92, 439.2, 165.2, 18],
  ["Gửi lịch cho các phòng ban liên quan.", 0, 72, 462.9, 253.1, 18],
  ["Trường tuỳ chỉnh", 0, 60, 523.7, 124.7, 19],
  ["Trường hiển thị trên thẻ", 0, 542.8, 525.5, 142.2, 16],
  ["Thêm trường", 0, 79, 597.1, 82.4, 16],
  ["Đính kèm", 0, 60, 645.7, 68.3, 19],
  ["Thêm", 1, 650.6, 647.5, 34.4, 16],
  ["XLSX", 0, 88.3, 701.6, 31.3, 14],
  ["Lich-dao-tao-ke-toan.xlsx", 0, 152, 690.3, 196.6, 18],
  ["1,1 MB · đang tải lên, 60%", 0, 152, 711.6, 150.6, 16],
  ["Lịch phòng đào tạo tháng 10", 0, 152, 750.3, 199.2, 18],
  ["minhan.example · đính kèm khi tạo", 0, 152, 771.6, 207.4, 16],
  ["Liên kết và phụ thuộc", 0, 60, 826.4, 150.5, 18],
  ["· Chưa có gì", 0, 218.5, 825.8, 77, 18],
  ["Thêm liên kết", 0, 599.8, 827.1, 86.2, 16],
  ["Bình luận và hoạt động", 0, 771, 76.1, 171.9, 19],
  ["Hiện chi tiết", 0, 1015.5, 77.9, 73.5, 16],
  ["Viết bình luận…", 0, 756, 124.8, 102.5, 18],
  ["NA", 0, 745.8, 177.1, 14.4, 13],
  ["Nguyễn Anh", 0, 775, 173, 78.1, 16],
  ["đã thêm thẻ này vào Cần làm", 0, 853.1, 173, 188.6, 16],
  ["vừa xong", 0, 775, 191.8, 55.3, 16],
];

/** Every wanted text of a frame is where the artboard puts it (1 px). */
async function expectFrame(
  pop: Locator,
  want: [string, number, number, number, number, number][],
  skip: (text: string, nth: number) => boolean = () => false,
) {
  const texts = await textsOf(pop);
  for (const [text, nth, x, y, w, h] of want) {
    if (skip(text, nth)) continue;
    const hit = texts.filter(([t]) => t === text)[nth];
    if (!hit) throw new Error(`no text "${text}"`);
    near(hit[1], x, `${text}#${nth}.x`);
    near(hit[2], y, `${text}#${nth}.y`);
    near(hit[3], w, `${text}#${nth}.w`);
    near(hit[4], h, `${text}#${nth}.h`);
  }
}

// The artboard's custom field is "Hạng mục = Đào tạo"; the mock board's own field stands for it.
const FIELD_ROW = (text: string, nth: number) =>
  text === "Hạng mục" ||
  (text === "Đào tạo" && nth === 1) ||
  // The app writes the dot and "Chưa có gì" as two text nodes; the title beside them is measured.
  text === "· Chưa có gì";

// The 390 px sheet (`Kanban-CardPopup-M-Create`), a 390 x 832 sheet under a 12 px gap.
const SHEET_0: [string, number, number, number, number, number][] = [
  ["Thêm vào cột", 0, 12, 19.4, 84.3, 16],
  ["Cần làm", 0, 139.3, 18.3, 57.2, 18],
  ["Thêm", 0, 52, 136.2, 37.2, 18],
  ["Nhãn", 0, 146.2, 136.2, 34.6, 18],
  ["Ngày", 0, 237.8, 136.2, 35.1, 18],
  ["Checklist", 0, 52, 188.2, 62.6, 18],
  ["Người làm", 0, 171.6, 188.2, 66.7, 18],
  ["Thêm mô tả…", 0, 29, 248.8, 91.7, 18],
  ["Thêm trường", 0, 45, 309.6, 82.4, 16],
  ["Đính kèm", 0, 16, 361, 64.3, 18],
  ["· Chưa có gì", 0, 80.3, 361, 77, 18],
  ["Thêm", 1, 45, 394.1, 36.1, 16],
  ["Liên kết và phụ thuộc", 0, 16, 445.6, 150.5, 18],
  ["· Chưa có gì", 1, 166.5, 445.6, 77, 18],
  ["Thêm liên kết", 0, 45, 478.7, 86.2, 16],
  ["Tạo tiếp thẻ khác", 0, 43, 736.2, 116.3, 18],
  ["Huỷ", 0, 31, 788.2, 25.4, 18],
  ["Tạo thẻ", 0, 200.6, 788.2, 52.2, 18],
];
const SHEET_1: [string, number, number, number, number, number][] = [
  ["Thêm vào cột", 0, 12, 19.4, 84.3, 16],
  ["Cần làm", 0, 139.3, 18.3, 57.2, 18],
  ["Việc cần chuẩn bị", 0, 16, 87.4, 141.4, 20],
  ["Ẩn mục đã xong", 0, 198.8, 89.6, 102.2, 16],
  ["Xoá", 0, 335.1, 89.6, 25.9, 16],
  ["0%", 0, 16, 127, 18.3, 15],
  ["Chốt danh sách người tham dự", 0, 50, 160.2, 208.4, 18],
  ["Đặt phòng đào tạo", 0, 50, 204.2, 130.8, 18],
  ["Gửi lịch cho các phòng ban liên quan", 0, 50, 248.2, 249.1, 18],
  ["Thêm mục", 0, 29, 301, 66.2, 16],
  ["Đính kèm", 0, 16, 362.8, 72.8, 20],
  ["Thêm", 0, 325.2, 365, 35.8, 16],
  ["XLSX", 0, 44.3, 431.9, 31.3, 14],
  ["Lich-dao-tao-ke-toan.xlsx", 0, 108, 410.4, 196.6, 18],
  ["1,1 MB · tải lên sau khi thẻ được tạo", 0, 108, 431.7, 183.9, 36.3],
  ["Lịch phòng đào tạo tháng 10", 0, 108, 483.3, 199.2, 18],
  ["minhan.example · đính kèm khi tạo", 0, 108, 504.6, 183, 36.3],
  ["Bình luận và hoạt động", 0, 46, 601.6, 183.4, 20],
  ["Hiện chi tiết", 0, 284.6, 603.8, 76.4, 16],
  ["Viết bình luận…", 0, 31, 661, 102.5, 18],
  ["Bình luận mở sau khi tạo thẻ", 0, 97, 791.2, 196, 18],
  [
    "Tạo thẻ xong, bạn viết bình luận, nhắc @ người làm và xem hoạt động ngay tại đây.",
    0,
    28.4,
    818.5,
    333.2,
    37.6,
  ],
  ["Tạo tiếp thẻ khác", 0, 43, 736.2, 116.3, 18],
  ["Huỷ", 0, 31, 788.2, 25.4, 18],
  ["Tạo thẻ", 0, 200.6, 788.2, 52.2, 18],
];
const SHEET_2: [string, number, number, number, number, number][] = [
  ["Thêm vào cột", 0, 12, 19.4, 84.3, 16],
  ["Cần làm", 0, 139.3, 18.3, 57.2, 18],
  ["Thêm", 0, 52, 136.2, 37.2, 18],
  ["Nhãn", 0, 146.2, 136.2, 34.6, 18],
  ["Ngày", 0, 237.8, 136.2, 35.1, 18],
  ["Checklist", 0, 52, 188.2, 62.6, 18],
  ["Người làm", 0, 171.6, 188.2, 66.7, 18],
  ["Người làm", 1, 16, 233, 59.8, 15],
  ["HP", 0, 27.3, 271.1, 13.4, 13],
  ["Nhãn", 1, 87.8, 233, 31.1, 15],
  ["Đào tạo", 0, 99.8, 263.6, 53, 16],
  ["Tài liệu", 0, 182.8, 263.6, 45.7, 16],
  ["Ngày", 1, 16, 313, 31.5, 15],
  ["15/10 – 20/10", 0, 49, 348.2, 91.2, 18],
  ["Còn 8 ngày", 0, 147.2, 349.9, 70, 16],
  ["Ưu tiên", 0, 264.3, 313, 41.7, 15],
  ["Cao", 0, 300.3, 348.2, 28, 18],
  ["Mô tả", 0, 16, 405.4, 45.5, 20],
  [
    "Làm việc với kế toán trưởng Minh An Group để chọn hai buổi đào tạo phân hệ công nợ trước ngày chạy thử.",
    0,
    28,
    451,
    312.2,
    61.4,
  ],
  ["Buổi 1: nhập chứng từ và đối chiếu", 0, 48, 522.1, 228.8, 18],
  ["Buổi 2: báo cáo công nợ", 0, 48, 545.8, 165.2, 18],
  ["Gửi lịch cho các phòng ban liên quan.", 0, 28, 569.4, 253.1, 18],
  ["Trường tuỳ chỉnh", 0, 16, 631.5, 133, 20],
  ["Đính kèm", 0, 16, 727.1, 64.3, 18],
  ["· Chưa có gì", 0, 80.3, 727.1, 77, 18],
  ["Liên kết và phụ thuộc", 0, 16, 767.7, 150.5, 18],
  ["· Chưa có gì", 1, 166.5, 767.7, 77, 18],
  ["Đang tạo thẻ, chỉ gửi một lần.", 0, 35, 750.9, 179.8, 16],
  ["Huỷ", 0, 31, 788.2, 25.4, 18],
  ["Đang tạo…", 0, 201.2, 788.2, 73.9, 18],
];

// The app writes "·" and the words as two text nodes where the artboard has one.
const SPLIT = (text: string) => text === "· Chưa có gì";

test.describe("create popup, states", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("frame 1: creating, the request goes once", async ({ page }) => {
    const pop = await openFilled(page);
    await page.evaluate((k) => localStorage.setItem(k, "hold"), CREATE_SCRIPT);
    await pop.getByRole("button", { name: "Tạo thẻ" }).click();
    await expect(pop.getByText("Đang tạo thẻ, chỉ gửi một lần.")).toBeVisible();
    await page.mouse.move(2, 2);
    await page.waitForTimeout(300);
    await expectFrame(
      pop,
      FRAME_0,
      (text, nth) => FIELD_ROW(text, nth) || text.startsWith("Đang tạo thẻ,"),
    );
    // The note is two text nodes in the app (the second hides on a phone); the artboard has one: 43 + 397 wide.
    const note = (await textsOf(pop)).filter(([t]) =>
      /^(Đang tạo thẻ, chỉ|Bấm lại cũng)/.test(t.trim()),
    );
    near(note[0]![1], 43, "note.x");
    near(note[0]![2], 733.9, "note.y");
    near(note[1]![1] + note[1]![3] - note[0]![1], 397, "note.w");
    await page.evaluate((k) => localStorage.removeItem(k), CREATE_SCRIPT);
  });

  test("frame 2: a lost connection keeps the draft and retries safely", async ({
    page,
  }) => {
    const pop = await openFilled(page);
    await page.evaluate(
      (k) => localStorage.setItem(k, "fail-once"),
      CREATE_SCRIPT,
    );
    await pop.getByRole("button", { name: "Tạo thẻ" }).click();
    await expect(pop.getByText("Chưa tạo được thẻ")).toBeVisible();
    await page.mouse.move(2, 2);
    await page.waitForTimeout(300);
    await expectFrame(pop, FRAME_1, FIELD_ROW);
  });
  test("frame 3: created, the card stays open in the popup", async ({
    page,
  }) => {
    const pop = await openFilled(page);
    await pop.getByRole("button", { name: "Tạo thẻ" }).click();
    await expect(pop.getByText(/^Đã tạo ERP-\d+ trong Cần làm$/)).toBeVisible();
    await page.waitForTimeout(1500);
    await page.mouse.move(2, 2);
    // Header, the notice and the quick row (the artboard's ERP-153 is the next key of its own world); the comments column and the rest are the open card's own popup, measured with the card popup specs.
    await expectFrame(
      pop,
      FRAME_2.filter(([, , x, y]) => y < 200 && x < 720),
      (text) => /^(ERP-\d+|Đã tạo ERP-\d+ trong Cần làm)$/.test(text),
    );
    const notice = pop.getByText(/^Đã tạo ERP-\d+ trong Cần làm$/);
    const box = await rel(notice, pop);
    near(box.x, 72, "notice.x");
    near(box.y, 80, "notice.y");
    await expect(
      pop.getByText(
        "Popup ở lại trên thẻ vừa tạo. Bình luận và hoạt động đã mở.",
      ),
    ).toBeVisible();
  });
  test("frame 4: closing a draft asks before it is lost", async ({ page }) => {
    const pop = await openFilled(page);
    const o = await pop.boundingBox();
    await pop.getByRole("button", { name: "Huỷ" }).click();
    const ask = page.getByRole("alertdialog");
    await expect(ask.getByText("Bỏ thẻ đang soạn?")).toBeVisible();
    await page.waitForTimeout(400);
    const a = await ask.boundingBox();
    const texts = (await textsOf(ask)).map(
      ([t, x, y, w, h]) =>
        [t, x + a!.x - o!.x, y + a!.y - o!.y, w, h] as [
          string,
          number,
          number,
          number,
          number,
        ],
    );
    // Artboard: the dialog is 440 wide, centred; its title, its body (the parts listed come from the draft), the two buttons.
    const find = (text: string) => texts.find(([t]) => t === text)!;
    for (const [text, x, y, w, h] of [
      ["Bỏ thẻ đang soạn?", 360, 327.2, 150.3, 20],
      ["Tiếp tục soạn", 572.8, 422.1, 90.5, 18],
      ["Bỏ thẻ", 701.3, 422.1, 43.7, 18],
    ] as const) {
      const got = find(text);
      near(got[1], x, `${text}.x`);
      near(got[2], y, `${text}.y`);
      near(got[3], w, `${text}.w`);
      near(got[4], h, `${text}.h`);
    }
  });
});

test.describe("create popup, desktop", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("frame 1: a new card, still empty", async ({ page }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the defaults apply.
      }
    });
    await page.goto(BOARD);
    await expect(page.getByRole("region", { name: "Cần làm" })).toBeVisible();
    await page
      .getByRole("button", { name: "Thêm công việc", exact: true })
      .first()
      .click();
    const pop = page.getByRole("dialog").first();
    await expect(pop.getByText("Thêm vào cột")).toBeVisible();
    await page.waitForTimeout(900);
    await page.mouse.move(2, 2);

    await expectRel(pop, pop, { w: 1120, h: 772 }, "popup");
    expect(await pop.evaluate((e) => getComputedStyle(e).borderRadius)).toBe(
      "16px",
    );

    // Header: the lane picker, the more button.
    const lane = pop.getByRole("button", { name: "Cần làm" });
    await expectRel(
      lane,
      pop,
      { x: 114.3, y: 9.5, w: 117.2, h: 36 },
      "lane picker",
    );
    expect(await lane.evaluate((e) => getComputedStyle(e).borderRadius)).toBe(
      "8px",
    );
    await expectRel(
      pop.getByText("Thêm vào cột"),
      pop,
      { x: 16, h: 18.1 },
      "header label",
    );

    // The two columns.
    const comments = pop.locator("aside");
    await expectRel(
      comments,
      pop,
      { x: 720, y: 56, w: 400, h: 655 },
      "comments column",
    );
    const kids = comments.locator("> div");
    await expectRel(
      kids.nth(0),
      pop,
      { x: 741, y: 72, w: 359, h: 28 },
      "comments header",
    );
    await expectRel(
      kids.nth(1),
      pop,
      { x: 741, y: 112, w: 359, h: 44 },
      "dashed composer",
    );
    expect(
      await kids.nth(1).evaluate((e) => getComputedStyle(e).borderRadius),
    ).toBe("10px");
    expect(
      await kids.nth(1).evaluate((e) => getComputedStyle(e).borderTopStyle),
    ).toBe("dashed");
    await expectRel(
      kids.nth(2),
      pop,
      { x: 741, y: 168, w: 359, h: 183.4 },
      "empty note",
    );
    await expect(pop.getByText("Bình luận mở sau khi tạo thẻ")).toBeVisible();
    await expect(
      pop.getByRole("button", { name: "Hiện chi tiết" }),
    ).toBeDisabled();

    // The item column: title row, quick row and the four sections.
    const left = pop.locator("section").first().locator("..");
    await expectRel(left, pop, { x: 0, y: 56, w: 720 }, "item column");
    const sections = pop.locator("section");
    // The canvas draws four sections (description, fields, attachments, links) at 152, 208, 260 and 312.
    for (const [i, y] of [152, 208, 260, 312].entries())
      near((await rel(sections.nth(i), pop)).y, y, `section ${i}.y`);
    await expect(sections).toHaveCount(4);

    // Footer: keep-going tick, Huỷ, Tạo thẻ.
    const keep = pop.getByLabel("Tạo tiếp thẻ khác");
    await expectRel(keep, pop, { x: 24, y: 733, w: 18, h: 18 }, "tick");
    await expectRel(
      pop.getByText("· giữ cột, nhãn, người làm"),
      pop,
      { x: 176.3, h: 18.1 },
      "tick note",
    );
    await expectRel(
      pop.getByRole("button", { name: "Huỷ" }),
      pop,
      { x: 948.5, y: 724, w: 55.4, h: 36 },
      "Huỷ",
    );
    const ok = pop.getByRole("button", { name: "Tạo thẻ", exact: true });
    await expectRel(ok, pop, { x: 1013.8, y: 724, w: 82.2, h: 36 }, "Tạo thẻ");
    expect(await ok.evaluate((e) => getComputedStyle(e).fontWeight)).toBe(
      "600",
    );
  });

  // Kanban-CardPopup-Create draws "Liên kết và phụ thuộc · Chưa có gì · Thêm liên kết" in a new card;
  // the draft holds the links and they are written once the card exists.
  test("frame 1: the links section is drawn under the attachments with its add button", async ({
    page,
  }) => {
    await page.goto(BOARD);
    await page
      .getByRole("button", { name: "Thêm công việc", exact: true })
      .first()
      .click();
    const pop = page.getByRole("dialog").first();
    await expect(pop.getByText("Liên kết và phụ thuộc")).toBeVisible();
    await expect(pop.getByText("Chưa có gì").last()).toBeVisible();
    await expect(
      pop.getByRole("button", { name: "Thêm liên kết" }),
    ).toBeVisible();
  });
});

/**
 * Frame 2, "Đã điền nháp": a draft with a title, people, labels, dates, priority and a description. The artboard's text
 * boxes are compared one by one (relative to the popup), so the summary row, the description and the comments column
 * are held to 1 px. Its custom-fields rows, the queued attachments and the links section below them are owed (the app
 * draws fields as pills where the artboard draws label and value rows).
 */
test.describe("create popup, desktop, filled draft", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("frame 2: the filled draft", async ({ page }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the defaults apply.
      }
    });
    await page.goto(BOARD);
    await page
      .getByRole("button", { name: "Thêm công việc", exact: true })
      .first()
      .click();
    const pop = page.getByRole("dialog").first();
    await expect(pop.getByText("Thêm vào cột")).toBeVisible();
    await page.waitForTimeout(900);
    await fillFrame2(page, pop);
    await page.mouse.move(2, 2);
    await page.waitForTimeout(300);
    await page.mouse.move(2, 2);

    const texts = await pop.evaluate((root) => {
      const o = root.getBoundingClientRect();
      const res: [string, number, number, number, number][] = [];
      const w = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
      let n: Node | null;
      while ((n = w.nextNode())) {
        const t = (n.textContent ?? "").trim();
        if (!t) continue;
        const r = document.createRange();
        r.selectNodeContents(n);
        const q = r.getBoundingClientRect();
        res.push([t, q.x - o.x, q.y - o.y, q.width, q.height]);
      }
      return res;
    });
    const at = (text: string, nth = 0) => {
      const hit = texts.filter(([t]) => t === text)[nth];
      if (!hit) throw new Error(`no text "${text}"`);
      return { x: hit[1], y: hit[2], w: hit[3], h: hit[4] };
    };
    // [text, nth, x, y, w, h] read from the artboard; x is left out where the width of a chosen label moves it.
    const want: [string, number, number, number, number, number][] = [
      ["Thêm vào cột", 0, 16, 19.4, 84.3, 16],
      ["Thêm", 0, 94, 119.6, 35.8, 16],
      ["Nhãn", 0, 182.8, 119.6, 33.4, 16],
      ["Ngày", 0, 269.2, 119.6, 33.8, 16],
      ["Checklist", 0, 356, 119.6, 60.3, 16],
      ["Người làm", 0, 469.2, 119.6, 64.2, 16],
      ["Người làm", 1, 60, 165, 59.8, 15],
      ["HP", 0, 71.3, 199.1, 13.4, 13],
      ["Nhãn", 1, 135.8, 165, 31.1, 15],
      ["Đào tạo", 0, 147.8, 195.6, 53, 16],
      ["Tài liệu", 0, 230.8, 195.6, 45.7, 16],
      ["Ngày", 1, 342.5, 165, 31.5, 15],
      ["Ưu tiên", 0, 594.7, 165, 41.7, 15],
      ["Mô tả", 0, 60, 250.1, 42.7, 19],
      ["Sửa", 0, 661.4, 251.9, 23.6, 16],
      ["Bình luận và hoạt động", 0, 771, 76.1, 171.9, 19],
      ["Hiện chi tiết", 0, 1015.5, 77.9, 73.5, 16],
      ["Bình luận mở sau khi tạo thẻ", 0, 822.5, 253, 196, 18],
      ["Tạo tiếp thẻ khác", 0, 51, 732.2, 116.3, 18],
      ["Tạo thẻ", 0, 1028.8, 732.2, 52.2, 18],
      [
        "Làm việc với kế toán trưởng Minh An Group để chọn hai buổi đào tạo phân hệ công nợ trước ngày chạy thử.",
        0,
        72,
        291,
        580.4,
        39.7,
      ],
      ["Buổi 1: nhập chứng từ và đối chiếu", 0, 92, 340.4, 228.8, 18],
      ["Gửi lịch cho các phòng ban liên quan.", 0, 72, 387.8, 253.1, 18],
      ["Trường tuỳ chỉnh", 0, 60, 448.6, 124.7, 19],
      ["Trường hiển thị trên thẻ", 0, 542.8, 450.4, 142.2, 16],
      ["Thêm trường", 0, 79, 522, 82.4, 16],
      ["Đính kèm", 0, 60, 570.6, 68.3, 19],
      ["XLSX", 0, 88.3, 626.5, 31.3, 14],
      ["Lich-dao-tao-ke-toan.xlsx", 0, 152, 615.1, 196.6, 18],
      ["1,1 MB · tải lên sau khi thẻ được tạo", 0, 152, 636.4, 208.3, 16],
      ["Lịch phòng đào tạo tháng 10", 0, 152, 675.1, 199.2, 18],
      ["minhan.example · đính kèm khi tạo", 0, 152, 696.4, 207.4, 16],
      ["Liên kết và phụ thuộc", 0, 60, 751.3, 150.5, 18],
    ];
    for (const [text, nth, x, y, w, h] of want) {
      const got = at(text, nth);
      near(got.x, x, `${text}#${nth}.x`);
      near(got.y, y, `${text}#${nth}.y`);
      near(got.w, w, `${text}#${nth}.w`);
      near(got.h, h, `${text}#${nth}.h`);
    }
  });
});

test.describe("create popup, 390 px sheet", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 390, height: 844 } });

  test("frame 1: a new card, the footer is fixed", async ({ page }) => {
    await page.addInitScript(() =>
      localStorage.setItem("notrelix-locale", "vi"),
    );
    await page.goto(BOARD);
    await page
      .getByRole("button", { name: "Thêm công việc", exact: true })
      .first()
      .click();
    const pop = page.getByRole("dialog").first();
    await expect(pop.getByText("Thêm vào cột")).toBeVisible();
    await page.waitForTimeout(900);
    await expectFrame(pop, SHEET_0, SPLIT);
    // The comments wait below the fold.
    const comments = await pop.locator("aside").boundingBox();
    expect(comments!.y).toBeGreaterThanOrEqual(723);
  });

  test("frame 3: creating, the sheet is locked", async ({ page }) => {
    // The draft is filled with the desktop pickers, then the window narrows to the phone.
    await page.setViewportSize({ width: 1440, height: 900 });
    const pop = await openFilled(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForTimeout(500);
    await page.evaluate((k) => localStorage.setItem(k, "hold"), CREATE_SCRIPT);
    await pop.getByRole("button", { name: "Tạo thẻ" }).click();
    await expect(pop.getByText("Đang tạo thẻ, chỉ gửi một lần.")).toBeVisible();
    await page.mouse.move(2, 2);
    await page.waitForTimeout(300);
    await expectFrame(
      pop,
      SHEET_2,
      (text, nth) =>
        FIELD_ROW(text, nth) ||
        text.startsWith("Đang tạo thẻ,") ||
        // The artboard cuts the sheet at the fold and draws these two sections empty; the app lists the rows it holds.
        text === "Đính kèm" ||
        text === "Liên kết và phụ thuộc" ||
        text === "· Chưa có gì",
    );
    await page.evaluate((k) => localStorage.removeItem(k), CREATE_SCRIPT);
  });

  test("frame 2: the checklist and the files, the comments wait", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.addInitScript(() =>
      localStorage.setItem("notrelix-locale", "vi"),
    );
    await page.goto(BOARD);
    await page
      .getByRole("button", { name: "Thêm công việc", exact: true })
      .first()
      .click();
    const pop = page.getByRole("dialog").first();
    await expect(pop.getByText("Thêm vào cột")).toBeVisible();
    await page.waitForTimeout(900);
    await pop
      .getByRole("textbox")
      .first()
      .fill("Chốt lịch đào tạo kế toán trưởng");
    await pop.getByRole("button", { name: "Checklist" }).first().click();
    await page.getByLabel("Tiêu đề").fill("Việc cần chuẩn bị");
    await page
      .getByRole("button", { name: "Thêm", exact: true })
      .last()
      .click();
    await page.waitForTimeout(500);
    for (const item of [
      "Chốt danh sách người tham dự",
      "Đặt phòng đào tạo",
      "Gửi lịch cho các phòng ban liên quan",
    ]) {
      await page.keyboard.type(item);
      await page.keyboard.press("Enter");
      await page.waitForTimeout(300);
    }
    await page.keyboard.press("Escape");
    await page.getByRole("button", { name: "Thêm đính kèm" }).click();
    await page
      .locator("input[type=file]")
      .first()
      .setInputFiles({
        name: "Lich-dao-tao-ke-toan.xlsx",
        mimeType: "application/vnd.ms-excel",
        buffer: Buffer.alloc(1153434),
      });
    await page.waitForTimeout(400);
    await page
      .getByPlaceholder(/Dán liên kết/)
      .fill("https://minhan.example/lich-phong-dao-tao");
    await page.getByLabel("Tên hiển thị").fill("Lịch phòng đào tạo tháng 10");
    await page.getByRole("button", { name: "Gắn", exact: true }).click();
    await page.waitForTimeout(500);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForTimeout(500);
    // The artboard is the sheet scrolled to where the checklist title sits 87.4 px from the sheet's top.
    const title = (await textsOf(pop)).find(
      ([t]) => t === "Việc cần chuẩn bị",
    )!;
    await pop.locator("[data-popup-left]").evaluate((left, delta) => {
      left.parentElement!.scrollTop -= delta;
    }, 87.4 - title[2]);
    await page.waitForTimeout(300);
    await expectFrame(
      pop,
      // The artboard leaves the empty links section out between the files and the comments, so the comments column is measured on x, width and height only (below).
      SHEET_1.filter(([, , , y]) => y > 80 && y < 590),
      // Kept deviation: "Ẩn mục đã xong" appears once an item is done; a button that hides nothing is not drawn.
      (text) => SPLIT(text) || text === "Ẩn mục đã xong" || text === "Thêm",
    );
  });
});
