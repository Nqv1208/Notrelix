import { useState } from "react";
import {
  FileText,
  Link2Off,
  Lock,
  Plus,
  Search,
  TriangleAlert,
} from "lucide-react";
import { toast } from "sonner";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Button,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import {
  useItemLinkedPages,
  useLinkablePages,
  type LinkedPageDto,
} from "@notrelix/work-management-state";
import { fillLifecycleCopy } from "../item-lifecycle/item-lifecycle.copy";
import type { ItemActionsCopy } from "./item-actions.copy";
import { useItemActionsCopy } from "./use-item-actions-copy";

/**
 * The Documents pages linked to an item: open one, unlink one (after asking),
 * or link another from a picker that lists only pages the person can see. A
 * linked page the person cannot see shows as a locked, nameless row. Seeing
 * the item is enough to read this; changing it needs permission to edit.
 */
export function LinkedDocsSection({
  itemId,
  workspaceId,
  canEdit,
  pageHref = (pageId) => `/workspaces/${workspaceId}/docs/${pageId}`,
}: {
  itemId: string;
  workspaceId: string;
  canEdit: boolean;
  /** Where a page opens; defaults to the docs route of the workspace. */
  pageHref?: (pageId: string) => string;
}) {
  const copy = useItemActionsCopy();
  const linked = useItemLinkedPages(itemId, workspaceId);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [unlinking, setUnlinking] = useState<LinkedPageDto | null>(null);
  const [unlinkFailed, setUnlinkFailed] = useState(false);
  const rows = linked.pages;
  const total = rows.length + linked.hiddenCount;

  const confirmUnlink = () => {
    if (!unlinking) return;
    const page = unlinking;
    linked.unlink.mutate(page.pageId, {
      onSuccess: () => {
        setUnlinking(null);
        setUnlinkFailed(false);
      },
      onError: () => setUnlinkFailed(true),
    });
  };

  return (
    <section
      aria-labelledby={`linked-docs-${itemId}`}
      className="flex flex-col gap-2"
    >
      <h2
        id={`linked-docs-${itemId}`}
        className="flex items-center gap-2 text-[15px] font-semibold"
      >
        {copy.docsTitle}
        {total > 0 ? (
          <span className="text-metadata font-normal text-muted-foreground">
            {total}
          </span>
        ) : null}
        {canEdit ? (
          <DocPicker
            copy={copy}
            workspaceId={workspaceId}
            linkedIds={new Set(rows.map((page) => page.pageId))}
            open={pickerOpen}
            onOpenChange={setPickerOpen}
            onPick={(pageId) =>
              linked.link.mutate(pageId, {
                onSuccess: () => setPickerOpen(false),
                onError: () => toast.error(copy.docsLinkFailed),
              })
            }
          />
        ) : null}
      </h2>
      {linked.isLoading ? (
        <div
          role="status"
          aria-label={copy.docsLoading}
          className="flex flex-col gap-1.5"
        >
          <span className="h-11 animate-pulse rounded-lg bg-muted" />
          <span className="h-11 animate-pulse rounded-lg bg-muted" />
        </div>
      ) : linked.isError ? (
        <p
          role="alert"
          className="flex items-center gap-2 rounded-lg bg-destructive/10 px-2.5 py-2 text-compact text-destructive"
        >
          <TriangleAlert aria-hidden="true" className="size-3.5 shrink-0" />
          <span className="flex-1">{copy.docsError}</span>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() => void linked.refetch()}
          >
            {copy.retry}
          </Button>
        </p>
      ) : total === 0 ? (
        <p className="rounded-lg border border-dashed border-line-strong px-3 py-2 text-metadata text-muted-foreground">
          {copy.docsEmpty}
        </p>
      ) : (
        <ul className="flex flex-col gap-1.5">
          {rows.map((page) => (
            <li
              key={page.pageId}
              className="flex items-center gap-2.5 rounded-lg border border-line px-2 py-1.5"
            >
              <span
                aria-hidden="true"
                className="grid h-[34px] w-[30px] shrink-0 place-items-center rounded-md bg-brand-subtle text-brand-subtle-foreground"
              >
                <FileText className="size-4" />
              </span>
              <span className="min-w-0 flex-1 truncate text-compact font-medium">
                {page.title || copy.docsUntitled}
              </span>
              <Button
                asChild
                variant="ghost"
                size="icon-sm"
                className="text-meta-foreground"
              >
                <a
                  href={pageHref(page.pageId)}
                  aria-label={`${copy.docsOpen}: ${page.title || copy.docsUntitled}`}
                >
                  <FileText aria-hidden="true" />
                </a>
              </Button>
              {canEdit ? (
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-sm"
                  className="text-meta-foreground"
                  aria-label={`${copy.docsUnlink}: ${page.title || copy.docsUntitled}`}
                  onClick={() => {
                    setUnlinkFailed(false);
                    setUnlinking(page);
                  }}
                >
                  <Link2Off aria-hidden="true" />
                </Button>
              ) : null}
            </li>
          ))}
          {Array.from({ length: linked.hiddenCount }, (_, index) => (
            <li
              key={`hidden-${index}`}
              aria-label={copy.docsRestrictedAria}
              className="flex items-center gap-2.5 rounded-lg border border-line px-2 py-1.5"
            >
              <span
                aria-hidden="true"
                className="grid h-[34px] w-[30px] shrink-0 place-items-center rounded-md bg-muted text-muted-foreground"
              >
                <Lock className="size-4" />
              </span>
              <span className="min-w-0 flex-1">
                <span className="block text-compact font-medium text-muted-foreground">
                  {copy.docsRestricted}
                </span>
                <small className="block text-metadata text-muted-foreground">
                  {copy.docsRestrictedBody}
                </small>
              </span>
              <span className="inline-flex h-[22px] items-center gap-1 rounded-md bg-muted px-2 text-[12px] font-semibold text-muted-foreground">
                <Lock aria-hidden="true" className="size-3" />
                {copy.docsRestrictedTag}
              </span>
            </li>
          ))}
        </ul>
      )}
      <AlertDialog
        open={unlinking !== null}
        onOpenChange={(next) => {
          if (!next && !linked.unlink.isPending) setUnlinking(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {fillLifecycleCopy(copy.docsUnlinkTitle, {
                title: unlinking?.title || copy.docsUntitled,
              })}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {copy.docsUnlinkBody}
            </AlertDialogDescription>
          </AlertDialogHeader>
          {unlinkFailed ? (
            <p role="alert" className="text-compact text-destructive">
              {copy.docsUnlinkFailed}
            </p>
          ) : null}
          <AlertDialogFooter>
            <AlertDialogCancel disabled={linked.unlink.isPending}>
              {copy.cancel}
            </AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={linked.unlink.isPending}
              onClick={(event) => {
                event.preventDefault();
                confirmUnlink();
              }}
            >
              {unlinkFailed ? copy.retry : copy.docsUnlinkConfirm}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </section>
  );
}

/** The "add" button and its picker: a search over the pages the person can see. */
function DocPicker({
  copy,
  workspaceId,
  linkedIds,
  open,
  onOpenChange,
  onPick,
}: {
  copy: ItemActionsCopy;
  workspaceId: string;
  linkedIds: ReadonlySet<string>;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onPick: (pageId: string) => void;
}) {
  const [query, setQuery] = useState("");
  const pages = useLinkablePages(workspaceId, open);
  const needle = query.trim().toLowerCase();
  const shown = pages.pages.filter((page) =>
    (page.title || copy.docsUntitled).toLowerCase().includes(needle),
  );
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (!next) setQuery("");
        onOpenChange(next);
      }}
    >
      <PopoverTrigger asChild>
        <Button type="button" variant="ghost" size="sm" className="ml-auto">
          <Plus aria-hidden="true" />
          {copy.docsAdd}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        role="dialog"
        aria-label={copy.docsPickerTitle}
        className="w-[380px] p-0"
      >
        <label className="flex h-10 items-center gap-2 border-b border-line px-3 text-compact text-muted-foreground">
          <Search aria-hidden="true" className="size-4" />
          <span className="sr-only">{copy.docsPickerSearch}</span>
          <input
            autoFocus
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder={copy.docsPickerSearch}
            className="flex-1 bg-transparent text-foreground outline-none"
          />
        </label>
        <div className="max-h-72 overflow-y-auto py-1">
          {pages.isLoading ? (
            <p
              role="status"
              className="px-3 py-2 text-compact text-muted-foreground"
            >
              {copy.docsPickerLoading}
            </p>
          ) : pages.isError ? (
            <p
              role="alert"
              className="flex items-center gap-2 px-3 py-2 text-compact text-destructive"
            >
              <span className="flex-1">{copy.docsPickerError}</span>
              <Button
                type="button"
                size="sm"
                variant="outline"
                onClick={() => void pages.refetch()}
              >
                {copy.retry}
              </Button>
            </p>
          ) : shown.length === 0 ? (
            <p className="px-3 py-2 text-compact text-muted-foreground">
              {fillLifecycleCopy(copy.docsPickerEmpty, { query })}
            </p>
          ) : (
            <ul role="listbox" aria-label={copy.docsPickerHeading}>
              <li
                role="presentation"
                className="px-3.5 pt-2 pb-1 text-[11.5px] font-semibold text-muted-foreground"
              >
                {copy.docsPickerHeading}
              </li>
              {shown.map((page) => {
                const already = linkedIds.has(page.id);
                const reasonId = `doc-linked-${page.id}`;
                return (
                  <li key={page.id} role="presentation">
                    <button
                      type="button"
                      role="option"
                      aria-selected={false}
                      aria-disabled={already}
                      aria-describedby={already ? reasonId : undefined}
                      onClick={() => {
                        if (!already) onPick(page.id);
                      }}
                      className={cn(
                        "mx-1 flex min-h-11 w-[calc(100%-0.5rem)] items-center gap-2.5 rounded-md px-2.5 text-left text-compact hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring",
                        already &&
                          "cursor-default text-muted-foreground hover:bg-transparent",
                      )}
                    >
                      <FileText
                        aria-hidden="true"
                        className="size-4 shrink-0"
                      />
                      <span className="min-w-0">
                        <span className="block truncate">
                          {page.title || copy.docsUntitled}
                        </span>
                        {already ? (
                          <small id={reasonId} className="block text-metadata">
                            {copy.docsPickerLinked}
                          </small>
                        ) : null}
                      </span>
                    </button>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
        <div className="border-t border-line px-3 py-2 text-metadata text-muted-foreground">
          {copy.docsPickerFoot}
        </div>
      </PopoverContent>
    </Popover>
  );
}
