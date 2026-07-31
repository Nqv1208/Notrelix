import type { Page, CreatePagePayload, UpdatePagePayload, PageDetail } from '@notrelix/docs-core';
import type { PageDtoApi, BreadcrumbDtoApi } from '../dto';
import { mapPage, mapBreadcrumb } from '../model/page.mapper';

export interface DocsApiClient {
  get<T>(url: string): Promise<T>;
  post<T>(url: string, body: unknown): Promise<T>;
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
  };
  blocks: {
    detail: (blockId: string) => string;
    reorder: string;
    batch: (pageId: string) => string;
  };
  comments: {
    detail: (commentId: string) => string;
  };
}

export function createPageApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
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
        api.get<BreadcrumbDtoApi[]>(
          endpoints.pages.breadcrumb(pageId),
        ),
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
        },
      );
      return this.getDetail(id);
    },

    async update(
      pageId: string,
      payload: UpdatePagePayload,
    ): Promise<PageDetail> {
      await api.patch<void>(endpoints.pages.detail(pageId), {
        title: payload.title,
        iconValue: payload.icon,
        coverUrl: payload.coverUrl,
      });
      return this.getDetail(pageId);
    },

    async delete(pageId: string): Promise<void> {
      await api.delete<void>(endpoints.pages.detail(pageId));
    },
  };
}
