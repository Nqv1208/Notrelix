import { describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import type { ChatService } from "../core/api/chat.service";
import { recordingFile } from "../core/model/voice-model";
import { chatQueryKeys } from "../query/keys";
import {
  dismissUpload,
  enqueueUploads,
  type UploadContext,
} from "../web/query/attachment-uploads";
import type { ChatUploadItem } from "../core/model/attachment-model";

const WORKSPACE = "ws-1";
const RECORDING = {
  blob: new Blob(["voice"], { type: "audio/webm" }),
  durationMs: 3000,
  waveform: [1, 2],
};

function contextWith(
  service: Partial<Record<"uploadAttachment" | "deleteMessage", unknown>>,
) {
  const queryClient = new QueryClient();
  const ctx: UploadContext = {
    queryClient,
    service: service as unknown as ChatService,
    workspaceId: WORKSPACE,
  };
  return { ctx, queryClient };
}

function uploads(queryClient: QueryClient): ChatUploadItem[] {
  return (
    queryClient.getQueryData<ChatUploadItem[]>(
      chatQueryKeys.uploads(WORKSPACE),
    ) ?? []
  );
}

/** Lets the delete that goes with a dismissal settle. */
async function settle() {
  await new Promise((resolve) => setTimeout(resolve, 5));
}

/** Waits until the queued upload has failed, so it can be dismissed. */
async function untilFailed(queryClient: QueryClient) {
  for (let attempt = 0; attempt < 50; attempt++) {
    if (uploads(queryClient).some((item) => item.status === "failed")) return;
    await new Promise((resolve) => setTimeout(resolve, 5));
  }
  throw new Error("the upload did not fail");
}

const message = (content: string | null) => ({
  id: "m-1",
  spaceId: "s-1",
  content,
  version: 7,
});

describe("voice-only uploads (UC-CHT-I04): dismissing a failed upload", () => {
  it("deletes the empty message it held, with its version, and clears the row", async () => {
    const deleteMessage = vi.fn(async () => undefined);
    const { ctx, queryClient } = contextWith({
      uploadAttachment: vi.fn(async () => {
        throw new Error("network");
      }),
      deleteMessage,
    });
    enqueueUploads(ctx, message(""), [recordingFile(RECORDING)]);
    await untilFailed(queryClient);

    dismissUpload(ctx, uploads(queryClient)[0]!.id);
    await settle();

    expect(deleteMessage).toHaveBeenCalledWith(WORKSPACE, "m-1", 7);
    expect(uploads(queryClient)).toEqual([]);
  });

  it("leaves the message alone when it has text (a voice file is only part of it)", async () => {
    const deleteMessage = vi.fn(async () => undefined);
    const { ctx, queryClient } = contextWith({
      uploadAttachment: vi.fn(async () => {
        throw new Error("network");
      }),
      deleteMessage,
    });
    enqueueUploads(ctx, message("hello"), [recordingFile(RECORDING)]);
    await untilFailed(queryClient);

    dismissUpload(ctx, uploads(queryClient)[0]!.id);
    await settle();

    expect(deleteMessage).not.toHaveBeenCalled();
    expect(uploads(queryClient)).toEqual([]);
  });

  it("brings the row back as failed when the message cannot be deleted, so the author can dismiss again", async () => {
    const deleteMessage = vi.fn(async () => {
      throw new Error("conflict");
    });
    const { ctx, queryClient } = contextWith({
      uploadAttachment: vi.fn(async () => {
        throw new Error("network");
      }),
      deleteMessage,
    });
    enqueueUploads(ctx, message(""), [recordingFile(RECORDING)]);
    await untilFailed(queryClient);

    dismissUpload(ctx, uploads(queryClient)[0]!.id);
    await settle();

    expect(deleteMessage).toHaveBeenCalledTimes(1);
    expect(uploads(queryClient).map((item) => item.status)).toEqual(["failed"]);
  });
});
