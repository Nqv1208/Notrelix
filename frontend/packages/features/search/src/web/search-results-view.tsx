import React, { useState, useEffect, useCallback } from "react";
import { Button, Input } from "@notrelix/ui-web";
import { Search, FileText, LayoutGrid, Filter } from "lucide-react";
import type { SearchResult, SearchResultType } from "../core/search-model";
import { RESULT_TYPES } from "../core/search-model";
import type { SearchApi } from "../core/search-api";

export interface SearchResultsViewProps {
  workspaceId: string;
  query: string;
  activeTypes: readonly SearchResultType[];
  searchApi: SearchApi;
  onSearchChange: (query: string, types: readonly SearchResultType[]) => void;
  onOpenResult: (result: SearchResult) => void;
}

function ResultIcon({ type }: { type: SearchResultType }) {
  if (type === "page" || type === "block") {
    return <FileText className="size-4" />;
  }
  return <LayoutGrid className="size-4" />;
}

export function SearchResultsView({
  workspaceId,
  query,
  activeTypes,
  searchApi,
  onSearchChange,
  onOpenResult,
}: SearchResultsViewProps): React.ReactNode {
  const [results, setResults] = useState<readonly SearchResult[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [inputValue, setInputValue] = useState(query);

  const fetchResults = useCallback(
    async (q: string, types: readonly SearchResultType[]) => {
      if (!q.trim()) {
        setResults([]);
        return;
      }
      setIsLoading(true);
      try {
        const data = await searchApi.search({
          workspaceId,
          query: q.trim(),
          types,
        });
        setResults(data);
      } catch {
        setResults([]);
      } finally {
        setIsLoading(false);
      }
    },
    [workspaceId, searchApi],
  );

  useEffect(() => {
    const timeout = setTimeout(() => fetchResults(query, activeTypes), 300);
    return () => clearTimeout(timeout);
  }, [query, activeTypes, fetchResults]);

  useEffect(() => {
    setInputValue(query);
  }, [query]);

  const handleSearch = () => {
    onSearchChange(inputValue.trim(), activeTypes);
  };

  const toggleType = (type: SearchResultType) => {
    const newTypes = activeTypes.includes(type)
      ? activeTypes.filter((t) => t !== type)
      : [...activeTypes, type];
    onSearchChange(query, newTypes);
  };

  const grouped = results.reduce(
    (acc, r) => {
      (acc[r.group] ??= []).push(r);
      return acc;
    },
    {} as Record<string, SearchResult[]>,
  );

  return (
    <div className="p-8 max-w-4xl">
      <h1 className="text-2xl font-bold tracking-tight mb-6">Search</h1>

      <div className="flex items-center gap-2 mb-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
          <Input
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleSearch()}
            placeholder="Search pages, boards, tasks..."
            className="pl-9"
          />
        </div>
        <Button onClick={handleSearch} size="sm">
          Search
        </Button>
      </div>

      <div className="flex items-center gap-1.5 mb-6">
        <Filter className="size-3.5 text-muted-foreground" />
        {RESULT_TYPES.map((t) => (
          <button
            key={t.value}
            onClick={() => toggleType(t.value)}
            className={`px-2 py-1 rounded-md text-xs font-medium transition-colors ${
              activeTypes.includes(t.value)
                ? "bg-primary text-primary-foreground"
                : "bg-muted text-muted-foreground hover:text-foreground"
            }`}
          >
            {t.label}
          </button>
        ))}
        {activeTypes.length > 0 && (
          <button
            onClick={() => onSearchChange(query, [])}
            className="ml-1 text-xs text-muted-foreground hover:text-foreground"
          >
            Clear
          </button>
        )}
      </div>

      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary" />
        </div>
      )}

      {!isLoading && query && results.length === 0 && (
        <div className="text-center py-12">
          <p className="text-sm text-muted-foreground">
            No results found for "{query}"
          </p>
        </div>
      )}

      {!isLoading && !query && (
        <div className="text-center py-12">
          <p className="text-sm text-muted-foreground">
            Enter a search query to find content across the workspace.
          </p>
        </div>
      )}

      {!isLoading && results.length > 0 && (
        <div className="space-y-6">
          {(Object.entries(grouped) as [string, SearchResult[]][]).map(
            ([group, items]) => (
              <div key={group}>
                <h2 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">
                  {group}
                </h2>
                <div className="space-y-1">
                  {items.map((result: SearchResult) => (
                    <button
                      key={`${result.type}-${result.id}`}
                      onClick={() => onOpenResult(result)}
                      className="w-full text-left flex items-start gap-3 p-3 rounded-lg hover:bg-muted/50 transition-colors"
                    >
                      <div className="mt-0.5 text-muted-foreground shrink-0">
                        <ResultIcon type={result.type} />
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium truncate">
                          {result.title}
                        </p>
                        {result.excerpt && (
                          <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">
                            {result.excerpt}
                          </p>
                        )}
                      </div>
                      <span className="text-[10px] text-muted-foreground bg-muted px-1.5 py-0.5 rounded shrink-0 mt-0.5">
                        {result.type}
                      </span>
                    </button>
                  ))}
                </div>
              </div>
            ),
          )}
        </div>
      )}

      {!isLoading && query && results.length > 0 && (
        <p className="text-xs text-muted-foreground mt-6">
          {results.length} result{results.length !== 1 ? "s" : ""}
        </p>
      )}
    </div>
  );
}
