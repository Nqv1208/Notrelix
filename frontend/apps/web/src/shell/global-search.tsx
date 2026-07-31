import { useState, useEffect, useRef } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { useWorkspaceContext } from '@/providers/workspace-provider';
import { Search, FileText, LayoutGrid, X } from 'lucide-react';

type SearchResultType = 'page' | 'block' | 'task' | 'board';

interface SearchResult {
  id: string;
  type: SearchResultType;
  title: string;
  excerpt: string;
  icon: string | null;
  pageId?: string;
  score: number;
  group: 'Pages' | 'Blocks' | 'Tasks' | 'Boards';
}

interface GlobalSearchProps {
  open: boolean;
  onClose: () => void;
}

export function GlobalSearch({ open, onClose }: GlobalSearchProps) {
  const { workspaceId } = useWorkspaceContext();
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchResult[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) {
      inputRef.current?.focus();
      setQuery('');
      setResults([]);
    }
  }, [open]);

  useEffect(() => {
    if (!query.trim()) {
      setResults([]);
      return;
    }

    const controller = new AbortController();
    const search = async () => {
      setIsLoading(true);
      try {
        const params = new URLSearchParams({
          q: query.trim(),
          workspaceId,
        });
        const res = await fetch(`/api/v1/search?${params}`, {
          signal: controller.signal,
        });
        if (res.ok) {
          const data = await res.json();
          setResults(data.results ?? data ?? []);
        }
      } catch {
        if (!controller.signal.aborted) {
          setResults([]);
        }
      } finally {
        setIsLoading(false);
      }
    };

    const timeout = setTimeout(search, 300);
    return () => {
      clearTimeout(timeout);
      controller.abort();
    };
  }, [query, workspaceId]);

  const handleSelect = (result: SearchResult) => {
    onClose();
    if (result.type === 'page' || result.type === 'block') {
      const docId = result.pageId ?? result.id;
      navigate({ to: `/workspaces/$workspaceId/docs/$docId`, params: { workspaceId, docId } });
    } else if (result.type === 'task' || result.type === 'board') {
      navigate({ to: `/workspaces/$workspaceId`, params: { workspaceId } });
    }
  };

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center pt-[15vh]">
      <div className="fixed inset-0 bg-background/80 backdrop-blur-sm" onClick={onClose} />
      <div className="relative w-full max-w-lg bg-card border border-border rounded-xl shadow-lg overflow-hidden">
        <div className="flex items-center gap-3 px-4 border-b">
          <Search className="size-4 text-muted-foreground shrink-0" />
          <input
            ref={inputRef}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search pages, boards, tasks..."
            className="flex-1 h-12 bg-transparent text-sm outline-none placeholder:text-muted-foreground"
            onKeyDown={(e) => {
              if (e.key === 'Escape') onClose();
            }}
          />
          {query && (
            <button
              onClick={() => setQuery('')}
              className="p-1 hover:bg-muted rounded"
            >
              <X className="size-3.5 text-muted-foreground" />
            </button>
          )}
        </div>

        <div className="max-h-80 overflow-y-auto p-2">
          {isLoading && (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary" />
            </div>
          )}

          {!isLoading && query && results.length === 0 && (
            <div className="text-center py-8">
              <p className="text-sm text-muted-foreground">No results found.</p>
            </div>
          )}

          {!isLoading && results.length > 0 && (
            <div className="space-y-1">
              {results.map((result) => (
                <button
                  key={`${result.type}-${result.id}`}
                  onClick={() => handleSelect(result)}
                  className="flex items-start gap-3 w-full p-2.5 rounded-lg hover:bg-muted/50 text-left transition-colors"
                >
                  <div className="mt-0.5 text-muted-foreground shrink-0">
                    {result.type === 'page' || result.type === 'block' ? (
                      <FileText className="size-4" />
                    ) : (
                      <LayoutGrid className="size-4" />
                    )}
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium truncate">{result.title}</p>
                    {result.excerpt && (
                      <p className="text-xs text-muted-foreground truncate mt-0.5">{result.excerpt}</p>
                    )}
                  </div>
                  <span className="text-[10px] text-muted-foreground bg-muted px-1.5 py-0.5 rounded shrink-0">
                    {result.group}
                  </span>
                </button>
              ))}
            </div>
          )}

          {!isLoading && !query && (
            <div className="text-center py-8">
              <p className="text-sm text-muted-foreground">Type to search across the workspace.</p>
            </div>
          )}
        </div>

        <div className="flex items-center justify-between px-3 py-2 border-t text-[10px] text-muted-foreground">
          <span>Press <kbd className="px-1 py-0.5 bg-muted rounded font-mono">Esc</kbd> to close</span>
          <span>{results.length} results</span>
        </div>
      </div>
    </div>
  );
}
