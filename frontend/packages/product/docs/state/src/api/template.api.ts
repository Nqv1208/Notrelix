import type {
  CreatePageFromTemplatePayload,
  CreatePageTemplatePayload,
  PageFromTemplate,
  PageTemplate,
} from "@notrelix/docs-core";
import type { PageFromTemplateDtoApi, PageTemplateDtoApi } from "../dto";
import { mapPageFromTemplate, mapTemplate } from "../model/version.mapper";
import type { DocsApiClient, PageApiEndpoints } from "./page.api";

/** `Documents.Templates.*`: workspace page templates (creation input, never a live link). */
export function createTemplateApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    async list(workspaceId: string): Promise<PageTemplate[]> {
      const dtos = await api.get<PageTemplateDtoApi[]>(
        endpoints.pageTemplates.list(workspaceId),
      );
      return dtos.map(mapTemplate);
    },

    /** Saves a page's content as a template. */
    async create(
      workspaceId: string,
      payload: CreatePageTemplatePayload,
    ): Promise<PageTemplate> {
      return mapTemplate(
        await api.post<PageTemplateDtoApi>(
          endpoints.pageTemplates.list(workspaceId),
          {
            pageId: payload.pageId,
            name: payload.name,
            ...(payload.description
              ? { description: payload.description }
              : {}),
          },
          { skipGlobalErrorToast: true },
        ),
      );
    },

    /** Creates a page from a template, placed like any new page. */
    async createPage(
      workspaceId: string,
      payload: CreatePageFromTemplatePayload,
    ): Promise<PageFromTemplate> {
      return mapPageFromTemplate(
        await api.post<PageFromTemplateDtoApi>(
          endpoints.pageTemplates.createPage(workspaceId, payload.templateId),
          {
            ...(payload.title ? { title: payload.title } : {}),
            ...(payload.parentId ? { parentId: payload.parentId } : {}),
            ...(payload.spaceId ? { spaceId: payload.spaceId } : {}),
          },
          {
            skipGlobalErrorToast: true,
            idempotencyKey: payload.idempotencyKey,
          },
        ),
      );
    },
  };
}
