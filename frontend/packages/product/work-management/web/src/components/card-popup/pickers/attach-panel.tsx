import { useMemo, useRef, useState, type ReactNode } from "react";
import {
  AlertCircle,
  ArrowLeft,
  Check,
  FileText,
  Link2,
  Loader2,
  Paperclip,
  Search,
  SquareCheck,
  X,
} from "lucide-react";
import {
  useFullBoard,
  useLinkablePages,
} from "@notrelix/work-management-state";
import { cn } from "@notrelix/ui-web";
import { foldForSearch } from "../../item-page/item-pickers";
import { useItemPageCopy } from "../../item-page/use-item-page-copy";
import { STATUS_DOT_CLASS } from "../../card-detail/item-field-editors";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup } from "../card-popup-context";
import { IconBtn, PanelBtn, PanelInput } from "../popup-ui";
import { Hint } from "./picker-bits";

/** `.cap`: the small heading of a list inside the panel. */
function ListCaption({ children }: { children: ReactNode }) {
  return (
    <div className="flex h-7 items-center px-2 text-[12px] font-medium text-meta-foreground">
      {children}
    </div>
  );
}

const ROW =
  "flex h-11 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2 text-left text-[13.5px] text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary md:h-9";

/** A pasted or typed address: it has a scheme, or a dotted host with no spaces. */
function asUrl(text: string): URL | null {
  const value = text.trim();
  if (!value || /\s/.test(value)) return null;
  const withScheme = /^[a-z][a-z0-9+.-]*:\/\//i.test(value)
    ? value
    : /^[^./]+\.[^./]+/.test(value)
      ? `https://${value}`
      : null;
  if (!withScheme) return null;
  try {
    return new URL(withScheme);
  } catch {
    return null;
  }
}

/** The files being sent to the card, with their progress, failures and "Thử lại". */
function Uploads() {
  const { data, callbacks, copy } = useCardPopup();
  const t = copy.pickers.attach;
  const pageCopy = useItemPageCopy();
  const list = (data.uploads ?? []).filter((u) => u.status !== "done");
  if (list.length === 0) return null;
  return (
    <div className="mb-2 flex flex-col gap-2">
      {list.map((upload) => {
        const failed = upload.status === "error";
        return (
          <div
            key={upload.id}
            className={cn(
              "box-border rounded-[10px] border bg-popover px-3 py-2.5",
              failed ? "border-danger" : "border-line-strong",
            )}
          >
            <div className="flex items-center gap-2">
              <b className="min-w-0 flex-1 truncate text-[14px] font-semibold">
                {upload.name}
              </b>
              {failed && upload.retryable ? (
                <PanelBtn
                  small
                  onClick={() => callbacks.onRetryUpload?.(upload.id)}
                >
                  {t.retry}
                </PanelBtn>
              ) : null}
              {failed ? (
                <IconBtn
                  small
                  aria-label={fillPopupCopy(t.dismiss, { name: upload.name })}
                  onClick={() => callbacks.onDismissUpload?.(upload.id)}
                >
                  <X aria-hidden="true" className="size-4" />
                </IconBtn>
              ) : (
                <Loader2
                  aria-hidden="true"
                  className="size-4 animate-spin text-meta-foreground"
                />
              )}
            </div>
            {failed ? (
              <p
                role="alert"
                className="m-0 mt-1 flex items-start gap-1.5 text-[12.5px] font-medium text-danger-text"
              >
                <AlertCircle
                  aria-hidden="true"
                  className="mt-px size-3.5 shrink-0"
                />
                {(pageCopy.files[upload.error ?? "failed"] ?? t.failed).replace(
                  "{{limit}}",
                  upload.limit ?? "",
                )}
              </p>
            ) : (
              <>
                <div
                  role="progressbar"
                  aria-label={upload.name}
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-valuenow={
                    upload.progress == null
                      ? undefined
                      : Math.round(upload.progress)
                  }
                  className="mt-2 h-1 overflow-hidden rounded-full bg-state-hover-strong"
                >
                  <div
                    className="h-full bg-primary transition-[width]"
                    style={{ width: `${Math.round(upload.progress ?? 0)}%` }}
                  />
                </div>
                <p className="m-0 mt-1 text-[12.5px] text-meta-foreground">
                  {upload.progress == null
                    ? t.uploadingPlain
                    : fillPopupCopy(t.uploading, {
                        percent: Math.round(upload.progress),
                      })}
                </p>
              </>
            )}
          </div>
        );
      })}
    </div>
  );
}

/** The address row: where it goes, a name to show, and "Gắn". */
function LinkRow({ url, onClose }: { url: URL; onClose: () => void }) {
  const { copy, actions } = useCardPopup();
  const t = copy.pickers.attach;
  const [name, setName] = useState("");
  const [state, setState] = useState<"idle" | "busy" | "invalid" | "failed">(
    "idle",
  );
  const path = url.pathname === "/" ? "" : url.pathname + url.search;
  const submit = async () => {
    if (!/^https?:$/.test(url.protocol)) return setState("invalid");
    setState("busy");
    const label = name.trim() || url.host + path;
    const ok = await (actions.addWebLink?.(url.href, label.slice(0, 256)) ??
      Promise.resolve(false));
    if (ok) onClose();
    else setState("failed");
  };
  return (
    <form
      className="mt-2.5"
      onSubmit={(event) => {
        event.preventDefault();
        void submit();
      }}
    >
      <p className="m-0 flex items-center gap-2 px-1 text-[13.5px]">
        <Link2
          aria-hidden="true"
          className="size-4 shrink-0 text-muted-foreground"
        />
        <b className="font-semibold">{url.host}</b>
        <span className="min-w-0 truncate text-muted-foreground">{path}</span>
      </p>
      <label
        htmlFor="popup-attach-name"
        className="mt-3 mb-1.5 block text-[12px] font-semibold text-muted-foreground"
      >
        {t.linkName}
      </label>
      <div className="flex gap-2">
        <PanelInput
          id="popup-attach-name"
          value={name}
          maxLength={256}
          placeholder={url.host + path}
          onChange={(event) => setName(event.target.value)}
          className="min-w-0 flex-1"
        />
        <PanelBtn
          type="submit"
          variant="primary"
          disabled={state === "busy" || !actions.addWebLink}
        >
          {t.linkAction}
        </PanelBtn>
      </div>
      {state === "invalid" ? (
        <Hint error className="mt-2">
          {t.urlInvalid}
        </Hint>
      ) : null}
      {state === "failed" ? (
        <Hint error className="mt-2">
          {t.failed}
        </Hint>
      ) : null}
    </form>
  );
}

interface Hit {
  readonly id: string;
  readonly kind: "page" | "task";
  readonly key?: string | null;
  readonly title: string;
  readonly meta?: string;
  readonly category?: "todo" | "in_progress" | "review" | "done" | null;
  readonly done?: boolean;
}

/**
 * The single attach panel: choose a file (25 MB each; dropping or pasting files anywhere on the card
 * does the same), and one smart field below it. A web address becomes a link row to name and attach;
 * words search the Documents pages and the cards of the board; before anything is typed the field
 * offers recent ones. A card is attached as a "Liên quan" link.
 */
export function AttachPanel({
  onClose,
  onBack,
}: {
  onClose: () => void;
  onBack?: () => void;
}) {
  const { card, copy, callbacks, actions, data } = useCardPopup();
  const t = copy.pickers.attach;
  const file = useRef<HTMLInputElement>(null);
  const [query, setQuery] = useState("");
  const [failed, setFailed] = useState(false);
  const pages = useLinkablePages(card.workspaceId, Boolean(actions.linkPage));
  const { groups } = useFullBoard(card.boardId, card.workspaceId);
  const tasks = useMemo(
    () =>
      data.linkCandidates ??
      groups
        .flatMap((group) => group.cards)
        .filter((item) => !item.parentItemId)
        .map((item) => ({
          id: item.id,
          key: item.itemKey ?? null,
          title: item.title,
          statusCategory: item.statusCategory ?? null,
        })),
    [data.linkCandidates, groups],
  );
  const url = asUrl(query);
  const needle = foldForSearch(query);

  const pageHits: Hit[] = actions.linkPage
    ? pages.pages
        .filter((p) => !needle || foldForSearch(p.title).includes(needle))
        .map((p) => ({
          id: p.id,
          kind: "page" as const,
          title: p.title || t.pageUntitled,
          meta: p.parentTitle,
        }))
    : [];
  const taskHits: Hit[] = callbacks.onAddLink
    ? tasks
        .filter((x) => x.id !== card.id)
        .filter(
          (x) =>
            !needle ||
            foldForSearch(`${x.key ?? ""} ${x.title}`).includes(needle),
        )
        .map((x) => ({
          id: x.id,
          kind: "task" as const,
          key: x.key,
          title: x.title,
          meta: "statusName" in x ? (x.statusName ?? undefined) : undefined,
          category: x.statusCategory ?? null,
          done: x.statusCategory === "done",
        }))
    : [];

  const pick = async (hit: Hit) => {
    if (hit.kind === "task") {
      callbacks.onAddLink?.(hit.id, "related");
      return onClose();
    }
    const ok = await (actions.linkPage?.(hit.id) ?? Promise.resolve(false));
    if (ok) onClose();
    else setFailed(true);
  };
  const icon = (hit: Hit): ReactNode =>
    hit.kind === "page" ? (
      <FileText
        aria-hidden="true"
        className="size-4 shrink-0 text-muted-foreground"
      />
    ) : hit.done ? (
      <Check
        aria-hidden="true"
        className="size-4 shrink-0 text-muted-foreground"
      />
    ) : (
      <SquareCheck
        aria-hidden="true"
        className="size-4 shrink-0 text-muted-foreground"
      />
    );
  const row = (hit: Hit) => (
    <button
      key={`${hit.kind}:${hit.id}`}
      type="button"
      className={ROW}
      onClick={() => void pick(hit)}
    >
      {icon(hit)}
      {hit.key ? (
        <span className="font-mono text-[12px] text-meta-foreground">
          {hit.key}
        </span>
      ) : null}
      <span className="min-w-0 flex-1 truncate">{hit.title}</span>
      {hit.meta ? (
        <span className="inline-flex shrink-0 items-center gap-1.5 text-[12.5px] text-meta-foreground">
          {hit.category ? (
            <span
              aria-hidden="true"
              className={cn(
                "size-[9px] rounded-full",
                STATUS_DOT_CLASS[hit.category],
              )}
            />
          ) : null}
          {hit.meta}
        </span>
      ) : null}
    </button>
  );
  // Before typing: a few of each kind, pages and cards alternating.
  const recent: Hit[] = [];
  for (let i = 0; i < 3 && recent.length < 5; i++) {
    if (pageHits[i]) recent.push(pageHits[i]!);
    if (taskHits[i] && recent.length < 5) recent.push(taskHits[i]!);
  }

  return (
    <>
      <div className="flex h-13 items-center px-2 md:h-12 md:pr-3">
        {onBack ? (
          <IconBtn small aria-label={copy.pickers.back} onClick={onBack}>
            <ArrowLeft aria-hidden="true" className="size-4" />
          </IconBtn>
        ) : (
          <span className="size-11 md:size-8" />
        )}
        <b className="ml-1 flex-1 text-center text-[14px] font-[650]">
          {t.title}
        </b>
        <IconBtn small aria-label={copy.pickers.close} onClick={onClose}>
          <X aria-hidden="true" className="size-4" />
        </IconBtn>
      </div>
      <div className="pb-3 md:px-3">
        <Uploads />
        <input
          ref={file}
          type="file"
          multiple
          hidden
          tabIndex={-1}
          onChange={(event) => {
            for (const picked of Array.from(event.target.files ?? []))
              callbacks.onAddFile?.(picked);
            event.target.value = "";
          }}
        />
        {callbacks.onAddFile ? (
          <div className="box-border rounded-[10px] border border-dashed border-line-strong p-3">
            <div className="flex items-center gap-2.5">
              <PanelBtn small onClick={() => file.current?.click()}>
                <Paperclip aria-hidden="true" className="size-4" />
                {t.chooseFile}
              </PanelBtn>
              <span className="text-[12.5px] text-muted-foreground">
                {t.limit}
              </span>
            </div>
            <span className="mt-2 hidden text-[12px] text-meta-foreground md:block">
              {t.dropHint}
            </span>
          </div>
        ) : null}
        <label className="mt-3 box-border flex h-11 w-full items-center gap-2 rounded-[8px] border border-line-strong bg-popover px-2.5 md:h-9 text-[14px] focus-within:border-primary focus-within:ring-[3px] focus-within:ring-brand-subtle">
          <span className="sr-only">{t.smartLabel}</span>
          {url ? (
            <Link2
              aria-hidden="true"
              className="size-4 shrink-0 text-brand-ink"
            />
          ) : (
            <Search
              aria-hidden="true"
              className="size-4 shrink-0 text-brand-ink"
            />
          )}
          <input
            type="text"
            autoFocus
            data-picker-search=""
            value={query}
            placeholder={t.smartPlaceholder}
            onChange={(event) => {
              setQuery(event.target.value);
              setFailed(false);
            }}
            className="h-full min-w-0 flex-1 border-0 bg-transparent p-0 text-[13.5px] text-foreground outline-none placeholder:text-meta-foreground"
          />
        </label>
        {url ? (
          <LinkRow key={url.href} url={url} onClose={onClose} />
        ) : needle === "" ? (
          recent.length > 0 ? (
            <div className="-mx-1 mt-1.5 pt-1">
              <ListCaption>{t.recent}</ListCaption>
              {recent.map(row)}
            </div>
          ) : null
        ) : (
          <>
            {pageHits.length > 0 ? (
              <div className="-mx-1 mt-1.5 pt-1">
                <ListCaption>{t.pages}</ListCaption>
                {pageHits.slice(0, 4).map(row)}
              </div>
            ) : null}
            {taskHits.length > 0 ? (
              <div className="-mx-1 pt-1">
                <ListCaption>{t.tasks}</ListCaption>
                {taskHits.slice(0, 4).map(row)}
                <Hint className="mt-1 px-2">{t.related}</Hint>
              </div>
            ) : null}
            {pageHits.length === 0 && taskHits.length === 0 ? (
              <p className="m-0 mt-3 flex items-center gap-2 px-1 text-[13.5px] text-muted-foreground">
                <Search aria-hidden="true" className="size-4 shrink-0" />
                {fillPopupCopy(t.noResults, { query: query.trim() })}
              </p>
            ) : null}
          </>
        )}
        {pages.isError ? (
          <Hint error className="mt-2 px-1">
            {t.pageError}{" "}
            <button
              type="button"
              className="underline"
              onClick={() => void pages.refetch()}
            >
              {t.retry}
            </button>
          </Hint>
        ) : null}
        {failed ? (
          <Hint error className="mt-2 px-1">
            {t.failed}
          </Hint>
        ) : null}
      </div>
    </>
  );
}
