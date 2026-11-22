/**
 * Documents: the files of a page (image and file blocks), as
 * `Collaboration.PageAttachments.*` serves them: same file policy as an item's
 * attachments (100 MB, blocked extensions, at most 50 per page), idempotent
 * uploads, bytes read back only through the page's own content route, and an
 * attachment of another page is never listed, opened or deleted.
 *
 * Operations:
 *   docs.pageAttachments.list    GET    /pages/:pageId/attachments
 *   docs.pageAttachments.create  POST   /pages/:pageId/attachments (a link)
 *   docs.pageAttachments.upload  POST   /pages/:pageId/attachments/files
 *   docs.pageAttachments.content GET    /pages/:pageId/attachments/:attachmentId/content
 *   docs.pageAttachments.delete  DELETE /pages/:pageId/attachments/:attachmentId
 */
import { defineMockOperation } from "../../operations/types";
import { noContent, notFound } from "../../transport/create-response";
import type { MockHttpResult } from "../../transport/create-response";
import type { MockPageAttachmentRecord } from "../../state/records";
import type { MockStore } from "../../state/mock-store";

export const MOCK_MAX_PAGE_FILE_BYTES = 100 * 1024 * 1024;
export const MOCK_MAX_PAGE_FILES = 50;
const BLOCKED_EXTENSIONS = new Set([
  "exe",
  "msi",
  "dll",
  "bat",
  "cmd",
  "com",
  "scr",
  "ps1",
  "vbs",
  "sh",
  "jar",
  "apk",
  "js",
  "mjs",
  "html",
  "htm",
]);

/** The producer's link allow-list: an absolute http or https address with a host. */
function isWebUrl(value: string): boolean {
  try {
    const url = new URL(value.trim());
    return (
      (url.protocol === "http:" || url.protocol === "https:") && !!url.host
    );
  } catch {
    return false;
  }
}

export function isBlocked(name: string): boolean {
  const dot = name.lastIndexOf(".");
  return (
    dot >= 0 &&
    BLOCKED_EXTENSIONS.has(
      name
        .slice(dot + 1)
        .trim()
        .toLowerCase(),
    )
  );
}

export function problem(
  status: number,
  errorCode: string,
  detail: string,
): MockHttpResult<unknown> {
  return {
    status,
    body: {
      status,
      title: detail,
      detail,
      message: detail,
      errorCode,
      code: errorCode,
    },
  };
}

function startsWith(bytes: Uint8Array, ...head: number[]): boolean {
  return head.every((value, index) => bytes[index] === value);
}

/** The type the producer stores: images by their bytes, anything else as sent. */
export function classify(bytes: Uint8Array, sent: string): string {
  if (startsWith(bytes, 0x89, 0x50, 0x4e, 0x47)) return "image/png";
  if (startsWith(bytes, 0xff, 0xd8, 0xff)) return "image/jpeg";
  if (startsWith(bytes, 0x47, 0x49, 0x46, 0x38)) return "image/gif";
  if (startsWith(bytes, 0x52, 0x49, 0x46, 0x46)) return "image/webp";
  return sent && !sent.startsWith("image/") ? sent : "application/octet-stream";
}

function attachmentDto(file: MockPageAttachmentRecord, store: MockStore) {
  return {
    id: file.id,
    resourceId: file.pageId,
    filename: file.filename,
    url: file.url ?? "",
    kind: file.url ? "Link" : "File",
    sizeBytes: file.sizeBytes,
    contentType: file.contentType,
    source: file.url ? "link" : "upload",
    uploadedBy: file.uploadedBy,
    uploadedByName: store.getUser(file.uploadedBy)?.name ?? null,
    createdAt: file.createdAt,
  };
}

/** A trashed or unknown page answers like an unknown one. */
function pageOf(store: MockStore, pageId: string) {
  const page = store.getPage(pageId);
  return page && !page.trashedAt ? page : undefined;
}

export const pageAttachmentsOperations = [
  defineMockOperation<{ pageId: string }, never, unknown>({
    id: "docs.pageAttachments.list",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.PageAttachments.Get",
    },
    method: "GET",
    route: "/pages/:pageId/attachments",
    async handle({ params, store }) {
      if (!pageOf(store, params.pageId)) return notFound("Page not found");
      return {
        status: 200,
        body: store
          .getPageAttachments(params.pageId)
          .map((file) => attachmentDto(file, store)),
      };
    },
  }),

  defineMockOperation<
    { pageId: string },
    { filename?: string | null; url?: string | null },
    unknown
  >({
    id: "docs.pageAttachments.create",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.PageAttachments.Create",
    },
    method: "POST",
    route: "/pages/:pageId/attachments",
    async handle({
      params,
      body,
      store,
      request,
    }): Promise<MockHttpResult<unknown>> {
      const key = request.headers.get("Idempotency-Key");
      if (!key) {
        return problem(
          400,
          "validation.failed",
          "Idempotency-Key is required.",
        );
      }
      const page = pageOf(store, params.pageId);
      if (!page) return notFound("Page not found");
      // A viewer (a guest here) can see the page but not add to it.
      if (store.getCurrentMemberRole(page.workspaceId) === "guest") {
        return problem(
          403,
          "collaboration.attachment_editor_only",
          "Only editors can add attachments.",
        );
      }
      const replay = store.getPageUploadByKey(params.pageId, key);
      if (replay) {
        return {
          status: 201,
          headers: { "Idempotency-Replayed": "true" },
          body: attachmentDto(replay, store),
        };
      }
      const url = body?.url?.trim() ?? "";
      const filename = body?.filename?.trim() ?? "";
      if (!isWebUrl(url)) {
        return problem(
          400,
          "validation.failed",
          "The link must be an absolute http or https address.",
        );
      }
      if (!filename || filename.length > 255) {
        return problem(
          400,
          "validation.failed",
          "A name of up to 255 characters is required.",
        );
      }
      if (
        store.getPageAttachments(params.pageId).length >= MOCK_MAX_PAGE_FILES
      ) {
        return problem(
          409,
          "attachment.limit_exceeded",
          "The page has the most attachments it can hold.",
        );
      }
      const record = store.addPageAttachment(params.pageId, {
        filename,
        contentType: "text/uri-list",
        bytes: new Uint8Array(),
        idempotencyKey: key,
        url,
      });
      if (!record) return notFound("Page not found");
      return { status: 201, body: attachmentDto(record, store) };
    },
  }),

  defineMockOperation<{ pageId: string }, never, unknown>({
    id: "docs.pageAttachments.upload",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.PageAttachments.UploadFile",
    },
    method: "POST",
    route: "/pages/:pageId/attachments/files",
    async handle({ params, store, request }): Promise<MockHttpResult<unknown>> {
      const key = request.headers.get("Idempotency-Key");
      if (!key) {
        return problem(
          400,
          "validation.failed",
          "Idempotency-Key is required.",
        );
      }
      if (!pageOf(store, params.pageId)) return notFound("Page not found");
      const replay = store.getPageUploadByKey(params.pageId, key);
      if (replay) {
        return {
          status: 201,
          headers: { "Idempotency-Replayed": "true" },
          body: attachmentDto(replay, store),
        };
      }
      const part = request.formData?.get("file");
      if (!part || typeof part === "string" || !part.name) {
        return problem(
          400,
          "validation.failed",
          "A file part with a file name is required.",
        );
      }
      if (part.size === 0) {
        return problem(400, "validation.failed", "The file is empty.");
      }
      if (part.size > MOCK_MAX_PAGE_FILE_BYTES) {
        return problem(
          413,
          "attachment.payload_too_large",
          "The file is over the size limit.",
        );
      }
      if (isBlocked(part.name)) {
        return problem(
          415,
          "attachment.unsupported_media_type",
          "This type of file is not allowed.",
        );
      }
      if (
        store.getPageAttachments(params.pageId).length >= MOCK_MAX_PAGE_FILES
      ) {
        return problem(
          409,
          "attachment.limit_exceeded",
          "The page has the most attachments it can hold.",
        );
      }
      const bytes = new Uint8Array(await part.arrayBuffer());
      const record = store.addPageAttachment(params.pageId, {
        filename: part.name,
        contentType: classify(bytes, part.type),
        bytes,
        idempotencyKey: key,
      });
      if (!record) return notFound("Page not found");
      return {
        status: 201,
        headers: {
          Location: `/api/v1/pages/${params.pageId}/attachments/${record.id}/content`,
        },
        body: attachmentDto(record, store),
      };
    },
  }),

  defineMockOperation<{ pageId: string; attachmentId: string }, never, unknown>(
    {
      id: "docs.pageAttachments.content",
      contract: {
        kind: "openapi",
        operationId: "Collaboration.PageAttachments.GetContent",
      },
      method: "GET",
      route: "/pages/:pageId/attachments/:attachmentId/content",
      async handle({ params, store }) {
        if (!pageOf(store, params.pageId))
          return notFound("Attachment not found");
        const file = store.getPageAttachment(
          params.pageId,
          params.attachmentId,
        );
        // A link has no stored bytes: there is nothing to stream.
        if (!file || file.url) return notFound("Attachment not found");
        const inline = file.contentType.startsWith("image/");
        return {
          status: 200,
          headers: {
            "Content-Type": file.contentType,
            "Content-Disposition": inline
              ? "inline"
              : `attachment; filename*=UTF-8''${encodeURIComponent(file.filename)}`,
            "X-Content-Type-Options": "nosniff",
            "Cache-Control": "private, no-store",
          },
          rawBody: file.bytes,
        };
      },
    },
  ),

  defineMockOperation<{ pageId: string; attachmentId: string }, never, unknown>(
    {
      id: "docs.pageAttachments.delete",
      contract: {
        kind: "openapi",
        operationId: "Collaboration.PageAttachments.Delete",
      },
      method: "DELETE",
      route: "/pages/:pageId/attachments/:attachmentId",
      async handle({ params, store }) {
        if (!pageOf(store, params.pageId)) return notFound("Page not found");
        if (!store.removePageAttachment(params.pageId, params.attachmentId)) {
          return notFound("Attachment not found");
        }
        return noContent();
      },
    },
  ),
];
