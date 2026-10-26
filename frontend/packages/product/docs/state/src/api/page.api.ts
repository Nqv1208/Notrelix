import type {
  Page,
  CreatePagePayload,
  MovePageToSpacePayload,
  UpdatePagePayload,
  PageDetail,
} from "@notrelix/docs-core";
import type { PageDtoApi, BreadcrumbDtoApi } from "../dto";
import { mapPage, mapBreadcrumb } from "../model/page.mapper";

/** Per-request options the Docs adapters use (a subset of the client's). */
export interface DocsRequestOptions {
  /** The caller shows the failure itself (a dialog), not the global toast. */
  readonly skipGlobalErrorToast?: boolean;
}

/** What a multipart upload reports while it runs. */
export interface DocsUploadProgress {
  readonly loaded: number;
  readonly total: number;
}

export interface DocsUploadOptions {
  /** One per user action; the same key only to retry that action. */
  readonly idempotencyKey: string;
  readonly signal?: AbortSignal;
  readonly onProgress?: (progress: DocsUploadProgress) => void;
}

/** The file transport a page's attachments need beyond JSON requests. */
export interface DocsFileClient {
  /** `multipart/form-data` upload; answers the parsed body. */
  upload<T>(
    url: string,
    form: FormData,
    options: DocsUploadOptions,
  ): Promise<T>;
  /** The bytes of a file, read with the session. */
  getBlob(url: string): Promise<Blob>;
}

export interface DocsApiClient {
  get<T>(url: string, options?: DocsRequestOptions): Promise<T>;
  post<T>(url: string, body: unknown, options?: DocsRequestOptions): Promise<T>;
  patch<T>(url: string, body: unknown): Promise<T>;
  delete<T>(url: string): Promise<T>;
}

export interface PageApiEndpoints {
  pages: {
    list: (workspaceId: string) => string;
    detail: (pageId: string) => string;
    breadcrumb: (pageId: string) => string;
    blocks: (pageId: string) => string;
    comments: (pageId: string) => string;
    history: (pageId: string) => string;
    search: (workspaceId: string) => string;
    moveToSpace: (pageId: string) => string;
    archive: (pageId: string) => string;
    restore: (pageId: string) => string;
    trash: (workspaceId: string) => string;
    backlinks: (pageId: string) => string;
  };
  resourcePermissions: {
    list: (resourceKind: string, resourceId: string) => string;
  };
  pageAttachments: {
    list: (pageId: string) => string;
    files: (pageId: string) => string;
    content: (pageId: string, attachmentId: string) => string;
    detail: (pageId: string, attachmentId: string) => string;
  };
  blocks: {
    detail: (pageId: string, blockId: string) => string;
    reorder: string;
    batch: (pageId: string) => string;
  };
  comments: {
    detail: (commentId: string) => string;
  };
  /** Work Management reads and writes a page turns a checklist line into. */
  boardItems: {
    create: (boardId: string) => string;
    linkPage: (itemId: string) => string;
  };
}

export function createPageApi(api: DocsApiClient, endpoints: PageApiEndpoints) {
  return {
    async getList(workspaceId: string): Promise<Page[]> {
      const pages = await api.get<PageDtoApi[]>(
        endpoints.pages.list(workspaceId),
      );
      return pages.map(mapPage);
    },

    async getDetail(pageId: string): Promise<PageDetail> {
      const [page, breadcrumb] = await Promise.all([
        api.get<PageDtoApi>(endpoints.pages.detail(pageId)),
        api.get<BreadcrumbDtoApi[]>(endpoints.pages.breadcrumb(pageId)),
      ]);
      return {
        ...mapPage(page),
        blocks: [],
        breadcrumb: breadcrumb.map(mapBreadcrumb),
        collaborators: [],
        linkedTasks: [],
        linkedBoards: [],
      };
    },

    async create(payload: CreatePagePayload): Promise<PageDetail> {
      const id = await api.post<string>(
        endpoints.pages.list(payload.workspaceId),
        {
          title: payload.title,
          parentId: payload.parentId,
          ...(payload.spaceId ? { spaceId: payload.spaceId } : {}),
        },
      );
      return this.getDetail(id);
    },

    async update(
      pageId: string,
      payload: UpdatePagePayload,
    ): Promise<PageDetail> {
      // `Documents.Pages.UpdatePage` renames; the icon and cover are not stored yet.
      await api.patch<void>(endpoints.pages.detail(pageId), {
        title: payload.title,
      });
      return this.getDetail(pageId);
    },

    /** `Documents.Pages.ArchivePage`: moves the page to the trash. */
    async archive(pageId: string): Promise<void> {
      await api.post<void>(endpoints.pages.archive(pageId), {});
    },

    /** `Documents.Pages.RestorePage`: brings a trashed page back. */
    async restore(pageId: string): Promise<void> {
      await api.post<void>(endpoints.pages.restore(pageId), {});
    },

    /** `Documents.Pages.ListTrashedPages`: the pages the caller may restore or delete. */
    async getTrash(workspaceId: string): Promise<Page[]> {
      const pages = await api.get<PageDtoApi[]>(
        endpoints.pages.trash(workspaceId),
      );
      return pages.map(mapPage);
    },

    /** `Documents.Pages.DeletePage`: removes a trashed page for good. */
    async delete(pageId: string): Promise<void> {
      await api.delete<void>(endpoints.pages.detail(pageId));
    },

    /**
     * `Documents.Pages.MoveToSpace` (GOV-028): authorized on the page and on
     * the target Space; a stale `expectedVersion` is a 412 and moves nothing.
     */
    async moveToSpace(payload: MovePageToSpacePayload): Promise<void> {
      await api.post<void>(
        endpoints.pages.moveToSpace(payload.pageId),
        { spaceId: payload.spaceId, expectedVersion: payload.expectedVersion },
        { skipGlobalErrorToast: true },
      );
    },
  };
}
