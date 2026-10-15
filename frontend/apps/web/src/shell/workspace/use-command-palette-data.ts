import { useMemo } from "react";
import {
  resolveNavigationEntries,
  spaceAncestors,
  markToneFor,
  type MyWorkItem,
  type SpaceIndex,
} from "@notrelix/features-workspace/core";
import {
  createUseWorkspaceRecents,
  type PaletteBoardEntry,
  type PaletteDepartment,
  type PaletteDocEntry,
  type PaletteRecentEntry,
  type PaletteWorkEntry,
  type PaletteWorkspace,
} from "@notrelix/features-workspace/web";
import { useWorkspaceCommandCopy } from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  toPaletteBoards,
  toPaletteDocs,
  toPaletteRecents,
  toPaletteWork,
  toSearchBoards,
  toSearchDocs,
  toSearchWork,
} from "./command-palette-view";
import type { CommandSearchDto } from "./use-command-search";
import { useBoardDepartments } from "./use-board-departments";
import { useNow } from "./use-now";
import type { WorkspaceResource } from "./use-workspace-resources";

export interface CommandPaletteData {
  readonly workspace: PaletteWorkspace;
  readonly work: readonly PaletteWorkEntry[];
  readonly docs: readonly PaletteDocEntry[];
  readonly boards: readonly PaletteBoardEntry[];
  readonly recents: readonly PaletteRecentEntry[];
}

/**
 * The lists the command palette searches, from the reads the shell already
 * holds (Boards, Docs, My work, the Space tree) plus the member's recents.
 * The My work read is the only Item read, so Items are the ones assigned to
 * the member.
 */
export function useCommandPaletteData(input: {
  readonly workspaceId: string;
  readonly workspaceName: string;
  readonly otherWorkspaceNames: readonly string[];
  readonly boards: readonly WorkspaceResource[];
  readonly docs: readonly WorkspaceResource[];
  readonly myWork: readonly MyWorkItem[];
  readonly spaceIndex: SpaceIndex;
  /** What the typed text found beyond the lists above, with the text. */
  readonly search?: {
    readonly term: string;
    readonly data: CommandSearchDto | null;
  };
}): CommandPaletteData {
  const { api: runtimeClient } = useAppRuntime();
  const copy = useWorkspaceCommandCopy();
  const formatters = useFormatters();
  const now = useNow();
  const useRecents = useMemo(
    () =>
      createUseWorkspaceRecents({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const recents = useRecents(input.workspaceId);
  const departments = useBoardDepartments(input.workspaceId);

  const { spaceIndex, boards, docs, myWork } = input;
  const departmentOfSpace = useMemo(
    () =>
      (spaceId: string | null): PaletteDepartment | null => {
        if (!spaceId) return null;
        const space = spaceIndex.byId.get(spaceId);
        if (!space) return null;
        const root = spaceAncestors(spaceIndex, space.id)[0] ?? space;
        return { name: root.name, markTone: markToneFor(root.id) };
      },
    [spaceIndex],
  );

  return useMemo(() => {
    const relativeTime = (value: string, at: number) =>
      formatters.relativeTime(value, at);
    const boardNames = new Map(boards.map((board) => [board.id, board.name]));
    const recentEntries = resolveNavigationEntries(recents.data ?? [], {
      boards,
      docs,
    });
    const found = input.search?.data ?? null;
    const term = input.search?.term ?? "";
    // What the search found replaces the same entries of the shell's lists
    // (it knows where the text matched), and adds the ones the lists lack.
    const merge = <T extends { readonly id: string }>(
      searched: readonly T[],
      local: readonly T[],
    ): T[] => {
      const ids = new Set(searched.map((entry) => entry.id));
      return [...searched, ...local.filter((entry) => !ids.has(entry.id))];
    };
    const localWork = toPaletteWork(myWork, boardNames, departments, now, copy);
    const localDocs = toPaletteDocs(docs, departmentOfSpace, relativeTime, now);
    const localBoards = toPaletteBoards(boards, departments);
    return {
      workspace: {
        name: input.workspaceName,
        markTone: markToneFor(input.workspaceId),
        others: input.otherWorkspaceNames,
      },
      work: found
        ? merge(toSearchWork(found.work, departments, now, copy), localWork)
        : localWork,
      docs: found
        ? merge(
            toSearchDocs(
              found.docs,
              departmentOfSpace,
              relativeTime,
              now,
              copy,
            ),
            localDocs,
          )
        : localDocs,
      boards: found
        ? merge(
            toSearchBoards(found.boards, departments, term, copy),
            localBoards,
          )
        : localBoards,
      recents: toPaletteRecents(recentEntries, departments, relativeTime, now),
    };
  }, [
    boards,
    copy,
    departmentOfSpace,
    departments,
    docs,
    formatters,
    input.otherWorkspaceNames,
    input.workspaceId,
    input.search,
    input.workspaceName,
    myWork,
    now,
    recents.data,
  ]);
}
