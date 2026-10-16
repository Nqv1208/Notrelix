import type { Meta, StoryObj } from "@storybook/react";

import {
  CATALOG_FIXTURE,
  INTEGRATIONS_FIXTURE_COPY as copy,
  INTEGRATIONS_FIXTURE_LINKS as links,
  TEAMS_DETAIL,
  WEBHOOK_DETAIL,
  listFixture,
} from "../../verification/workspace-integrations-fixtures";
import {
  IntegrationConnectDialog,
  IntegrationDetailSurface,
  IntegrationDisconnectDialog,
  IntegrationUpgradeSheet,
  IntegrationsListSurface,
} from "./integrations-surface";

/** The Integrations pages at the mockup's 1440 width (Final-Integrations), content area only. */
const meta: Meta = {
  title: "Workspace/Integrations",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

const noop = () => undefined;
const Page = ({ children }: { children: React.ReactNode }) => (
  <div className="w-296 bg-background p-7">{children}</div>
);
const list = (
  state: Parameters<typeof IntegrationsListSurface>[0]["state"],
) => (
  <Page>
    <IntegrationsListSurface
      copy={copy}
      links={links}
      state={state}
      onReload={noop}
      onConnect={noop}
    />
  </Page>
);
const detail = (data: typeof TEAMS_DETAIL) => (
  <Page>
    <IntegrationDetailSurface
      copy={copy}
      links={links}
      state={{ status: "ready", data }}
      busy={false}
      onReload={noop}
      onReauthorize={noop}
      onDisconnect={noop}
      onTest={noop}
      onPause={noop}
      onRegenerate={noop}
    />
  </Page>
);

export const ListDefault: Story = {
  render: () => list({ status: "ready", data: listFixture() }),
  tags: ["fui-surface--workspace.integrations", "fui-state--Default"],
};
export const ListMember: Story = {
  render: () => list({ status: "ready", data: listFixture(false) }),
  tags: ["fui-surface--workspace.integrations", "fui-state--PermissionLimited"],
};
export const ListLoading: Story = {
  render: () => list({ status: "loading" }),
  tags: ["fui-surface--workspace.integrations", "fui-state--Loading"],
};
export const ListError: Story = {
  render: () => list({ status: "error" }),
  tags: ["fui-surface--workspace.integrations", "fui-state--Error"],
};
export const ListEmpty: Story = {
  render: () => list({ status: "ready", data: { ...listFixture(), rows: [] } }),
  tags: ["fui-surface--workspace.integrations", "fui-state--Empty"],
};
export const NeedsReauthorization: Story = {
  render: () => detail(TEAMS_DETAIL),
  tags: ["fui-surface--workspace.integrations", "fui-state--Default"],
};
export const WebhookRetrying: Story = {
  render: () => detail(WEBHOOK_DETAIL),
  tags: ["fui-surface--workspace.integrations", "fui-state--Default"],
};
export const DetailError: Story = {
  render: () => (
    <Page>
      <IntegrationDetailSurface
        copy={copy}
        links={links}
        state={{ status: "error" }}
        busy={false}
        onReload={noop}
        onReauthorize={noop}
        onDisconnect={noop}
        onTest={noop}
        onPause={noop}
        onRegenerate={noop}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.integrations", "fui-state--EdgeData"],
};

export const ConnectDialog: Story = {
  render: () => (
    <IntegrationConnectDialog
      copy={copy}
      links={links}
      open
      item={CATALOG_FIXTURE[0]!}
      workspaceName="Minh An Group"
      scope="workspace"
      submitting={false}
      onScope={noop}
      onNext={noop}
      onClose={noop}
    />
  ),
  tags: ["fui-surface--workspace.integrations", "fui-state--Default"],
};
export const DisconnectDialog: Story = {
  render: () => (
    <IntegrationDisconnectDialog
      copy={copy}
      links={links}
      open
      detail={TEAMS_DETAIL}
      submitting={false}
      onConfirm={noop}
      onClose={noop}
    />
  ),
  tags: ["fui-surface--workspace.integrations", "fui-state--Default"],
};
export const UpgradeSheet: Story = {
  render: () => (
    <IntegrationUpgradeSheet
      copy={copy}
      links={links}
      open
      item={CATALOG_FIXTURE[1]!}
      plan={listFixture().plan}
      onContact={noop}
      onClose={noop}
    />
  ),
  tags: ["fui-surface--workspace.integrations", "fui-state--ReadOnly"],
};
