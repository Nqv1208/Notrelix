import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import { BillingCapabilitiesSurface } from "../../billing/billing-capabilities-surface";
import { en, vi as copy } from "../feature-availability.copy";
import { FeatureUnavailableSurface } from "../feature-unavailable-surface";

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
});
