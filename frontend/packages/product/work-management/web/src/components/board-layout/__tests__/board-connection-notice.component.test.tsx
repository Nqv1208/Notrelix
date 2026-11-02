import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { BoardConnectionNotice } from "../board-connection-notice";
import { en, vi as viCopy } from "../board-header.copy";

const lostAt = new Date("2026-11-02T10:40:00+07:00");

describe("BoardConnectionNotice", () => {
  it("says nothing while the board is connected", () => {
    const { container } = renderPureUi(
      <BoardConnectionNotice
        state="connected"
        lostAt={null}
        copy={viCopy}
        locale="vi"
        onReconnect={vi.fn()}
      />,
    );
    expect(container.textContent).toBe("");
  });

  it("shows a pill while it reconnects", () => {
    renderPureUi(
      <BoardConnectionNotice
        state="reconnecting"
        lostAt={lostAt}
        copy={en}
        locale="en"
        onReconnect={vi.fn()}
      />,
    );
    expect(screen.getByRole("status").textContent).toBe("Reconnecting…");
  });

  for (const state of ["offline", "failed", "closed"] as const)
    it(`${state}: says the data may be old, that a write fails, and offers Reconnect`, () => {
      const onReconnect = vi.fn();
      renderPureUi(
        <BoardConnectionNotice
          state={state}
          lostAt={lostAt}
          copy={viCopy}
          locale="vi"
          onReconnect={onReconnect}
        />,
      );
      const notice = screen.getByRole("status");
      expect(notice.textContent).toContain("Dữ liệu có thể đã cũ");
      expect(notice.textContent).toContain("Thay đổi bạn làm sẽ báo lỗi");
      fireEvent.click(screen.getByRole("button", { name: "Kết nối lại" }));
      expect(onReconnect).toHaveBeenCalledTimes(1);
    });
});
