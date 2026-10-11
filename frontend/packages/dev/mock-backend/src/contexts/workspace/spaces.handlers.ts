/**
 * Workspaces — Space org tree and Teams handlers (OT-5), with producer
 * shapes (Workspaces.Spaces.*, Workspaces.Teams.GetWorkspaceTeams):
 * `SpaceTreeNodeDto` (version, position, accessLevel), `SpaceMemberDto`,
 * `TeamDto`; 201 + id for create, 204 for writes; a stale `expectedVersion`
 * is a 412 and a rule failure the producer's 400 ProblemDetails.
 */

import { defineMockOperation } from "../../operations/types";
import {
  created,
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
  type MockHttpResult,
} from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";
import type {
  MockSpaceRole,
  MockSpaceSubjectType,
  MockSpaceType,
  MockSpaceVisibility,
  MockSpaceWrite,
} from "../../state/mock-spaces";

const TYPES: readonly MockSpaceType[] = [
  "Folder",
  "Portfolio",
  "Department",
  "ProjectArea",
];
const VISIBILITIES: readonly MockSpaceVisibility[] = ["Private", "Workspace"];
const ROLES: readonly MockSpaceRole[] = ["Admin", "Member", "Viewer"];
const SUBJECTS: readonly MockSpaceSubjectType[] = ["User", "Team"];

function pick<T extends string>(
  options: readonly T[],
  value: unknown,
): T | null {
  if (typeof value !== "string") return null;
  return (
    options.find((option) => option.toLowerCase() === value.toLowerCase()) ??
    null
  );
}

function ruleViolation(detail: string): MockHttpResult<unknown> {
  return {
    status: 400,
    body: {
      type: "https://docs.notrelix.com/problems/business_rule-violation",
      title: "Business rule violation",
      status: 400,
      detail,
      errorCode: "business_rule.violation",
    },
  };
}

function preconditionFailed(): MockHttpResult<unknown> {
  return {
    status: 412,
    body: {
      type: "https://docs.notrelix.com/problems/common-precondition-failed",
      title: "Precondition failed",
      status: 412,
      detail: "The resource was modified by another request. Reload and retry.",
      errorCode: "common.precondition-failed",
    },
  };
}

function writeResult(write: MockSpaceWrite): MockHttpResult<unknown> {
  switch (write.kind) {
    case "ok":
      return noContent();
    case "not-found":
      return notFound();
    case "forbidden":
      return forbidden();
    case "stale":
      return preconditionFailed();
    case "rule":
      return ruleViolation(write.message);
  }
}

function guard(
  store: MockStore,
  workspaceId: string,
): MockHttpResult<unknown> | null {
  return store.isCurrentMemberOf(workspaceId) ? null : notFound();
}

export const spacesOperations = [
  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "workspace.spaces.tree",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Spaces.GetSpaceTree",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/spaces/tree",
    async handle({ params, store }) {
      return (
        guard(store, params.workspaceId) ??
        ok(
          store.spaces.visibleTree(
            params.workspaceId,
            store.spaceViewer(params.workspaceId),
          ),
        )
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    {
      name?: string | null;
      visibility?: string | null;
      description?: string | null;
      spaceType?: string | null;
      parentSpaceId?: string | null;
      previousSiblingId?: string | null;
      nextSiblingId?: string | null;
    },
    unknown
  >({
    id: "workspace.spaces.create",
    contract: { kind: "openapi", operationId: "Workspaces.Spaces.CreateSpace" },
    method: "POST",
    route: "/workspaces/:workspaceId/spaces",
    async handle({ params, body, store }) {
      const denied = guard(store, params.workspaceId);
      if (denied) return denied;
      const name = (body?.name ?? "").trim();
      if (!name) return validationError({ Name: ["Name must not be empty."] });
      if (name.length > 160)
        return validationError({
          Name: ["Name must be 160 characters or fewer."],
        });
      const type = body?.spaceType ? pick(TYPES, body.spaceType) : "Folder";
      const visibility = body?.visibility
        ? pick(VISIBILITIES, body.visibility)
        : null;
      if (!type) return validationError({ SpaceType: ["Unknown space type."] });
      if (body?.visibility && !visibility)
        return validationError({ Visibility: ["Unknown visibility."] });
      const id = store.nextId("space");
      const write = store.spaces.create(
        params.workspaceId,
        store.spaceViewer(params.workspaceId),
        {
          id,
          name,
          type,
          visibility,
          description: body?.description ?? null,
          parentId: body?.parentSpaceId ?? null,
          previousSiblingId: body?.previousSiblingId ?? null,
          nextSiblingId: body?.nextSiblingId ?? null,
          now: store.getClock().isoNow(),
        },
      );
      return write.kind === "ok" ? created(id) : writeResult(write);
    },
  }),

  defineMockOperation<
    { workspaceId: string; spaceId: string },
    { name?: string | null },
    void
  >({
    id: "workspace.spaces.rename",
    contract: { kind: "openapi", operationId: "Workspaces.Spaces.RenameSpace" },
    method: "PUT",
    route: "/workspaces/:workspaceId/spaces/:spaceId",
    async handle({ params, body, store }) {
      const name = (body?.name ?? "").trim();
      if (!name) return validationError({ Name: ["Name must not be empty."] });
      return writeResult(
        store.spaces.update(
          params.workspaceId,
          params.spaceId,
          store.spaceViewer(params.workspaceId),
          {
            name,
          },
        ),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string; spaceId: string },
    { description?: string | null },
    void
  >({
    id: "workspace.spaces.description",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Spaces.UpdateSpaceDescription",
    },
    method: "PUT",
    route: "/workspaces/:workspaceId/spaces/:spaceId/description",
    async handle({ params, body, store }) {
      const description = body?.description?.trim() || null;
      if (description && description.length > 1024)
        return validationError({
          Description: ["Use 1,024 characters or fewer."],
        });
      return writeResult(
        store.spaces.update(
          params.workspaceId,
          params.spaceId,
          store.spaceViewer(params.workspaceId),
          {
            description,
          },
        ),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string; spaceId: string },
    { visibility?: string | null },
    void
  >({
    id: "workspace.spaces.visibility",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Spaces.ChangeSpaceVisibility",
    },
    method: "PUT",
    route: "/workspaces/:workspaceId/spaces/:spaceId/visibility",
    async handle({ params, body, store }) {
      const visibility = pick(VISIBILITIES, body?.visibility);
      if (!visibility)
        return validationError({ Visibility: ["Unknown visibility."] });
      return writeResult(
        store.spaces.update(
          params.workspaceId,
          params.spaceId,
          store.spaceViewer(params.workspaceId),
          {
            visibility,
          },
        ),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string; spaceId: string },
    { spaceType?: string | null },
    void
  >({
    id: "workspace.spaces.type",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Spaces.ChangeSpaceType",
    },
    method: "PUT",
    route: "/workspaces/:workspaceId/spaces/:spaceId/type",
    async handle({ params, body, store }) {
      const type = pick(TYPES, body?.spaceType);
      if (!type) return validationError({ SpaceType: ["Unknown space type."] });
      return writeResult(
        store.spaces.update(
          params.workspaceId,
          params.spaceId,
          store.spaceViewer(params.workspaceId),
          {
            type,
          },
        ),
      );
    },
  }),

  defineMockOperation<{ workspaceId: string; spaceId: string }, never, void>({
    id: "workspace.spaces.archive",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Spaces.ArchiveSpace",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/spaces/:spaceId/archive",
    async handle({ params, store }) {
      return writeResult(
        store.spaces.archive(
          params.workspaceId,
          params.spaceId,
          store.spaceViewer(params.workspaceId),
          store.countPlacedInSpace(params.spaceId),
        ),
      );
    },
  }),

  defineMockOperation<{ workspaceId: string; spaceId: string }, never, void>({
    id: "workspace.spaces.unarchive",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Spaces.UnarchiveSpace",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/spaces/:spaceId/unarchive",
    async handle({ params, store }) {
      return writeResult(
        store.spaces.unarchive(
          params.workspaceId,
          params.spaceId,
          store.spaceViewer(params.workspaceId),
        ),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string; spaceId: string },
    {
      newParentSpaceId?: string | null;
      previousSiblingId?: string | null;
      nextSiblingId?: string | null;
      expectedVersion?: number;
    },
    void
  >({
    id: "workspace.spaces.move",
    contract: { kind: "openapi", operationId: "Workspaces.Spaces.MoveSpace" },
    method: "POST",
    route: "/workspaces/:workspaceId/spaces/:spaceId/move",
    async handle({ params, body, store }) {
      return writeResult(
        store.spaces.move(
          params.workspaceId,
          params.spaceId,
          store.spaceViewer(params.workspaceId),
          {
            parentId: body?.newParentSpaceId ?? null,
            previousSiblingId: body?.previousSiblingId ?? null,
            nextSiblingId: body?.nextSiblingId ?? null,
            expectedVersion: body?.expectedVersion,
          },
        ),
      );
    },
  }),

  defineMockOperation<{ workspaceId: string; spaceId: string }, never, unknown>(
    {
      id: "workspace.spaces.members.list",
      contract: {
        kind: "openapi",
        operationId: "Workspaces.Spaces.GetSpaceMembers",
      },
      method: "GET",
      route: "/workspaces/:workspaceId/spaces/:spaceId/members",
      async handle({ params, store }) {
        const viewer = store.spaceViewer(params.workspaceId);
        const space = store.spaces.getSpace(params.spaceId);
        if (
          !space ||
          space.workspaceId !== params.workspaceId ||
          store.spaces.accessFor(params.spaceId, viewer) === null
        ) {
          return notFound();
        }
        const rank: Record<MockSpaceRole, number> = {
          Admin: 3,
          Member: 2,
          Viewer: 1,
        };
        return ok(
          store.spaces
            .membersOf(params.spaceId)
            .sort((a, b) => rank[b.role] - rank[a.role])
            .map((entry) => {
              const user =
                entry.subjectType === "User"
                  ? store.getUser(entry.subjectId)
                  : null;
              const team =
                entry.subjectType === "Team"
                  ? store.spaces.getTeam(entry.subjectId)
                  : null;
              return {
                subjectType: entry.subjectType,
                subjectId: entry.subjectId,
                role: entry.role,
                displayName: team ? team.name : (user?.name ?? "Unknown"),
                avatarUrl: user?.avatarUrl ?? null,
                addedAt: entry.addedAt,
              };
            }),
        );
      },
    },
  ),

  defineMockOperation<
    { workspaceId: string; spaceId: string },
    {
      subjectType?: string | null;
      subjectId?: string;
      role?: string | null;
      expectedVersion?: number;
    },
    void
  >({
    id: "workspace.spaces.members.add",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Spaces.AddSpaceMember",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/spaces/:spaceId/members",
    async handle({ params, body, store }) {
      const subjectType = pick(SUBJECTS, body?.subjectType);
      const role = pick(ROLES, body?.role);
      if (!subjectType)
        return validationError({
          SubjectType: ["Subject type must be User or Team."],
        });
      if (!role) return validationError({ Role: ["Unknown role."] });
      const subjectId = body?.subjectId ?? "";
      const subjectExists =
        subjectType === "User"
          ? store
              .getWorkspaceMembers(params.workspaceId)
              .some((m) => m.userId === subjectId)
          : store.spaces.getTeam(subjectId)?.workspaceId ===
              params.workspaceId && !store.spaces.getTeam(subjectId)?.archived;
      return writeResult(
        store.spaces.addMember(
          params.workspaceId,
          params.spaceId,
          store.spaceViewer(params.workspaceId),
          {
            subjectType,
            subjectId,
            role,
            expectedVersion: body?.expectedVersion,
            now: store.getClock().isoNow(),
            subjectExists,
          },
        ),
      );
    },
  }),

  defineMockOperation<
    {
      workspaceId: string;
      spaceId: string;
      subjectType: string;
      subjectId: string;
    },
    { role?: string | null; expectedVersion?: number },
    void
  >({
    id: "workspace.spaces.members.role",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Spaces.ChangeSpaceMemberRole",
    },
    method: "PATCH",
    route:
      "/workspaces/:workspaceId/spaces/:spaceId/members/:subjectType/:subjectId",
    async handle({ params, body, store }) {
      const subjectType = pick(SUBJECTS, params.subjectType);
      const role = pick(ROLES, body?.role);
      if (!subjectType)
        return validationError({
          SubjectType: ["Subject type must be User or Team."],
        });
      if (!role) return validationError({ Role: ["Unknown role."] });
      return writeResult(
        store.spaces.changeMemberRole(
          params.workspaceId,
          params.spaceId,
          store.spaceViewer(params.workspaceId),
          {
            subjectType,
            subjectId: params.subjectId,
            role,
            expectedVersion: body?.expectedVersion,
          },
        ),
      );
    },
  }),

  defineMockOperation<
    {
      workspaceId: string;
      spaceId: string;
      subjectType: string;
      subjectId: string;
    },
    never,
    void
  >({
    id: "workspace.spaces.members.remove",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Spaces.RemoveSpaceMember",
    },
    method: "DELETE",
    route:
      "/workspaces/:workspaceId/spaces/:spaceId/members/:subjectType/:subjectId",
    async handle({ params, request, store }) {
      const subjectType = pick(SUBJECTS, params.subjectType);
      if (!subjectType)
        return validationError({
          SubjectType: ["Subject type must be User or Team."],
        });
      const raw = request.searchParams.get("expectedVersion");
      return writeResult(
        store.spaces.removeMember(
          params.workspaceId,
          params.spaceId,
          store.spaceViewer(params.workspaceId),
          {
            subjectType,
            subjectId: params.subjectId,
            expectedVersion: raw === null ? 0 : Number(raw),
          },
        ),
      );
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "workspace.teams.list",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Teams.GetWorkspaceTeams",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/teams",
    async handle({ params, store }) {
      return (
        guard(store, params.workspaceId) ??
        ok(
          store.spaces.teamsOf(params.workspaceId).map((team) => ({
            id: team.id,
            name: team.name,
            description: team.description,
            isArchived: team.archived,
            memberCount: team.memberIds.length,
            createdAt: team.createdAt,
          })),
        )
      );
    },
  }),
];
