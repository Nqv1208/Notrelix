import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  createWorkReportApi,
  type WorkReportApiClient,
} from "../../../../../apps/web/src/routes/workspaces/$workspaceId/reports/work-report-api";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";

const workspaceId = mockIds.workspaces.primary;

function setup(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return {
    store,
    api: createWorkReportApi(client.api as unknown as WorkReportApiClient),
  };
}

describe("work reports in the mock world", () => {
  it("reports the whole workspace with eight weeks and a status split that adds up", async () => {
    const { api } = setup();
    const report = await api.workspace(workspaceId, null);

    expect(report.total).toBeGreaterThan(0);
    expect(report.open + report.done).toBe(report.total);
    expect(report.completedPerWeek).toHaveLength(8);
    expect(
      report.byStatus
        .filter((s) => s.category !== "None")
        .reduce((sum, s) => sum + s.count, 0),
    ).toBe(report.total);
    expect(report.byAssignee.reduce((sum, a) => sum + a.items, 0)).toBe(
      report.total,
    );
  });

  it("reports one board, which is a part of the workspace figures", async () => {
    const { api, store } = setup();
    const board = store
      .getBoards(workspaceId)
      .find((b) => b.id === mockIds.boards.erp);
    expect(board).toBeDefined();
    const one = await api.board(mockIds.boards.erp);
    const all = await api.workspace(workspaceId, null);
    expect(one.total).toBeGreaterThan(0);
    expect(one.total).toBeLessThanOrEqual(all.total);
  });

  it("lets only an owner or admin narrow the workspace report to a department", async () => {
    const { api } = setup("viewer");
    await expect(
      api.workspace(workspaceId, mockIds.spaces.engineering),
    ).rejects.toMatchObject({ kind: "forbidden" });
    await expect(api.workspace(workspaceId, null)).resolves.toBeDefined();
  });
});
