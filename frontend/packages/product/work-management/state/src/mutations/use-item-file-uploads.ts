import { useCallback, useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { refuseItemFile } from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/** Why a file did not make it onto the item (client pre-check or server answer). */
export type ItemFileUploadError =
  /** Over the per-file limit, found before sending. */
  | "tooBig"
  /** A blocked extension, found before sending. */
  | "blockedType"
  /** The server answered 413 `attachment.payload_too_large`. */
  | "serverTooLarge"
  /** The server answered 415 `attachment.unsupported_media_type`. */
  | "unsupportedType"
  /** The server answered 409 `attachment.limit_exceeded`. */
  | "limitReached"
  | "forbidden"
  | "notFound"
  /** The network or the server broke the transfer: retryable. */
  | "failed";

/** A file on its way to an item, or one that did not make it. */
export interface ItemFileUpload {
  readonly id: string;
  readonly name: string;
  readonly size: number;
  readonly status: "uploading" | "done" | "error";
  /** 0 to 100 while uploading; null when the transfer reports none. */
  readonly progress: number | null;
  readonly error: ItemFileUploadError | null;
}

/** The parts of an `AppError` the reason is read from. */
function readFailure(error: unknown): {
  kind?: unknown;
  status?: unknown;
  code?: unknown;
} {
  return typeof error === "object" && error !== null ? error : {};
}

/** Maps what the API answered to the reason shown to the member. */
export function classifyItemFileUploadError(
  error: unknown,
): ItemFileUploadError {
  const { status, code } = readFailure(error);
  if (status === 413 || code === "attachment.payload_too_large")
    return "serverTooLarge";
  if (status === 415 || code === "attachment.unsupported_media_type")
    return "unsupportedType";
  if (status === 409 && code === "attachment.limit_exceeded")
    return "limitReached";
  if (status === 403) return "forbidden";
  if (status === 404) return "notFound";
  return "failed";
}

/** Reasons a retry can change: the rest need another file or another item. */
const RETRYABLE: ReadonlySet<ItemFileUploadError> = new Set([
  "failed",
  "limitReached",
]);

export function canRetryItemFileUpload(upload: ItemFileUpload): boolean {
  return upload.status === "error" && RETRYABLE.has(upload.error ?? "failed");
}

/** How long a finished upload row stays before the saved file replaces it. */
export const ITEM_FILE_UPLOAD_DONE_MS = 2_500;

interface Pending {
  readonly file: File;
  /** One per user action; reused only to retry that same action. */
  readonly idempotencyKey: string;
  controller: AbortController | null;
}

/**
 * The files being sent to an item: one row each, kept until sent, skipped or
 * dismissed. The server owns the attachment; a successful upload refreshes the
 * item's files and activity from it (no optimistic row).
 */
export function useItemFileUploads(itemId: string, workspaceId: string) {
  const { cards } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const [uploads, setUploads] = useState<ItemFileUpload[]>([]);
  const pending = useRef(new Map<string, Pending>());
  const timers = useRef(new Set<ReturnType<typeof setTimeout>>());

  const patch = useCallback((id: string, change: Partial<ItemFileUpload>) => {
    setUploads((list) =>
      list.map((entry) => (entry.id === id ? { ...entry, ...change } : entry)),
    );
  }, []);

  const refresh = useCallback(() => {
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardFiles(workspaceId, itemId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardActivity(workspaceId, itemId),
    });
  }, [queryClient, workspaceId, itemId]);

  const send = useCallback(
    (id: string) => {
      const entry = pending.current.get(id);
      if (!entry) return;
      const controller = new AbortController();
      entry.controller = controller;
      patch(id, { status: "uploading", error: null, progress: null });
      cards
        .uploadCardFile(itemId, entry.file, {
          idempotencyKey: entry.idempotencyKey,
          signal: controller.signal,
          onProgress: ({ loaded, total }) => {
            if (total > 0) patch(id, { progress: (loaded / total) * 100 });
          },
        })
        .then(() => {
          pending.current.delete(id);
          patch(id, { status: "done", progress: 100 });
          refresh();
          const timer = setTimeout(() => {
            timers.current.delete(timer);
            setUploads((list) => list.filter((row) => row.id !== id));
          }, ITEM_FILE_UPLOAD_DONE_MS);
          timers.current.add(timer);
        })
        .catch((error: unknown) => {
          if (readFailure(error).kind === "aborted") {
            // A cancelled request does not prove the server did nothing.
            refresh();
            return;
          }
          patch(id, {
            status: "error",
            error: classifyItemFileUploadError(error),
          });
        });
    },
    [cards, itemId, patch, refresh],
  );

  const start = useCallback(
    (file: File) => {
      const id = crypto.randomUUID();
      const row = { id, name: file.name, size: file.size, progress: null };
      pending.current.set(id, {
        file,
        idempotencyKey: crypto.randomUUID(),
        controller: null,
      });
      const refusal = refuseItemFile(file);
      if (refusal) {
        setUploads((list) => [
          ...list,
          { ...row, status: "error", error: refusal },
        ]);
        return;
      }
      setUploads((list) => [
        ...list,
        { ...row, status: "uploading", error: null },
      ]);
      send(id);
    },
    [send],
  );

  const retry = useCallback(
    (id: string) => {
      const row = uploads.find((candidate) => candidate.id === id);
      if (!row || !canRetryItemFileUpload(row)) return;
      send(id);
    },
    [send, uploads],
  );

  const dismiss = useCallback((id: string) => {
    pending.current.get(id)?.controller?.abort();
    pending.current.delete(id);
    setUploads((list) => list.filter((row) => row.id !== id));
  }, []);

  useEffect(() => {
    const live = pending.current;
    const waiting = timers.current;
    return () => {
      for (const entry of live.values()) entry.controller?.abort();
      for (const timer of waiting) clearTimeout(timer);
    };
  }, []);

  return { uploads, start, retry, dismiss };
}
