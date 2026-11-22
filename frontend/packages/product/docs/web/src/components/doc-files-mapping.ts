import {
  formatFileSize,
  isWebUrl,
  type PageAttachment,
} from "@notrelix/docs-core";
import { classifyPageFilesFailure } from "@notrelix/docs-state";
import { dayMonth } from "./doc-page-mapping";
import type { DocFileEntry, DocFilesStatus } from "./doc-page-models";

/** The short type label of a file ("PDF", "PNG"): its extension, else its type. */
function typeLabel(fileName: string, contentType: string): string {
  const dot = fileName.lastIndexOf(".");
  if (dot >= 0 && dot < fileName.length - 1) {
    return fileName.slice(dot + 1).toUpperCase();
  }
  return contentType.split("/")[1]?.toUpperCase() ?? "";
}

/** The host of a web address, or the address itself when it has none. */
export function linkHost(url: string): string {
  try {
    return new URL(url).host || url;
  } catch {
    return url;
  }
}

/** The page's attachments as the Files tab lists them (newest first, as the server sends them). */
export function toFileEntries(
  attachments: readonly PageAttachment[],
  locale: string,
): DocFileEntry[] {
  return attachments.map((attachment) => {
    const isLink = attachment.kind === "link";
    const date = new Date(attachment.createdAt);
    const detail = [
      isLink
        ? linkHost(attachment.url ?? "")
        : `${formatFileSize(attachment.sizeBytes, locale)} · ${typeLabel(attachment.fileName, attachment.contentType)}`,
      attachment.uploadedByName,
      Number.isNaN(date.getTime()) ? null : dayMonth(date),
    ]
      .filter(Boolean)
      .join(" · ");
    return {
      id: attachment.id,
      name: attachment.fileName || attachment.url || attachment.id,
      detail,
      kind: isLink ? "link" : "file",
      // Only an absolute http or https address becomes an anchor.
      href:
        isLink && attachment.url && isWebUrl(attachment.url)
          ? attachment.url
          : null,
    };
  });
}

/** What the Files tab shows for the list query: a refetch that fails keeps the files it has. */
export function filesStatusOf(query: {
  readonly isLoading: boolean;
  readonly isError: boolean;
  readonly data: unknown;
  readonly error: unknown;
}): DocFilesStatus {
  if (query.data !== undefined) return "ready";
  if (query.isLoading) return "loading";
  if (!query.isError) return "loading";
  const failure = classifyPageFilesFailure(query.error);
  if (failure === "forbidden") return "denied";
  if (failure === "notFound") return "notFound";
  return "error";
}
