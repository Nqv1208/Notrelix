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

export const attachmentsOperations = [
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
        const row: ChatAttachmentRecord = {
          id: state.nextId("attachment"),
          workspaceId: params.workspaceId,
          spaceId: space.id,
          messageId: message.id,
          filename,
          contentType: classify(bytes, part.type),
          sizeBytes: bytes.byteLength,
          bytes,
          uploadedBy: caller.userId,
          createdAt: state.stampWrite(),
          idempotencyKey: key,
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
