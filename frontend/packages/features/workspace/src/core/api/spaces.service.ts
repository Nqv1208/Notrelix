import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import {
  parseSpaceAccess,
  parseSpaceRole,
  parseSpaceSubjectType,
  parseSpaceType,
  parseSpaceVisibility,
  type SpaceMemberEntry,
  type SpaceNode,
  type SpaceRole,
  type SpaceSubjectType,
  type SpaceType,
  type SpaceVisibility,
} from "../model/spaces";

type SpaceTreeNodeDto =
  OperationResponse<
    "Workspaces.Spaces.GetSpaceTree",
    200
  > extends readonly (infer Item)[]
    ? Item
    : never;
type SpaceMemberDto =
  OperationResponse<
    "Workspaces.Spaces.GetSpaceMembers",
    200
  > extends readonly (infer Item)[]
    ? Item
    : never;
type CreateSpaceBody = OperationRequestBody<"Workspaces.Spaces.CreateSpace">;
type MoveSpaceBody = OperationRequestBody<"Workspaces.Spaces.MoveSpace">;
type RenameSpaceBody = OperationRequestBody<"Workspaces.Spaces.RenameSpace">;
type DescriptionBody =
  OperationRequestBody<"Workspaces.Spaces.UpdateSpaceDescription">;
type VisibilityBody =
  OperationRequestBody<"Workspaces.Spaces.ChangeSpaceVisibility">;
type TypeBody = OperationRequestBody<"Workspaces.Spaces.ChangeSpaceType">;
type AddMemberBody = OperationRequestBody<"Workspaces.Spaces.AddSpaceMember">;
type ChangeRoleBody =
  OperationRequestBody<"Workspaces.Spaces.ChangeSpaceMemberRole">;

export interface SpacesApiClient {
  get<T>(url: string): Promise<T>;
  post<T>(url: string, body: unknown): Promise<T>;
  put<T>(url: string, body: unknown): Promise<T>;
  patch<T>(url: string, body: unknown): Promise<T>;
  delete<T>(url: string): Promise<T>;
}

export interface SpacesEndpoints {
  workspaces: {
    spaces: (workspaceId: string) => string;
    spaceTree: (workspaceId: string) => string;
    space: (workspaceId: string, spaceId: string) => string;
    spaceDescription: (workspaceId: string, spaceId: string) => string;
    spaceVisibility: (workspaceId: string, spaceId: string) => string;
    spaceType: (workspaceId: string, spaceId: string) => string;
    spaceArchive: (workspaceId: string, spaceId: string) => string;
    spaceUnarchive: (workspaceId: string, spaceId: string) => string;
    spaceMove: (workspaceId: string, spaceId: string) => string;
    spaceMembers: (workspaceId: string, spaceId: string) => string;
    spaceMember: (
      workspaceId: string,
      spaceId: string,
      subjectType: string,
      subjectId: string,
    ) => string;
  };
}

/** Rows missing an identity or a known type are dropped, never guessed. */
export function parseSpaceTree(items: unknown): SpaceNode[] {
  if (!Array.isArray(items)) return [];
  const nodes: SpaceNode[] = [];
  for (const raw of items as SpaceTreeNodeDto[]) {
    const type = parseSpaceType(raw?.spaceType);
    const visibility = parseSpaceVisibility(raw?.visibility);
    if (!raw?.id || !type || !visibility) continue;
    nodes.push({
      id: raw.id,
      parentId: raw.parentSpaceId ?? null,
      name: raw.name ?? "",
      description: raw.description ?? null,
      type,
      visibility,
      isArchived: raw.isArchived === true,
      position: raw.position ?? "",
      version: typeof raw.version === "number" ? raw.version : 0,
      createdAt: raw.createdAt ?? "",
      access: parseSpaceAccess(raw.accessLevel),
    });
  }
  return nodes;
}

export function parseSpaceMembers(items: unknown): SpaceMemberEntry[] {
  if (!Array.isArray(items)) return [];
  const entries: SpaceMemberEntry[] = [];
  for (const raw of items as SpaceMemberDto[]) {
    const subjectType = parseSpaceSubjectType(raw?.subjectType);
    const role = parseSpaceRole(raw?.role);
    if (!raw?.subjectId || !subjectType || !role) continue;
    entries.push({
      subjectType,
      subjectId: raw.subjectId,
      role,
      displayName: raw.displayName ?? "",
      avatarUrl: raw.avatarUrl ?? null,
      addedAt: raw.addedAt ?? "",
    });
  }
  return entries;
}

export interface CreateSpaceInput {
  readonly name: string;
  readonly type: SpaceType;
  readonly visibility: SpaceVisibility;
  readonly description?: string | null;
  readonly parentId: string | null;
  readonly previousSiblingId: string | null;
  readonly nextSiblingId: string | null;
}

export interface MoveSpaceInput {
  readonly spaceId: string;
  readonly parentId: string | null;
  readonly previousSiblingId: string | null;
  readonly nextSiblingId: string | null;
  readonly expectedVersion: number;
}

export interface SpaceDetailsInput {
  readonly name?: string;
  readonly description?: string | null;
  readonly type?: SpaceType;
  readonly visibility?: SpaceVisibility;
}

/**
 * Adapter for the Workspaces Space producer (OT-4). Membership and move
 * writes carry the Space version read from the tree; a stale one is
 * rejected by the producer and never retried automatically.
 */
export function createSpacesService(
  api: SpacesApiClient,
  endpoints: SpacesEndpoints,
) {
  const e = endpoints.workspaces;
  return {
    async getTree(workspaceId: string): Promise<SpaceNode[]> {
      return parseSpaceTree(await api.get<unknown>(e.spaceTree(workspaceId)));
    },

    async getMembers(
      workspaceId: string,
      spaceId: string,
    ): Promise<SpaceMemberEntry[]> {
      return parseSpaceMembers(
        await api.get<unknown>(e.spaceMembers(workspaceId, spaceId)),
      );
    },

    /** Returns the new Space id (201 with the id). */
    async create(
      workspaceId: string,
      input: CreateSpaceInput,
    ): Promise<string | null> {
      const body: CreateSpaceBody = {
        name: input.name,
        spaceType: input.type,
        visibility: input.visibility,
        description: input.description ?? null,
        parentSpaceId: input.parentId,
        previousSiblingId: input.previousSiblingId,
        nextSiblingId: input.nextSiblingId,
      };
      const id = await api.post<unknown>(e.spaces(workspaceId), body);
      return typeof id === "string" ? id : null;
    },

    async move(workspaceId: string, input: MoveSpaceInput): Promise<void> {
      const body: MoveSpaceBody = {
        newParentSpaceId: input.parentId,
        previousSiblingId: input.previousSiblingId,
        nextSiblingId: input.nextSiblingId,
        expectedVersion: input.expectedVersion,
      };
      await api.post<void>(e.spaceMove(workspaceId, input.spaceId), body);
    },

    /** Writes only the fields that changed, one producer call each. */
    async updateDetails(
      workspaceId: string,
      spaceId: string,
      input: SpaceDetailsInput,
    ): Promise<void> {
      if (input.name !== undefined) {
        const body: RenameSpaceBody = { name: input.name };
        await api.put<void>(e.space(workspaceId, spaceId), body);
      }
      if (input.description !== undefined) {
        const body: DescriptionBody = { description: input.description };
        await api.put<void>(e.spaceDescription(workspaceId, spaceId), body);
      }
      if (input.type !== undefined) {
        const body: TypeBody = { spaceType: input.type };
        await api.put<void>(e.spaceType(workspaceId, spaceId), body);
      }
      if (input.visibility !== undefined) {
        const body: VisibilityBody = { visibility: input.visibility };
        await api.put<void>(e.spaceVisibility(workspaceId, spaceId), body);
      }
    },

    async archive(workspaceId: string, spaceId: string): Promise<void> {
      await api.post<void>(e.spaceArchive(workspaceId, spaceId), {});
    },

    async unarchive(workspaceId: string, spaceId: string): Promise<void> {
      await api.post<void>(e.spaceUnarchive(workspaceId, spaceId), {});
    },

    async addMember(
      workspaceId: string,
      spaceId: string,
      input: {
        subjectType: SpaceSubjectType;
        subjectId: string;
        role: SpaceRole;
        expectedVersion: number;
      },
    ): Promise<void> {
      const body: AddMemberBody = { ...input };
      await api.post<void>(e.spaceMembers(workspaceId, spaceId), body);
    },

    async changeMemberRole(
      workspaceId: string,
      spaceId: string,
      input: {
        subjectType: SpaceSubjectType;
        subjectId: string;
        role: SpaceRole;
        expectedVersion: number;
      },
    ): Promise<void> {
      const body: ChangeRoleBody = {
        role: input.role,
        expectedVersion: input.expectedVersion,
      };
      await api.patch<void>(
        e.spaceMember(workspaceId, spaceId, input.subjectType, input.subjectId),
        body,
      );
    },

    /**
     * Remove carries no body; the producer reads `expectedVersion` from the
     * query string like the other versioned deletes.
     */
    async removeMember(
      workspaceId: string,
      spaceId: string,
      input: {
        subjectType: SpaceSubjectType;
        subjectId: string;
        expectedVersion: number;
      },
    ): Promise<void> {
      await api.delete<void>(
        `${e.spaceMember(workspaceId, spaceId, input.subjectType, input.subjectId)}?expectedVersion=${input.expectedVersion}`,
      );
    },
  };
}

export type SpacesService = ReturnType<typeof createSpacesService>;
