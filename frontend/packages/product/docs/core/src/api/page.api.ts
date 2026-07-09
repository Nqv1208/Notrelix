import type { Page, BreadcrumbItem, CreatePagePayload, UpdatePagePayload, PageDetail } from '../types/page';
import type { SearchResult } from '../types/tree';
import type { PageDtoApi, BreadcrumbDtoApi } from '../dto';
import { mapPage, mapBreadcrumb } from '../model/page.mapper';

/**
 * Page API client.
 *
 * Uses the api client injected via config.
 * This avoids direct dependency on @notrelix/contracts.
 */
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
    search: (workspaceId: string) => string;
  };
  blocks: {
    detail: (blockId: string) => string;
    reorder: string;
    batch: (pageId: string) => string;
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

    async getBreadcrumb(pageId: string): Promise<BreadcrumbItem[]> {
      const breadcrumb = await api.get<BreadcrumbDtoApi[]>(
        endpoints.pages.breadcrumb(pageId),
      );
      return breadcrumb.map(mapBreadcrumb);
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

    async search(
      workspaceId: string,
      query: string,
    ): Promise<SearchResult[]> {
      const pages = await api.get<PageDtoApi[]>(
        `${endpoints.pages.search(workspaceId)}?query=${encodeURIComponent(query)}`,
      );
      return pages.map((page) => ({
        id: page.id,
        type: 'page' as const,
        title: page.title,
        excerpt: page.title,
        icon: page.iconValue ?? null,
        pageId: page.id,
        score: 1,
        group: 'Pages' as const,
      }));
    },

    async getFavorites(workspaceId: string): Promise<Page[]> {
      const pages = await this.getList(workspaceId);
      return pages.filter((page) => page.isFavorited);
    },
  };
}
