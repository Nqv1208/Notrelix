import type { Meta, StoryObj } from "@storybook/react";

import {
  DRILL_QUAN,
  dashboardScenario,
  emptyReport,
  listScenario,
  viewerReport,
  weeklyReport,
  zeroReport,
} from "../../verification/workspace-reports-fixtures";
import { ReportDashboardSurface } from "./report-dashboard-surface";
import { ReportListSurface } from "./report-list-surface";

/** The Reports pages at the mockup's 1440 x 900 (Final-Reports), main area only. */
const meta: Meta = {
  title: "Workspace/Reports",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

function Page({ children }: { children: React.ReactNode }) {
  return <div className="h-225 w-296 overflow-hidden">{children}</div>;
}


export const ErpDashboard: Story = {
  render: () => (
    <Page>
      <ReportDashboardSurface {...dashboardScenario()} />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-dashboard", "fui-state--Default"],
};

export const EditLayout: Story = {
  render: () => (
    <Page>
      <ReportDashboardSurface {...dashboardScenario({ editing: true })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-dashboard", "fui-state--Default"],
};

export const MovingWidget: Story = {
  render: () => (
    <Page>
      <ReportDashboardSurface
        {...dashboardScenario({ editing: true, initialMovingId: "w-velocity" })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-dashboard", "fui-state--Default"],
};

export const AddWidget: Story = {
  render: () => (
    <Page>
      <ReportDashboardSurface
        {...dashboardScenario({ editing: true, addOpen: true })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-dashboard", "fui-state--Default"],
};

export const DrillDown: Story = {
  render: () => (
    <Page>
      <ReportDashboardSurface
        {...dashboardScenario({ drillKey: "user-quan", drill: DRILL_QUAN })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-dashboard", "fui-state--Default"],
};

export const ExportRunning: Story = {
  render: () => (
    <Page>
      <ReportDashboardSurface
        {...dashboardScenario({
          exportOpen: true,
          exportJob: {
            id: "job-1",
            format: "pdf",
            status: "running",
            startedAt: "2026-10-12T03:41:00.000Z",
            filename: "tong-quan-trien-khai-erp-q4.pdf",
            downloadUrl: null,
            expiresAt: null,
          },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-dashboard", "fui-state--Default"],
};

export const EmptyReport: Story = {
  render: () => (
    <Page>
      <ReportDashboardSurface {...dashboardScenario({ report: emptyReport() })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-dashboard", "fui-state--Empty"],
};

export const ViewerOnly: Story = {
  render: () => (
    <Page>
      <ReportDashboardSurface {...dashboardScenario({ report: viewerReport() })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-dashboard", "fui-state--ReadOnly"],
};

export const PartialNumbers: Story = {
  render: () => (
    <Page>
      <ReportDashboardSurface {...dashboardScenario({ report: weeklyReport() })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-dashboard", "fui-state--PermissionLimited"],
};

export const NothingToCount: Story = {
  render: () => (
    <Page>
      <ReportDashboardSurface {...dashboardScenario({ report: zeroReport() })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-dashboard", "fui-state--EdgeData"],
};

export const ListDefault: Story = {
  render: () => (
    <Page>
      <ReportListSurface {...listScenario()} />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-list", "fui-state--Default"],
};

export const ListLoading: Story = {
  render: () => (
    <Page>
      <ReportListSurface {...listScenario({ state: { status: "loading" } })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-list", "fui-state--Loading"],
};

export const ListEmpty: Story = {
  render: () => (
    <Page>
      <ReportListSurface
        {...listScenario({ state: { status: "ready", data: [] } })}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-list", "fui-state--Empty"],
};

export const ListError: Story = {
  render: () => (
    <Page>
      <ReportListSurface {...listScenario({ state: { status: "error" } })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-list", "fui-state--Error"],
};

export const ListMineEmpty: Story = {
  render: () => (
    <Page>
      <ReportListSurface {...listScenario({ filter: "mine" })} />
    </Page>
  ),
  tags: ["fui-surface--workspace.report-list", "fui-state--EdgeData"],
};
