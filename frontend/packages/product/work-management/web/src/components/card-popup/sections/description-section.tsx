import { useEffect, useRef, useState, type ReactNode } from "react";
import {
  AlertCircle,
  Bold,
  ChevronDown,
  CircleHelp,
  Italic,
  Link2,
  List,
  Loader2,
  Minus,
  Plus,
  Table2,
  Type,
  Calendar,
  Code2,
} from "lucide-react";
import { PopoverTrigger, cn } from "@notrelix/ui-web";
import { useCardPopup, useCanEdit } from "../card-popup-context";
import { fillPopupCopy } from "../card-popup.copy";
import { DESCRIPTION_MAX_LENGTH } from "../../shared/item-description-editor";
import { Markdown } from "../markdown";
import {
  PanelBtn,
  PanelPopover,
  PopSection,
  EmptySection,
  SECTION_ICONS,
  SectionHead,
  SmallBtn,
} from "../popup-ui";

const TBTN =
  "inline-flex h-8 min-w-8 items-center justify-center gap-0.5 rounded-md border-0 bg-transparent px-1.5 text-[14px] font-semibold text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

type Edit = (
  text: string,
  from: number,
  to: number,
) => { text: string; from: number; to: number };

/** Wraps the selection (or a sample word) in Markdown marks. */
const wrap =
  (mark: string, sample: string): Edit =>
  (text, from, to) => {
    const body = text.slice(from, to) || sample;
    return {
      text: text.slice(0, from) + mark + body + mark + text.slice(to),
      from: from + mark.length,
      to: from + mark.length + body.length,
    };
  };

/** Puts `prefix` at the start of every selected line, replacing a heading or quote mark. */
const linePrefix =
  (prefix: string, strip = /^(#{1,3}\s+|>\s?)/): Edit =>
  (text, from, to) => {
    const start = text.lastIndexOf("\n", from - 1) + 1;
    const endAt = text.indexOf("\n", to);
    const end = endAt === -1 ? text.length : endAt;
    const block = text
      .slice(start, end)
      .split("\n")
      .map((line) => prefix + line.replace(strip, ""))
      .join("\n");
    return {
      text: text.slice(0, start) + block + text.slice(end),
      from: start,
      to: start + block.length,
    };
  };

/** Inserts a block of text on its own lines at the cursor. */
const insert =
  (block: string): Edit =>
  (text, from, to) => {
    const lead = from > 0 && text[from - 1] !== "\n" ? "\n" : "";
    const put = lead + block + "\n";
    return {
      text: text.slice(0, from) + put + text.slice(to),
      from: from + put.length,
      to: from + put.length,
    };
  };

function ToolMenu({
  label,
  icon,
  width,
  children,
}: {
  label: string;
  icon: ReactNode;
  width: number;
  children: (close: () => void) => ReactNode;
}) {
  const [open, setOpen] = useState(false);
  return (
    <PanelPopover
      open={open}
      onOpenChange={setOpen}
      width={width}
      trigger={
        <PopoverTrigger asChild>
          <button
            type="button"
            aria-label={label}
            title={label}
            aria-haspopup="menu"
            aria-expanded={open}
            className={cn(TBTN, open && "bg-state-hover-strong")}
          >
            {icon}
            <ChevronDown aria-hidden="true" className="size-[13px]" />
          </button>
        </PopoverTrigger>
      }
    >
      {children(() => setOpen(false))}
    </PanelPopover>
  );
}

const ROW =
  "flex min-h-10 w-full items-center gap-2.5 rounded-lg border-0 bg-transparent px-2.5 text-left text-foreground hover:bg-state-hover-strong";

function HelpTable() {
  const t = useCardPopup().copy.sections.description;
  const rows: ReadonlyArray<[string, ReactNode]> = [
    [`**${t.boldSample}**`, <b key="b">{t.boldSample}</b>],
    [`*${t.italicSample}*`, <i key="i">{t.italicSample}</i>],
    [`~~${t.strikeSample}~~`, <s key="s">{t.strikeSample}</s>],
    [
      `# ${t.headingSample}`,
      <b key="h" className="text-[16px]">
        {t.headingSample}
      </b>,
    ],
    [`- ${t.itemSample}`, `• ${t.itemSample}`],
    [`1. ${t.itemSample}`, `1. ${t.itemSample}`],
    [`- [ ] ${t.taskSample}`, `☐ ${t.taskSample}`],
    [
      `> ${t.quoteSample}`,
      <span
        key="q"
        className="border-l-[3px] border-line-strong pl-2 text-muted-foreground"
      >
        {t.quoteSample}
      </span>,
    ],
    [
      `\`${t.codeSample}\``,
      <span key="c" className="rounded bg-state-hover-strong px-1 font-mono">
        {t.codeSample}
      </span>,
    ],
    [
      `[${t.linkSample}](${t.linkTarget})`,
      <span key="l" className="text-brand-subtle-foreground underline">
        {t.linkSample}
      </span>,
    ],
  ];
  return (
    <div className="mt-3 rounded-[10px] border border-line-strong bg-background px-4 pt-3 pb-2">
      <div className="mb-1.5 flex items-center">
        <b className="flex-1 text-[14px]">{t.help}</b>
      </div>
      <div className="grid grid-cols-2 gap-2.5 pb-1 text-[12px] font-semibold text-muted-foreground">
        <span>{t.helpType}</span>
        <span>{t.helpShows}</span>
      </div>
      {rows.map(([typed, shown]) => (
        <div
          key={typed}
          className="grid min-h-[30px] grid-cols-2 items-center gap-2.5 border-t border-line"
        >
          <span className="font-mono text-[12.5px]">{typed}</span>
          <span className="text-[13.5px]">{shown}</span>
        </div>
      ))}
    </div>
  );
}

function Editor({
  initial,
  onClose,
}: {
  initial: string;
  onClose: () => void;
}) {
  const { card, copy, callbacks, saves, track } = useCardPopup();
  const t = copy.sections.description;
  const [draft, setDraft] = useState(initial);
  const [help, setHelp] = useState(false);
  const area = useRef<HTMLTextAreaElement>(null);
  const save = saves.description;
  const saving = save?.status === "saving";
  const over = draft.length - DESCRIPTION_MAX_LENGTH;
  const tooLong = over > 0;
  const submitted = useRef(false);

  useEffect(() => {
    const node = area.current;
    node?.focus();
    node?.setSelectionRange(node.value.length, node.value.length);
  }, []);
  // The write finished and nothing is left to retry: the editor closes.
  useEffect(() => {
    if (submitted.current && save === undefined) onClose();
  }, [save, onClose]);

  const apply = (edit: Edit) => {
    const node = area.current;
    if (!node) return;
    const next = edit(draft, node.selectionStart, node.selectionEnd);
    setDraft(next.text);
    requestAnimationFrame(() => {
      node.focus();
      node.setSelectionRange(next.from, next.to);
    });
  };
  const submit = () => {
    if (tooLong || saving) return;
    if (draft.trim() === initial.trim()) return onClose();
    submitted.current = true;
    track("description", async () =>
      callbacks.onUpdateItem?.(card.id, { descriptionMd: draft }),
    );
  };
  const today = new Intl.DateTimeFormat(copy.locale, {
    dateStyle: "long",
  }).format(new Date());

  return (
    <>
      <div
        className={cn(
          "mt-2 rounded-[10px] border-2 bg-popover",
          tooLong ? "border-danger" : "border-primary",
        )}
      >
        <div
          role="toolbar"
          aria-label={t.toolbar}
          className="relative flex items-center gap-0.5 border-b border-line px-2 py-1.5"
        >
          <ToolMenu
            label={t.style}
            icon={<Type aria-hidden="true" className="size-4" />}
            width={220}
          >
            {(close) => (
              <>
                {(
                  [
                    [t.paragraph, "", "text-[14px] font-normal"],
                    [t.heading1, "# ", "text-[20px] font-[650]"],
                    [t.heading2, "## ", "text-[17px] font-[650]"],
                    [t.heading3, "### ", "text-[15px] font-[650]"],
                    [t.quote, "> ", "text-[14px] font-normal"],
                  ] as const
                ).map(([label, prefix, cls]) => (
                  <button
                    key={label}
                    type="button"
                    className={ROW}
                    onClick={() => {
                      apply(linePrefix(prefix));
                      close();
                    }}
                  >
                    <span className={cls}>{label}</span>
                  </button>
                ))}
              </>
            )}
          </ToolMenu>
          <button
            type="button"
            aria-label={t.bold}
            title={t.bold}
            className={TBTN}
            onClick={() => apply(wrap("**", t.boldSample))}
          >
            <Bold aria-hidden="true" className="size-4" />
          </button>
          <button
            type="button"
            aria-label={t.italic}
            title={t.italic}
            className={TBTN}
            onClick={() => apply(wrap("*", t.italicSample))}
          >
            <Italic aria-hidden="true" className="size-4" />
          </button>
          <span aria-hidden="true" className="mx-1 h-5 w-px bg-line-strong" />
          <button
            type="button"
            aria-label={t.list}
            title={t.list}
            className={TBTN}
            onClick={() => apply(linePrefix("- ", /^[-*]\s+/))}
          >
            <List aria-hidden="true" className="size-4" />
          </button>
          <span aria-hidden="true" className="mx-1 h-5 w-px bg-line-strong" />
          <button
            type="button"
            aria-label={t.link}
            title={t.link}
            className={TBTN}
            onClick={() =>
              apply((text, from, to) => {
                const body = text.slice(from, to) || t.linkSample;
                const put = `[${body}](https://)`;
                return {
                  text: text.slice(0, from) + put + text.slice(to),
                  from: from + put.length - 1 - 8,
                  to: from + put.length - 1,
                };
              })
            }
          >
            <Link2 aria-hidden="true" className="size-4" />
          </button>
          <ToolMenu
            label={t.more}
            icon={<Plus aria-hidden="true" className="size-4" />}
            width={280}
          >
            {(close) => (
              <>
                {(
                  [
                    [
                      t.table,
                      t.tableHint,
                      <Table2 key="t" className="size-4" />,
                      insert(
                        "| A | B | C |\n| --- | --- | --- |\n|   |   |   |\n|   |   |   |",
                      ),
                    ],
                    [
                      t.code,
                      t.codeHint,
                      <Code2 key="c" className="size-4" />,
                      insert("```\n" + t.codeSample + "\n```"),
                    ],
                    [
                      t.rule,
                      t.ruleHint,
                      <Minus key="r" className="size-4" />,
                      insert("---"),
                    ],
                    [
                      t.date,
                      t.dateHint,
                      <Calendar key="d" className="size-4" />,
                      (text: string, from: number, to: number) => ({
                        text: text.slice(0, from) + today + text.slice(to),
                        from: from + today.length,
                        to: from + today.length,
                      }),
                    ],
                  ] as const
                ).map(([label, hint, icon, edit]) => (
                  <button
                    key={label}
                    type="button"
                    role="menuitem"
                    className="flex w-full items-center gap-3 rounded-lg border-0 bg-transparent p-2 text-left text-foreground hover:bg-state-hover-strong"
                    onClick={() => {
                      apply(edit);
                      close();
                    }}
                  >
                    <span className="grid size-9 shrink-0 place-items-center rounded-lg border border-line-strong bg-popover text-muted-foreground">
                      {icon}
                    </span>
                    <span>
                      <b className="block text-[14px] leading-[1.3] font-semibold">
                        {label}
                      </b>
                      <small className="block text-[12.5px] leading-[1.35] font-normal text-meta-foreground">
                        {hint}
                      </small>
                    </span>
                  </button>
                ))}
              </>
            )}
          </ToolMenu>
          <span className="flex-1" />
          <button
            type="button"
            aria-label={t.help}
            title={t.help}
            aria-pressed={help}
            className={cn(TBTN, help && "bg-state-hover-strong")}
            onClick={() => setHelp((v) => !v)}
          >
            <CircleHelp aria-hidden="true" className="size-4" />
          </button>
        </div>
        <textarea
          ref={area}
          value={draft}
          rows={6}
          aria-label={t.editorLabel}
          aria-invalid={tooLong || undefined}
          placeholder={t.placeholder}
          readOnly={saving}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Escape") {
              event.stopPropagation();
              onClose();
            } else if (
              event.key === "Enter" &&
              (event.metaKey || event.ctrlKey)
            ) {
              event.preventDefault();
              submit();
            }
          }}
          className="box-border block min-h-[150px] w-full resize-y rounded-b-lg border-0 bg-transparent px-3.5 py-3 text-[14px] leading-[1.55] text-foreground outline-none"
        />
      </div>
      <div className="mt-2.5 flex items-center gap-2">
        <PanelBtn
          variant="primary"
          disabled={tooLong}
          aria-disabled={tooLong || saving}
          onClick={submit}
        >
          {saving ? (
            <Loader2 aria-hidden="true" className="size-4 animate-spin" />
          ) : null}
          {saving ? t.saving : t.save}
        </PanelBtn>
        <PanelBtn variant="ghost" onClick={onClose}>
          {t.cancel}
        </PanelBtn>
        {tooLong ? (
          <span
            role="alert"
            className="flex flex-1 items-start gap-1.5 text-[12.5px] leading-[1.45] font-medium text-danger-text"
          >
            <AlertCircle aria-hidden="true" className="size-[13px] shrink-0" />
            <span>
              {fillPopupCopy(t.overLimit, {
                count: over.toLocaleString(copy.locale),
              })}
            </span>
          </span>
        ) : save?.status === "error" ? (
          <span
            role="alert"
            className="flex flex-1 items-center gap-2 text-[12.5px] font-medium text-danger-text"
          >
            {t.saveFailed}
            <PanelBtn small onClick={save.retry}>
              {t.retry}
            </PanelBtn>
          </span>
        ) : (
          <span className="flex-1" />
        )}
        <span
          className={cn(
            "text-[12.5px] tabular-nums",
            tooLong ? "text-danger-text" : "text-meta-foreground",
          )}
        >
          {fillPopupCopy(t.counter, {
            count: draft.length.toLocaleString(copy.locale),
            max: DESCRIPTION_MAX_LENGTH.toLocaleString(copy.locale),
          })}
        </span>
        <PanelBtn
          small
          aria-pressed={help}
          onClick={() => setHelp((v) => !v)}
          className={help ? "bg-state-hover-strong" : undefined}
        >
          {t.help}
        </PanelBtn>
      </div>
      {help ? (
        <div>
          <HelpTable />
        </div>
      ) : null}
    </>
  );
}

/** The description: Markdown to read, a Markdown editor with a toolbar to write. */
export function DescriptionSection() {
  const { card, copy } = useCardPopup();
  const t = copy.sections.description;
  const canEdit = useCanEdit();
  const [editing, setEditing] = useState(false);
  const text = card.descriptionMd?.trim() ?? "";
  const close = () => setEditing(false);

  // Nothing written: one line, the invitation to write (a reader sees the folded section).
  if (text === "" && !editing)
    return canEdit ? (
      <PopSection label={t.title} icon={SECTION_ICONS.description}>
        <button
          type="button"
          onClick={() => setEditing(true)}
          className="box-border flex h-9 w-full items-center rounded-lg border border-line-strong bg-popover px-3 text-left text-[14px] text-meta-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
        >
          {t.emptyHint}
        </button>
      </PopSection>
    ) : (
      <EmptySection
        label={t.title}
        icon={SECTION_ICONS.description}
        none={copy.shell.noneYet}
      />
    );

  return (
    <PopSection label={t.title} icon={SECTION_ICONS.description}>
      <SectionHead title={t.title}>
        {canEdit && !editing && text !== "" ? (
          <SmallBtn onClick={() => setEditing(true)}>{t.edit}</SmallBtn>
        ) : null}
      </SectionHead>
      {editing && canEdit ? (
        <Editor
          key={card.id}
          initial={card.descriptionMd ?? ""}
          onClose={close}
        />
      ) : text !== "" ? (
        <div
          className={cn(
            "mt-1.5 rounded-lg bg-background px-3 py-2 text-[14px] leading-[1.55]",
            canEdit && "cursor-text",
          )}
          onClick={
            canEdit
              ? (event) => {
                  if (!(event.target as HTMLElement).closest("a"))
                    setEditing(true);
                }
              : undefined
          }
        >
          <Markdown text={text} workspaceId={card.workspaceId} />
        </div>
      ) : null}
    </PopSection>
  );
}
