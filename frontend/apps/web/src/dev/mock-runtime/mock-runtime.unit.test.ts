import { describe, expect, it } from "vitest";
import { endpoints } from "@notrelix/contracts";
import { createWebMockRuntime } from "./index";
import { MockUnhandledOperationError } from "./transport/mock-unhandled-operation-error";
import { createWorkManagementServices } from "@notrelix/work-management-state";
import { createPageApi, createBlockApi, createCommentApi } from "@notrelix/docs-state";
import { createNotificationsService } from "@notrelix/features-notifications";
import { readMockRuntimeConfig } from "./config/read-mock-runtime-config";
import { mockIds } from "./state/mock-ids";

const defaultConfig = {
  persona: "owner" as const,
  scenario: "default" as const,
  latencyMs: 0,
};

describe("Web mock runtime", () => {
  it("serves auth and workspace through the NotrelixClient interface", async () => {
    const runtime = createWebMockRuntime(defaultConfig);
    const profile = await runtime.api.api.get<{ id: string }>(endpoints.auth.profile);
    const workspaces = await runtime.api.api.get<Array<{ id: string }>>(endpoints.workspaces.list);

    expect(profile.id).toBe("mock-user-owner");
    expect(workspaces.map(({ id }) => id)).toContain("mock-workspace-primary");
    expect(runtime.journal.getEntries().map(({ matchedHandlerId }) => matchedHandlerId)).toEqual([
      "auth.profile",
      "workspace.list",
    ]);
  });

  it("ends and resets the mock authentication session", async () => {
    const runtime = createWebMockRuntime(defaultConfig);

    await runtime.api.api.post(endpoints.auth.logout, {});
    await expect(runtime.api.api.get(endpoints.auth.profile)).rejects.toMatchObject({
      kind: "auth",
      status: 401,
    });

    runtime.store.reset();
    await expect(runtime.api.api.get(endpoints.auth.profile)).resolves.toMatchObject({
      id: expect.any(String),
    });
  });

  it("is closed-world for unmatched operations", async () => {
    const runtime = createWebMockRuntime(defaultConfig);
    await expect(runtime.api.api.get("/unknown-operation")).rejects.toBeInstanceOf(
      MockUnhandledOperationError,
    );
  });

  it("persists a deterministic workspace mutation and resets the store", async () => {
    const runtime = createWebMockRuntime(defaultConfig);
    const created = await runtime.api.api.post<{ id: string }, { name: string; slug: string; isPersonal: boolean }>(
      endpoints.workspaces.list,
      { name: "Created Workspace", slug: "created-workspace", isPersonal: false },
    );
    expect(created.id).toBe("mock-created-workspace-0001");
    expect((await runtime.api.api.get<Array<{ id: string }>>(endpoints.workspaces.list)).some(({ id }) => id === created.id)).toBe(true);

    runtime.store.reset();
    expect((await runtime.api.api.get<Array<{ id: string }>>(endpoints.workspaces.list)).some(({ id }) => id === created.id)).toBe(false);
  });

  it("keeps new-user distinct from an error response", async () => {
    const newUser = createWebMockRuntime({ ...defaultConfig, scenario: "new-user" });
    expect(await newUser.api.api.get(endpoints.workspaces.list)).toEqual([]);

    const error = createWebMockRuntime({ ...defaultConfig, scenario: "error" });
    await expect(error.api.api.get(endpoints.workspaces.list)).rejects.toMatchObject({ kind: "network" });
  });

  it("builds deterministic empty, large, permissions, and isolated scenario graphs", () => {
    const empty = createWebMockRuntime({ ...defaultConfig, scenario: "empty" });
    expect(empty.store.getVisibleWorkspaces()).toHaveLength(1);
    expect(empty.store.getSnapshot().boards).toEqual([]);
    expect(empty.store.getSnapshot().pages).toEqual([]);

    const large = createWebMockRuntime({ ...defaultConfig, scenario: "large" });
    expect(large.store.getVisibleWorkspaces().length).toBeGreaterThan(20);

    const viewer = createWebMockRuntime({ ...defaultConfig, persona: "viewer", scenario: "permissions" });
    expect(viewer.store.getCurrentUser().id).toBe(mockIds.users.viewer);
    expect(viewer.store.getVisibleWorkspaces()).toHaveLength(1);

    const second = createWebMockRuntime(defaultConfig);
    empty.store.update((draft) => { draft.workspaces[0]!.name = "Changed only here"; });
    expect(second.store.getSnapshot().workspaces[0]?.name).toBe("Notrelix Product Lab");
  });

  it("keeps canonical IDs unique and rejects invalid configuration", () => {
    const flatten = (value: unknown): string[] => typeof value === "string" ? [value] : Object.values(value as Record<string, unknown>).flatMap(flatten);
    const ids = flatten(mockIds);
    expect(new Set(ids).size).toBe(ids.length);
    expect(() => readMockRuntimeConfig({ VITE_MOCK_PERSONA: "invalid" })).toThrow(/VITE_MOCK_PERSONA/);
    expect(() => readMockRuntimeConfig({ VITE_MOCK_LATENCY_MS: "-1" })).toThrow(/VITE_MOCK_LATENCY_MS/);
  });

  it("honors an already-aborted request signal", async () => {
    const runtime = createWebMockRuntime({ ...defaultConfig, latencyMs: 10 });
    const controller = new AbortController();
    controller.abort();
    await expect(runtime.api.api.get(endpoints.workspaces.list, { signal: controller.signal })).rejects.toMatchObject({ kind: "aborted" });
  });

  it("uses an in-process realtime adapter", async () => {
    const runtime = createWebMockRuntime(defaultConfig);
    await runtime.realtime.connect({ sessionGeneration: "mock-session" });
    expect(runtime.realtime.getState()).toBe("connected");
    runtime.realtime.dispose();
    expect(runtime.realtime.getState()).toBe("closed");
  });

  it("runs Work Management wire DTOs through production mappers and persists mutations", async () => {
    const runtime = createWebMockRuntime(defaultConfig);
    const services = createWorkManagementServices(runtime.api);
    const boards = await services.boards.getBoardsByWorkspaceId("mock-workspace-primary");
    expect(boards[0]?.title).toBe("Product Roadmap");

    const full = await services.boards.getFullBoard("mock-board-roadmap", { workspaceId: "mock-workspace-primary" });
    expect(full.groups.flatMap((group) => group.cards).map((card) => card.title)).toContain("Ship mock runtime");

    await services.cards.moveCard({ cardId: "mock-card-research", listId: "mock-group-doing", position: 1 });
    expect(runtime.store.getSnapshot().cards.find(({ id }) => id === "mock-card-research")?.listId).toBe("mock-group-doing");

    const label = await services.labels.createLabel({ boardId: "mock-board-roadmap", name: "Runtime", color: "#123456" });
    await services.labels.updateLabel({ boardId: "mock-board-roadmap", labelId: label.id, name: "Runtime verified" });
    await services.labels.addLabelToCard("mock-card-research", label.id);
    expect(runtime.store.getSnapshot().cards.find(({ id }) => id === "mock-card-research")?.labels[0]?.name).toBe("Runtime verified");
    await services.labels.removeLabelFromCard("mock-card-research", label.id);
    await services.labels.deleteLabel("mock-board-roadmap", label.id);

    const checklistId = await services.checklists.createChecklist({ cardId: "mock-card-research", title: "Certification" });
    const itemId = await services.checklists.createChecklistItem({ checklistId, title: "Run production adapters" });
    await services.checklists.updateChecklistItem({ itemId, isChecked: true });
    expect((await services.checklists.getChecklists("mock-card-research"))[0]?.items[0]?.isChecked).toBe(true);
    await services.checklists.deleteChecklistItem(itemId);
    await services.checklists.deleteChecklist(checklistId);
  });

  it("runs Documents adapters through wire projections and persists page/block mutations", async () => {
    const runtime = createWebMockRuntime(defaultConfig);
    const pages = createPageApi(runtime.api.api, runtime.api.endpoints);
    const blocks = createBlockApi(runtime.api.api, runtime.api.endpoints);
    const comments = createCommentApi(runtime.api.api, runtime.api.endpoints);
    expect((await pages.getList("mock-workspace-primary")).map(({ title }) => title)).toContain("Product specification");

    const createdPage = await pages.create({ workspaceId: "mock-workspace-primary", title: "Runtime notes" });
    expect(createdPage.id).toBe("mock-created-page-0001");
    const createdBlock = await blocks.create(createdPage.id, { type: "paragraph", properties: { text: "Created through the real adapter." } });
    expect(createdBlock.id).toBe("mock-created-block-0001");
    expect(await blocks.batchUpdate(createdPage.id, [{ properties: { text: "Batch updated." } }])).toHaveLength(1);

    const comment = await comments.create(createdPage.id, { pageId: createdPage.id, body: "Mock runtime comment" });
    expect((await comments.getList(createdPage.id))[0]?.body).toBe("Mock runtime comment");
    await comments.delete(comment.id);
    expect(await comments.getList(createdPage.id)).toEqual([]);
  });

  it("persists official notification read operations without service-local mock fallback", async () => {
    const runtime = createWebMockRuntime(defaultConfig);
    const service = createNotificationsService(runtime.api.api, runtime.api.endpoints);
    expect((await service.getList()).filter(({ isRead }) => !isRead)).toHaveLength(2);
    await service.markAsRead("mock-notification-mention");
    expect((await service.getList()).filter(({ isRead }) => !isRead)).toHaveLength(1);
    await service.markAllAsRead();
    expect((await service.getList()).filter(({ isRead }) => !isRead)).toHaveLength(0);
  });
});
