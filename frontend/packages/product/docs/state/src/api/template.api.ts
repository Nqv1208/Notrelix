import type {
  CreatePageFromTemplatePayload,
  CreatePageTemplatePayload,
  PageFromTemplate,
  PageTemplate,
  PageTemplateVersionPayload,
  UpdatePageTemplatePayload,
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

    /** Renames a template or changes its description; a stale version is refused (412). */
    async update(
      workspaceId: string,
      payload: UpdatePageTemplatePayload,
    ): Promise<PageTemplate> {
      return mapTemplate(
        await api.patch<PageTemplateDtoApi>(
          endpoints.pageTemplates.detail(workspaceId, payload.templateId),
          {
            name: payload.name,
            description: payload.description ?? null,
            expectedVersion: payload.expectedVersion,
          },
          { skipGlobalErrorToast: true },
        ),
      );
    },

    /** Archives a template: it leaves the gallery; pages made from it are unchanged. */
    async archive(
      workspaceId: string,
      payload: PageTemplateVersionPayload,
    ): Promise<void> {
      await api.post<void>(
        endpoints.pageTemplates.archive(workspaceId, payload.templateId),
        { expectedVersion: payload.expectedVersion },
        { skipGlobalErrorToast: true },
      );
    },

    /** Deletes a template for good; pages made from it stay. */
    async remove(
      workspaceId: string,
      payload: PageTemplateVersionPayload,
    ): Promise<void> {
      await api.delete<void>(
        `${endpoints.pageTemplates.detail(workspaceId, payload.templateId)}?expectedVersion=${payload.expectedVersion}`,
        { skipGlobalErrorToast: true },
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
