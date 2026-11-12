import { describe, it, expect } from "vitest";
import type { RealtimeEnvelope } from "@notrelix/realtime";
import { MockRealtimeTransport } from "../realtime/mock-realtime-transport";
import { createMockFetch } from "../transport/create-mock-fetch";
import { SPACES, WORKSPACE_ID, actAs, chat, storeAs } from "./chat-test-kit";

const GENERAL = SPACES.general;
const LEADERSHIP = SPACES.leadership;
const PNG = new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 1]);

type Store = ReturnType<typeof storeAs>;

const BASE = `http://localhost:8000/api/v1/workspaces/${WORKSPACE_ID}/chat`;

/** A user message of general by the current persona that is not deleted. */
function ownMessage(store: Store) {
  const me = store.getCurrentUser().id;
  const found = store.chat
    .messagesOf(GENERAL)
    .find(
      (m) => m.kind === "user" && m.deletedAt === null && m.authorId === me,
    );
  expect(found).toBeDefined();
  return found!;
}

/** A user message of general by someone else. */
function othersMessage(store: Store) {
  const me = store.getCurrentUser().id;
  const found = store.chat
    .messagesOf(GENERAL)
    .find(
      (m) => m.kind === "user" && m.deletedAt === null && m.authorId !== me,
    );
  expect(found).toBeDefined();
  return found!;
}

async function upload(
  store: Store,
  messageId: string,
  name: string,
  bytes: Uint8Array = PNG,
  idempotencyKey: string | null = `upload-${name}-${messageId}`,
) {
  const form = new FormData();
  form.append("file", new File([bytes as BlobPart], name));
  const headers: Record<string, string> = {};
  if (idempotencyKey !== null) headers["Idempotency-Key"] = idempotencyKey;
  const response = await createMockFetch(store)(
    `${BASE}/messages/${messageId}/attachments`,
    { method: "POST", headers, body: form },
  );
  const text = await response.text();
  return {
    status: response.status,
    headers: response.headers,
    body: text ? JSON.parse(text) : null,
  };
}

async function download(store: Store, messageId: string, attachmentId: string) {
  const response = await createMockFetch(store)(
    `${BASE}/messages/${messageId}/attachments/${attachmentId}/content`,
    { method: "GET" },
  );
  const buffer = new Uint8Array(await response.arrayBuffer());
  return { status: response.status, headers: response.headers, buffer };
}

describe("MFB-CHAT-S2.6: Chat mock handlers (attachments)", () => {
  describe("upload (UC-CHT-G08)", () => {
    it("stores a file on the author's own message and answers 201 with its metadata and a download path", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);

      const res = await upload(store, message.id, "photo.png");

      expect(res.status).toBe(201);
      expect(res.body).toMatchObject({
        messageId: message.id,
        fileName: "photo.png",
        sizeBytes: PNG.byteLength,
        contentType: "image/png",
        downloadPath: `/api/v1/workspaces/${WORKSPACE_ID}/chat/messages/${message.id}/attachments/${res.body.id}/content`,
      });
      expect(store.chat.attachmentsOfMessage(message.id)).toHaveLength(1);
    });

    it("requires an Idempotency-Key (400) and returns the stored file for a retry with the same key", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);

      const missing = await upload(store, message.id, "a.png", PNG, null);
      expect(missing.status).toBe(400);

      const first = await upload(
        store,
        message.id,
        "a.png",
        PNG,
        "retry-key-1",
      );
      const again = await upload(
        store,
        message.id,
        "a.png",
        PNG,
        "retry-key-1",
      );
      expect(again.status).toBe(201);
      expect(again.headers.get("Idempotency-Replayed")).toBe("true");
      expect(again.body.id).toBe(first.body.id);
      expect(store.chat.attachmentsOfMessage(message.id)).toHaveLength(1);
    });

    it("refuses a blocked extension with 415 before anything is stored", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);

      const res = await upload(store, message.id, "setup.exe");

      expect(res.status).toBe(415);
      expect(res.body.errorCode).toBe("attachment.unsupported_media_type");
      expect(store.chat.attachmentsOfMessage(message.id)).toHaveLength(0);
    });

    it("refuses a file over the size cap with 413", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);
      // A part reports its own size; the handler reads it, so a part that reports more than the cap stands for a large upload.
      const large = new File([PNG as BlobPart], "big.bin");
      Object.defineProperty(large, "size", { value: 100 * 1024 * 1024 + 1 });
      const form = new FormData();
      form.append("file", large);
      const response = await createMockFetch(store)(
        `${BASE}/messages/${message.id}/attachments`,
        {
          method: "POST",
          headers: { "Idempotency-Key": "too-large-key" },
          body: form,
        },
      );

      expect(response.status).toBe(413);
      expect((await response.json()).errorCode).toBe(
        "attachment.payload_too_large",
      );
    });

    it("refuses an empty file with 400", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);

      const res = await upload(
        store,
        message.id,
        "empty.txt",
        new Uint8Array(),
      );

      expect(res.status).toBe(400);
    });

    it("holds at most ten files on a message (409 attachment.limit_exceeded)", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);
      for (let i = 0; i < 10; i += 1) {
        expect((await upload(store, message.id, `f${i}.png`)).status).toBe(201);
      }

      const res = await upload(store, message.id, "eleven.png");

      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("attachment.limit_exceeded");
    });

    it("refuses another person's message with 403", async () => {
      const store = storeAs("owner");
      const message = othersMessage(store);

      const res = await upload(store, message.id, "a.png");

      expect(res.status).toBe(403);
    });

    it("answers a message in a private channel the caller is not in with 404", async () => {
      const store = storeAs("owner");
      const leadership = store.chat
        .messagesOf(LEADERSHIP)
        .find((m) => m.kind === "user" && m.deletedAt === null);

      const res = await upload(store, leadership!.id, "a.png");

      expect(res.status).toBe(404);
    });

    it("refuses a new file in an archived channel with 409 channel-archived", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);
      store.chat.spaces.get(GENERAL)!.archivedAt = "2026-01-01T00:00:00.000Z";

      const res = await upload(store, message.id, "a.png");

      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("channel-archived");
    });

    it("refuses a system message and a deleted message with 409", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);
      // A system message of the caller's own: the author check passes, so the kind is what refuses it.
      const system = {
        ...message,
        id: store.chat.nextId("message"),
        kind: "system" as const,
        sequence: message.sequence + 1000,
      };
      store.chat.appendMessage(system);
      const refused = await upload(store, system.id, "a.png");
      expect(refused.status).toBe(409);
      expect(refused.body.errorCode).toBe("message-not-attachable");

      message.deletedAt = "2026-01-01T00:00:00.000Z";
      message.content = null;
      const res = await upload(store, message.id, "a.png");
      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("message-deleted");
    });
  });

  describe("read and download (UC-CHT-G08)", () => {
    it("lists the files of a message and carries them in the message view without another request", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);
      await upload(store, message.id, "one.png");
      await upload(store, message.id, "two.png");

      const list = await chat(
        store,
        "GET",
        `/messages/${message.id}/attachments`,
      );
      expect(list.status).toBe(200);
      expect(list.body.map((a: { fileName: string }) => a.fileName)).toEqual([
        "one.png",
        "two.png",
      ]);

      const history = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: `around=${message.sequence}&limit=10`,
      });
      const view = history.body.items.find(
        (m: { id: string }) => m.id === message.id,
      );
      expect(view.attachments).toHaveLength(2);
      const other = history.body.items.find(
        (m: { id: string; attachments: unknown[] }) =>
          m.id !== message.id && m.attachments.length === 0,
      );
      expect(other).toBeDefined();
    });

    it("lets another member of the channel download the bytes, inline only for an image, never cacheable", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);
      const png = await upload(store, message.id, "photo.png");
      const pdf = await upload(
        store,
        message.id,
        "notes.pdf",
        new Uint8Array([1, 2, 3]),
      );
      actAs(store, "member");

      const image = await download(store, message.id, png.body.id);
      expect(image.status).toBe(200);
      expect(image.headers.get("Content-Type")).toBe("image/png");
      expect(image.headers.get("Content-Disposition")).toBe("inline");
      expect(Array.from(image.buffer)).toEqual(Array.from(PNG));
      expect(image.headers.get("Cache-Control")).toBe("private, no-store");

      const file = await download(store, message.id, pdf.body.id);
      expect(file.headers.get("Content-Disposition")).toContain("attachment");
    });

    it("does not serve a file through another message, and answers a missing one with 404", async () => {
      const store = storeAs("owner");
      const mine = ownMessage(store);
      const other = othersMessage(store);
      const uploaded = await upload(store, mine.id, "photo.png");

      expect((await download(store, other.id, uploaded.body.id)).status).toBe(
        404,
      );
      expect(
        (await download(store, mine.id, "00000000-0000-4000-8000-000000000000"))
          .status,
      ).toBe(404);
    });

    it("answers the files of a private channel the caller is not in with 404", async () => {
      const store = storeAs("owner");
      const leadership = store.chat
        .messagesOf(LEADERSHIP)
        .find((m) => m.kind === "user" && m.deletedAt === null);

      const res = await chat(
        store,
        "GET",
        `/messages/${leadership!.id}/attachments`,
      );

      expect(res.status).toBe(404);
    });
  });

  describe("remove and delete (UC-CHT-G08)", () => {
    it("lets the author remove a file (204) and not another person (403)", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);
      const uploaded = await upload(store, message.id, "photo.png");

      actAs(store, "member");
      const denied = await chat(
        store,
        "DELETE",
        `/messages/${message.id}/attachments/${uploaded.body.id}`,
      );
      expect(denied.status).toBe(403);

      actAs(store, "owner");
      const removed = await chat(
        store,
        "DELETE",
        `/messages/${message.id}/attachments/${uploaded.body.id}`,
      );
      expect(removed.status).toBe(204);
      expect(store.chat.attachmentsOfMessage(message.id)).toHaveLength(0);

      const again = await chat(
        store,
        "DELETE",
        `/messages/${message.id}/attachments/${uploaded.body.id}`,
      );
      expect(again.status).toBe(404);
    });

    it("removes the files of a message with the message, and the tombstone carries none", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);
      await upload(store, message.id, "photo.png");

      const res = await chat(store, "DELETE", `/messages/${message.id}`, {
        query: `expectedVersion=${message.version}`,
      });
      expect(res.status).toBe(204);
      expect(store.chat.attachmentsOfMessage(message.id)).toHaveLength(0);

      const list = await chat(
        store,
        "GET",
        `/messages/${message.id}/attachments`,
      );
      expect(list.body).toEqual([]);
    });
  });

  describe("realtime (UC-CHT-G08)", () => {
    async function captureChannelFrames(
      run: () => Promise<void>,
    ): Promise<RealtimeEnvelope[]> {
      const transport = new MockRealtimeTransport();
      const frames: RealtimeEnvelope[] = [];
      const stop = transport.subscribeScope(
        { kind: "channel", id: GENERAL },
        (envelope) => {
          frames.push(envelope);
        },
      );
      try {
        await run();
      } finally {
        stop();
        transport.dispose();
      }
      return frames.filter(
        (f) => f.eventType === "chat.message.attachment.changed",
      );
    }

    it("sends an added frame with the file metadata (no bytes) and a removed frame with only the id", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);
      let fileId = "";

      const frames = await captureChannelFrames(async () => {
        const up = await upload(store, message.id, "photo.png");
        fileId = up.body.id;
        await chat(
          store,
          "DELETE",
          `/messages/${message.id}/attachments/${fileId}`,
        );
      });

      expect(frames).toHaveLength(2);
      expect(frames[0]!.payloadContract).toBe("chat.attachment.v1");
      expect(frames[0]!.payload).toMatchObject({
        messageId: message.id,
        change: "added",
        attachmentId: fileId,
        fileName: "photo.png",
        detailsOmitted: false,
        downloadPath: `/api/v1/workspaces/${WORKSPACE_ID}/chat/messages/${message.id}/attachments/${fileId}/content`,
      });
      expect(JSON.stringify(frames[0]!.payload)).not.toContain("bytes");
      expect(frames[1]!.payload).toMatchObject({
        change: "removed",
        attachmentId: fileId,
        fileName: null,
      });
    });

    it("sends nothing for a refused upload or an idempotent replay", async () => {
      const store = storeAs("owner");
      const message = ownMessage(store);

      const frames = await captureChannelFrames(async () => {
        await upload(store, message.id, "setup.exe");
        await upload(store, message.id, "a.png", PNG, "same-key");
        await upload(store, message.id, "a.png", PNG, "same-key");
      });

      expect(frames).toHaveLength(1);
    });
  });
});
