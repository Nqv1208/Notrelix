import { describe, it, expect } from "vitest";
import { buildOperationRegistry } from "../operations/build-registry";
import { createMockFetch } from "../transport/create-mock-fetch";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import {
  createBoardApi,
  createGroupApi,
  createListApi,
  createCardApi,
  createColumnApi,
  createLabelApi,
  createChecklistApi,
  createCommentApi,
} from "../../../../product/work-management/state/src/index";
import { createAccountService } from "../../../../features/account/src/core/api/account.service";

const CONFORMANCE_CATALOG = [
  "identity.profile",
  "identity.login",
  "identity.register",
  "identity.refresh",
  "identity.logout",
  "identity.forgotPassword",
  "identity.resetPassword",
  "workspace.list",
  "workspace.create",
  "workspace.get",
  "workspace.views.list",
  "workspace.members.list",
  "account.preferences.get",
  "account.preferences.update",
  "account.profile.update",
  "account.security.get",
  "notifications.list",
  "notifications.read",
  "notifications.readAll",
  "boards.listByWorkspace",
  "boards.detail",
  "boards.full",
  "boards.create",
  "boards.view.get",
  "boards.view.save",
  "boards.columns.list",
  "boards.columns.create",
  "boards.columns.update",
  "boards.columns.delete",
  "boards.columns.reorder",
  "boards.labels.list",
  "boards.labels.create",
  "boards.labels.update",
  "boards.labels.delete",
  "lists.byBoard",
  "lists.create",
  "lists.update",
  "lists.delete",
  "lists.duplicate",
  "lists.reorder",
  "cards.byList",
  "cards.detail",
  "cards.create",
  "cards.update",
  "cards.delete",
  "cards.archive",
  "cards.duplicate",
  "cards.move",
  "cards.fieldValues",
  "cards.attachments",
  "cards.activity",
  "cards.labels.list",
  "cards.labels.add",
  "cards.labels.remove",
  "checklists.list",
  "checklists.create",
  "checklists.update",
  "checklists.delete",
  "checklistItems.create",
  "checklistItems.update",
  "checklistItems.delete",
  "comments.list",
  "comments.create",
  "comments.update",
  "comments.delete",
  "pages.list",
  "pages.tree",
  "pages.detail",
  "pages.create",
  "pages.update",
  "pages.delete",
  "pages.breadcrumb",
  "pages.blocks",
] as const;

describe("MFB-FZ-07: Contract Behavior and Conformance Matrix", () => {
  it("T-MFB-001: real createNotrelixClient loads endpoints via mockFetch", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    const user = await client.api.get<{ id: string }>(endpoints.auth.profile);
    expect(user.id).toBe(store.getCurrentUser().id);
  });

  it("T-MFB-002: unmapped operation fails closed with error and never falls back to network", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    await expect(
      client.api.get("/api/v1/completely/unmapped/endpoint"),
    ).rejects.toThrow();
  });

  it("T-MFB-023: contract catalog closure — all registered operations match the conformance catalog", () => {
    const registry = buildOperationRegistry();
    const registeredIds = new Set(registry.operationIds());
    const catalogIds = new Set(CONFORMANCE_CATALOG);

    expect(registeredIds).toEqual(catalogIds);
  });

  it("T-MFB-024: GET operations do not mutate store state", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    const snapshotBefore = JSON.stringify(store.getSnapshot());

    // Call various GET endpoints
    await client.api.get(endpoints.auth.profile);
    await client.api.get(endpoints.workspaces.list);
    await client.api.get(
      endpoints.workspaces.detail(mockIds.workspaces.primary),
    );
    await client.api.get(
      endpoints.boards.listByWorkspaceId(mockIds.workspaces.primary),
    );
    await client.api.get(endpoints.notifications.list);
    await client.api.get(endpoints.boards.view(mockIds.boards.roadmap));
    await client.api.get(endpoints.boards.columns(mockIds.boards.roadmap));
    await client.api.get(endpoints.boards.labels(mockIds.boards.roadmap));
    await client.api.get(endpoints.pages.list(mockIds.workspaces.primary));

    const snapshotAfter = JSON.stringify(store.getSnapshot());
    expect(snapshotAfter).toBe(snapshotBefore);
  });

  it("T-MFB-025: production Work Management consumers execute truthful read-after-write mutations", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    const boardApi = createBoardApi(client);
    const groupApi = createGroupApi(client);
    const listApi = createListApi(client);
    const cardApi = createCardApi(client);
    const columnApi = createColumnApi(client);
    const labelApi = createLabelApi(client);
    const checklistApi = createChecklistApi(client);
    const commentApi = createCommentApi(client);

    const boardId = mockIds.boards.roadmap;

    // 1. Board View read/save
    const initialView = await boardApi.getBoardView(boardId);
    expect(initialView.viewMode).toBe("table");
    await boardApi.saveBoardView(boardId, {
      viewMode: "kanban",
      viewConfig: { ...initialView.viewConfig, groupBy: "status" },
    });
    const updatedView = await boardApi.getBoardView(boardId);
    expect(updatedView.viewMode).toBe("kanban");
    expect(updatedView.viewConfig.groupBy).toBe("status");

    // 2. Group / List CRUD + Reorder
    const listId = await groupApi.createGroup({
      boardId,
      title: "QA Review",
      color: "#FF00FF",
    });
    expect(typeof listId).toBe("string");
    expect(listId.startsWith("list-")).toBe(true);

    await listApi.updateList({
      listId,
      title: "QA In Review",
      color: "#00FFFF",
    });
    const fullBoard = await boardApi.getFullBoard(boardId, {
      workspaceId: mockIds.workspaces.primary,
    });
    const createdGroup = fullBoard.groups.find((g) => g.id === listId);
    expect(createdGroup?.title).toBe("QA In Review");
    expect(createdGroup?.color).toBe("#00FFFF");

    // Reorder groups
    await groupApi.reorderGroups(boardId, [
      { id: "list-done", position: 0 },
      { id: "list-inprogress", position: 1 },
      { id: "list-todo", position: 2 },
      { id: listId, position: 3 },
    ]);
    const reorderedBoard = await boardApi.getFullBoard(boardId, {
      workspaceId: mockIds.workspaces.primary,
    });
    expect(reorderedBoard.groups[0]?.id).toBe("list-done");

    // Duplicate list
    const duplicatedListId = await listApi.duplicateList(listId);
    expect(typeof duplicatedListId).toBe("string");

    // Delete list
    await listApi.deleteList(duplicatedListId);

    // 3. Card CRUD + Move + Archive + FieldValues + Labels + Checklists + Comments
    const createdCard = await cardApi.createCard(boardId, {
      listId,
      title: "Automated Verification Card",
      position: 0,
    });
    expect(createdCard.title).toBe("Automated Verification Card");
    expect(createdCard.listId).toBe(listId);

    await cardApi.updateCard(createdCard.id, {
      title: "Updated Verification Card",
      descriptionMd: "Detailed description of verification step.",
    });
    const fetchedCard = await cardApi.getCard(createdCard.id);
    expect(fetchedCard.title).toBe("Updated Verification Card");
    expect(fetchedCard.descriptionMd).toBe(
      "Detailed description of verification step.",
    );

    // Move card
    await cardApi.moveCard({
      cardId: createdCard.id,
      listId: "list-todo",
      position: 5,
    });
    const movedCard = await cardApi.getCard(createdCard.id);
    expect(movedCard.listId).toBe("list-todo");

    // Duplicate card
    const duplicatedCardId = await cardApi.duplicateCard(createdCard.id);
    expect(typeof duplicatedCardId).toBe("string");
    await cardApi.deleteCard(duplicatedCardId);

    // Columns / Fields
    const colId = await columnApi.createColumn({
      boardId,
      name: "Estimate",
      fieldType: "number",
      position: 5,
    });
    expect(typeof colId).toBe("string");
    await columnApi.updateColumn({
      boardId,
      columnId: colId,
      name: "Story Points",
    });
    await columnApi.deleteColumn(boardId, colId);

    // Labels
    const newLabel = await labelApi.createLabel({
      boardId,
      name: "Frontend",
      color: "#00E5FF",
    });
    expect(newLabel.name).toBe("Frontend");
    await labelApi.addLabelToCard(createdCard.id, newLabel.id);
    const cardLabels = await client.api.get<{ id: string }[]>(
      endpoints.cards.labels(createdCard.id),
    );
    expect(cardLabels.some((l) => l.id === newLabel.id)).toBe(true);
    await labelApi.removeLabelFromCard(createdCard.id, newLabel.id);
    await labelApi.deleteLabel(boardId, newLabel.id);

    // Checklists & Items
    const chkId = await checklistApi.createChecklist({
      cardId: createdCard.id,
      title: "Pre-Flight Tasks",
    });
    expect(typeof chkId).toBe("string");
    const itemId = await checklistApi.createChecklistItem({
      checklistId: chkId,
      title: "Verify tests",
    });
    expect(typeof itemId).toBe("string");
    await checklistApi.updateChecklistItem({
      itemId,
      isChecked: true,
    });
    const checklists = await checklistApi.getChecklists(createdCard.id);
    const foundChk = checklists.find((c) => c.id === chkId);
    expect(foundChk?.items[0]?.isChecked).toBe(true);
    await checklistApi.deleteChecklistItem(itemId);
    await checklistApi.deleteChecklist(chkId);

    // Comments / Updates
    const comment = await commentApi.createCardUpdate({
      cardId: createdCard.id,
      body: "Work in progress comment.",
      mentionUserIds: [],
      attachmentIds: [],
    });
    expect(comment.body).toBe("Work in progress comment.");
    await commentApi.updateCardUpdate(
      comment.id,
      "Updated work in progress comment.",
    );
    const updates = await commentApi.getCardUpdates(createdCard.id);
    expect(updates.some((u) => u.body.includes("Updated"))).toBe(true);
    await commentApi.deleteCardUpdate(comment.id);

    // Relational Invariants preserved
    store.assertInvariants();
  });

  it("T-MFB-026: account profile and preferences mutations persist across requests", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    const accountService = createAccountService(client.api, {
      auth: { profile: endpoints.auth.profile },
      users: {
        updateProfile: endpoints.users.updateProfile,
        preferences: "/account/preferences",
        security: "/users/security",
      },
    });

    // Profile update
    const updatedProfile = await accountService.updateProfile({
      name: "Jane Doe (Updated)",
    });
    expect(updatedProfile.name).toBe("Jane Doe (Updated)");
    const fetchedProfile = await accountService.getProfile();
    expect(fetchedProfile.name).toBe("Jane Doe (Updated)");

    // Preferences update
    const updatedPrefs = await accountService.updatePreferences({
      theme: "dark",
      colorTheme: "emerald",
    });
    expect(updatedPrefs.theme).toBe("dark");
    expect(updatedPrefs.colorTheme).toBe("emerald");
    const fetchedPrefs = await accountService.getPreferences();
    expect(fetchedPrefs.theme).toBe("dark");
    expect(fetchedPrefs.colorTheme).toBe("emerald");

    // Security settings
    const security = await accountService.getSecuritySettings();
    expect(security.twoFactorEnabled).toBe(false);

    // Relational Invariants preserved
    store.assertInvariants();
  });

  it("T-MFB-027: identity logout persists signed-out state and subsequent auth calls fail with 401", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    const user = await client.api.get<{ id: string }>(endpoints.auth.profile);
    expect(user.id).toBe(mockIds.users.owner);

    await client.api.post(endpoints.auth.logout);

    // Profile request should now fail with 401
    await expect(client.api.get(endpoints.auth.profile)).rejects.toThrow();

    // Refresh should also fail with 401
    await expect(client.api.post(endpoints.auth.refresh)).rejects.toThrow();
  });
});
