/**
 * The preview of a link to a Board item or a Page (UC-CHT-G05, CHT-023), as `Chat.Links.Resolve` serves it. The title is returned only
 * when the signed-in person can view the resource now: an Item on a Board they can see, a Page that is not in the trash and whose Space
 * they can see. Every other case (hidden, trashed, gone) is the same generic answer, `accessible: false` with no title. A product URL of
 * this Workspace or a ResourceRef names the link; any other URL is a 400. Resolving reads only: it never changes the resource.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  LINK_ID,
  LINK_URL_MAX,
  isLinkableKind,
  isSupportedKind,
  targetOfUrl,
  viewableTitle,
  type LinkTarget,
} from "./chat-links";
import { channelViewOf } from "./chat-permissions";
import { announceLinkChanged } from "./chat-realtime";
import {
  archivedProblem,
  callerIn,
  forbiddenProblem,
  guarded,
  liveChannel,
  optionalInteger,
  parseVersion,
  requireAction,
  requireManager,
  requireVersion,
  requireVisible,
  validationProblem,
  visibleSpaceIds,
} from "./chat-access";

type WorkspaceParams = { workspaceId: string };
type ChannelParams = { workspaceId: string; channelId: string };
type LinkBody = {
  kind?: unknown;
  resourceId?: unknown;
  expectedVersion?: unknown;
};

export {
  LINK_KIND,
  LINK_URL_MAX,
  targetOfUrl,
  viewableTitle,
} from "./chat-links";

export const linksOperations = [
  defineMockOperation<WorkspaceParams>({
    id: "chat.links.resolve",
    contract: { kind: "openapi", operationId: "Chat.Links.Resolve" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/links/resolve",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const url = query["url"];
        const kind = query["kind"];
        const resourceId = query["resourceId"];
        const named = kind !== undefined || resourceId !== undefined;
        if ((url !== undefined) === named) {
          validationProblem("Send either a url or a kind with a resourceId.");
        }
        let target: LinkTarget | null;
        if (url !== undefined) {
          if (url.length > LINK_URL_MAX) {
            validationProblem(
              `The url must be at most ${LINK_URL_MAX} characters.`,
            );
          }
          target = targetOfUrl(url, params.workspaceId);
        } else {
          if (!isSupportedKind(kind)) {
            validationProblem(
              "The kind must be work-management.board-item, work-management.board or documents.page.",
            );
          }
          if (!resourceId || !LINK_ID.test(resourceId)) {
            validationProblem("The resourceId must be an id.");
          }
          target = { kind: kind!, id: resourceId! };
        }
        if (!target) {
          validationProblem(
            "This link is not a Board item or Page of this Workspace.",
          );
        }
        const title = viewableTitle(store, params.workspaceId, target);
        return ok({
          kind: target.kind,
          resourceId: target.id,
          accessible: title !== null,
          title,
        });
      });
    },
  }),

  // ─── PUT /channels/:channelId/link ────────────────────────────────────────

  defineMockOperation<ChannelParams, LinkBody>({
    id: "chat.channels.link",
    contract: { kind: "openapi", operationId: "Chat.Channels.Link" },
    method: "PUT",
    route: "/workspaces/:workspaceId/chat/channels/:channelId/link",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "channelManage");
        const kind = typeof body?.kind === "string" ? body.kind : "";
        if (!isLinkableKind(kind)) {
          validationProblem(
            "kind must be work-management.board or documents.page; a Team cannot be linked yet.",
          );
        }
        const resourceId =
          typeof body?.resourceId === "string" ? body.resourceId : "";
        if (!LINK_ID.test(resourceId)) {
          validationProblem("The resourceId must be an id.");
        }
        const expected = parseVersion(body?.expectedVersion, "expectedVersion");
        const space = liveChannel(state, params.workspaceId, params.channelId);
        requireVisible(state, space, caller);
        requireManager(state, space, caller);
        requireVersion(expected, space.version);
        // The manager must be able to view the resource now (the same answer for hidden, trashed and missing).
        if (
          viewableTitle(store, params.workspaceId, { kind, id: resourceId }) ===
          null
        ) {
          forbiddenProblem(
            "You cannot view this resource, so you cannot link a channel to it.",
          );
        }
        if (space.archivedAt !== null) archivedProblem();
        const same =
          space.link?.kind === kind && space.link.resourceId === resourceId;
        if (!same) {
          space.link = { kind, resourceId };
          space.version += 1;
          announceLinkChanged(state, space);
        }
        return ok(channelViewOf(state, space, caller));
      });
    },
  }),

  // ─── DELETE /channels/:channelId/link ─────────────────────────────────────

  defineMockOperation<ChannelParams>({
    id: "chat.channels.unlink",
    contract: { kind: "openapi", operationId: "Chat.Channels.Unlink" },
    method: "DELETE",
    route: "/workspaces/:workspaceId/chat/channels/:channelId/link",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "channelManage");
        const expected = parseVersion(
          optionalInteger(query.expectedVersion, "expectedVersion"),
          "expectedVersion",
        );
        const space = liveChannel(state, params.workspaceId, params.channelId);
        requireVisible(state, space, caller);
        requireManager(state, space, caller);
        requireVersion(expected, space.version);
        if (space.link) {
          if (space.archivedAt !== null) archivedProblem();
          space.link = null;
          space.version += 1;
          announceLinkChanged(state, space);
        }
        return ok(channelViewOf(state, space, caller));
      });
    },
  }),

  // ─── GET /links/linked-channels ───────────────────────────────────────────

  defineMockOperation<WorkspaceParams>({
    id: "chat.channels.listLinked",
    contract: { kind: "openapi", operationId: "Chat.Channels.ListLinked" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/links/linked-channels",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const kind = query["kind"] ?? "";
        const id = query["id"] ?? "";
        if (!isLinkableKind(kind)) {
          validationProblem(
            "kind must be work-management.board or documents.page.",
          );
        }
        if (!LINK_ID.test(id)) validationProblem("The id must be an id.");
        // A caller who cannot view the resource learns nothing about its channels.
        if (viewableTitle(store, params.workspaceId, { kind, id }) === null) {
          return ok([]);
        }
        const visible = visibleSpaceIds(state, caller);
        const rows = [...state.spaces.values()]
          .filter(
            (space) =>
              space.workspaceId === params.workspaceId &&
              space.deletedAt === null &&
              visible.has(space.id) &&
              (space.kind === "channel_public" ||
                space.kind === "channel_private") &&
              space.link?.kind === kind &&
              space.link.resourceId === id,
          )
          .sort((a, b) => a.name.localeCompare(b.name))
          .map((space) => ({
            id: space.id,
            name: space.name,
            kind: space.kind,
            isMember:
              state.activeMembershipOf(space.id, caller.userId) !== undefined,
            isArchived: space.archivedAt !== null,
          }));
        return ok(rows);
      });
    },
  }),
];
