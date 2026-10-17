import { useCallback } from "react";
import { useNavigate, useParams, useSearch } from "@tanstack/react-router";
import {
  BoardSettingsPage,
  type BoardSettingsSection,
} from "@notrelix/work-management-web";
import { useFullBoard } from "@notrelix/work-management-state";
import type { BoardSettingsSearch } from "../../../../router/schemas/board-settings-search-schema";
import { useBoardHeaderData } from "../../../../shell/workspace/use-board-header-data";

const ROUTE = "/workspaces/$workspaceId/boards/$boardId/settings";

/**
 * "Thiết lập board": the page the board header's "Thiết lập" opens, on every
 * view. The section and the open field or view live in the address, so a
 * panel can be linked to and Back closes it.
 */
export function BoardSettingsRoutePage() {
  const { workspaceId, boardId } = useParams({ from: ROUTE });
  const search: BoardSettingsSearch = useSearch({ from: ROUTE });
  const navigate = useNavigate({ from: ROUTE });
  const { board } = useFullBoard(boardId, workspaceId);
  const headerData = useBoardHeaderData(workspaceId, boardId, board);

  const boardPath = `/workspaces/${workspaceId}/boards/${encodeURIComponent(boardId)}?view=${search.from ?? "kanban"}`;

  const goToBoard = useCallback(
    (options: { share?: boolean } = {}) =>
      void navigate({
        to: "/workspaces/$workspaceId/boards/$boardId",
        params: { workspaceId, boardId },
        search: {
          view: search.from ?? "kanban",
          share: options.share ? true : undefined,
        },
      }),
    [boardId, navigate, search.from, workspaceId],
  );

  const select = useCallback(
    (patch: Partial<BoardSettingsSearch>, replace = false) =>
      void navigate({
        search: (prev: BoardSettingsSearch) => ({ ...prev, ...patch }),
        replace,
      }),
    [navigate],
  );

  return (
    <BoardSettingsPage
      workspaceId={workspaceId}
      boardId={boardId}
      integrationsHref={`/workspaces/${encodeURIComponent(workspaceId)}/integrations`}
      section={search.section}
      onSelectSection={(section: BoardSettingsSection) =>
        select({
          section,
          field: undefined,
          view: undefined,
          rule: undefined,
          log: undefined,
          run: undefined,
        })
      }
      fieldId={search.field ?? null}
      onSelectField={(field) =>
        select({ field: field ?? undefined }, field === null)
      }
      viewId={search.view ?? null}
      onSelectView={(view) =>
        select({ view: view ?? undefined }, view === null)
      }
      ruleId={search.rule ?? null}
      onSelectRule={(rule) =>
        select({ rule: rule ?? undefined, log: undefined, run: undefined })
      }
      log={search.log === true}
      onSelectLog={(open) =>
        select({
          log: open ? true : undefined,
          rule: undefined,
          run: undefined,
        })
      }
      runId={search.run ?? null}
      onSelectRun={(run) => select({ run: run ?? undefined }, run === null)}
      billingHref={`/workspaces/${workspaceId}/billing`}
      space={
        headerData.space
          ? {
              name: headerData.space.label ?? headerData.space.name,
              mark: {
                initials: headerData.space.initials,
                tone: headerData.space.markTone,
              },
            }
          : undefined
      }
      boardHref={boardPath}
      onBack={() => goToBoard()}
      onOpenAccess={() => goToBoard({ share: true })}
      onLeave={() =>
        void navigate({
          to: "/workspaces/$workspaceId",
          params: { workspaceId },
        })
      }
    />
  );
}
