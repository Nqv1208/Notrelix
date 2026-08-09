import { useCallback, useMemo } from "react";
import type { BoardGroup, ViewConfig } from "@notrelix/work-management-core";

export function useBoardGroups(
  groups: BoardGroup[],
  viewConfig: ViewConfig,
  updateViewConfig: (patch: Partial<ViewConfig>) => void,
) {
  const tableGroups = useMemo(
    () =>
      groups.map((group) => ({
        ...group,
        isCollapsed: viewConfig.collapsedGroups[group.id] ?? group.isCollapsed,
      })),
    [groups, viewConfig.collapsedGroups],
  );

  const toggleGroup = useCallback(
    (groupId: string) => {
      const baseValue =
        groups.find((group) => group.id === groupId)?.isCollapsed ?? false;
      updateViewConfig({
        collapsedGroups: {
          ...viewConfig.collapsedGroups,
          [groupId]: !(viewConfig.collapsedGroups[groupId] ?? baseValue),
        },
      });
    },
    [groups, updateViewConfig, viewConfig.collapsedGroups],
  );

  return {
    groups: tableGroups,
    collapsedGroups: viewConfig.collapsedGroups,
    toggleGroup,
  };
}
