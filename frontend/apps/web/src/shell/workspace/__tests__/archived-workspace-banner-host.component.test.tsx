import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { endpoints } from "@notrelix/contracts";

const h = vi.hoisted(() => ({
  post: vi.fn(),
  refetch: vi.fn(),
  workspace: null as null | Record<string, unknown>,
  role: "owner" as string | null,
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: { post: h.post }, endpoints } }),
}));
vi.mock("@/providers/workspace-provider", () => ({
  useWorkspaceContext: () => ({
    workspaceId: "w1",
    workspace: h.workspace,
    refetch: h.refetch,
  }),
}));
vi.mock("../use-workspace-viewer", () => ({
  useWorkspaceViewer: () => ({ role: h.role }),
}));
vi.mock("@notrelix/query", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/query")>()),
  useAccountId: () => "acc",
}));
vi.mock("sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

import { toast } from "sonner";
import { ArchivedWorkspaceBannerHost } from "../archived-workspace-banner-host";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

let root: Root | null = null;
let host: HTMLDivElement | null = null;
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
});
beforeEach(() => {
  h.post.mockReset();
  h.refetch.mockReset();
  h.role = "owner";
  h.workspace = { id: "w1", name: "Minh An", version: 7, isArchived: true };
});

let client: QueryClient;
async function mount() {
  client = new QueryClient();
  vi.spyOn(client, "invalidateQueries");
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  await act(async () => {
    root!.render(
      <QueryClientProvider client={client}>
        <I18nProvider catalogs={[]} initialLocale="vi">
          <ArchivedWorkspaceBannerHost />
        </I18nProvider>
      </QueryClientProvider>,
    );
  });
}
const text = () => host?.textContent ?? "";
const unarchive = () =>
  [...(host?.querySelectorAll("button") ?? [])].find((b) =>
    /Bỏ lưu trữ/.test(b.textContent ?? ""),
  ) as HTMLButtonElement | undefined;
const click = async (el: HTMLElement | undefined) =>
  act(async () => {
    el?.click();
  });
const failure = (status: number) =>
  Object.assign(new Error("x"), {
    status,
    kind: status === 403 ? "forbidden" : "conflict",
  });

describe("ArchivedWorkspaceBannerHost", () => {
  it("shows nothing for an active workspace", async () => {
    h.workspace = { id: "w1", name: "Minh An", version: 7, isArchived: false };
    await mount();
    expect(text()).toBe("");
  });

  it("FUI[workspace.archived:member-no-action] an Admin or Member sees the banner and no Unarchive, and nothing is sent", async () => {
    for (const role of ["admin", "member", "guest"]) {
      h.role = role;
      await mount();
      expect(text()).toContain("đang được lưu trữ");
      expect(unarchive()).toBeUndefined();
      act(() => root?.unmount());
      host?.remove();
    }
    expect(h.post).not.toHaveBeenCalled();
  });

  it("FUI[workspace.archived:unarchive] the Owner's action quotes the workspace version and the workspace is read again", async () => {
    h.post.mockResolvedValue(undefined);
    await mount();
    await click(unarchive());
    expect(h.post).toHaveBeenCalledWith("/workspaces/w1/archive/unarchive", {
      expectedVersion: 7,
    });
    expect(toast.success).toHaveBeenCalled();
    // The workspace record is read again, so the banner leaves and writes work again.
    expect(client.invalidateQueries).toHaveBeenCalled();
  });

  it("FUI[workspace.archived:stale-version] a stale version (412) shows the conflict state and Reload reads the workspace again", async () => {
    h.post.mockRejectedValue(failure(412));
    await mount();
    await click(unarchive());
    expect(text()).toContain("Workspace vừa thay đổi");
    expect(unarchive()).toBeUndefined();
    await click(
      [...(host?.querySelectorAll("button") ?? [])].find((b) =>
        /Tải lại/.test(b.textContent ?? ""),
      ) as HTMLElement,
    );
    expect(h.refetch).toHaveBeenCalled();
    expect(text()).toContain("đang được lưu trữ");
  });

  it("FUI[workspace.archived:forbidden] a refusal (403) says only the owner can", async () => {
    h.post.mockRejectedValue(failure(403));
    await mount();
    await click(unarchive());
    expect(text()).toContain("Chỉ chủ sở hữu mới bỏ lưu trữ được");
  });
});
