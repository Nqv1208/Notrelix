import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { spacesCopyFixture } from "../../../verification/workspace-spaces-fixtures";
import { SpaceTrashSurface } from "../space-trash-surface";

const copy = spacesCopyFixture("vi");

describe("Deleted departments: one restore at a time", () => {
  it("does not start a second restore while one is in flight, so no outcome is lost", () => {
    // The host keeps one pending row and one failure; a second restore would drop the first one's answer.
    const onRestore = vi.fn();
    renderPureUi(
      <SpaceTrashSurface
        copy={copy}
        rows={{
          status: "ready",
          data: [
            {
              id: "sp-1",
              name: "Phòng Kế toán",
              deletedAt: "2026-11-10T02:00:00Z",
              restorableUntil: "2026-12-10T02:00:00Z",
            },
            {
              id: "sp-2",
              name: "Phòng Marketing",
              deletedAt: "2026-11-02T02:00:00Z",
              restorableUntil: "2026-12-02T02:00:00Z",
            },
          ],
        }}
        pendingId="sp-1"
        failed={null}
        formatDate={(iso) => iso.slice(0, 10)}
        onRestore={onRestore}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );
    const other = screen.getByRole("button", { name: copy.trash.restore });
    expect(other.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(other);
    expect(onRestore).not.toHaveBeenCalled();
  });
});
