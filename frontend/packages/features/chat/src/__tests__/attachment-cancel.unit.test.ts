import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import type { ChatService } from "../core/api/chat.service";
import type { ChatUploadItem } from "../core/model/attachment-model";
import type { ChatAttachment } from "../core/types/chat";
import { chatQueryKeys } from "../query/keys";
import {
  cancelUpload,
  enqueueUploads,
  retryUpload,
  type UploadContext,
} from "../web/query/attachment-uploads";
import { putObjectWithProgress } from "../web/query/put-object";

const WORKSPACE = "ws-1";
const message = { id: "m-1", spaceId: "s-1", content: "see", version: 1 };

function stored(name: string): ChatAttachment {
  return {
    id: `a-${name}`,
    messageId: "m-1",
    fileName: name,
    sizeBytes: 3,
    contentType: "image/png",
    uploadedBy: "u",
    createdAt: "2026-11-19T09:00:00Z",
    downloadPath: `/x/${name}`,
  };
}

function contextWith(service: Partial<Record<keyof ChatService, unknown>>) {
  const queryClient = new QueryClient();
  const ctx: UploadContext = {
    queryClient,
    service: service as unknown as ChatService,
    workspaceId: WORKSPACE,
  };
  return { ctx, queryClient };
}

const uploads = (queryClient: QueryClient) =>
  queryClient.getQueryData<ChatUploadItem[]>(
    chatQueryKeys.uploads(WORKSPACE),
  ) ?? [];

async function until(check: () => boolean) {
  for (let attempt = 0; attempt < 100; attempt++) {
    if (check()) return;
    await new Promise((resolve) => setTimeout(resolve, 5));
  }
  throw new Error("the condition did not come true");
}

const png = (name: string) => new File(["abc"], name, { type: "image/png" });

describe("cancelling an upload", () => {
  it("aborts the running request, removes the tile, and the next file still goes", async () => {
    let firstSignal: AbortSignal | undefined;
    const uploadAttachment = vi.fn(
      (
        _workspace: string,
        _message: string,
        file: File,
        options: { signal?: AbortSignal },
      ) => {
        if (file.name === "slow.png") {
          firstSignal = options.signal;
          return new Promise<ChatAttachment>((_resolve, reject) =>
            options.signal?.addEventListener("abort", () =>
              reject(new DOMException("aborted", "AbortError")),
            ),
          );
        }
        return Promise.resolve(stored(file.name));
      },
    );
    const { ctx, queryClient } = contextWith({
      uploadAttachment,
      listAttachments: vi.fn(async () => []),
    });
    enqueueUploads(ctx, message, [png("slow.png"), png("next.png")]);
    await until(() =>
      uploads(queryClient).some((item) => item.status === "uploading"),
    );

    const running = uploads(queryClient).find(
      (item) => item.status === "uploading",
    )!;
    cancelUpload(ctx, running.id);

    expect(firstSignal?.aborted).toBe(true);
    expect(uploads(queryClient).map((item) => item.file.name)).toEqual([
      "next.png",
    ]);
    await until(() => uploads(queryClient).length === 0);
    expect(uploadAttachment).toHaveBeenCalledTimes(2);
  });

  it("removes a queued tile before it starts, so its request is never sent", async () => {
    const uploadAttachment = vi.fn(
      (_workspace: string, _message: string, file: File) =>
        new Promise<ChatAttachment>((resolve) =>
          setTimeout(() => resolve(stored(file.name)), 20),
        ),
    );
    const { ctx, queryClient } = contextWith({
      uploadAttachment,
      listAttachments: vi.fn(async () => []),
    });
    enqueueUploads(ctx, message, [png("a.png"), png("b.png")]);
    const queued = uploads(queryClient).find(
      (item) => item.file.name === "b.png",
    )!;
    cancelUpload(ctx, queued.id);
    await until(() => uploads(queryClient).length === 0);
    expect(uploadAttachment).toHaveBeenCalledTimes(1);
  });
});

describe("an upload that fails on the network", () => {
  it("keeps its tile with a retryable reason while the other tiles still send, and Retry sends it again", async () => {
    let attempts = 0;
    const uploadAttachment = vi.fn(
      async (_workspace: string, _message: string, file: File) => {
        if (file.name === "bad.png" && attempts++ === 0) {
          throw new TypeError("Network error during upload.");
        }
        return stored(file.name);
      },
    );
    const { ctx, queryClient } = contextWith({
      uploadAttachment,
      listAttachments: vi.fn(async () => []),
    });
    enqueueUploads(ctx, message, [png("bad.png"), png("good.png")]);
    await until(
      () =>
        uploads(queryClient).length === 1 &&
        uploadAttachment.mock.calls.length === 2,
    );

    const [failed] = uploads(queryClient);
    expect(failed).toMatchObject({ status: "failed", reason: "retryable" });
    expect(failed!.file.name).toBe("bad.png");

    retryUpload(ctx, failed!.id);
    await until(() => uploads(queryClient).length === 0);
    expect(uploadAttachment).toHaveBeenCalledTimes(3);
  });
});

describe("putObjectWithProgress", () => {
  class FakeXhr {
    static last: FakeXhr;
    upload: { onprogress: ((event: unknown) => void) | null } = {
      onprogress: null,
    };
    onload: (() => void) | null = null;
    onerror: (() => void) | null = null;
    onabort: (() => void) | null = null;
    status = 200;
    aborted = false;
    constructor() {
      FakeXhr.last = this;
    }
    open() {}
    setRequestHeader() {}
    send() {}
    abort() {
      this.aborted = true;
      this.onabort?.();
    }
  }

  afterEach(() => vi.unstubAllGlobals());

  it("reports progress and aborts the request when its signal fires", async () => {
    vi.stubGlobal("XMLHttpRequest", FakeXhr);
    const controller = new AbortController();
    const seen: number[] = [];
    const put = putObjectWithProgress({
      url: "https://bucket.test/put",
      method: "PUT",
      contentType: "image/png",
      body: new Blob(["abc"]),
      onProgress: ({ loaded, total }) => seen.push(loaded / total),
      signal: controller.signal,
    });
    FakeXhr.last.upload.onprogress?.({
      lengthComputable: true,
      loaded: 1,
      total: 4,
    });
    controller.abort();
    await expect(put).rejects.toMatchObject({ status: 0 });
    expect(seen).toEqual([0.25]);
    expect(FakeXhr.last.aborted).toBe(true);
  });

  it("does not send at all when the signal is already aborted", async () => {
    vi.stubGlobal("XMLHttpRequest", FakeXhr);
    const controller = new AbortController();
    controller.abort();
    await expect(
      putObjectWithProgress({
        url: "https://bucket.test/put",
        method: "PUT",
        contentType: "image/png",
        body: new Blob(["abc"]),
        signal: controller.signal,
      }),
    ).rejects.toMatchObject({ name: "AbortError" });
  });
});
