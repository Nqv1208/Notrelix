/**
 * Account — the pages of the 2026-10 Account mockup (gap
 * `CTR-GAP-ACC-PAGES`, see docs/superpowers/be-blockers-2026-10.md, "BE
 * requirement from FE: Account"):
 *
 *   account.avatar.set           — PUT       /account/avatar
 *   account.workspaces.list      — GET       /account/workspaces
 *   account.workspaces.setDefault — PUT      /account/default-workspace
 *   account.workspaces.leave     — DELETE    /account/workspaces/:workspaceId
 *
 * The world the mockup draws: the second workspace lists the member as an
 * ordinary member and the third as a guest of one board, whatever the
 * persona's seeded role there is.
 */
import { defineMockOperation } from "../../operations/types";
import {
  conflict,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";

const gap = { kind: "gap", gapId: "CTR-GAP-ACC-PAGES" } as const;

function workspacesBody(store: MockStore) {
  const user = store.getCurrentUser();
  const all = store
    .getWorkspaces()
    .filter(
      (workspace) =>
        !store.isWorkspaceArchived(workspace.id) &&
        store.isCurrentMemberOf(workspace.id) &&
        !store.accountPages.hasLeft(user.id, workspace.id),
    );
  const defaultId =
    store.accountPages.defaultWorkspaceOf(user.id) ?? all[0]?.id ?? null;
  return {
    workspaces: all.map((workspace, index) => {
      const seededRole = store.getCurrentMemberRole(workspace.id) ?? "member";
      const role = index === 1 ? "member" : index === 2 ? "guest" : seededRole;
      const owners = store
        .getWorkspaceMembers(workspace.id)
        .filter((member) => member.role === "owner")
        .map((member) => store.getUser(member.userId))
        .filter((u) => u && u.id !== user.id);
      const soleOwner = role === "owner" && owners.length === 0;
      return {
        id: workspace.id,
        name:
          index === 1
            ? "Minh An Đà Nẵng"
            : index === 2
              ? "Tân Lộc Logistics"
              : workspace.name,
        role,
        memberCount:
          index === 1 ? 18 : store.getWorkspaceMemberCount(workspace.id),
        departmentCount:
          index === 0
            ? store.spaces
                .spacesOf(workspace.id)
                .filter((space) => !space.parentId && !space.archived).length
            : null,
        unreadCount: index === 1 ? 2 : null,
        guestBoard: index === 2 ? "Điều phối xe tháng 10" : null,
        otherOwnerName: role === "admin" ? (owners[0]?.name ?? null) : null,
        isDefault: workspace.id === defaultId,
        canLeave: !soleOwner,
      };
    }),
  };
}

export const accountPagesOperations = [
  defineMockOperation<Record<string, never>, { dataUrl?: string }, unknown>({
    id: "account.avatar.set",
    contract: gap,
    method: "PUT",
    route: "/account/avatar",
    async handle({ body, store }) {
      const dataUrl = body?.dataUrl ?? "";
      if (!/^data:image\/(png|jpeg|webp|gif);base64,/.test(dataUrl))
        return validationError({ dataUrl: ["Choose a PNG, JPEG or WebP."] });
      if (dataUrl.length > 2_000_000)
        return validationError({ dataUrl: ["The photo is too large."] });
      store.updateUserProfile(store.getCurrentUser().id, {
        avatarUrl: dataUrl,
      });
      return ok({ avatarUrl: dataUrl });
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "account.workspaces.list",
    contract: gap,
    method: "GET",
    route: "/account/workspaces",
    async handle({ store }) {
      return ok(workspacesBody(store));
    },
  }),

  defineMockOperation<Record<string, never>, { workspaceId?: string }, unknown>(
    {
      id: "account.workspaces.setDefault",
      contract: gap,
      method: "PUT",
      route: "/account/default-workspace",
      async handle({ body, store }) {
        const id = body?.workspaceId ?? "";
        if (!store.isCurrentMemberOf(id)) return notFound();
        store.accountPages.setDefaultWorkspace(store.getCurrentUser().id, id);
        return ok(workspacesBody(store));
      },
    },
  ),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "account.workspaces.leave",
    contract: gap,
    method: "DELETE",
    route: "/account/workspaces/:workspaceId",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const entry = workspacesBody(store).workspaces.find(
        (workspace) => workspace.id === params.workspaceId,
      );
      if (!entry?.canLeave)
        return conflict(
          "The last owner cannot leave; hand the workspace over first.",
        );
      store.accountPages.leave(store.getCurrentUser().id, params.workspaceId);
      return noContent();
    },
  }),
];
