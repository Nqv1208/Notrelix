import { useMemo, useState, type ReactNode } from "react";
import {
  ArrowLeft,
  ChevronDown,
  ChevronRight,
  CornerDownRight,
  Eye,
  Image as ImageIcon,
  Link2,
  Paperclip,
  Plus,
  Search,
  X,
} from "lucide-react";
import { PopoverTrigger, cn } from "@notrelix/ui-web";
import { customItemFields } from "../card-detail/item-field-editors";
import { isFieldValueEmpty } from "../card-detail/item-field-more-editors";
import { foldForSearch } from "../item-page/item-pickers";
import { FaceFieldsButton } from "./capabilities/face-fields-button";
import { usePinnedFieldIds } from "./capabilities/use-pinned-fields";
import { fillPopupCopy } from "./card-popup.copy";
import { useCardPopup } from "./card-popup-context";
import { AttachPanel } from "./pickers/attach-panel";
import { PickerHead, SearchField } from "./pickers/picker-bits";
import { PanelBtn, PanelInput, PanelPopover, QuickBtn } from "./popup-ui";
import { AddLinkPanel } from "./sections/links-section";
import { fieldTypeIcon } from "./sections/add-field-panel";

/** Board fields listed straight in the menu before "Xem tất cả N trường". */
const FIELDS_SHOWN = 6;

interface Entry {
  readonly id: string;
  readonly label: string;
  readonly icon: ReactNode;
  readonly chevron?: boolean;
  readonly strong?: boolean;
  readonly run: () => void;
  /** Wraps the row in a control of its own (the face-fields popover). */
  readonly wrap?: (row: (props: object) => ReactNode) => ReactNode;
}

interface Group {
  readonly id: string;
  readonly title: string;
  readonly entries: readonly Entry[];
}

const ICON = "size-4 shrink-0 text-muted-foreground";

/** The label with the typed letters in bold blue (diacritics and case ignored). */
function Marked({ text, query }: { text: string; query: string }) {
  const needle = foldForSearch(query);
  if (!needle) return <>{text}</>;
  const chars = Array.from(text);
  let folded = "";
  const at: number[] = [];
  chars.forEach((c, i) => {
    const f = foldForSearch(c.trim() === "" ? "·" : c).replace("·", " ");
    for (const _ of f) at.push(i);
    folded += f;
  });
  const hit = folded.indexOf(needle);
  if (hit < 0) return <>{text}</>;
  const from = at[hit]!;
  const to = at[hit + needle.length - 1]!;
  return (
    <>
      {chars.slice(0, from).join("")}
      <b className="font-semibold text-brand-ink">
        {chars.slice(from, to + 1).join("")}
      </b>
      {chars.slice(to + 1).join("")}
    </>
  );
}

/** The sub-item title field the menu's "Việc con" opens. */
function SubItemPanel({ onBack, onClose }: { onBack: () => void; onClose: () => void }) {
  const { copy, callbacks } = useCardPopup();
  const t = copy.shell.addMenu;
  const [title, setTitle] = useState("");
  const submit = () => {
    const name = title.trim();
    if (!name) return;
    callbacks.onAddChildItem?.(name);
    onClose();
  };
  return (
    <div>
      <PickerHead title={t.subitem.title} onBack={onBack} onClose={onClose} />
      <PanelInput
        data-autofocus=""
        value={title}
        maxLength={200}
        placeholder={t.subitem.placeholder}
        aria-label={t.subitem.title}
        onChange={(event) => setTitle(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === "Enter") submit();
        }}
      />
      <PanelBtn
        variant="primary"
        className="mt-3 w-full"
        disabled={title.trim() === ""}
        onClick={submit}
      >
        {t.subitem.add}
      </PanelBtn>
    </div>
  );
}

/**
 * The "+ Thêm" menu: a search box first, then three groups of 36 px rows. Typing filters every row
 * (the letters typed show in blue), the arrow keys move and Enter picks. Content opens its panel,
 * a board field adds its chip and opens its editor, Display opens the cover or the face fields.
 */
function MenuBody({ onClose }: { onClose: () => void }) {
  const {
    board,
    card,
    copy,
    callbacks,
    fields,
    mode,
    slots,
    addedFieldIds,
    addField,
    openPicker,
    startNewField,
  } = useCardPopup();
  const t = copy.shell.addMenu;
  const pinned = usePinnedFieldIds();
  const [query, setQuery] = useState("");
  const [all, setAll] = useState(false);
  const [active, setActive] = useState(0);
  const needle = foldForSearch(query);

  const empty = useMemo(() => {
    const own = fields.priority ? [fields.priority] : [];
    return [...own, ...customItemFields(board)]
      .filter(
        (f, i, list) =>
          list.findIndex((o) => o.id === f.id) === i &&
          !pinned.includes(f.id) &&
          !addedFieldIds.includes(f.id) &&
          f.fieldType !== "formula" &&
          f.fieldType !== "rollup" &&
          isFieldValueEmpty(card.fieldValues[f.id]),
      );
  }, [addedFieldIds, board, card.fieldValues, fields.priority, pinned]);

  const groups = useMemo<Group[]>(() => {
    const content: Entry[] = [
      {
        id: "attach",
        label: t.attach.title,
        icon: <Paperclip aria-hidden="true" className={ICON} />,
        chevron: true,
        run: () => openPicker("attach"),
      },
    ];
    if (callbacks.onAddChildItem)
      content.push({
        id: "subitem",
        label: t.subitem.title,
        icon: <CornerDownRight aria-hidden="true" className={ICON} />,
        run: () => openPicker("subitem"),
      });
    if (callbacks.onAddLink)
      content.push({
        id: "link",
        label: t.link.title,
        icon: <Link2 aria-hidden="true" className={ICON} />,
        chevron: true,
        run: () => openPicker("link"),
      });
    const shown = needle || all ? empty : empty.slice(0, FIELDS_SHOWN);
    const props: Entry[] = shown.map((field) => {
      const Icon = fieldTypeIcon(field.fieldType);
      return {
        id: `field:${field.id}`,
        label: field.name,
        icon: <Icon aria-hidden="true" className={ICON} />,
        run: () => {
          addField(field.id);
          onClose();
        },
      };
    });
    if (!needle && !all && empty.length > FIELDS_SHOWN)
      props.push({
        id: "all",
        label: fillPopupCopy(t.fields.seeAll, { count: empty.length }),
        icon: <ChevronDown aria-hidden="true" className="size-4 shrink-0 text-brand-ink" />,
        strong: true,
        run: () => setAll(true),
      });
    if (!needle && callbacks.onCreateField)
      props.push({
        id: "create",
        label: t.fields.create,
        icon: <Plus aria-hidden="true" className="size-4 shrink-0 text-brand-ink" />,
        strong: true,
        run: () => startNewField(""),
      });
    const display: Entry[] = [];
    if (slots.cover)
      display.push({
        id: "cover",
        label: t.cover.title,
        icon: <ImageIcon aria-hidden="true" className={ICON} />,
        run: () => {
          onClose();
          window.setTimeout(
            () =>
              document
                .querySelector<HTMLButtonElement>("[data-popup-cover]")
                ?.click(),
            120,
          );
        },
      });
    if (mode === "view")
      display.push({
        id: "face",
        label: t.face.title,
        icon: <Eye aria-hidden="true" className={ICON} />,
        run: () => undefined,
        wrap: (row) => (
          <FaceFieldsButton
            boardId={board.id}
            workspaceId={board.workspaceId}
            fields={board.fieldDefinitions}
            canEdit
            trigger={row({}) as React.ReactElement}
          />
        ),
      });
    const keep = (e: Entry) => !needle || foldForSearch(e.label).includes(needle);
    return [
      { id: "content", title: t.groups.content, entries: content.filter(keep) },
      { id: "fields", title: t.groups.fields, entries: props.filter(keep) },
      { id: "display", title: t.groups.display, entries: display.filter(keep) },
    ];
  }, [addField, all, board, callbacks, empty, mode, needle, onClose, openPicker, slots.cover, startNewField, t]);

  const flat = groups.flatMap((g) => g.entries);
  const noMatch = needle !== "" && flat.length === 0;
  const emptyBoard = !needle && empty.length === 0;
  const createNamed = noMatch && Boolean(callbacks.onCreateField);
  const rows: Entry[] = createNamed
    ? [
        {
          id: "create-named",
          label: fillPopupCopy(t.fields.createNamed, { name: query.trim() }),
          icon: <Plus aria-hidden="true" className="size-4 shrink-0 text-brand-ink" />,
          strong: true,
          run: () => startNewField(query.trim()),
        },
      ]
    : flat;
  const current = Math.min(active, Math.max(rows.length - 1, 0));
  const activeId = rows[current] ? `add-menu-${rows[current]!.id}` : undefined;

  const renderRow = (entry: Entry) => {
    const props = {
      id: `add-menu-${entry.id}`,
      type: "button" as const,
      role: "option",
      "aria-selected": rows[current]?.id === entry.id,
      tabIndex: -1,
      onClick: entry.wrap ? undefined : entry.run,
      onMouseMove: () => setActive(rows.findIndex((r) => r.id === entry.id)),
      className: cn(
        "flex h-11 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2 text-left text-[13.5px] text-foreground md:h-9",
        entry.strong && "font-medium text-brand-ink",
        rows[current]?.id === entry.id && "bg-state-hover-strong",
      ),
    };
    const body = (
      <>
        {entry.icon}
        <span className="min-w-0 flex-1 truncate">
          {entry.strong ? entry.label : <Marked text={entry.label} query={query} />}
        </span>
        {entry.chevron ? (
          <ChevronRight aria-hidden="true" className="size-3.5 text-meta-foreground" />
        ) : null}
      </>
    );
    return entry.wrap ? (
      entry.wrap((extra) => (
        <button {...props} {...extra}>
          {body}
        </button>
      ))
    ) : (
      <button key={entry.id} {...props}>
        {body}
      </button>
    );
  };

  return (
    <div>
      <div className="flex h-11 items-center gap-2 border-b-2 border-primary pr-1.5 pl-3">
        <Search aria-hidden="true" className="size-4 shrink-0 text-meta-foreground" />
        <input
          data-picker-search=""
          type="text"
          role="combobox"
          aria-expanded="true"
          aria-controls="add-menu-list"
          aria-activedescendant={activeId}
          aria-label={t.searchLabel}
          placeholder={t.search}
          value={query}
          onChange={(event) => {
            setQuery(event.target.value);
            setActive(0);
          }}
          onKeyDown={(event) => {
            if (event.key === "ArrowDown" || event.key === "ArrowUp") {
              event.preventDefault();
              const step = event.key === "ArrowDown" ? 1 : -1;
              setActive((current + step + rows.length) % Math.max(rows.length, 1));
            } else if (event.key === "Enter") {
              event.preventDefault();
              rows[current]?.run();
            }
          }}
          className="h-8 min-w-0 flex-1 border-0 bg-transparent p-0 text-[14px] text-foreground outline-none placeholder:text-meta-foreground"
        />
        {query ? (
          <button
            type="button"
            aria-label={t.clear}
            onClick={() => setQuery("")}
            className="grid size-8 place-items-center rounded-[8px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong"
          >
            <X aria-hidden="true" className="size-4" />
          </button>
        ) : null}
      </div>
      <div
        id="add-menu-list"
        role="listbox"
        aria-label={copy.shell.addTitle}
        className="px-1.5 pt-1 pb-1.5"
      >
        {noMatch ? (
          <>
            <p className="m-0 flex h-9 items-center gap-2.5 px-2 text-[13.5px] text-muted-foreground">
              <Search aria-hidden="true" className={ICON} />
              {fillPopupCopy(t.noResults, { query: query.trim() })}
            </p>
            {createNamed ? renderRow(rows[0]!) : null}
          </>
        ) : (
          groups.map((group) =>
            group.entries.length === 0 && !(group.id === "fields" && emptyBoard) ? null : (
              <div key={group.id} role="group" aria-label={group.title}>
                <div className="flex h-7 items-center px-2 text-[12px] font-medium text-muted-foreground">
                  {group.title}
                </div>
                {group.id === "fields" && emptyBoard ? (
                  <p className="m-0 flex h-9 items-center px-2 text-[13.5px] text-meta-foreground">
                    {t.fields.allSet}
                  </p>
                ) : null}
                {group.entries.map(renderRow)}
              </div>
            )
          )
        )}
      </div>
    </div>
  );
}

/** The "+ Thêm" button and its menu; "Đính kèm", "Liên kết công việc" and "Việc con" open their own panel in place of it. */
export function AddMenu() {
  const { copy, picker, pickerAnchor, openPicker } = useCardPopup();
  const t = copy.shell;
  const isOpen =
    picker === "add" ||
    picker === "attach" ||
    picker === "link" ||
    picker === "subitem";
  const close = () => openPicker(null);
  const back = () => openPicker("add");
  return (
    <PanelPopover
      open={isOpen}
      anchor={pickerAnchor}
      onOpenChange={(next) =>
        openPicker(next ? "add" : null, next ? null : undefined)
      }
      width={picker === "link" ? 380 : picker === "add" ? 300 : 344}
      className={picker === "add" ? "p-0" : undefined}
      trigger={
        <PopoverTrigger asChild>
          <QuickBtn on={isOpen} title={t.addTitle} aria-haspopup="dialog">
            <Plus aria-hidden="true" className="size-4" />
            {t.add}
          </QuickBtn>
        </PopoverTrigger>
      }
    >
      {picker === "attach" ? (
        <AttachPanel onClose={close} onBack={back} />
      ) : picker === "link" ? (
        <>
          <button
            type="button"
            onClick={back}
            aria-label={copy.pickers.back}
            className="mb-1 grid size-8 place-items-center rounded-[8px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong"
          >
            <ArrowLeft aria-hidden="true" className="size-4" />
          </button>
          <AddLinkPanel onClose={close} />
        </>
      ) : picker === "subitem" ? (
        <SubItemPanel onBack={back} onClose={close} />
      ) : (
        <MenuBody onClose={close} />
      )}
    </PanelPopover>
  );
}
