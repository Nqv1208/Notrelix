/**
 * Documents — page history and templates (`Documents.Versions.*`,
 * `Documents.Templates.*`).
 *
 * Operations:
 *   docs.versions.list      — GET  /pages/:pageId/versions
 *   docs.versions.get       — GET  /pages/:pageId/versions/:versionId
 *   docs.versions.create    — POST /pages/:pageId/versions (a named version)
 *   docs.versions.restore   — POST /pages/:pageId/versions/:versionId/restore
 *   docs.templates.list     — GET  /workspaces/:workspaceId/page-templates
 *   docs.templates.create   — POST /workspaces/:workspaceId/page-templates
 *   docs.templates.createPage — POST /workspaces/:workspaceId/page-templates/:templateId/pages
 */

import { defineMockOperation } from "../../operations/types";
import {
  created,
  notFound,
  ok,
  type MockHttpResult,
} from "../../transport/create-response";
import type {
  MockPageTemplateRecord,
  MockPageVersionRecord,
} from "../../state/records";
import { readBlockBody } from "./block-body";
import { toBlockDto } from "./blocks.handlers";

type Json = Record<string, unknown>;

const PRODUCER_TYPE: Record<string, string> = {
  paragraph: "Text",
  heading_1: "Heading1",
  heading_2: "Heading2",
  heading_3: "Heading3",
  bulleted_list: "BulletList",
  numbered_list: "NumberedList",
  todo: "ToDo",
  toggle: "Toggle",
  quote: "Quote",
  divider: "Divider",
  code: "Code",
  image: "Image",
  file: "File",
  board_reference: "Board",
  table: "Table",
  callout: "Callout",
  page_reference: "PageReference",
};

function problem(
  status: number,
  errorCode: string,
  title: string,
  extensions?: Json,
): MockHttpResult<unknown> {
  return {
    status,
    body: {
      title,
      status,
      errorCode,
      ...(extensions ? { extensions } : {}),
    },
  };
}

function toVersionDto(entry: MockPageVersionRecord): Json {
  return {
    id: entry.id,
    pageId: entry.pageId,
    versionNumber: entry.versionNumber,
    kind: entry.kind,
    name: entry.name,
    createdBy: entry.createdBy,
    createdAt: entry.createdAt,
  };
}

function toTemplateDto(template: MockPageTemplateRecord): Json {
  return {
    id: template.id,
    name: template.name,
    description: template.description,
    title: template.title,
    icon: template.icon,
    blockCount: template.blocks.length,
    createdBy: template.createdBy,
    createdAt: template.createdAt,
  };
}

/** A captured block: the producer's block shape without a concurrency version. */
function toVersionBlockDto(
  block: MockPageVersionRecord["blocks"][number],
): Json {
  const body = readBlockBody({
    type: block.type,
    properties: block.properties,
  });
  return {
    id: block.id,
    parentId: block.parentBlockId ?? null,
    type: PRODUCER_TYPE[block.type] ?? "Text",
    content: body.content,
    properties: body.properties,
    position: String(Math.round(block.position ?? 0)).padStart(12, "0"),
  };
}

interface SeenBlock {
  blockId: string;
  version: number;
}

export const versionsOperations = [
  defineMockOperation<{ pageId: string }, never, Json[]>({
    id: "docs.versions.list",
    contract: {
      kind: "openapi",
      operationId: "Documents.Versions.ListPageVersions",
    },
    method: "GET",
    route: "/pages/:pageId/versions",
    async handle({ params, store }) {
      const page = store.getPage(params.pageId);
      if (!page || page.trashedAt) return notFound("Page not found");
      return ok<Json[]>(
        store.docsHistory.listVersions(params.pageId).map(toVersionDto),
      );
    },
  }),

  defineMockOperation<{ pageId: string; versionId: string }, never, Json>({
    id: "docs.versions.get",
    contract: {
      kind: "openapi",
      operationId: "Documents.Versions.GetPageVersion",
    },
    method: "GET",
    route: "/pages/:pageId/versions/:versionId",
    async handle({ params, store }) {
      const page = store.getPage(params.pageId);
      if (!page || page.trashedAt) return notFound("Page not found");
      const entry = store.docsHistory.getVersion(
        params.pageId,
        params.versionId,
      );
      if (!entry) return notFound("Version not found");
      return ok<Json>({
        version: toVersionDto(entry),
        title: entry.title,
        blocks: entry.blocks
          .slice()
          .sort((a, b) => a.position - b.position)
          .map(toVersionBlockDto),
      });
    },
  }),

  defineMockOperation<{ pageId: string }, { name?: string }, Json>({
    id: "docs.versions.create",
    contract: {
      kind: "openapi",
      operationId: "Documents.Versions.CreatePageVersion",
    },
    method: "POST",
    route: "/pages/:pageId/versions",
    async handle({ params, body, store }) {
      const page = store.getPage(params.pageId);
      if (!page || page.trashedAt) return notFound("Page not found");
      const name = body?.name?.trim() ?? "";
      if (!name) {
        return problem(400, "validation.failed", "A version needs a name.");
      }
      if (name.length > 200) {
        return problem(
          400,
          "validation.failed",
          "A version name is at most 200 characters.",
        );
      }
      const entry = store.docsHistory.addVersion(params.pageId, {
        userId: store.getCurrentUser().id,
        kind: "named",
        name,
      });
      return created<Json>(toVersionDto(entry));
    },
  }),

  defineMockOperation<
    { pageId: string; versionId: string },
    { currentBlocks?: SeenBlock[] },
    Json
  >({
    id: "docs.versions.restore",
    contract: {
      kind: "openapi",
      operationId: "Documents.Versions.RestorePageVersion",
    },
    method: "POST",
    route: "/pages/:pageId/versions/:versionId/restore",
    async handle({ params, body, store }) {
      const page = store.getPage(params.pageId);
      if (!page || page.trashedAt) return notFound("Page not found");
      const result = store.docsHistory.restoreVersion(
        params.pageId,
        params.versionId,
        store.getCurrentUser().id,
        body?.currentBlocks ?? [],
      );
      if (!result) return notFound("Version not found");
      if (!result.ok) {
        return problem(
          412,
          "documents.version.restore-conflict",
          "The page changed since you looked at it.",
          { currentBlocks: result.currentBlocks.map(toBlockDto) },
        );
      }
      return ok<Json>({
        version: toVersionDto(result.version),
        blocks: store.getPageBlocks(params.pageId).map(toBlockDto),
      });
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, Json[]>({
    id: "docs.templates.list",
    contract: {
      kind: "openapi",
      operationId: "Documents.Templates.ListPageTemplates",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/page-templates",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) {
        return notFound("Workspace not found");
      }
      return ok<Json[]>(
        store.docsHistory.listTemplates(params.workspaceId).map(toTemplateDto),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    { pageId?: string; name?: string; description?: string | null },
    Json
  >({
    id: "docs.templates.create",
    contract: {
      kind: "openapi",
      operationId: "Documents.Templates.CreatePageTemplate",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/page-templates",
    async handle({ params, body, store }) {
      const page = body?.pageId ? store.getPage(body.pageId) : undefined;
      if (!page || page.workspaceId !== params.workspaceId || page.trashedAt) {
        return notFound("Page not found");
      }
      const name = body?.name?.trim() ?? "";
      if (!name || name.length > 256) {
        return problem(
          400,
          "validation.failed",
          "A template needs a name of at most 256 characters.",
        );
      }
      const template = store.docsHistory.createTemplate(page.id, {
        name,
        description: body?.description,
        userId: store.getCurrentUser().id,
      });
      return created<Json>(toTemplateDto(template!));
    },
  }),

  defineMockOperation<
    { workspaceId: string; templateId: string },
    {
      title?: string | null;
      parentId?: string | null;
      spaceId?: string | null;
    },
    Json
  >({
    id: "docs.templates.createPage",
    contract: {
      kind: "openapi",
      operationId: "Documents.Templates.CreatePageFromTemplate",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/page-templates/:templateId/pages",
    async handle({ params, body, store }) {
      const template = store.docsHistory.getTemplate(
        params.workspaceId,
        params.templateId,
      );
      if (!template) return notFound("Template not found");
      if (body?.parentId && body.spaceId) {
        return problem(
          400,
          "validation.failed",
          "A nested page follows its parent's space.",
        );
      }
      if (body?.parentId) {
        const parent = store.getPage(body.parentId);
        if (
          !parent ||
          parent.workspaceId !== params.workspaceId ||
          parent.trashedAt
        ) {
          return notFound("Page not found");
        }
      }
      return created<Json>(
        store.docsHistory.createPageFromTemplate(template, {
          title: body?.title,
          parentId: body?.parentId,
          spaceId: body?.spaceId,
        }),
      );
    },
  }),
];
