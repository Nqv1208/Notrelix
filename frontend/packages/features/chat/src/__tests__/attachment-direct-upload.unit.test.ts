import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import type { ChatService } from "../core/api/chat.service";
import type { ChatUploadItem } from "../core/model/attachment-model";
import type { ChatAttachment, ChatAttachmentUpload } from "../core/types/chat";
import { chatQueryKeys } from "../query/keys";
import {
  enqueueUploads,
  type UploadContext,
} from "../web/query/attachment-uploads";
import { putObjectWithProgress } from "../web/query/put-object";

vi.mock("../web/query/put-object", () => ({
  putObjectWithProgress: vi.fn(async () => undefined),
  ObjectPutError: class ObjectPutError extends Error {},
}));

const WORKSPACE = "ws-1";
const put = vi.mocked(putObjectWithProgress);

const message = { id: "m-1", spaceId: "s-1", content: "see", version: 1 };

const ticket: ChatAttachmentUpload = {
  uploadId: "u-1",
  method: "PUT",
  uploadUrl: "https://bucket.test/put/u-1?X-Amz-Signature=abc",
  contentType: "image/png",
  contentLength: 4,
  expiresAt: "2026-11-13T09:05:00Z",
  commitPath:
    "/api/v1/workspaces/ws-1/chat/messages/m-1/attachments/uploads/u-1/commit",
};

const stored: ChatAttachment = {
  id: "a-1",
  messageId: "m-1",
  fileName: "photo.png",
  sizeBytes: 4,
  contentType: "image/png",
  uploadedBy: "u",
  createdAt: "2026-11-13T09:00:00Z",
  downloadPath:
    "/api/v1/workspaces/ws-1/chat/messages/m-1/attachments/a-1/content",
};

function contextWith(service: Partial<Record<keyof ChatService, unknown>>) {
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

/** Waits until the queue has drained (every file stored or failed). */
async function untilDrained(queryClient: QueryClient) {
  for (let attempt = 0; attempt < 100; attempt++) {
    if (uploads(queryClient).length === 0) return;
    await new Promise((resolve) => setTimeout(resolve, 5));
  }
  throw new Error("the queue did not drain");
}

function png(name: string) {
  return new File([new Uint8Array([0x89, 0x50, 0x4e, 0x47])], name, {
    type: "image/png",
  });
}

describe("direct (two-step) attachment uploads", () => {
  beforeEach(() => {
    put.mockReset();
    put.mockImplementation(async () => undefined);
  });

  it("PUTs the bytes with progress reported on the upload item, then commits the stored file", async () => {
    let progressDuringPut: number | undefined;
    put.mockImplementation(async ({ onProgress }) => {
      onProgress?.({ loaded: 2, total: 4 });
      progressDuringPut = uploads(queryClient)[0]?.progress;
    });
    const service = {
      createAttachmentUpload: vi.fn(async () => ticket),
      commitAttachmentUpload: vi.fn(async () => stored),
      uploadAttachment: vi.fn(),
    };
    const { ctx, queryClient } = contextWith(service);

    enqueueUploads(ctx, message, [png("photo.png")]);
    await untilDrained(queryClient);

    expect(progressDuringPut).toBe(0.5);
    expect(put).toHaveBeenCalledWith(
      expect.objectContaining({
        url: ticket.uploadUrl,
        method: "PUT",
        contentType: "image/png",
      }),
    );
    expect(service.commitAttachmentUpload).toHaveBeenCalledWith(
      WORKSPACE,
      "m-1",
      "u-1",
    );
    expect(service.uploadAttachment).not.toHaveBeenCalled();
  });

  it("falls back to the streaming upload when the server refuses direct uploads, and stops probing", async () => {
    const refusal = Object.assign(new Error("unsupported"), {
      status: 409,
      code: "storage-direct-upload-unsupported",
    });
    const service = {
      createAttachmentUpload: vi.fn(async () => {
        throw refusal;
      }),
      commitAttachmentUpload: vi.fn(),
      uploadAttachment: vi.fn(async () => stored),
    };
    const { ctx, queryClient } = contextWith(service);

    enqueueUploads(ctx, message, [png("one.png")]);
    await untilDrained(queryClient);
    enqueueUploads(ctx, message, [png("two.png")]);
    await untilDrained(queryClient);

    expect(service.createAttachmentUpload).toHaveBeenCalledTimes(1);
    expect(service.uploadAttachment).toHaveBeenCalledTimes(2);
    expect(put).not.toHaveBeenCalled();
    expect(service.commitAttachmentUpload).not.toHaveBeenCalled();
  });

  it("keeps a failed PUT as that file's failure and does not fall back", async () => {
    put.mockRejectedValueOnce(
      Object.assign(new Error("network"), { status: 0 }),
    );
    const service = {
      createAttachmentUpload: vi.fn(async () => ticket),
      commitAttachmentUpload: vi.fn(),
      uploadAttachment: vi.fn(),
    };
    const { ctx, queryClient } = contextWith(service);

    enqueueUploads(ctx, message, [png("photo.png")]);
    for (let attempt = 0; attempt < 100; attempt++) {
      if (uploads(queryClient).some((item) => item.status === "failed")) break;
      await new Promise((resolve) => setTimeout(resolve, 5));
    }

    expect(uploads(queryClient)[0]?.status).toBe("failed");
    expect(service.uploadAttachment).not.toHaveBeenCalled();
    expect(service.commitAttachmentUpload).not.toHaveBeenCalled();
  });
});
