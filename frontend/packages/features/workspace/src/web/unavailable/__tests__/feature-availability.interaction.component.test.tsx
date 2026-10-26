import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { BillingCapabilitiesSurface } from "../../billing/billing-capabilities-surface";
import { WorkReportSurface } from "../../reports/work-report-surface";
import { en, vi as copy } from "../feature-availability.copy";
import { FeatureUnavailableSurface } from "../feature-unavailable-surface";

/** Opens the styled Select behind a label and picks one option by its text. */
function choose(trigger: HTMLElement, option: string | RegExp) {
  fireEvent.keyDown(trigger, { key: "Enter" });
  fireEvent.click(screen.getByRole("option", { name: option }));
}

describe("Feature availability", () => {
  it("FUI[workspace.feature-unavailable:integrations] says plainly that the page is not available yet, with no retry", () => {
    renderPureUi(
      <FeatureUnavailableSurface copy={copy} feature="integrations" />,
    );
    expect(screen.getByText("Tích hợp chưa khả dụng")).toBeTruthy();
    expect(screen.getByText("Chưa khả dụng")).toBeTruthy();
    expect(screen.queryByRole("button")).toBeNull();
  });

  it("FUI[workspace.feature-unavailable:reports] names the reports page and keeps the copy in English too", () => {
    renderPureUi(<FeatureUnavailableSurface copy={en} feature="reports" />);
    expect(screen.getByText("Reports are not available yet")).toBeTruthy();
  });

  it("FUI[workspace.billing-capabilities:capabilities] draws a granted capability with its use and what the page cannot do yet", () => {
    renderPureUi(
      <BillingCapabilitiesSurface
        copy={copy}
        onRetry={vi.fn()}
        state={{
          status: "ready",
          data: [
            {
              code: "AUTOMATION_RULE",
              isGranted: true,
              isUnlimited: false,
              limit: 50,
              used: 12,
              remaining: 38,
            },
          ],
        }}
      />,
    );
    expect(screen.getByText("Quy tắc tự động hoá")).toBeTruthy();
    expect(screen.getByText("Đã dùng 12 trên 50")).toBeTruthy();
    expect(
      screen.getByRole("meter", { name: "Quy tắc tự động hoá" }),
    ).toBeTruthy();
    expect(screen.getByText(/hoá đơn và phương thức thanh toán/)).toBeTruthy();
  });

  it("FUI[workspace.billing-capabilities:states] retries a failed load and tells a member why the page is closed", () => {
    const onRetry = vi.fn();
    const view = renderPureUi(
      <BillingCapabilitiesSurface
        copy={copy}
        onRetry={onRetry}
        state={{ status: "error" }}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Thử lại" }));
    expect(onRetry).toHaveBeenCalledTimes(1);
    view.rerender(
      <BillingCapabilitiesSurface
        copy={copy}
        onRetry={onRetry}
        state={{ status: "forbidden" }}
      />,
    );
    expect(
      screen.getByText("Chỉ quản trị workspace xem được thanh toán"),
    ).toBeTruthy();
  });

  it("FUI[workspace.billing-capabilities:plan] shows the current plan and lets the owner switch to another", () => {
    const onChangePlan = vi.fn();
    const plans = [
      { code: "free", name: "Free" },
      { code: "pro", name: "Pro" },
    ];
    const state = {
      status: "ready" as const,
      data: [
        {
          code: "STORAGE_GB",
          isGranted: true,
          isUnlimited: false,
          limit: 100,
          used: 12.4,
          remaining: 88,
        },
      ],
    };
    const view = renderPureUi(
      <BillingCapabilitiesSurface
        copy={en}
        onRetry={vi.fn()}
        state={state}
        plan={{
          currentCode: "pro",
          plans,
          canChange: true,
          changing: false,
          failed: false,
        }}
        onChangePlan={onChangePlan}
      />,
    );
    expect(screen.getByText("12.4 of 100 GB used")).toBeTruthy();
    expect(screen.getByText("Pro")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Switch to Free" }));
    expect(onChangePlan).toHaveBeenCalledWith("free");
    expect(screen.queryByRole("button", { name: "Switch to Pro" })).toBeNull();

    view.rerender(
      <BillingCapabilitiesSurface
        copy={en}
        onRetry={vi.fn()}
        state={state}
        plan={{
          currentCode: "pro",
          plans,
          canChange: false,
          changing: false,
          failed: false,
        }}
        onChangePlan={onChangePlan}
      />,
    );
    expect(screen.queryByRole("button", { name: /Switch to/ })).toBeNull();
  });

  it("FUI[workspace.work-report:report] draws the figures, the status split, the assignees and the weekly bars", () => {
    renderPureUi(
      <WorkReportSurface
        copy={en}
        onRetry={vi.fn()}
        onBoard={vi.fn()}
        onSpace={vi.fn()}
        scope={{ boards: [], boardId: null, spaceId: null }}
        state={{
          status: "ready",
          data: {
            total: 10,
            open: 6,
            done: 4,
            overdue: 2,
            byStatus: [
              {
                optionId: null,
                name: null,
                color: null,
                category: "Todo",
                count: 6,
              },
              {
                optionId: null,
                name: null,
                color: null,
                category: "Done",
                count: 4,
              },
            ],
            byAssignee: [
              { userId: "u", name: "An", items: 7, open: 4, overdue: 2 },
              { userId: null, name: null, items: 3, open: 2, overdue: 0 },
            ],
            completedPerWeek: [
              { weekStart: "2026-10-12T00:00:00.000Z", completed: 3 },
              { weekStart: "2026-10-19T00:00:00.000Z", completed: 1 },
            ],
            averageCompletionSeconds: 2 * 86_400,
          },
        }}
      />,
    );
    expect(screen.getByText("2 days")).toBeTruthy();
    expect(screen.getByRole("meter", { name: "To do" })).toBeTruthy();
    expect(screen.getByText("7 items, 2 overdue")).toBeTruthy();
    expect(screen.getByText("Unassigned")).toBeTruthy();
    expect(screen.getByLabelText("Week of 12/10: 3 finished")).toBeTruthy();
  });

  it("FUI[workspace.work-report:scope] narrows by board and, for people who may, by department", () => {
    const onBoard = vi.fn();
    const onSpace = vi.fn();
    const view = renderPureUi(
      <WorkReportSurface
        copy={en}
        onRetry={vi.fn()}
        onBoard={onBoard}
        onSpace={onSpace}
        scope={{
          boards: [{ id: "b1", title: "Roadmap" }],
          boardId: null,
          spaces: [{ id: "s1", name: "Engineering" }],
          spaceId: null,
        }}
        state={{ status: "loading" }}
      />,
    );
    choose(screen.getByLabelText("Board"), "Roadmap");
    expect(onBoard).toHaveBeenCalledWith("b1");
    choose(screen.getByLabelText("Department"), "Engineering");
    expect(onSpace).toHaveBeenCalledWith("s1");
    view.rerender(
      <WorkReportSurface
        copy={en}
        onRetry={vi.fn()}
        onBoard={onBoard}
        onSpace={onSpace}
        scope={{ boards: [], boardId: null, spaceId: null }}
        state={{ status: "forbidden" }}
      />,
    );
    expect(screen.queryByLabelText("Department")).toBeNull();
    expect(screen.getByText("This report is not open to you")).toBeTruthy();
  });
});
