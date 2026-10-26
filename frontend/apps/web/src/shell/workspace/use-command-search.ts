import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import type { OperationResponse } from "@notrelix/contracts";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";

type CommandSearchResponse =
  OperationResponse<"Workspaces.Search.CommandSearch">;

/**
 * What a typed word finds beyond the lists the shell already holds
 * (`Workspaces.Search.CommandSearch`): Items the member can read whether or not
 * they are assigned (by name, key or a sub-item), Docs by their title, and
 * Boards by name or by how many Items match. The server decides what the
 * member may see; rows without an id are dropped.
 */
export interface CommandSearchDto {
  readonly work: readonly {
    readonly id: string;
    readonly key: string | null;
    readonly title: string;
    readonly boardId: string;
    readonly boardName: string;
    readonly assigneeName: string | null;
    readonly mine: boolean;
    readonly statusName: string | null;
    readonly statusCategory: string | null;
    readonly dueAt: string | null;
    readonly match:
      | { readonly kind: "title" }
      | { readonly kind: "child"; readonly snippet: string };
  }[];
  readonly docs: readonly {
    readonly id: string;
    readonly title: string;
    readonly spaceId: string | null;
    readonly location: string | null;
    readonly editedAt: string | null;
    readonly editedByName: string | null;
    readonly match:
      | { readonly kind: "title" }
      | { readonly kind: "content"; readonly snippet: string };
  }[];
  readonly boards: readonly {
    readonly id: string;
    readonly name: string;
    readonly spaceId: string | null;
    readonly matchedItems: number;
  }[];
}

/** The producer's search answer as the palette draws it. */
export function toCommandSearch(
  dto: CommandSearchResponse | null | undefined,
): CommandSearchDto {
  return {
    work: (dto?.work ?? []).flatMap((row) =>
      row.id && row.boardId
        ? [
            {
              id: row.id,
              key: row.key ?? null,
              title: row.title ?? "",
              boardId: row.boardId,
              boardName: row.boardName ?? "",
              assigneeName: row.assigneeName ?? null,
              mine: row.mine === true,
              statusName: row.statusName ?? null,
              statusCategory: row.statusCategory ?? null,
              dueAt: row.dueAt ?? null,
              match:
                row.match?.kind === "child" && row.match.snippet
                  ? { kind: "child" as const, snippet: row.match.snippet }
                  : { kind: "title" as const },
            },
          ]
        : [],
    ),
    docs: (dto?.docs ?? []).flatMap((row) =>
      row.id
        ? [
            {
              id: row.id,
              title: row.title ?? "",
              spaceId: row.spaceId ?? null,
              location: row.location ?? null,
              editedAt: row.editedAt ?? null,
              editedByName: row.editedByName ?? null,
              match:
                row.match?.kind === "content" && row.match.snippet
                  ? { kind: "content" as const, snippet: row.match.snippet }
                  : { kind: "title" as const },
            },
          ]
        : [],
    ),
    boards: (dto?.boards ?? []).flatMap((row) =>
      row.id
        ? [
            {
              id: row.id,
              name: row.name ?? "",
              spaceId: row.spaceId ?? null,
              matchedItems: row.matchedItems ?? 0,
            },
          ]
        : [],
    ),
  };
}

const MIN_LENGTH = 2;
const DEBOUNCE_MS = 120;

/** The search for the typed text (debounced); `null` while none applies. */
export function useCommandSearch(
  workspaceId: string,
  typed: string,
  enabled: boolean,
): { readonly term: string; readonly data: CommandSearchDto | null } {
  const { api } = useAppRuntime();
  const [term, setTerm] = useState("");
  useEffect(() => {
    const timer = setTimeout(() => setTerm(typed.trim()), DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [typed]);
  // A command prefix (">", "@", "#") is not a search of Items.
  const searchable =
    enabled && term.length >= MIN_LENGTH && !/^[>@#]/.test(term);
  const query = useQuery({
    queryKey: workspaceQueryKey(workspaceId, "command-search", term),
    queryFn: async () =>
      toCommandSearch(
        await api.api.get<CommandSearchResponse | null>(
          `/workspaces/${workspaceId}/command-search?q=${encodeURIComponent(term)}`,
          { skipGlobalErrorToast: true },
        ),
      ),
    enabled: searchable,
    staleTime: 15_000,
    retry: false,
  });
  return { term, data: searchable ? (query.data ?? null) : null };
}
