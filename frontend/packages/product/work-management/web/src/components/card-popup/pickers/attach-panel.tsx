import { useMemo, useRef, useState, type ReactNode } from "react";
import {
  FileText,
  Link2,
  Paperclip,
  SquareArrowOutUpRight,
} from "lucide-react";
import {
  useFullBoard,
  useLinkablePages,
} from "@notrelix/work-management-state";
import { cn } from "@notrelix/ui-web";
import { STATUS_DOT_CLASS } from "../../card-detail/item-field-editors";
import { foldForSearch } from "../../item-page/item-pickers";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup } from "../card-popup-context";
import { PanelBtn, PanelInput } from "../popup-ui";
import {
  Caption,
  FieldLabel,
  Hint,
  PickerHead,
  SearchField,
} from "./picker-bits";

type View = "menu" | "link" | "page" | "other";

const ROW =
  "flex w-full items-center gap-3 rounded-[8px] border-0 bg-transparent p-2 text-left text-foreground hover:bg-state-hover-strong";
const OPTION =
  "flex min-h-9 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2.5 text-left text-[14px] text-foreground hover:bg-state-hover-strong";

function MenuRow({
  icon,
  title,
  hint,
  onClick,
}: {
  icon: ReactNode;
  title: string;
  hint: string;
  onClick: () => void;
}) {
  return (
    <button type="button" role="menuitem" onClick={onClick} className={ROW}>
      <span className="grid size-[38px] shrink-0 place-items-center rounded-[8px] border border-line-strong bg-popover text-muted-foreground">
        {icon}
      </span>
      <span>
        <b className="block text-[14px] leading-[1.3] font-semibold">{title}</b>
        <small className="block text-[12.5px] leading-[1.35] font-normal text-meta-foreground">
          {hint}
        </small>
      </span>
    </button>
  );
}

function LinkForm({
  onBack,
  onClose,
}: {
  onBack: () => void;
  onClose: () => void;
}) {
  const { copy, actions } = useCardPopup();
  const t = copy.pickers.attach;
  const [url, setUrl] = useState("");
  const [name, setName] = useState("");
  const [state, setState] = useState<"idle" | "busy" | "invalid" | "failed">(
    "idle",
  );
  const submit = async () => {
    let parsed: URL | null = null;
    try {
      parsed = new URL(url.trim());
    } catch {
      parsed = null;
    }
    if (!parsed || !/^https?:$/.test(parsed.protocol))
      return setState("invalid");
    setState("busy");
    const label =
      name.trim() ||
      parsed.host + (parsed.pathname === "/" ? "" : parsed.pathname);
    const ok = await (actions.addWebLink?.(parsed.href, label.slice(0, 256)) ??
      Promise.resolve(false));
    if (ok) onClose();
    else setState("failed");
  };
  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        void submit();
      }}
    >
      <PickerHead title={t.linkTitle} onBack={onBack} onClose={onClose} />
      <FieldLabel htmlFor="popup-attach-url">{t.linkUrl}</FieldLabel>
      <PanelInput
        id="popup-attach-url"
        type="url"
        autoFocus
        value={url}
        invalid={state === "invalid"}
        onChange={(event) => {
          setUrl(event.target.value);
          setState("idle");
        }}
      />
      <FieldLabel htmlFor="popup-attach-name" className="mt-3">
        {t.linkName}{" "}
        <span className="font-normal text-meta-foreground">{t.optional}</span>
      </FieldLabel>
      <PanelInput
        id="popup-attach-name"
        value={name}
        maxLength={256}
        onChange={(event) => setName(event.target.value)}
      />
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
      <PanelBtn
        type="submit"
        variant="primary"
        className="mt-3.5 w-full"
        disabled={!url.trim() || state === "busy" || !actions.addWebLink}
      >
        {t.linkAction}
      </PanelBtn>
    </form>
  );
}

function PageSearch({
  onBack,
  onClose,
}: {
  onBack: () => void;
  onClose: () => void;
}) {
  const { card, copy, actions } = useCardPopup();
  const t = copy.pickers.attach;
  const [query, setQuery] = useState("");
  const [failed, setFailed] = useState(false);
  const pages = useLinkablePages(card.workspaceId, true);
  const needle = foldForSearch(query);
  const matches = pages.pages.filter(
    (page) => !needle || foldForSearch(page.title).includes(needle),
  );
  const link = async (pageId: string) => {
    const ok = await (actions.linkPage?.(pageId) ?? Promise.resolve(false));
    if (ok) onClose();
    else setFailed(true);
  };
  return (
    <div>
      <PickerHead title={t.pageTitle} onBack={onBack} onClose={onClose} />
      <SearchField
        value={query}
        onChange={setQuery}
        label={t.pageSearch}
        placeholder={t.pageSearch}
      />
      <Caption>
        {fillPopupCopy(t.pageResults, { count: matches.length })}
      </Caption>
      {pages.isLoading ? <Hint className="px-1">{t.pageLoading}</Hint> : null}
      {pages.isError ? (
        <Hint error className="px-1">
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
      <div className="max-h-[260px] overflow-y-auto">
        {matches.slice(0, 30).map((page) => (
          <button
            key={page.id}
            type="button"
            className={ROW}
            onClick={() => void link(page.id)}
          >
            <span className="grid size-[38px] shrink-0 place-items-center rounded-[8px] border border-line-strong bg-popover text-muted-foreground">
              <FileText aria-hidden="true" className="size-4" />
            </span>
            <b className="min-w-0 truncate text-[14px] font-semibold">
              {page.title || t.pageUntitled}
            </b>
          </button>
        ))}
      </div>
      {!pages.isLoading && !pages.isError && matches.length === 0 ? (
        <Hint className="px-1">{fillPopupCopy(t.pageEmpty, { query })}</Hint>
      ) : null}
      {failed ? (
        <Hint error className="mt-2">
          {t.failed}
        </Hint>
      ) : null}
    </div>
  );
}

function OtherSearch({
  onBack,
  onClose,
}: {
  onBack: () => void;
  onClose: () => void;
}) {
  const { card, board, data, copy, callbacks } = useCardPopup();
  const t = copy.pickers.attach;
  const [query, setQuery] = useState("");
  const { groups } = useFullBoard(card.boardId, card.workspaceId);
  const candidates = useMemo(
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
  const needle = foldForSearch(query);
  const matches = candidates
    .filter((item) => item.id !== card.id)
    .filter(
      (item) =>
        !needle ||
        foldForSearch(`${item.key ?? ""} ${item.title}`).includes(needle),
    )
    .slice(0, 8);
  return (
    <div>
      <PickerHead title={t.otherTitle} onBack={onBack} onClose={onClose} />
      <SearchField
        value={query}
        onChange={setQuery}
        label={t.otherSearch}
        placeholder={t.otherSearch}
      />
      <Caption>{fillPopupCopy(t.otherHeading, { board: board.title })}</Caption>
      {matches.map((item) => (
        <button
          key={item.id}
          type="button"
          className={OPTION}
          onClick={() => {
            callbacks.onAddLink?.(item.id, "related");
            onClose();
          }}
        >
          <span className="font-mono text-[12px] text-meta-foreground">
            {item.key}
          </span>
          <span className="min-w-0 flex-1 truncate">{item.title}</span>
          <span
            aria-hidden="true"
            className={cn(
              "size-[9px] shrink-0 rounded-full",
              item.statusCategory
                ? STATUS_DOT_CLASS[item.statusCategory]
                : "bg-meta-foreground",
            )}
          />
        </button>
      ))}
      {matches.length === 0 ? (
        <Hint className="px-1">{fillPopupCopy(t.otherEmpty, { query })}</Hint>
      ) : null}
      <Hint className="mt-2 mr-2 mb-0.5 ml-2">
        {t.otherNoteBefore} <b>{t.otherNoteKind}</b>
        {t.otherNoteAfter}
      </Hint>
    </div>
  );
}

/**
 * The attach menu: a file, a web link, a Documents page, or another card of the
 * board. Each of the last three opens a small form in place with a back arrow.
 */
export function AttachPanel({
  onClose,
  onBack,
}: {
  onClose: () => void;
  /** Where the arrow of the menu goes (the "Thêm" menu); absent: no arrow. */
  onBack?: () => void;
}) {
  const { copy, callbacks, actions, slots } = useCardPopup();
  const t = copy.pickers.attach;
  const [view, setView] = useState<View>("menu");
  const file = useRef<HTMLInputElement>(null);
  const back = () => setView("menu");
  if (view === "link") return <LinkForm onBack={back} onClose={onClose} />;
  if (view === "page") return <PageSearch onBack={back} onClose={onClose} />;
  if (view === "other") return <OtherSearch onBack={back} onClose={onClose} />;
  const ic = "size-4";
  return (
    <div>
      <PickerHead title={t.title} onBack={onBack} onClose={onClose} />
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
          onClose();
        }}
      />
      <div role="menu" className="flex flex-col">
        {callbacks.onAddFile ? (
          <MenuRow
            icon={<Paperclip aria-hidden="true" className={ic} />}
            title={t.file}
            hint={t.fileHint}
            onClick={() => file.current?.click()}
          />
        ) : null}
        {actions.addWebLink ? (
          <MenuRow
            icon={<Link2 aria-hidden="true" className={ic} />}
            title={t.link}
            hint={t.linkHint}
            onClick={() => setView("link")}
          />
        ) : null}
        {actions.linkPage ? (
          <MenuRow
            icon={<FileText aria-hidden="true" className={ic} />}
            title={t.page}
            hint={t.pageHint}
            onClick={() => setView("page")}
          />
        ) : null}
        {callbacks.onAddLink ? (
          <MenuRow
            icon={<SquareArrowOutUpRight aria-hidden="true" className={ic} />}
            title={t.other}
            hint={t.otherHint}
            onClick={() => setView("other")}
          />
        ) : null}
      </div>
      {slots.cover ? (
        <Hint className="mt-2 mr-2 mb-0.5 ml-2">{t.coverHint}</Hint>
      ) : null}
    </div>
  );
}
