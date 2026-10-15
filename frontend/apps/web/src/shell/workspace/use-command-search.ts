import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";

/**
 * What a typed word finds beyond the lists the shell already holds (gap
 * `CTR-GAP-WS-COMMAND-SEARCH`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Command"): Items the member can read whether or not
 * they are assigned (by name or by a sub-item), Docs by their text, and Boards
 * by how many Items match. The server decides what the member may see.
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
    queryFn: () =>
      api.api.get<CommandSearchDto>(
        `/workspaces/${workspaceId}/command-search?q=${encodeURIComponent(term)}`,
        { skipGlobalErrorToast: true },
      ),
    enabled: searchable,
    staleTime: 15_000,
    retry: false,
  });
  return { term, data: searchable ? (query.data ?? null) : null };
}
