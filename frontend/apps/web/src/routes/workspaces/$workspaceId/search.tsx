import {
  useParams,
  useSearch,
  useNavigate,
  useRouteContext,
} from "@tanstack/react-router";
import {
  SearchResultsView,
  type SearchResult,
  type SearchResultType,
} from "@notrelix/features-search";
import type { AppRouterContext } from "../../../router/context";

export function SearchResultsPage() {
  const { workspaceId } = useParams({
    from: "/workspaces/$workspaceId/search",
  });
  const search = useSearch({
    from: "/workspaces/$workspaceId/search",
  });
  const navigate = useNavigate();
  const context = useRouteContext({
    from: "/workspaces/$workspaceId/search",
  }) as AppRouterContext;

  const query = search.q ?? "";
  const activeTypes =
    (search.types?.split(",").filter(Boolean) as SearchResultType[]) ?? [];

  const handleSearchChange = (
    newQuery: string,
    newTypes: readonly SearchResultType[],
  ) => {
    const params: Record<string, string> = {};
    if (newQuery) params.q = newQuery;
    if (newTypes.length > 0) params.types = newTypes.join(",");
    navigate({
      to: "/workspaces/$workspaceId/search",
      params: { workspaceId },
      search: params,
    });
  };

  const handleOpenResult = (result: SearchResult) => {
    if (result.type === "page" || result.type === "block") {
      navigate({
        to: "/workspaces/$workspaceId/docs/$docId",
        params: { workspaceId, docId: result.pageId ?? result.id },
      });
    } else {
      navigate({
        to: "/workspaces/$workspaceId",
        params: { workspaceId },
      });
    }
  };

  return (
    <SearchResultsView
      workspaceId={workspaceId}
      query={query}
      activeTypes={activeTypes}
      searchApi={context.services.searchApi}
      onSearchChange={handleSearchChange}
      onOpenResult={handleOpenResult}
    />
  );
}
