import type { Meta, StoryObj } from "@storybook/react";

import {
  BILLING_FIXTURE_COPY as copy,
  BILLING_FIXTURE_FORMATTERS as formatters,
  BILLING_FIXTURE_LINKS as links,
  DOWNGRADE_FIXTURE,
  INVITE_EMAILS,
  INVOICES_FIXTURE,
  INVOICE_DETAIL_FIXTURE,
  PLANS_FIXTURE,
  QUOTE_FIXTURE,
  SEATS_FIXTURE,
  overviewFixture,
} from "../../verification/workspace-billing-fixtures";
import { BillingBuyDialog, BillingSeatInviteDialog } from "./billing-dialogs";
import { BillingCompareSurface } from "./billing-compare-surface";
import { BillingDowngradeSurface } from "./billing-downgrade-surface";
import { BillingInvoicesSurface } from "./billing-invoices-surface";
import { BillingOverviewSurface } from "./billing-overview-surface";
import type { BillingOverviewDto } from "./billing-models";

/** The Plans and billing pages at the mockup's 1440 width (Final-Billing), content area only. */
const meta: Meta = {
  title: "Workspace/Billing",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;

function Page({ children }: { children: React.ReactNode }) {
  return <div className="w-296 bg-background p-7">{children}</div>;
}

const overview = (
  state:
    | { status: "ready"; data: BillingOverviewDto }
    | { status: "loading" }
    | { status: "error" },
) => (
  <Page>
    <BillingOverviewSurface
      copy={copy}
      formatters={formatters}
      links={links}
      state={state}
      reloading={false}
      paying={false}
      onReload={noop}
      onBuy={noop}
      onPayNow={noop}
      onUnavailable={noop}
      onContactSales={noop}
    />
  </Page>
);

export const Overview: Story = {
  render: () => overview({ status: "ready", data: overviewFixture() }),
  tags: ["fui-surface--workspace.billing", "fui-state--Default"],
};

export const PastDue: Story = {
  render: () =>
    overview({ status: "ready", data: overviewFixture({ pastDue: true }) }),
  tags: ["fui-surface--workspace.billing", "fui-state--EdgeData"],
};

export const ViewerOnly: Story = {
  render: () =>
    overview({ status: "ready", data: overviewFixture({ viewer: true }) }),
  tags: ["fui-surface--workspace.billing", "fui-state--ReadOnly"],
};

export const OverviewLoading: Story = {
  render: () => overview({ status: "loading" }),
  tags: ["fui-surface--workspace.billing", "fui-state--Loading"],
};

export const OverviewError: Story = {
  render: () => overview({ status: "error" }),
  tags: ["fui-surface--workspace.billing", "fui-state--Error"],
};

export const Compare: Story = {
  render: () => (
    <Page>
      <BillingCompareSurface
        copy={copy}
        formatters={formatters}
        links={links}
        accountName="Tập đoàn Minh An"
        state={{ status: "ready", data: PLANS_FIXTURE }}
        onContactSales={noop}
        onRetry={noop}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.billing", "fui-state--Default"],
};

export const Invoices: Story = {
  render: () => (
    <Page>
      <BillingInvoicesSurface
        copy={copy}
        formatters={formatters}
        links={links}
        accountName="Tập đoàn Minh An"
        state={{ status: "ready", data: INVOICES_FIXTURE }}
        selectedId={INVOICES_FIXTURE.invoices[0]!.id}
        detail={{ status: "ready", data: INVOICE_DETAIL_FIXTURE }}
        downloading={false}
        pdfFailed={false}
        onDownload={noop}
        onRetry={noop}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.billing", "fui-state--Default"],
};

export const InvoicesEmpty: Story = {
  render: () => (
    <Page>
      <BillingInvoicesSurface
        copy={copy}
        formatters={formatters}
        links={links}
        accountName="Tập đoàn Minh An"
        state={{
          status: "ready",
          data: { email: "ketoan@minhan.vn", invoices: [] },
        }}
        selectedId={null}
        detail={null}
        downloading={false}
        pdfFailed={false}
        onDownload={noop}
        onRetry={noop}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.billing", "fui-state--Empty"],
};

export const Downgrade: Story = {
  render: () => (
    <Page>
      <BillingDowngradeSurface
        copy={copy}
        formatters={formatters}
        links={links}
        state={{ status: "ready", data: DOWNGRADE_FIXTURE }}
        saving={false}
        onConfirm={noop}
        onCancelRequest={noop}
        onRetry={noop}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.billing", "fui-state--Default"],
};

const buy = (pending: boolean) => (
  <BillingBuyDialog
    copy={copy}
    formatters={formatters}
    open
    quantity={10}
    quote={{ status: "ready", data: QUOTE_FIXTURE }}
    pending={
      pending
        ? {
            id: "purchase-1",
            quantity: 10,
            submittedAt: "2026-10-12T03:41:00.000Z",
            lastCheckedAt: "2026-10-12T03:41:00.000Z",
          }
        : null
    }
    submitting={false}
    checking={false}
    failed={false}
    onQuantity={noop}
    onConfirm={noop}
    onRecheck={noop}
    onChangeCard={noop}
    onClose={noop}
  />
);

export const BuySeats: Story = {
  render: () => buy(false),
  tags: ["fui-surface--workspace.billing", "fui-state--Default"],
};

export const BuyPending: Story = {
  render: () => buy(true),
  tags: ["fui-surface--workspace.billing", "fui-state--Default"],
};

export const InviteBeyondSeats: Story = {
  render: () => (
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
      onEmails={noop}
      onRole={noop}
      onSend={noop}
      onClose={noop}
    />
  ),
  tags: ["fui-surface--workspace.billing", "fui-state--PermissionLimited"],
};
