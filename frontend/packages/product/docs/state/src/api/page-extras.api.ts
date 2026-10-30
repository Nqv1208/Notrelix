import type { Block } from "@notrelix/docs-core";
import type { DocsApiClient, PageApiEndpoints } from "./page.api";

/**
 * `Documents.Pages.GetPageBacklinks`: the pages that name this page (a page
 * reference block or a page mention), already filtered to what the caller may
 * see. The Space is masked when the caller cannot see it.
 */
export interface PageBacklinksDto {
  readonly docs: readonly {
    readonly id: string;
    readonly title: string;
    readonly icon: string | null;
    readonly spaceId: string | null;
  }[];
}

/** Permissions given on the page itself (gap `CTR-GAP-DOCS-PAGE-EXT`). */
export interface PagePermissionsDto {
  readonly members: readonly {
    readonly id: string;
    readonly kind: "person" | "team";
    readonly userId: string | null;
    /** The subject's id; the host names a person from the workspace's people when `name` is empty. */
    readonly subjectId?: string;
    readonly name: string;
    /** The producer's description of how the person has access. */
    readonly detail: string;
    readonly outside: boolean;
    readonly creator: boolean;
    readonly level: "manage" | "edit" | "comment" | "view";
  }[];
}

/** Where a checklist line becomes a work item: a board the caller can add to and one of its groups. */
export interface ConvertTodoTarget {
  readonly boardId: string;
  readonly groupId: string;
}

export interface ConvertedTaskDto {
  readonly id: string;
  readonly title: string;
}

/** The write of a conversion that failed; the work item exists once it is past `item`. */
export type ConvertTodoStep = "item" | "link" | "line";

class ConvertTodoFailed extends Error {
  constructor(
    readonly step: ConvertTodoStep,
    readonly cause: unknown,
  ) {
    super(`Converting the line failed at the ${step} step.`);
    this.name = "ConvertTodoFailed";
  }
}

/** The step a conversion failed at, or `null` when the error is not one of its steps. */
export function convertTodoFailedStep(error: unknown): ConvertTodoStep | null {
  return error instanceof ConvertTodoFailed ? error.step : null;
}

async function step<T>(
  name: ConvertTodoStep,
  run: () => Promise<T>,
): Promise<T> {
  try {
    return await run();
  } catch (error) {
    throw new ConvertTodoFailed(name, error);
  }
}

export type PagePermissionLevel =
  PagePermissionsDto["members"][number]["level"];

/** `Governance.ResourcePermissions.Get` row. */
interface ResourcePermissionDto {
  readonly id: string;
  readonly subjectType: string;
  readonly subjectId: string;
  readonly level: string;
  readonly isRevoked: boolean;
}

const PAGE_KIND = "documents.page";
const QUIET = { skipGlobalErrorToast: true } as const;

const FROM_LEVEL: Readonly<Record<string, PagePermissionLevel>> = {
  Viewer: "view",
  Commenter: "comment",
  Editor: "edit",
  Manager: "manage",
  Owner: "manage",
};

const TO_LEVEL: Readonly<Record<PagePermissionLevel, string>> = {
  view: "Viewer",
  comment: "Commenter",
  edit: "Editor",
  manage: "Manager",
};

export function createPageExtrasApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    async getBacklinks(pageId: string): Promise<PageBacklinksDto> {
      const dto = await api.get<Partial<PageBacklinksDto> | null>(
        endpoints.pages.backlinks(pageId),
        QUIET,
      );
      return { docs: dto?.docs ?? [] };
    },
    /**
     * The page's own grants (`Governance.ResourcePermissions.Get`, readable by
     * an owner or manager of the page). Where the producer has no grants to
     * show, the page-scoped read (gap `CTR-GAP-DOCS-PAGE-EXT`) is tried.
     */
    async getPermissions(pageId: string): Promise<PagePermissionsDto> {
      const rows = await api
        .get<ResourcePermissionDto[] | null>(
          endpoints.resourcePermissions.list(PAGE_KIND, pageId),
          QUIET,
        )
        .catch(() => null);
      const granted = (rows ?? []).filter((row) => !row.isRevoked);
      if (granted.length > 0) {
        return {
          members: granted.map((row) => ({
            id: row.id,
            kind:
              row.subjectType === "Team"
                ? ("team" as const)
                : ("person" as const),
            userId: row.subjectType === "Team" ? null : row.subjectId,
            subjectId: row.subjectId,
            name: "",
            detail: "",
            outside: false,
            creator: false,
            level: FROM_LEVEL[row.level] ?? "view",
          })),
        };
      }
      const dto = await api
        .get<Partial<PagePermissionsDto> | null>(
          `${endpoints.pages.detail(pageId)}/permissions`,
          QUIET,
        )
        .catch(() => null);
      return { members: dto?.members ?? [] };
    },
    /** Gives a workspace member a permission on the page (`Governance.ResourcePermissions.Grant`). */
    async grantPermission(
      pageId: string,
      userId: string,
      level: PagePermissionLevel,
    ): Promise<void> {
      await api.post<unknown>(
        endpoints.resourcePermissions.list(PAGE_KIND, pageId),
        { subjectType: "User", subjectId: userId, level: TO_LEVEL[level] },
        QUIET,
      );
    },
    /**
     * Turns a checklist line into a work item: creates the item in the chosen
     * board group (`WorkManagement.BoardItems.Create`), attaches the page to it
     * (`LinkPage`) and marks the line with the item (`UpdateBlock`). Each step is
     * its own write and a failure names its step (`ConvertTodoFailed`). The
     * caller keeps `attempt` for a retry of the same conversion: the item and the
     * link are sent under keys derived from it, so a retry answers the item that
     * already exists instead of creating another, and a line that already holds
     * the chip counts as done.
     */
    async convertTodo(
      pageId: string,
      block: Block,
      target: ConvertTodoTarget,
      attempt: string,
    ): Promise<ConvertedTaskDto> {
      const title = (block.properties.text ?? "").trim();
      const created = await step("item", () =>
        api.post<{ id: string }>(
          endpoints.boardItems.create(target.boardId),
          { groupId: target.groupId, title },
          { ...QUIET, idempotencyKey: `${attempt}:item` },
        ),
      );
      await step("link", () =>
        api.post<unknown>(
          endpoints.boardItems.linkPage(created.id),
          { pageId },
          { ...QUIET, idempotencyKey: `${attempt}:link` },
        ),
      );
      // The line becomes a chip of the task: a `[[task:id]]` run, which the
      // producer stores as plain text of the block.
      const chip = `[[task:${created.id}]]`;
      await step("line", async () => {
        try {
          await api.patch<unknown>(endpoints.blocks.detail(pageId, block.id), {
            expectedVersion: block.version,
            content: { v: 1, runs: [{ text: chip, marks: [] }] },
          });
        } catch (error) {
          if ((error as { status?: unknown }).status !== 412) throw error;
          // A retry after the line was already marked: the stored line has the chip.
          const stored = await api.get<{ id: string }[]>(
            endpoints.pages.blocks(pageId),
          );
          const line = stored.find((candidate) => candidate.id === block.id);
          if (!line || !JSON.stringify(line).includes(chip)) throw error;
        }
      });
      return { id: created.id, title };
    },
  };
}
