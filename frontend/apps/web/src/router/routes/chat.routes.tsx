import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { ChatPage } from "../../routes/workspaces/$workspaceId/chat";

export const chatRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/chat",
  component: ChatPage,
});

/** One channel of the Workspace's Chat (a channel is a space). */
export const chatChannelRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/chat/$channelId",
  component: ChatPage,
});

/** One direct or group conversation (a conversation is a space too; its stream is the channel stream). */
export const chatConversationRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/chat/dm/$conversationId",
  component: ChatPage,
});
