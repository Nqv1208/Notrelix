import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createMockFetch } from "../transport/create-mock-fetch";
import { MockStore } from "../state/mock-store";

/** The Reports pages of the Reports mockup in the mock world (CTR-GAP-REPORTS). */
function client() {
  const store = new MockStore();
  const { api } = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, api };
}

const WS = "/workspaces/mock-workspace-primary";
const ERP = `${WS}/reports/mock-report-erp`;

interface Widget {
  kind: string;
  [key: string]: unknown;
}
interface Report {
  id: string;
  name: string | null;
  access: string;
  summary: Record<string, number | string | null> | null;
  widgets: Widget[];
  dataAsOf: string;
}

describe("the report list", () => {
  it("lists the three reports the mockup shows, with what the viewer may do", async () => {
    const { api } = client();
    const { reports } = (await api.get(`${WS}/reports`)) as {
      reports: { name: string; access: string; mine: boolean }[];
    };
    expect(reports.map((report) => report.name).sort()).toEqual([
      "Báo cáo tuần Ban giám đốc",
      "Tổng quan triển khai ERP Q4",
      "Đóng sổ tháng 10",
    ]);
    expect(reports.filter((report) => report.mine)).toHaveLength(0);
    expect(
      reports.find((report) => report.name.startsWith("Tổng quan"))?.access,
    ).toBe("edit");
    expect(
      reports.find((report) => report.name.startsWith("Đóng sổ"))?.access,
    ).toBe("view");
  });
});

describe("the ERP report", () => {
  it("counts the ERP board the way the mockup does", async () => {
    const { api } = client();
    const report = (await api.get(ERP)) as Report;
    expect(report.summary).toMatchObject({
      done: 5,
      total: 16,
      overdue: 1,
      dueSoon: 4,
      daysToLaunch: 20,
    });
    const byKind = (kind: string) =>
      report.widgets.find((widget) => widget.kind === kind) as Widget;
    expect(byKind("status").counts).toEqual({
      todo: 6,
      doing: 3,
      review: 2,
      done: 5,
    });
    const velocity = byKind("velocity");
    expect(
      (velocity.weeks as { count: number | null }[]).map((w) => w.count),
    ).toEqual([2, 2, 1, 0, null, null]);
    expect(velocity.average as number).toBeCloseTo(5 / 3);
    expect(velocity.pace as number).toBeCloseTo(11 / 3);
    expect(velocity.verdict).toBe("late");
    const workload = byKind("workload");
    expect(
      (workload.rows as { open: number; overdue: number }[]).map((row) => [
        row.open,
        row.overdue,
      ]),
    ).toEqual([
      [2, 1],
      [2, 0],
      [2, 0],
      [1, 0],
      [1, 0],
      [1, 0],
      [1, 0],
      [1, 0],
    ]);
    const due = byKind("due");
    expect((due.items as { key: string }[]).map((item) => item.key)).toEqual([
      "ERP-138",
      "ERP-140",
      "ERP-146",
      "ERP-151",
    ]);
  });

  it("follows the board: an item finished on it moves the report", async () => {
    const { api, store } = client();
    const first = (await api.get(ERP)) as Report;
    const card = store.getCard("mock-erp-item-144");
    expect(card).toBeDefined();
    store.completeCard("mock-erp-item-144");
    const next = (await api.get(ERP)) as Report;
    expect(next.summary?.done).toBe((first.summary?.done as number) + 1);
    const velocity = next.widgets.find((w) => w.kind === "velocity") as Widget;
    expect((velocity.weeks as { count: number | null }[])[3]?.count).toBe(1);
  });

  it("narrows to one person when the report is filtered by assignee", async () => {
    const { api } = client();
    const report = (await api.get(
      `${ERP}?assignee=mock-user-erp-quan`,
    )) as Report;
    expect(report.summary).toMatchObject({ total: 2, overdue: 1 });
  });

  it("lists the open items behind one person, and refuses nobody it does not know", async () => {
    const { api } = client();
    const drill = (await api.get(
      `${ERP}/drilldown?assignee=mock-user-erp-quan`,
    )) as { items: { key: string; overdueDays: number }[] };
    expect(drill.items.map((item) => item.key)).toEqual(["ERP-142", "ERP-138"]);
    expect(drill.items[0]?.overdueDays).toBe(2);
    await expect(
      api.get(`${ERP}/drilldown?assignee=nobody`),
    ).rejects.toBeDefined();
  });
});

describe("the partial report", () => {
  it("draws the stale widget until it is recomputed", async () => {
    const { api } = client();
    const path = `${WS}/reports/mock-report-weekly`;
    const before = (await api.get(path)) as Report;
    const stale = before.widgets.find((w) => w.kind === "bars" && w.staleAsOf);
    expect(stale).toBeDefined();
    const after = (await api.post(`${path}/refresh`, {})) as Report;
    expect(after.widgets.every((widget) => !(widget as Widget).staleAsOf)).toBe(
      true,
    );
  });
});

describe("editing", () => {
  it("creates an unnamed empty report that only its creator sees as theirs", async () => {
    const { api } = client();
    const made = (await api.post(`${WS}/reports`, {})) as Report;
    expect(made.name).toBeNull();
    expect(made.widgets).toEqual([]);
    expect(made.access).toBe("edit");
    const { reports } = (await api.get(`${WS}/reports`)) as {
      reports: { id: string; mine: boolean }[];
    };
    expect(reports.find((report) => report.id === made.id)?.mine).toBe(true);
  });

  it("applies a layout, renames and deletes", async () => {
    const { api } = client();
    const made = (await api.post(`${WS}/reports`, {})) as Report;
    const path = `${WS}/reports/${made.id}`;
    const laid = (await api.post(`${path}/layout`, {
      layout: "project",
    })) as Report;
    expect(laid.widgets.map((widget) => widget.kind)).toEqual([
      "status",
      "velocity",
      "due",
    ]);
    await expect(
      api.post(`${path}/layout`, { layout: "x" }),
    ).rejects.toBeDefined();
    const named = (await api.patch(path, { name: "  Theo dõi  " })) as Report;
    expect(named.name).toBe("Theo dõi");
    await expect(api.patch(path, { name: "   " })).rejects.toBeDefined();
    await api.delete(path);
    await expect(api.get(path)).rejects.toBeDefined();
  });

  it("refuses an edit of a report the viewer may only read", async () => {
    const { api } = client();
    const path = `${WS}/reports/mock-report-close`;
    await expect(api.patch(path, { name: "x" })).rejects.toBeDefined();
    await expect(api.delete(path)).rejects.toBeDefined();
    await expect(
      api.put(`${path}/widgets`, { widgets: [] }),
    ).rejects.toBeDefined();
  });

  it("saves a layout of widgets and keeps their order", async () => {
    const { api } = client();
    const erp = (await api.get(ERP)) as Report;
    const specs = erp.widgets.map((widget) => (widget as Widget).spec);
    const reordered = [specs[1], specs[0], ...specs.slice(2)];
    const saved = (await api.put(`${ERP}/widgets`, {
      widgets: reordered,
    })) as Report;
    expect(saved.widgets.slice(0, 2).map((widget) => widget.kind)).toEqual([
      "velocity",
      "status",
    ]);
  });

  it("previews a custom widget before it is added", async () => {
    const { api } = client();
    const preview = (await api.post(`${WS}/report-widget-preview`, {
      spec: {
        kind: "columns",
        boardId: "mock-board-erp",
        groupBy: "assignee",
        metric: "count",
        filters: ["not-done"],
      },
    })) as { widget: Widget };
    expect(preview.widget.kind).toBe("columns");
    expect(preview.widget.total).toBe(11);
    expect(
      (preview.widget.rows as { count: number }[]).map((row) => row.count),
    ).toEqual([2, 2, 2, 1, 1, 1, 1, 1]);
  });
});

describe("exports", () => {
  it("runs a file for one check and finishes on the next, leaving a link in the inbox", async () => {
    const { api, store } = client();
    const user = store.getCurrentUser();
    const before = store.getNotifications(user.id).length;
    const job = (await api.post(`${ERP}/exports`, { format: "csv" })) as {
      id: string;
      status: string;
    };
    expect(job.status).toBe("running");
    const path = `${WS}/report-exports/${job.id}`;
    expect(((await api.get(path)) as { status: string }).status).toBe(
      "running",
    );
    const done = (await api.get(path)) as {
      status: string;
      downloadUrl: string;
      filename: string;
    };
    expect(done.status).toBe("done");
    expect(done.filename).toBe("tong-quan-trien-khai-erp-q4.csv");
    expect(decodeURIComponent(done.downloadUrl)).toContain("Theo trạng thái");
    expect(store.getNotifications(user.id).length).toBe(before + 1);
    await expect(
      api.post(`${ERP}/exports`, { format: "xls" }),
    ).rejects.toBeDefined();
  });
});
