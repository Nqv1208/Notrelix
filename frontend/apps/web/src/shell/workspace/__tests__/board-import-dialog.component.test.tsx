import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { BoardImportDialog } from "../board-import-dialog";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const runtime = vi.hoisted(() => ({
  api: null as unknown as {
    api: {
      upload: ReturnType<typeof vi.fn>;
      post: ReturnType<typeof vi.fn>;
      get: ReturnType<typeof vi.fn>;
    };
  },
}));
vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: runtime.api }),
}));
vi.mock("@tanstack/react-router", () => ({ useNavigate: () => vi.fn() }));

let root: Root | null = null;
let host: HTMLDivElement | null = null;
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
});

const sleep = (ms: number) =>
  act(async () => {
    await new Promise((r) => setTimeout(r, ms));
  });
const text = () => document.body.textContent ?? "";
const button = (name: RegExp) =>
  [...document.querySelectorAll("button")].find((b) =>
    name.test(b.textContent ?? ""),
  ) as HTMLButtonElement | undefined;

async function open(api: Partial<(typeof runtime)["api"]["api"]>) {
  runtime.api = {
    api: { upload: vi.fn(), post: vi.fn(), get: vi.fn(), ...api },
  };
  runtime.api.api.upload.mockResolvedValue({
    jobId: "j1",
    fileName: "a.csv",
    rowCount: 1,
    columnCount: 1,
    columns: [{ index: 0, name: "Tên" }],
    targets: [],
    suggested: [{ column: 0, target: "$title" }],
  });
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  await act(async () => {
    root!.render(
      <I18nProvider catalogs={[]} initialLocale="vi">
        <QueryClientProvider client={new QueryClient()}>
          <BoardImportDialog
            workspaceId="w"
            boardId="b"
            boardName="B"
            open
            onClose={() => undefined}
          />
        </QueryClientProvider>
      </I18nProvider>,
    );
  });
  const input = document.querySelector(
    'input[type="file"]',
  ) as HTMLInputElement;
  Object.defineProperty(input, "files", {
    value: [new File(["Tên\nx"], "a.csv")],
  });
  await act(async () => {
    input.dispatchEvent(new Event("change", { bubbles: true }));
  });
  await sleep(20);
}

const cleanPreview = { checks: [], errorCount: 0, errors: [] };

describe("BoardImportDialog failures", () => {
  it("says the check failed and reruns it with Kiểm tra lại", async () => {
    const post = vi
      .fn()
      .mockRejectedValueOnce(Object.assign(new Error("x"), { status: 500 }))
      .mockResolvedValue(cleanPreview);
    await open({ post });
    expect(document.querySelector('[role="alert"]')?.textContent).toContain(
      "Không kiểm tra được tệp",
    );
    await act(async () => button(/Kiểm tra lại/)!.click());
    await sleep(20);
    expect(post).toHaveBeenCalledTimes(2);
    expect(document.querySelector('[role="alert"]')).toBeNull();
  });

  it("shows the field errors of a refused confirm and sends it again", async () => {
    const post = vi
      .fn()
      .mockResolvedValueOnce(cleanPreview)
      .mockRejectedValueOnce(
        Object.assign(new Error("x"), {
          status: 400,
          validationErrors: { mapping: ["Cột 1 trùng đích."] },
        }),
      )
      .mockResolvedValue({ id: "j1", status: "queued" });
    await open({ post, get: vi.fn().mockResolvedValue({ status: "queued" }) });
    await act(async () => button(/Tiếp tục/)!.click());
    await sleep(20);
    await act(async () => button(/Nhập 1 việc/)!.click());
    await sleep(20);
    expect(text()).toContain("Cột 1 trùng đích.");
    await act(async () => button(/Kiểm tra lại/)!.click());
    await sleep(20);
    expect(text()).not.toContain("Cột 1 trùng đích.");
  });

  it("stops waiting after three failed polls in a row, and a good poll resets the count", async () => {
    const get = vi
      .fn()
      .mockRejectedValueOnce(new Error("net"))
      .mockRejectedValueOnce(new Error("net"))
      .mockResolvedValueOnce({ status: "running", total: 1, processed: 0 })
      .mockRejectedValue(new Error("net"));
    const post = vi
      .fn()
      .mockResolvedValueOnce(cleanPreview)
      .mockResolvedValue({ id: "j1", status: "queued" });
    await open({ post, get });
    await act(async () => button(/Tiếp tục/)!.click());
    await sleep(20);
    await act(async () => button(/Nhập 1 việc/)!.click());
    await sleep(1800);
    // Two failures, one good poll, then failures again: not yet three in a row.
    expect(text()).not.toContain("Mất kết nối giữa chừng");
    await sleep(1500);
    expect(text()).toContain("Mất kết nối giữa chừng");
  }, 15_000);
});
