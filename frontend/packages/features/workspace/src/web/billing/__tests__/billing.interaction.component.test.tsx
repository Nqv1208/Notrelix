import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  BILLING_FIXTURE_COPY as copy,
  BILLING_FIXTURE_FORMATTERS as formatters,
  BILLING_FIXTURE_LINKS as links,
  INVITE_EMAILS,
  PLANS_FIXTURE,
  QUOTE_FIXTURE,
  SEATS_FIXTURE,
  overviewFixture,
} from "../../../verification/workspace-billing-fixtures";
import { BillingBuyDialog, BillingSeatInviteDialog } from "../billing-dialogs";
import { BillingCompareSurface } from "../billing-compare-surface";
import { BillingOverviewSurface } from "../billing-overview-surface";
import type { BillingOverviewDto } from "../billing-models";

const noop = () => undefined;

function overview(
  data: BillingOverviewDto,
  handlers: Partial<{ onBuy: () => void; onPayNow: () => void }> = {},
) {
  return renderPureUi(
    <BillingOverviewSurface
      copy={copy}
      formatters={formatters}
      links={links}
      state={{ status: "ready", data }}
      reloading={false}
      paying={false}
      onReload={noop}
      onBuy={handlers.onBuy ?? noop}
      onPayNow={handlers.onPayNow ?? noop}
      onUnavailable={noop}
      onContactSales={noop}
    />,
  );
}

describe("BillingOverviewSurface", () => {
  it("FUI[workspace.billing:overview] draws the plan, the usage meters and the payment boxes of the mockup", () => {
    const onBuy = vi.fn();
    overview(overviewFixture(), { onBuy });
    expect(screen.getByRole("heading", { name: "Business" })).toBeTruthy();
    expect(screen.getByText("Đang hoạt động")).toBeTruthy();
    expect(
      screen
        .getAllByRole("meter")
        .map((meter) => meter.getAttribute("aria-valuenow")),
    ).toEqual(["104", "312", "4210", "2"]);
    expect(screen.getByText("Còn 6 chỗ")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Mua thêm chỗ" }));
    expect(onBuy).toHaveBeenCalledOnce();
  });

  it("FUI[workspace.billing:past-due] says what the overdue invoice changes and keeps buying seats off", () => {
    const onPayNow = vi.fn();
    overview(overviewFixture({ pastDue: true }), { onPayNow });
    expect(
      screen.getByText("Hoá đơn HĐ-2026-0131 quá hạn thanh toán"),
    ).toBeTruthy();
    expect(
      screen.getByText("Dữ liệu và công việc không bị ảnh hưởng."),
    ).toBeTruthy();
    const buy = screen.getByRole("button", { name: "Mua thêm chỗ" });
    expect((buy as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(screen.getByRole("button", { name: "Thanh toán ngay" }));
    expect(onPayNow).toHaveBeenCalledOnce();
  });

  it("FUI[workspace.billing:viewer] shows the person who does not manage billing only their workspace's usage", () => {
    overview(overviewFixture({ viewer: true }));
    expect(screen.getByText(/Bạn không quản lý thanh toán\./)).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Mua thêm chỗ" })).toBeNull();
    expect(screen.queryByText("Thanh toán")).toBeNull();
  });
});

describe("BillingCompareSurface", () => {
  it("FUI[workspace.billing:compare] marks the current plan and blocks the plan the account has outgrown", () => {
    renderPureUi(
      <BillingCompareSurface
        copy={copy}
        formatters={formatters}
        links={links}
        accountName="Tập đoàn Minh An"
        state={{ status: "ready", data: PLANS_FIXTURE }}
        onContactSales={noop}
        onRetry={noop}
      />,
    );
    expect(screen.getAllByText("Gói hiện tại").length).toBeGreaterThan(0);
    expect(
      (
        screen.getByRole("button", {
          name: "Không chọn được",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });
});

describe("BillingBuyDialog", () => {
  it("FUI[workspace.billing:buy] shows the price by day and locks the form while the bank confirms", () => {
    const onConfirm = vi.fn();
    const props = {
      copy,
      formatters,
      open: true,
      quantity: 10,
      quote: { status: "ready" as const, data: QUOTE_FIXTURE },
      submitting: false,
      checking: false,
      failed: false,
      onQuantity: noop,
      onConfirm,
      onRecheck: noop,
      onChangeCard: noop,
      onClose: noop,
    };
    const { rerender } = renderPureUi(
      <BillingBuyDialog {...props} pending={null} />,
    );
    expect(screen.getByText("4.572.927 đ")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Xác nhận mua" }));
    expect(onConfirm).toHaveBeenCalledOnce();
    rerender(
      <BillingBuyDialog
        {...props}
        pending={{
          id: "p",
          quantity: 10,
          submittedAt: "2026-10-12T03:41:00.000Z",
          lastCheckedAt: "2026-10-12T03:41:00.000Z",
        }}
      />,
    );
    expect(
      screen.getByText(/Đang xác nhận thanh toán với ngân hàng/),
    ).toBeTruthy();
    expect(
      (screen.getByRole("button", { name: "Đang xử lý" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
  });
});

describe("BillingSeatInviteDialog", () => {
  it("FUI[workspace.billing:invite] stops the invitation at the seats left and offers to buy or to trim", () => {
    const onEmails = vi.fn();
    renderPureUi(
      <BillingSeatInviteDialog
        copy={copy}
        open
        workspaceName="Minh An Group"
        seats={SEATS_FIXTURE}
        emails={INVITE_EMAILS}
        role="member"
        sending={false}
        error={null}
        buyLink={links.buy(10)}
        onEmails={onEmails}
        onRole={noop}
        onSend={noop}
        onClose={noop}
      />,
    );
    expect(
      screen.getByText(/Còn 6 chỗ\. Mua thêm chỗ hoặc bớt 1 người/),
    ).toBeTruthy();
    expect(
      (
        screen.getByRole("button", {
          name: "Gửi 7 lời mời",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    fireEvent.click(screen.getByRole("button", { name: "Bớt người" }));
    expect(onEmails).toHaveBeenCalledWith(INVITE_EMAILS.slice(0, 6));
  });
});
