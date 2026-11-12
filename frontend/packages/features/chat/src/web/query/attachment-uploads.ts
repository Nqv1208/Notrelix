/**
 * The uploads of message files (UC-CHT-G08). The author sends the message first; the files then go to that
 * message id one after the other (the per-message cap is checked by the server, so parallel files would race
 * on it). The list of uploads is a client-only projection in the query cache: it outlives the composer, so the
 * message shows each file's progress, a failure with its reason and a retry for that file alone. A stored file
 * leaves the list and joins the message in every cache that holds it. The realtime stream has no frame for
 * attachment changes, so this client patches its own copy.
 */

import type { QueryClient } from "@tanstack/react-query";
import type { ChatService } from "../../core/api/chat.service";
import {
  mergeAttachment,
  uploadFailureOf,
  type ChatUploadItem,
} from "../../core/model/attachment-model";
import type { ChatAttachment } from "../../core/types/chat";
import { chatQueryKeys } from "../../query/keys";
import { patchMessageInData } from "./interaction-cache";

export interface UploadContext {
  readonly queryClient: QueryClient;
  readonly service: ChatService;
  readonly workspaceId: string;
}

function readUploads(ctx: UploadContext): ChatUploadItem[] {
  return (
    ctx.queryClient.getQueryData<ChatUploadItem[]>(
      chatQueryKeys.uploads(ctx.workspaceId),
    ) ?? []
  );
}

function writeUploads(
  ctx: UploadContext,
  update: (items: ChatUploadItem[]) => ChatUploadItem[],
): void {
  ctx.queryClient.setQueryData<ChatUploadItem[]>(
    chatQueryKeys.uploads(ctx.workspaceId),
    update(readUploads(ctx)),
  );
}

function patchUpload(
  ctx: UploadContext,
  id: string,
  patch: Partial<ChatUploadItem>,
): void {
  writeUploads(ctx, (items) =>
    items.map((item) => (item.id === id ? { ...item, ...patch } : item)),
  );
}

/** Puts a stored file on its message in every loaded cache (history, thread, pins). */
export function attachToCachedMessage(
  ctx: UploadContext,
  messageId: string,
  attachment: ChatAttachment,
): void {
  ctx.queryClient.setQueriesData(
    { queryKey: chatQueryKeys.all(ctx.workspaceId) },
    (data: unknown) =>
      patchMessageInData(data, messageId, (message) => ({
        ...message,
        attachments: mergeAttachment(message.attachments, attachment),
      })),
  );
}

/** Takes a removed file off its message in every loaded cache. */
export function detachFromCachedMessage(
  ctx: UploadContext,
  messageId: string,
  attachmentId: string,
): void {
  ctx.queryClient.setQueriesData(
    { queryKey: chatQueryKeys.all(ctx.workspaceId) },
    (data: unknown) =>
      patchMessageInData(data, messageId, (message) =>
        (message.attachments ?? []).some((item) => item.id === attachmentId)
          ? {
              ...message,
              attachments: (message.attachments ?? []).filter(
                (item) => item.id !== attachmentId,
              ),
            }
          : message,
      ),
  );
}

async function runOne(ctx: UploadContext, id: string): Promise<void> {
  const item = readUploads(ctx).find((entry) => entry.id === id);
  if (!item || item.status === "uploading") return;
  patchUpload(ctx, id, { status: "uploading", progress: 0, reason: null });
  try {
    const attachment = await ctx.service.uploadAttachment(
      ctx.workspaceId,
      item.messageId,
      item.file,
      {
        idempotencyKey: item.idempotencyKey,
        onProgress: ({ loaded, total }) =>
          patchUpload(ctx, id, {
            progress: total > 0 ? Math.min(loaded / total, 1) : 0,
          }),
      },
    );
    writeUploads(ctx, (items) => items.filter((entry) => entry.id !== id));
    attachToCachedMessage(ctx, item.messageId, attachment);
  } catch (error) {
    patchUpload(ctx, id, {
      status: "failed",
      progress: 0,
      reason: uploadFailureOf(error),
    });
  }
}

/**
 * Reads the files of the message back (`Chat.Attachments.List`) once its batch is done: the server's list is
 * the truth, so a file whose answer was lost on the way still shows. A failed read changes nothing.
 */
async function reconcileAttachments(
  ctx: UploadContext,
  messageId: string,
): Promise<void> {
  try {
    const stored = await ctx.service.listAttachments(
      ctx.workspaceId,
      messageId,
    );
    for (const attachment of stored) {
      attachToCachedMessage(ctx, messageId, attachment);
    }
  } catch {
    // The files already patched stay as they are.
  }
}

/** Queues the files of a message that is stored and uploads them one after the other. */
export function enqueueUploads(
  ctx: UploadContext,
  message: { readonly id: string; readonly spaceId: string },
  files: readonly File[],
): void {
  if (files.length === 0) return;
  const items: ChatUploadItem[] = files.map((file) => ({
    id: crypto.randomUUID(),
    messageId: message.id,
    spaceId: message.spaceId,
    file,
    status: "queued",
    progress: 0,
    reason: null,
    idempotencyKey: crypto.randomUUID(),
  }));
  writeUploads(ctx, (held) => [...held, ...items]);
  void (async () => {
    for (const item of items) await runOne(ctx, item.id);
    await reconcileAttachments(ctx, message.id);
  })();
}

/** Uploads one failed file again with its own Idempotency-Key. */
export function retryUpload(ctx: UploadContext, id: string): void {
  const item = readUploads(ctx).find((entry) => entry.id === id);
  if (!item || item.status !== "failed") return;
  void runOne(ctx, id);
}

/** Drops a failed upload the author no longer wants. */
export function dismissUpload(ctx: UploadContext, id: string): void {
  writeUploads(ctx, (items) => items.filter((entry) => entry.id !== id));
}
