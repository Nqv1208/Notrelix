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
