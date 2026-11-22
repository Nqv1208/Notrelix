import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import type { PageAttachment } from "@notrelix/docs-core";
import type {
  DocsApiClient,
  DocsFileClient,
  DocsUploadOptions,
  PageApiEndpoints,
} from "./page.api";

/**
 * `Collaboration.PageAttachments.UploadFile` answers the stored attachment.
 * The producer's list answers the same item (its OpenAPI response is not typed
 * yet, so the upload's 201 body names the shape).
 */
export type PageAttachmentDtoApi = OperationResponse<
  "Collaboration.PageAttachments.UploadFile",
  201
>;

/** `Collaboration.PageAttachments.Create` registers a link: the server owns its size, type and source. */
type CreateLinkBody =
  OperationRequestBody<"Collaboration.PageAttachments.Create">;

/** A stored attachment as the page reads it; an entry without an id is dropped. */
export function mapPageAttachment(
  dto: PageAttachmentDtoApi,
): PageAttachment | null {
  if (!dto.id) return null;
  return {
    id: dto.id,
    fileName: dto.filename ?? "",
    sizeBytes: dto.sizeBytes ?? 0,
    contentType: dto.contentType ?? "application/octet-stream",
    createdAt: dto.createdAt ?? "",
    ...(dto.kind === "Link" ? { kind: "link" as const } : {}),
    ...(dto.url ? { url: dto.url } : {}),
    ...(dto.uploadedByName ? { uploadedByName: dto.uploadedByName } : {}),
  };
}

function mapList(dtos: readonly PageAttachmentDtoApi[]): PageAttachment[] {
  return dtos.flatMap((dto) => mapPageAttachment(dto) ?? []);
}

/**
 * The files of a Documents page (`documents.page` attachment target): listed
 * and read by people who can view the page, added and removed by people who
 * can edit it. There is no public address; bytes are read with the session.
 */
export function createAttachmentApi(
  api: DocsApiClient & DocsFileClient,
  endpoints: PageApiEndpoints,
) {
  return {
    /** `Collaboration.PageAttachments.Get`, newest first. */
    async list(pageId: string): Promise<PageAttachment[]> {
      return mapList(
        await api.get<PageAttachmentDtoApi[]>(
          endpoints.pageAttachments.list(pageId),
        ),
      );
    },

    /**
     * `Collaboration.PageAttachments.UploadFile`: multipart, part `file`.
     * `idempotencyKey` belongs to one user action (a retry reuses it).
     */
    async upload(
      pageId: string,
      file: File,
      options: DocsUploadOptions,
    ): Promise<PageAttachment> {
      const form = new FormData();
      form.append("file", file, file.name);
      const dto = await api.upload<PageAttachmentDtoApi>(
        endpoints.pageAttachments.files(pageId),
        form,
        options,
      );
      const attachment = mapPageAttachment(dto);
      if (!attachment) throw new Error("The upload answered no attachment.");
      return attachment;
    },

    /**
     * `Collaboration.PageAttachments.Create`: registers an external link (only
     * an absolute http or https address). `idempotencyKey` belongs to one user
     * action; retrying the same link reuses it. The caller shows the failure.
     */
    async createLink(
      pageId: string,
      link: { readonly url: string; readonly name: string },
      options: { readonly idempotencyKey: string },
    ): Promise<void> {
      const body: CreateLinkBody = { filename: link.name, url: link.url };
      await api.post<unknown>(endpoints.pageAttachments.list(pageId), body, {
        idempotencyKey: options.idempotencyKey,
        skipGlobalErrorToast: true,
      });
    },

    /** `Collaboration.PageAttachments.GetContent`: the bytes, authorised by page view. */
    content(pageId: string, attachmentId: string): Promise<Blob> {
      return api.getBlob(
        endpoints.pageAttachments.content(pageId, attachmentId),
      );
    },

    /** `Collaboration.PageAttachments.Delete`. */
    async remove(pageId: string, attachmentId: string): Promise<void> {
      await api.delete<void>(
        endpoints.pageAttachments.detail(pageId, attachmentId),
      );
    },
  };
}
