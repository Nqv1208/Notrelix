/**
 * Account — the profile photo and the member's own workspaces, with the
 * routes, shapes and problem codes of the producer:
 *
 *   Identity.Profile.UploadAvatar              — PUT    /profile/avatar (multipart part `file`)
 *   Identity.Profile.RemoveAvatar              — DELETE /profile/avatar
 *   Workspaces.Members.ListMyWorkspaces        — GET    /account/workspaces
 *   Workspaces.Members.SetDefaultWorkspace     — PUT    /account/default-workspace
 *   Workspaces.Members.LeaveWorkspace          — DELETE /account/workspaces/:workspaceId
 *
 * The photo is recognised from its bytes (PNG, JPEG, WebP), never from the
 * declared type or the file name; the mock keeps no bytes, only the address
 * the producer would answer.
 */
import { defineMockOperation } from "../../operations/types";
import { noContent, ok } from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";
import { identityProblem } from "../identity/auth-flow.handlers";

/** The producer refuses a body over 2,000,000 bytes. */
export const MOCK_MAX_AVATAR_BYTES = 2_000_000;

function problem(status: number, errorCode: string, detail: string) {
  return identityProblem(status, errorCode, detail);
}

function isKnownImage(bytes: Uint8Array): boolean {
  const png =
    bytes[0] === 0x89 &&
    bytes[1] === 0x50 &&
    bytes[2] === 0x4e &&
    bytes[3] === 0x47;
  const jpeg = bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff;
  const webp =
    bytes[0] === 0x52 &&
    bytes[1] === 0x49 &&
    bytes[2] === 0x46 &&
    bytes[3] === 0x46 &&
    bytes[8] === 0x57 &&
    bytes[9] === 0x45 &&
    bytes[10] === 0x42 &&
    bytes[11] === 0x50;
  return png || jpeg || webp;
}

const ROLE_NAME = {
  owner: "Owner",
  admin: "Admin",
  member: "Member",
  guest: "Guest",
} as const;

/**
 * The caller's own active memberships of active workspaces, ordered by name; an archived workspace is
 * listed (`isArchived`) only to its owners and admins.
 */
function myWorkspaces(store: MockStore) {
  const user = store.getCurrentUser();
  const mine = store.getWorkspaces().filter((workspace) => {
    if (!store.isCurrentMemberOf(workspace.id)) return false;
    if (!store.isWorkspaceArchived(workspace.id)) return true;
    const role = store.getCurrentMemberRole(workspace.id);
    return role === "owner" || role === "admin";
  });
  const defaultId =
    store.accountPages.defaultWorkspaceOf(user.id) ??
    mine.find((workspace) => !store.isWorkspaceArchived(workspace.id))?.id ??
    null;
  return mine
    .map((workspace) => {
      const role = store.getCurrentMemberRole(workspace.id) ?? "member";
      const owners = store
        .getWorkspaceMembers(workspace.id)
        .filter((member) => member.role === "owner");
      const soleOwner = role === "owner" && owners.length <= 1;
      const sees = role === "owner" || role === "admin";
      return {
        id: workspace.id,
        name: workspace.name,
        slug: workspace.slug,
        role: ROLE_NAME[role],
        memberCount:
          role === "guest" ? null : store.getWorkspaceMemberCount(workspace.id),
        departmentCount: sees
          ? store.spaces
              .spacesOf(workspace.id)
              .filter((space) => !space.parentId && !space.archived).length
          : null,
        isDefault:
          workspace.id === defaultId &&
          !store.isWorkspaceArchived(workspace.id),
        canLeave: !soleOwner,
        isArchived: store.isWorkspaceArchived(workspace.id),
      };
    })
    .sort((a, b) => (a.name < b.name ? -1 : a.name > b.name ? 1 : 0));
}

export const accountPagesOperations = [
  defineMockOperation<Record<string, never>, never, unknown>({
    id: "account.avatar.upload",
    contract: { kind: "openapi", operationId: "Identity.Profile.UploadAvatar" },
    method: "PUT",
    route: "/profile/avatar",
    async handle({ request, store }) {
      const part = request.formData?.get("file");
      if (!part || typeof part === "string" || !part.name)
        return problem(
          400,
          "validation.failed",
          "A file part with a file name is required.",
        );
      if (part.size > MOCK_MAX_AVATAR_BYTES)
        return problem(
          413,
          "identity.avatar.too-large",
          "The photo is over the size limit.",
        );
      const head = new Uint8Array(await part.slice(0, 12).arrayBuffer());
      if (!isKnownImage(head))
        return problem(
          415,
          "identity.avatar.unsupported-format",
          "Use a PNG, JPEG or WebP photo.",
        );
      const user = store.getCurrentUser();
      const avatarUrl = store.accountPages.nextAvatarUrl(user.id);
      store.updateUserProfile(user.id, { avatarUrl });
      return ok({ avatarUrl });
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "account.avatar.remove",
    contract: { kind: "openapi", operationId: "Identity.Profile.RemoveAvatar" },
    method: "DELETE",
    route: "/profile/avatar",
    async handle({ store }) {
      store.updateUserProfile(store.getCurrentUser().id, { avatarUrl: null });
      return ok({ avatarUrl: null });
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "account.workspaces.list",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.ListMyWorkspaces",
    },
    method: "GET",
    route: "/account/workspaces",
    async handle({ store }) {
      return ok(myWorkspaces(store));
    },
  }),

  defineMockOperation<Record<string, never>, { workspaceId?: string }, unknown>(
    {
      id: "account.workspaces.setDefault",
      contract: {
        kind: "openapi",
        operationId: "Workspaces.Members.SetDefaultWorkspace",
      },
      method: "PUT",
      route: "/account/default-workspace",
      async handle({ body, store }) {
        const id = body?.workspaceId ?? "";
        if (id === "")
          return problem(400, "validation.failed", "Choose a workspace.");
        if (!myWorkspaces(store).some((workspace) => workspace.id === id))
          return problem(
            404,
            "workspaces.member.not-found",
            "You are not a member of this workspace.",
          );
        store.accountPages.setDefaultWorkspace(store.getCurrentUser().id, id);
        return noContent();
      },
    },
  ),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "account.workspaces.leave",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.LeaveWorkspace",
    },
    method: "DELETE",
    route: "/account/workspaces/:workspaceId",
    async handle({ params, store }) {
      const entry = myWorkspaces(store).find(
        (workspace) => workspace.id === params.workspaceId,
      );
      if (!entry)
        return problem(
          404,
          "workspaces.member.not-found",
          "You are not a member of this workspace.",
        );
      if (!entry.canLeave)
        return problem(
          409,
          "workspaces.member.last-owner-cannot-leave",
          "The last owner cannot leave; hand the ownership over first.",
        );
      const userId = store.getCurrentUser().id;
      store.removeMember(params.workspaceId, userId);
      store.accountPages.forgetDefaultWorkspace(userId, params.workspaceId);
      return noContent();
    },
  }),
];
