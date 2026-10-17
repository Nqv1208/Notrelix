import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createBoardAutomationsApi } from "../../../../product/work-management/state/src/api/board-automations.api";
import { createBoardSettingsApi } from "../../../../product/work-management/state/src/api/board-settings.api";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";

const boardId = mockIds.boards.erp;

function setup(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return {
    store,
    automations: createBoardAutomationsApi(client),
    settings: createBoardSettingsApi(client),
  };
}

describe("board automations in the mock world (CTR-GAP-WM-BOARD-AUTOMATIONS)", () => {
  it("lists the four rules and the two drafts of the settings list", async () => {
    const { settings } = setup();
    const dto = await settings.get(boardId);
    expect(
      dto.automations.map((rule) => [rule.id, rule.draft ?? false]),
    ).toEqual([
      ["sa-review", false],
      ["sa-overdue", false],
      ["sa-urgent", false],
      ["sa-warehouse", false],
      ["sa-draft", true],
      ["sa-urgent-copy", true],
    ]);
  });

  it("reads a draft that passes every check and one that cannot be turned on", async () => {
    const { automations } = setup();
    const draft = await automations.get(boardId, "sa-draft");
    expect(draft.status).toBe("draft");
    expect(draft.blocked).toBe(false);
    expect(draft.checks.map((check) => check.code)).toEqual([
      "trigger-valid",
      "condition-valid",
      "team-can-see-board",
      "runner-can-notify",
    ]);
    const copy = await automations.get(boardId, "sa-urgent-copy");
    expect(copy.blocked).toBe(true);
    expect(copy.checks.map((check) => [check.level, check.code])).toEqual([
      ["error", "step-field-archived"],
      ["warning", "step-teams-reauthorization"],
      ["ok", "trigger-valid"],
      ["ok", "runner-can-notify"],
    ]);
    expect(copy.checks[0]?.params).toMatchObject({
      step: 1,
      field: "Ngày bàn giao",
      by: "Trần Minh Quân",
    });
  });

  it("refuses to turn on a rule with a blocking check, then accepts it once the field is restored", async () => {
    const { automations, settings } = setup();
    const copy = await automations.get(boardId, "sa-urgent-copy");
    await expect(
      automations.save(
        boardId,
        copy.id,
        copy.revision,
        copy.definition,
        "enable",
      ),
    ).rejects.toMatchObject({ kind: "validation" });
    const read = await settings.get(boardId);
    await settings.restoreField(boardId, read.version, "sf-handover");
    const fixed = await automations.get(boardId, "sa-urgent-copy");
    expect(fixed.blocked).toBe(false);
    await automations.save(
      boardId,
      fixed.id,
      fixed.revision,
      fixed.definition,
      "enable",
    );
    expect((await automations.get(boardId, fixed.id)).status).toBe("on");
  });

  it("turns a draft on: it joins the running list and gets version 1", async () => {
    const { automations, settings } = setup();
    const draft = await automations.get(boardId, "sa-draft");
    await automations.save(
      boardId,
      draft.id,
      draft.revision,
      draft.definition,
      "enable",
    );
    const after = await automations.get(boardId, "sa-draft");
    expect(after.status).toBe("on");
    expect(after.version).toBe(1);
    const list = await settings.get(boardId);
    expect(
      list.automations.find((rule) => rule.id === "sa-draft"),
    ).toMatchObject({ enabled: true });
    expect(list.automations.some((rule) => rule.draft)).toBe(true);
  });

  it("saves a running rule as a new version and rejects a stale revision", async () => {
    const { automations } = setup();
    const rule = await automations.get(boardId, "sa-review");
    expect(rule.version).toBe(3);
    await automations.save(
      boardId,
      rule.id,
      rule.revision,
      { ...rule.definition, name: "Gửi duyệt" },
      "version",
    );
    const next = await automations.get(boardId, "sa-review");
    expect(next.version).toBe(4);
    expect(next.versions[0]?.number).toBe(4);
    expect(next.definition.name).toBe("Gửi duyệt");
    await expect(
      automations.save(
        boardId,
        rule.id,
        rule.revision,
        rule.definition,
        "version",
      ),
    ).rejects.toMatchObject({ kind: "conflict" });
  });

  it("creates a blank draft and changes who runs a rule", async () => {
    const { automations } = setup();
    const made = await automations.create(boardId);
    const blank = await automations.get(boardId, made.id);
    expect(blank).toMatchObject({ status: "draft", version: 0 });
    expect(blank.definition.steps).toHaveLength(0);
    expect(blank.checks.some((check) => check.code === "no-steps")).toBe(true);
    await automations.changeRunner(boardId, "sa-review", "p-minh");
    const rule = await automations.get(boardId, "sa-review");
    expect(rule.runAs.name).toBe("Hồ Quang Minh");
  });

  it("reads the run log with its nine runs and one failed run with fixes", async () => {
    const { automations } = setup();
    const log = await automations.runs(boardId);
    expect(log.runs).toHaveLength(9);
    expect(log.runs.filter((run) => run.outcome === "success")).toHaveLength(4);
    expect(log.runs.filter((run) => run.outcome === "error")).toHaveLength(1);
    expect(log.runs.filter((run) => run.outcome === "skipped")).toHaveLength(4);
    const failed = log.runs.find((run) => run.outcome === "error")!;
    expect(failed.steps.map((step) => step.outcome)).toEqual([
      "success",
      "error",
    ]);
    expect(failed.fixes.map((fix) => fix.kind)).toEqual([
      "reauthorize",
      "rerun-step",
    ]);
    expect(failed.fixes[1]?.blockedReason).not.toBeNull();
    await expect(
      automations.rerunStep(boardId, failed.id, 2),
    ).rejects.toMatchObject({ kind: "validation" });
  });

  it("is only for a manager of the board", async () => {
    const { automations } = setup("member");
    await expect(automations.get(boardId, "sa-draft")).rejects.toMatchObject({
      kind: "forbidden",
    });
    await expect(automations.runs(boardId)).rejects.toMatchObject({
      kind: "forbidden",
    });
  });
});
