import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { filtersCopy } from "../filters.copy";
import { ViewStateNotices } from "../view-state-bar";
import {
  viewControl,
  viewFields,
} from "../../../verification/view-state-fixtures";

const copy = filtersCopy("vi");
const condition = {
  fieldId: "$title",
  operator: "contains" as const,
  values: ["a"],
};

describe("the personal view read back", () => {
  it("shows three bars while the saved view is read", () => {
    renderPureUi(
      <ViewStateNotices
        copy={copy}
        view={viewControl({}, { loaded: false })}
        fields={viewFields("vi")}
      />,
    );
    expect(
      screen.getByRole("status", { name: "Đang khôi phục cách xem" }),
    ).toBeTruthy();
  });

  it("says what came back and returns to the shared view on the button", () => {
    const reset = vi.fn();
    renderPureUi(
      <ViewStateNotices
        copy={copy}
        view={viewControl(
          { filter: { join: "and", conditions: [condition] } },
          { restored: true, reset },
        )}
        fields={viewFields("vi")}
      />,
    );
    expect(screen.getByRole("status").textContent).toContain(
      "Đã khôi phục cách xem của bạn: Lọc 1.",
    );
    fireEvent.click(screen.getByRole("button", { name: "Về view chung" }));
    expect(reset).toHaveBeenCalled();
  });

  it("stays quiet once the member has changed the view", () => {
    const { container } = renderPureUi(
      <ViewStateNotices
        copy={copy}
        view={viewControl(
          { filter: { join: "and", conditions: [condition] } },
          { restored: false },
        )}
        fields={viewFields("vi")}
      />,
    );
    expect(container.textContent).not.toContain("Đã khôi phục");
  });
});
