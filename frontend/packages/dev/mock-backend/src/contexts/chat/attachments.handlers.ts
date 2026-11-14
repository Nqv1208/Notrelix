/**
 * Files on a message (UC-CHT-G08, CHT-012), as `Chat.Attachments.*` serve them: the author uploads a file to their own message through
 * the shared file policy (size cap, blocked extensions, sniffed type, per-message cap), a reader of the space lists and downloads it,
 * and the author removes it. The bytes are read back only through the message's own content route; a file of another message is never
 * listed, opened or removed through this one. A system or deleted message takes no file; an archived channel is read-only; deleting a
 * message removes its files. The message view carries the list.
 */

import { defineMockOperation } from "../../operations/types";
import { ok, noContent } from "../../transport/create-response";
import type { MockHttpResult } from "../../transport/create-response";
import { classify, isBlocked } from "../documents/page-attachments.handlers";
import {
  archivedProblem,
  attachmentView,
  callerIn,
  fail,
  forbiddenProblem,
  guarded,
  liveSpace,
  notFoundProblem,
  requireAction,
  requireIdempotencyKey,
  requireVisible,
  validationProblem,
} from "./chat-access";
import { requireChannelAction } from "./chat-permissions";
import { requireWithinLimit } from "./chat-limits";
import {
  CHAT_ERROR,
  CHAT_LIMITS,
  type ChatAttachmentRecord,
} from "./chat-model";
import { announceAttachmentChanged } from "./chat-realtime";
import type { MockChatState } from "./chat-state";

type MessageParams = { workspaceId: string; messageId: string };
type FileParams = MessageParams & { attachmentId: string };

/** The message in the Workspace, else the same NotFound as a missing one. */
function messageIn(
  state: MockChatState,
  workspaceId: string,
  messageId: string,
) {
  const message = state.messages.get(messageId);
  if (!message || message.workspaceId !== workspaceId) notFoundProblem();
  return message;
}

/** The longest voice message in the mock, in milliseconds, and its byte ceiling per second (the backend defaults). */
const MOCK_VOICE_MAX_MS = 300_000;
const MOCK_VOICE_MAX_BYTES_PER_SECOND = 16_000;
const MOCK_WAVEFORM_PEAKS = 48;

/**
 * The voice fields of an upload (UC-CHT-I04): `voice`, `durationMs` (a hint, clamped) and `waveform` (comma-separated peaks). With no
 * waveform the mock fakes one from the bytes, and with no hint it fakes a length from the size at the byte rate.
 */
function voiceFieldsOf(
  form: FormData | null | undefined,
  sizeBytes: number,
): {
  isVoice: boolean;
  durationMs: number | null;
  waveform: number[] | null;
  refusal?: { status: number; code: string; title: string; detail: string };
} {
  if (!form || form.get("voice") !== "true") {
    return { isVoice: false, durationMs: null, waveform: null };
  }
  if (
    sizeBytes >
    (MOCK_VOICE_MAX_MS / 1000) * MOCK_VOICE_MAX_BYTES_PER_SECOND
  ) {
    return {
      isVoice: true,
      durationMs: null,
      waveform: null,
      refusal: {
        status: 413,
        code: "voice-too-long",
        title: "Voice message too long",
        detail: "A voice message is at most 300 seconds long.",
      },
    };
  }
  const hint = Number(form.get("durationMs"));
  const durationMs =
    Number.isFinite(hint) && hint >= 0
      ? Math.min(Math.round(hint), MOCK_VOICE_MAX_MS)
      : Math.min(Math.round(sizeBytes / 16), MOCK_VOICE_MAX_MS);
  const sent = String(form.get("waveform") ?? "").trim();
  const waveform = sent
    ? sent
        .split(",")
        .map((peak) => Math.min(255, Math.max(0, Math.round(Number(peak)))))
    : Array.from(
        { length: MOCK_WAVEFORM_PEAKS },
        (_, index) => 40 + ((index * 37) % 180),
      );
  return { isVoice: true, durationMs, waveform };
}

/** The sanitised name the producer stores: no path, no control or reserved characters. */
function sanitise(name: string): string {
  const base = name.split(/[\\/]/).pop() ?? "";
  const cleaned = Array.from(base, (ch) =>
    ch.charCodeAt(0) < 0x20 || '<>:"|?*'.includes(ch) ? "_" : ch,
  )
    .join("")
    .trim();
  return cleaned === "" ? "file" : cleaned;
}

/** The problem the API answers when the storage provider signs no URLs (a ConflictException with a stable error code). */
function directUploadUnsupported(
  errorCode: string,
): MockHttpResult<{ message: string; errorCode: string }> {
  return {
    status: 409,
    body: {
      message: "The storage provider does not issue pre-signed URLs.",
      errorCode,
    },
  };
}

export const attachmentsOperations = [
  // ─── Two-step (pre-signed) transfer ───────────────────────────────────────
  //
  // The mock backend has no object storage that a browser PUT can reach (the byte leg is an XHR to the signed URL, which the fetch
  // interceptor does not serve). It therefore reports the same state as a server whose storage provider signs no URLs: every two-step
  // route answers 409 storage-direct-upload-unsupported (or -download-unsupported), and the client keeps the streaming upload, which the
  // mock serves in full above. A request for a message the caller cannot see is still answered by the streaming path's own checks.

  defineMockOperation<MessageParams>({
    id: "chat.attachments.create-upload",
    contract: { kind: "openapi", operationId: "Chat.Attachments.CreateUpload" },
    method: "POST",
    route:
      "/workspaces/:workspaceId/chat/messages/:messageId/attachments/uploads",
    async handle() {
      return directUploadUnsupported("storage-direct-upload-unsupported");
    },
  }),

  defineMockOperation<MessageParams & { uploadId: string }>({
    id: "chat.attachments.commit-upload",
    contract: { kind: "openapi", operationId: "Chat.Attachments.CommitUpload" },
    method: "POST",
    route:
      "/workspaces/:workspaceId/chat/messages/:messageId/attachments/uploads/:uploadId/commit",
    async handle() {
      return directUploadUnsupported("storage-direct-upload-unsupported");
    },
  }),

  defineMockOperation<FileParams>({
    id: "chat.attachments.get-url",
    contract: { kind: "openapi", operationId: "Chat.Attachments.GetUrl" },
    method: "GET",
    route:
      "/workspaces/:workspaceId/chat/messages/:messageId/attachments/:attachmentId/url",
    async handle() {
      return directUploadUnsupported("storage-direct-download-unsupported");
    },
  }),

  // ─── GET /messages/:messageId/attachments ─────────────────────────────────

  defineMockOperation<MessageParams>({
    id: "chat.attachments.list",
    contract: { kind: "openapi", operationId: "Chat.Attachments.List" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/messages/:messageId/attachments",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const message = messageIn(state, params.workspaceId, params.messageId);
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        requireVisible(state, space, caller);
        return ok(state.attachmentsOfMessage(message.id).map(attachmentView));
      });
    },
  }),

  // ─── POST /messages/:messageId/attachments (multipart) ────────────────────

  defineMockOperation<MessageParams>({
    id: "chat.attachments.upload",
    contract: { kind: "openapi", operationId: "Chat.Attachments.Upload" },
    method: "POST",
    route: "/workspaces/:workspaceId/chat/messages/:messageId/attachments",
    async handle({ params, store, request }): Promise<MockHttpResult<unknown>> {
      return guarded(async (): Promise<MockHttpResult<unknown>> => {
        const state = store.chat;
        const key = requireIdempotencyKey(request.headers);
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "messagePost");
        const message = messageIn(state, params.workspaceId, params.messageId);
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        if (!state.activeMembershipOf(space.id, caller.userId)) {
          notFoundProblem();
        }
        if (message.authorId !== caller.userId) {
          forbiddenProblem(
            "Only the author can attach a file to this message.",
          );
        }
        if (space.archivedAt !== null) archivedProblem();
        requireChannelAction(state, space, caller, "attach");
        if (message.kind === "system") {
          fail(
            409,
            CHAT_ERROR.messageNotAttachable,
            "Message not attachable",
            "A system message cannot carry a file.",
          );
        }
        if (message.deletedAt !== null) {
          fail(
            409,
            CHAT_ERROR.messageDeleted,
            "Message deleted",
            "A deleted message cannot carry a file.",
          );
        }

        // An idempotent replay returns the stored file and stores nothing.
        const replay = Array.from(state.attachments.values()).find(
          (row) =>
            row.messageId === message.id &&
            row.uploadedBy === caller.userId &&
            row.idempotencyKey === key,
        );
        if (replay) {
          return {
            status: 201,
            headers: { "Idempotency-Replayed": "true" },
            body: attachmentView(replay),
          };
        }
        requireWithinLimit(state, "upload", space.id, caller.userId);

        const part = request.formData?.get("file");
        if (!part || typeof part === "string" || !part.name) {
          validationProblem("A file part with a file name is required.");
        }
        const filename = sanitise(part.name);
        // Refused before any byte is stored: a blocked type, then the per-message cap, then the size.
        if (isBlocked(filename)) {
          fail(
            415,
            CHAT_ERROR.attachmentUnsupportedMediaType,
            "Unsupported media type",
            "Files of this type cannot be attached.",
          );
        }
        if (
          state.attachmentsOfMessage(message.id).length >=
          CHAT_LIMITS.attachmentsPerMessageMax
        ) {
          fail(
            409,
            CHAT_ERROR.attachmentLimitExceeded,
            "Attachment limit reached",
            `A message holds at most ${CHAT_LIMITS.attachmentsPerMessageMax} files.`,
          );
        }
        if (part.size === 0) validationProblem("The file is empty.");
        if (part.size > CHAT_LIMITS.attachmentMaxBytes) {
          fail(
            413,
            CHAT_ERROR.attachmentPayloadTooLarge,
            "Payload too large",
            `The upload exceeds the maximum allowed size of ${CHAT_LIMITS.attachmentMaxBytes} bytes.`,
          );
        }
        const bytes = new Uint8Array(await part.arrayBuffer());
        // A voice message (UC-CHT-I05): the fields arrive before the file; the mock reads the hint and fakes the header.
        const voice = voiceFieldsOf(request.formData, bytes.byteLength);
        if (voice.refusal)
          fail(
            voice.refusal.status,
            voice.refusal.code,
            voice.refusal.title,
            voice.refusal.detail,
          );
        const row: ChatAttachmentRecord = {
          id: state.nextId("attachment"),
          workspaceId: params.workspaceId,
          spaceId: space.id,
          messageId: message.id,
          filename,
          contentType: voice.isVoice
            ? part.type || "audio/webm"
            : classify(bytes, part.type),
          sizeBytes: bytes.byteLength,
          bytes,
          uploadedBy: caller.userId,
          createdAt: state.stampWrite(),
          idempotencyKey: key,
          durationMs: voice.isVoice ? voice.durationMs : null,
          waveform: voice.isVoice ? voice.waveform : null,
        };
        state.attachments.set(row.id, row);
        const view = attachmentView(row);
        announceAttachmentChanged(state, space, message.id, "added", {
          id: row.id,
          fileName: view.fileName,
          sizeBytes: view.sizeBytes,
          contentType: view.contentType,
          uploadedBy: view.uploadedBy,
          createdAt: view.createdAt,
          downloadPath: view.downloadPath,
        });
        return {
          status: 201,
          headers: { Location: view.downloadPath },
          body: view,
        };
      });
    },
  }),

  // ─── GET /messages/:messageId/attachments/:attachmentId/content ───────────

  defineMockOperation<FileParams>({
    id: "chat.attachments.content",
    contract: { kind: "openapi", operationId: "Chat.Attachments.GetContent" },
    method: "GET",
    route:
      "/workspaces/:workspaceId/chat/messages/:messageId/attachments/:attachmentId/content",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const message = messageIn(state, params.workspaceId, params.messageId);
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        requireVisible(state, space, caller);
        if (message.deletedAt !== null) notFoundProblem();
        const file = state.attachments.get(params.attachmentId);
        if (!file || file.messageId !== message.id) notFoundProblem();
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
      });
    },
  }),

  // ─── DELETE /messages/:messageId/attachments/:attachmentId ────────────────

  defineMockOperation<FileParams>({
    id: "chat.attachments.delete",
    contract: { kind: "openapi", operationId: "Chat.Attachments.Delete" },
    method: "DELETE",
    route:
      "/workspaces/:workspaceId/chat/messages/:messageId/attachments/:attachmentId",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "messagePost");
        const message = messageIn(state, params.workspaceId, params.messageId);
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        if (!state.activeMembershipOf(space.id, caller.userId)) {
          notFoundProblem();
        }
        if (message.authorId !== caller.userId) {
          forbiddenProblem(
            "Only the author can remove a file from this message.",
          );
        }
        if (space.archivedAt !== null) archivedProblem();
        const file = state.attachments.get(params.attachmentId);
        if (!file || file.messageId !== message.id) notFoundProblem();
        state.attachments.delete(file.id);
        announceAttachmentChanged(state, space, message.id, "removed", {
          id: file.id,
        });
        return noContent();
      });
    },
  }),
];
