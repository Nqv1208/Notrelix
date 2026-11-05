import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { filtersCopy } from "../filters.copy";
import { ViewStateNotices } from "../view-state-bar";
import {
  erpViewBar,
  viewControl,
  viewFields,
} from "../../../verification/view-state-fixtures";

const state = {
  filter: {
    join: "and" as const,
    conditions: [
      {
        fieldId: "$label",
        operator: "has_any" as const,
        values: ["label-deleted"],
      },
      {
        fieldId: "$label",
        operator: "has_any" as const,
        values: ["label-security"],
      },
    ],
  },
};

describe("a filter on a label the board deleted", () => {
  it("draws its chip as ignored and keeps the live label's chip as it was", () => {
    const { container } = renderPureUi(<>{erpViewBar({ state })}</>);
    const stale = container.querySelectorAll("[data-stale]");
    expect(stale).toHaveLength(1);
    expect(stale[0]!.textContent).toContain("lựa chọn đã xoá");
    expect(screen.getByText("Nhãn: Bảo mật")).toBeTruthy();
  });

  it("says so under the bar, and removing the ignored conditions keeps the live one", () => {
    const setFilter = vi.fn();
    renderPureUi(
      <ViewStateNotices
        copy={filtersCopy("vi")}
        view={viewControl(state, { setFilter })}
        fields={viewFields("vi")}
      />,
    );
    expect(screen.getByRole("status").textContent).toContain(
      "1 điều kiện dùng nhãn đã bị xoá",
    );
    fireEvent.click(
      screen.getByRole("button", { name: "Xoá điều kiện bị bỏ qua" }),
    );
    expect(setFilter).toHaveBeenCalledWith({
      join: "and",
      conditions: [state.filter.conditions[1]],
    });
  });
});
