import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import { BoardSettingsSurface } from "../board-settings-surface";
import { en, vi as viCopy } from "../board-settings.copy";
import type {
  BoardSettingsActions,
  DeletedFieldsPanelProps,
} from "../board-settings-types";
import {
  ERP_SETTINGS_TIME_ZONE,
  erpBoardSettings,
  noopSettingsActions,
} from "../../../verification/board-settings-fixtures";

const NOW = Date.now();
const row = (id: string, name: string, key: string, left: number) => ({
  id,
  name,
  key,
  type: "text" as const,
  deletedAt: new Date(NOW - 5 * 86_400_000).toISOString(),
  restorableUntil: new Date(NOW + left * 86_400_000 + 3_600_000).toISOString(),
  deletedByName: "Trần Minh Quân",
  valueCount: 16,
});

function renderDeleted(onRestore: DeletedFieldsPanelProps["onRestore"]) {
  const settings = erpBoardSettings();
  renderPureUi(
    <BoardSettingsSurface
      status="ready"
      settings={settings}
      copy={viCopy as typeof en}
      boardTitle="ERP"
      timeZone={ERP_SETTINGS_TIME_ZONE}
      section="fields"
      onSelectSection={vi.fn()}
      fieldId={null}
      onSelectField={vi.fn()}
      viewId={null}
      onSelectView={vi.fn()}
      actions={noopSettingsActions() as unknown as BoardSettingsActions}
      deletedFields={{
        status: "ready",
        fields: [
          row("d1", "Ngày bàn giao", "ngay-ban-giao", 23),
          row("d2", "Mã hợp đồng", "ma-hop-dong", 1),
        ],
        onReload: vi.fn(),
        onRestore,
      }}
      onReload={vi.fn()}
      onBack={vi.fn()}
    />,
  );
}

describe("the deleted fields list", () => {
  it("names each field's key, how many items hold a value, and the days left", async () => {
    renderDeleted(vi.fn().mockResolvedValue("restored"));
    fireEvent.click(await screen.findByRole("tab", { name: /Đã xoá/ }));
    const rows = await screen.findAllByRole("row");
    const first = rows.find((r) => r.textContent?.includes("Ngày bàn giao"));
    expect(first?.textContent).toContain("ngay-ban-giao");
    expect(first?.textContent).toContain("16 việc có giá trị");
    expect(first?.textContent).toContain("Còn 23 ngày");
    expect(
      rows.find((r) => r.textContent?.includes("Mã hợp đồng"))?.textContent,
    ).toContain("Còn 1 ngày");
  });

  it("keeps the row and says why when the name is in use", async () => {
    const onRestore = vi.fn().mockResolvedValue("name-taken");
    renderDeleted(onRestore);
    fireEvent.click(await screen.findByRole("tab", { name: /Đã xoá/ }));
    fireEvent.click(
      await screen.findByRole("button", {
        name: /Khôi phục trường Ngày bàn giao/,
      }),
    );
    await waitFor(() => expect(onRestore).toHaveBeenCalledWith("d1"));
    expect((await screen.findByRole("alert")).textContent).toContain(
      "Ngày bàn giao",
    );
  });

  it.each([
    ["Hà Nguyễn", "Hà Nguyễn vừa khôi phục trường này. Danh sách đã làm mới."],
    [null, "Người khác vừa khôi phục trường này. Danh sách đã làm mới."],
  ])(
    "keeps the row with a neutral note when someone restored it first (%s)",
    async (by, text) => {
      renderDeleted(vi.fn().mockResolvedValue({ kind: "already-active", by }));
      fireEvent.click(await screen.findByRole("tab", { name: /Đã xoá/ }));
      fireEvent.click(
        await screen.findByRole("button", {
          name: /Khôi phục trường Ngày bàn giao/,
        }),
      );
      expect((await screen.findByRole("status")).textContent).toBe(text);
      expect(screen.queryByRole("alert")).toBeNull();
    },
  );
});
