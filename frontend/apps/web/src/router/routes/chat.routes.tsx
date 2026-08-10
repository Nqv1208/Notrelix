import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { ChatPage } from "../../routes/workspaces/$workspaceId/chat";

export const chatRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/chat",
  component: ChatPage,
});
