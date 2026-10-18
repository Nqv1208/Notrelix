import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  CATALOG_FIXTURE,
  INTEGRATIONS_FIXTURE_COPY as copy,
  INTEGRATIONS_FIXTURE_LINKS as links,
  TEAMS_DETAIL,
  listFixture,
} from "../../../verification/workspace-integrations-fixtures";
import {
  IntegrationConnectDialog,
  IntegrationDetailSurface,
  IntegrationsListSurface,
} from "../integrations-surface";

const noop = () => undefined;

describe("Integrations", () => {
  it("FUI[workspace.integrations:list] draws the connections with their two statuses and the catalog an admin may add from", () => {
    const onConnect = vi.fn();
    renderPureUi(
      <IntegrationsListSurface
        copy={copy}
        links={links}
        state={{ status: "ready", data: listFixture() }}
        onReload={noop}
        onConnect={onConnect}
      />,
    );
    expect(screen.getByText("Đang dùng 3")).toBeTruthy();
    expect(screen.getByText("Cần uỷ quyền lại")).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: "Kết nối Google Drive" }),
    );
    expect(onConnect).toHaveBeenCalledWith("google-drive");
  });

  it("FUI[workspace.integrations:member] shows a member the list without the catalog or the open buttons", () => {
    renderPureUi(
      <IntegrationsListSurface
        copy={copy}
        links={links}
        state={{ status: "ready", data: listFixture(false) }}
        onReload={noop}
        onConnect={noop}
      />,
    );
    expect(
      screen.getByText(/Chỉ quản trị workspace thêm hoặc sửa tích hợp/),
    ).toBeTruthy();
    expect(
      screen.queryByRole("list", { name: "Tích hợp có thể thêm" }),
    ).toBeNull();
    expect(screen.queryByRole("link", { name: /^Mở / })).toBeNull();
  });

  it("FUI[workspace.integrations:detail] says what the expired authorization holds back and offers the way out", () => {
    renderPureUi(
      <IntegrationDetailSurface
        copy={copy}
        links={links}
        state={{ status: "ready", data: TEAMS_DETAIL }}
        busy={false}
        onReload={noop}
        onReauthorize={noop}
        onDisconnect={noop}
        onTest={noop}
        onPause={noop}
        onRegenerate={noop}
      />,
    );
    expect(screen.getByText(/Tin nhắn mới tạm giữ, không mất/)).toBeTruthy();
    expect(
      screen.getByRole("button", { name: /Uỷ quyền lại với Microsoft/ }),
    ).toBeTruthy();
  });

  it("FUI[workspace.integrations:connect] makes the person choose a scope before leaving for the provider", () => {
    const onScope = vi.fn();
    renderPureUi(
      <IntegrationConnectDialog
        copy={copy}
        links={links}
        open
        item={CATALOG_FIXTURE[0]!}
        workspaceName="Minh An Group"
        scope="workspace"
        submitting={false}
        onScope={onScope}
        onNext={noop}
        onClose={noop}
      />,
    );
    fireEvent.click(screen.getByRole("radio", { name: /Chỉ một board/ }));
    expect(onScope).toHaveBeenCalledWith("board");
    expect(
      screen.getByRole("button", { name: /Tiếp tục với Google/ }),
    ).toBeTruthy();
  });
});
