import { useCallback, useState } from "react";
import { Link } from "@tanstack/react-router";
import {
  HomeSidebarSurface,
  type HomeSidebarNavigationRenderer,
} from "@/home/home-sidebar-surface";
import type { HomeSidebarData } from "./types";

export function AppSidebar({
  data,
  onCreateWorkspace,
  mode = "desktop",
}: {
  data: HomeSidebarData;
  onCreateWorkspace?: (name: string) => void;
  mode?: "desktop" | "mobile";
}) {
  const [collapsed, setCollapsed] = useState(false);
  const [previewOpen, setPreviewOpen] = useState(false);
  const isMobile = mode === "mobile";

  const renderNavigation = useCallback<HomeSidebarNavigationRenderer>(
    (target, children, props) => {
      const commonProps = {
        className: props.className,
        title: props.title,
        "aria-label": props.ariaLabel,
        "aria-current": props.ariaCurrent,
        onClick: props.onClick,
      };

      switch (target.kind) {
        case "home":
          return (
            <Link to="/home" {...commonProps}>
              {children}
            </Link>
          );
        case "my-work":
          return (
            <a href="#my-work" {...commonProps}>
              {children}
            </a>
          );
        case "doc":
          return (
            <Link
              to="/workspaces/$workspaceId/docs/$docId"
              params={{
                workspaceId: target.resource.workspaceId,
                docId: target.resource.id,
              }}
              {...commonProps}
            >
              {children}
            </Link>
          );
        case "board":
          return (
            <Link
              to="/workspaces/$workspaceId/boards/$boardId"
              params={{
                workspaceId: target.resource.workspaceId,
                boardId: target.resource.id,
              }}
              {...commonProps}
            >
              {children}
            </Link>
          );
        case "workspace":
          return (
            <Link
              to="/workspaces/$workspaceId"
              params={{ workspaceId: target.resource.workspaceId }}
              {...commonProps}
            >
              {children}
            </Link>
          );
      }
    },
    [],
  );

  return (
    <HomeSidebarSurface
      data={data}
      mode={mode}
      collapsed={collapsed && !isMobile}
      previewOpen={previewOpen}
      onPreviewChange={setPreviewOpen}
      onToggleCollapsed={() => setCollapsed((value) => !value)}
      onCreateWorkspace={onCreateWorkspace}
      renderNavigation={renderNavigation}
    />
  );
}
