import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { spacesCopyFixture } from "../../../verification/workspace-spaces-fixtures";
import {
  SpaceTrashSurface,
  type SpaceTrashSurfaceProps,
} from "../space-trash-surface";

const copy = spacesCopyFixture("vi");

const ROWS = [
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
] as const;

function renderTrash(overrides: Partial<SpaceTrashSurfaceProps> = {}) {
  const props: SpaceTrashSurfaceProps = {
    copy,
    rows: { status: "ready", data: ROWS },
    pendingId: null,
    failed: null,
    formatDate: (iso) => iso.slice(0, 10),
    onRestore: vi.fn(),
    onRetry: vi.fn(),
    onBack: vi.fn(),
    ...overrides,
  };
  renderPureUi(<SpaceTrashSurface {...props} />);
  return props;
}

describe("Deleted departments", () => {
  it("lists each department with when it was deleted and until when it can return", () => {
    renderTrash();
    expect(
      screen.getByRole("heading", { name: copy.trash.title }),
    ).toBeTruthy();
    expect(screen.getByText("Phòng Kế toán")).toBeTruthy();
    expect(
      screen.getByText("Xoá 2026-11-10 · Khôi phục được đến 2026-12-10"),
    ).toBeTruthy();
    expect(
      screen.getAllByRole("button", { name: copy.trash.restore }),
    ).toHaveLength(2);
  });

  it("restores the department that was chosen", () => {
    const props = renderTrash();
    fireEvent.click(
      screen.getAllByRole("button", { name: copy.trash.restore })[1]!,
    );
    expect(props.onRestore).toHaveBeenCalledWith("sp-2");
  });

  it("shows Restoring on the working row and ignores a second click", () => {
    const props = renderTrash({ pendingId: "sp-1" });
    const working = screen.getByRole("button", { name: copy.trash.restoring });
    expect(working.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(working);
    expect(props.onRestore).not.toHaveBeenCalled();
  });

  it("says why a restore was refused, in the producer's own words for a rule", () => {
    renderTrash({
      failed: {
        id: "sp-1",
        reason: "rule",
        detail: "Cannot restore a space under a parent that is not active.",
      },
    });
    expect(screen.getByRole("alert").textContent).toBe(
      "Cannot restore a space under a parent that is not active.",
    );
  });

  it("words a conflict and a vanished department without the producer's text", () => {
    renderTrash({
      failed: { id: "sp-1", reason: "conflict", detail: "raw" },
    });
    expect(screen.getByRole("alert").textContent).toBe(
      copy.trash.failures.conflict,
    );
  });

  it("shows the empty, loading, error and permission-denied states", () => {
    const { unmount } = renderPureUi(
      <SpaceTrashSurface
        copy={copy}
        rows={{ status: "ready", data: [] }}
        pendingId={null}
        failed={null}
        formatDate={String}
        onRestore={vi.fn()}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );
    expect(
      screen.getByText(/Không có phòng ban nào bị xoá trong 30 ngày qua/),
    ).toBeTruthy();
    unmount();

    const loading = renderTrash({ rows: { status: "loading" } });
    expect(screen.getByRole("status").textContent).toBe(copy.trash.loading);
    expect(loading.onRetry).not.toHaveBeenCalled();
  });

  it("offers Retry after an error and a way back after a refusal", () => {
    const failed = renderTrash({ rows: { status: "error" } });
    fireEvent.click(screen.getByRole("button", { name: copy.trash.retry }));
    expect(failed.onRetry).toHaveBeenCalledTimes(1);
  });

  it("tells a person who is not an Owner or Admin whom the page is for", () => {
    const props = renderTrash({
      rows: { status: "error", reason: "forbidden" },
    });
    expect(screen.getByRole("alert").textContent).toContain(
      copy.trash.forbidden,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.trash.back }));
    expect(props.onBack).toHaveBeenCalledTimes(1);
  });
});
