import {
  Fragment,
  useRef,
  type ClipboardEvent,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { Check, CircleAlert, Lock, Table2 } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { BlockType } from "@notrelix/docs-core";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import {
  isRestrictedTask,
  type DocBlockView,
  type DocChip,
  type DocOverlay,
  type DocPageCallbacks,
  type DocSegment,
  type DocTaskRef,
  type DocTextBlock,
  type DocTaskResolution,
} from "./doc-page-models";
import { serializeEditable } from "./doc-page-text";
import { DocStatusDot, ICON_CLASS } from "./doc-page-ui";
import {
  DocMentionMenuSurface,
  DocSlashMenuSurface,
} from "./doc-popups-surface";

const CHIP =
  "inline-flex h-6.5 items-center gap-1.5 rounded-[7px] bg-state-hover px-[9px] text-crumb leading-[1.45] font-medium text-muted-foreground no-underline";

/** A referenced task inside text (Final-Docs `.task`): dot, key, title. */
export function DocTaskChip({
  task,
  copy,
}: {
  task: DocTaskResolution;
  copy: DocPageCopy;
}) {
  const base =
    "inline-flex h-6.5 items-center gap-1.5 rounded-[6px] border border-line-strong bg-state-hover py-0 pr-2 pl-[7px] align-[1px] text-compact leading-none font-medium text-foreground no-underline";
  if (isRestrictedTask(task)) {
    return (
      <span
        data-task-id={task.id}
        contentEditable={false}
        className={cn(base, "text-meta-foreground italic")}
      >
        <Lock aria-hidden="true" className="size-3.5 shrink-0 stroke-[1.8]" />
        {copy.panel.restrictedTask}
      </span>
    );
  }
  const body = (
    <>
      <DocStatusDot status={task.status} />
      <span className="font-mono text-micro leading-none font-medium text-meta-foreground">
        {task.key}
      </span>
      <span className="max-w-[300px] truncate">{task.title}</span>
    </>
  );
  return task.href ? (
    <a
      data-task-id={task.id}
      contentEditable={false}
      href={task.href}
      className={base}
    >
      {body}
    </a>
  ) : (
    <span data-task-id={task.id} contentEditable={false} className={base}>
      {body}
    </span>
  );
}

function renderSegments(
  segments: readonly DocSegment[],
  copy: DocPageCopy,
): ReactNode {
  return segments.map((segment, index) => {
    switch (segment.kind) {
      case "text":
        return <Fragment key={index}>{segment.text}</Fragment>;
      case "task":
        return <DocTaskChip key={index} task={segment.task} copy={copy} />;
      case "mark":
        return (
          <mark
            key={index}
            className="rounded-[3px] bg-doc-highlight text-inherit"
          >
            {segment.text}
          </mark>
        );
      case "ins":
        return (
          <ins
            key={index}
            className="rounded-[2px] bg-doc-chip-done px-px text-inherit underline decoration-work-status-done decoration-[1.5px] underline-offset-3"
          >
            {segment.text}
          </ins>
        );
      case "del":
        return (
          <del
            key={index}
            className="text-meta-foreground line-through decoration-meta-foreground"
          >
            {segment.text}
          </del>
        );
    }
  });
}

function segmentsKey(segments: readonly DocSegment[]): string {
  return segments
    .map((segment) =>
      segment.kind === "task" ? `{${segment.task.id}}` : segment.text,
    )
    .join("");
}

interface EditableProps {
  readonly as: "p" | "li" | "h1" | "h2" | "h3" | "span" | "div";
  readonly blockId: string;
  readonly segments: readonly DocSegment[];
  readonly label: string;
  readonly placeholder?: string;
  readonly readOnly: boolean;
  readonly copy: DocPageCopy;
  readonly callbacks?: DocPageCallbacks;
  readonly className?: string;
  readonly multiline?: boolean;
}

/**
 * One editable text run. The DOM is the source while the caret is inside;
 * the stored text is read back on blur or Enter. Typing "/" at the start
 * opens the block menu; "@" opens the task mention list.
 */
function DocEditable({
  as: Tag,
  blockId,
  segments,
  label,
  placeholder,
  readOnly,
  copy,
  callbacks,
  className,
}: EditableProps) {
  const ref = useRef<HTMLElement | null>(null);
  const committed = useRef(segmentsKey(segments));

  const read = () => (ref.current ? serializeEditable(ref.current) : "");

  const commit = () => {
    const text = read();
    if (text !== committed.current) {
      committed.current = text;
      callbacks?.onBlockCommit?.(blockId, text);
    }
  };

  /** The stored text from the start of the block to the caret. */
  const readBeforeCaret = () => {
    const element = ref.current;
    const selection = window.getSelection();
    if (!element || !selection || selection.rangeCount === 0) return read();
    const caret = selection.getRangeAt(0);
    if (!element.contains(caret.endContainer)) return read();
    const head = document.createRange();
    head.selectNodeContents(element);
    head.setEnd(caret.endContainer, caret.endOffset);
    const holder = document.createElement("div");
    holder.appendChild(head.cloneContents());
    return serializeEditable(holder);
  };

  const onInput = () => {
    const text = read();
    const before = readBeforeCaret();
    if (text.startsWith("/")) {
      callbacks?.onBlockTrigger?.(blockId, {
        kind: "slash",
        query: text.slice(1),
        text,
        before,
      });
      return;
    }
    const at = before.lastIndexOf("@");
    if (at >= 0 && !/\s/.test(before.slice(at + 1))) {
      callbacks?.onBlockTrigger?.(blockId, {
        kind: "mention",
        query: before.slice(at + 1),
        text,
        before,
      });
      return;
    }
    callbacks?.onBlockTriggerClose?.();
  };

  const onKeyDown = (event: KeyboardEvent<HTMLElement>) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      commit();
      callbacks?.onAddBlock?.("paragraph");
      return;
    }
    if (
      event.key === "Backspace" &&
      read() === "" &&
      callbacks?.onBlockDeleteEmpty
    ) {
      event.preventDefault();
      callbacks.onBlockDeleteEmpty(blockId);
    }
  };

  const onPaste = (event: ClipboardEvent<HTMLElement>) => {
    event.preventDefault();
    const text = event.clipboardData.getData("text/plain").replace(/\s+/g, " ");
    document.execCommand("insertText", false, text);
  };

  return (
    <Tag
      ref={(node: HTMLElement | null) => {
        ref.current = node;
      }}
      key={segmentsKey(segments)}
      role={Tag === "h2" || Tag === "h3" ? undefined : "textbox"}
      aria-multiline={Tag === "h2" || Tag === "h3" ? undefined : "false"}
      aria-label={label}
      aria-readonly={
        readOnly && Tag !== "h2" && Tag !== "h3" ? true : undefined
      }
      contentEditable={readOnly ? false : "plaintext-only"}
      suppressContentEditableWarning
      spellCheck={false}
      data-placeholder={placeholder}
      data-block-id={blockId}
      onInput={readOnly ? undefined : onInput}
      onKeyDown={readOnly ? undefined : onKeyDown}
      onPaste={readOnly ? undefined : onPaste}
      onFocus={() => callbacks?.onFocusBlock?.(blockId)}
      onBlur={() => {
        commit();
      }}
      className={cn(
        "outline-none empty:before:text-meta-foreground empty:before:content-[attr(data-placeholder)] focus-visible:outline-none",
        className,
      )}
    >
      {renderSegments(segments, copy)}
    </Tag>
  );
}

function DueText({
  due,
  copy,
}: {
  due: NonNullable<DocTaskRef["due"]>;
  copy: DocPageCopy;
}) {
  const b = copy.blocks.due;
  switch (due.kind) {
    case "overdue":
      return (
        <span
          role="cell"
          className="text-crumb font-semibold text-danger-text tabular-nums"
        >
          {fillDocCopy(due.days === 1 ? b.overdue_one : b.overdue_other, {
            count: due.days,
          })}
        </span>
      );
    case "today":
      return (
        <span
          role="cell"
          className="text-crumb font-semibold text-warning-text tabular-nums"
        >
          {b.today}
        </span>
      );
    case "done":
      return (
        <span
          role="cell"
          className="text-crumb text-meta-foreground tabular-nums"
        >
          {fillDocCopy(b.doneOn, { date: due.label })}
        </span>
      );
    case "date":
      return (
        <span
          role="cell"
          className="text-crumb text-meta-foreground tabular-nums"
        >
          {due.label}
        </span>
      );
  }
}

const CHIP_STATUS_CLASS = {
  todo: "bg-doc-chip-todo",
  doing: "bg-doc-chip-doing",
  review: "bg-doc-chip-review",
  done: "bg-doc-chip-done",
} as const;

/** The embedded live task table (Final-Docs `.live` and its note). */
function DocLiveTable({
  block,
  copy,
}: {
  block: Extract<DocBlockView, { kind: "liveTable" }>;
  copy: DocPageCopy;
}) {
  const noteId = `live-note-${block.id}`;
  const restricted = block.tasks.filter(isRestrictedTask);
  const visible = block.tasks.filter(
    (task): task is DocTaskRef => !isRestrictedTask(task),
  );
  const headerLink = block.boardHref ? (
    <a
      href={block.boardHref}
      className="ml-auto font-semibold text-brand-subtle-foreground no-underline"
    >
      {copy.blocks.openInTable}
    </a>
  ) : null;

  return (
    <>
      <div className="mt-1.5 mb-5 overflow-hidden rounded-[12px] border border-line-strong bg-doc-panel">
        <div className="flex flex-wrap items-center gap-2 border-b border-border px-3.5 py-2.5 text-compact leading-[1.45] text-meta-foreground">
          <Table2
            aria-hidden="true"
            className="size-3.5 shrink-0 stroke-[1.8]"
          />
          <b className="font-semibold text-foreground">{block.boardName}</b>·{" "}
          {block.filterLabel}
          {headerLink}
        </div>
        {visible.length === 0 && restricted.length === 0 ? (
          <div className="px-3.5 py-3 text-nav text-meta-foreground leading-[1.45]">
            {copy.blocks.liveEmpty}
          </div>
        ) : null}
        <div
          role="table"
          aria-label={copy.blocks.liveTable}
          aria-describedby={noteId}
        >
          {visible.map((task) => (
            <div
              key={task.id}
              role="row"
              className="box-content grid min-h-[42px] grid-cols-[76px_minmax(0,1fr)_104px_32px_108px] items-center gap-2.5 border-b border-border px-3.5 text-nav leading-[1.45] font-normal"
            >
              <span role="cell">
                <a
                  href={task.href}
                  tabIndex={-1}
                  className="font-mono text-metadata text-meta-foreground no-underline leading-[1.45] tracking-normal"
                >
                  {task.key}
                </a>
              </span>
              <span role="cell" className="truncate font-medium">
                <a
                  href={task.href}
                  className="text-inherit no-underline hover:underline"
                >
                  {task.title}
                </a>
              </span>
              <span role="cell">
                <span
                  className={cn(
                    "inline-flex h-5.5 w-max items-center gap-1.5 rounded-full px-2 text-metadata font-semibold leading-[1.45] tracking-normal",
                    CHIP_STATUS_CLASS[task.status],
                  )}
                >
                  <DocStatusDot status={task.status} />
                  {copy.blocks.tableStatus[task.status]}
                </span>
              </span>
              <span role="cell">
                {task.assignee ? (
                  <span
                    aria-hidden="true"
                    className={cn(
                      "grid size-6 place-items-center rounded-full text-mark-xs font-bold",
                      TONE_CLASS[task.assignee.tone],
                    )}
                  >
                    {task.assignee.initials}
                  </span>
                ) : null}
              </span>
              {task.due ? (
                <DueText due={task.due} copy={copy} />
              ) : (
                <span role="cell" />
              )}
            </div>
          ))}
          {restricted.length > 0 ? (
            <div
              role="row"
              className="box-content grid min-h-[42px] grid-cols-[76px_minmax(0,1fr)] items-center gap-2.5 px-3.5 text-nav leading-[1.45] font-normal text-meta-foreground"
            >
              <span role="cell">
                <Lock
                  aria-hidden="true"
                  className="size-3.5 shrink-0 stroke-[1.8]"
                />
              </span>
              <span
                role="cell"
                className="font-normal whitespace-normal italic"
              >
                {fillDocCopy(copy.blocks.restricted, {
                  count: restricted.length,
                })}
              </span>
            </div>
          ) : null}
        </div>
      </div>
      <p
        id={noteId}
        className="-mt-3 mb-5 flex items-start gap-[7px] text-crumb leading-[1.5] text-meta-foreground"
      >
        <Lock
          aria-hidden="true"
          className="mt-0.5 size-3.5 shrink-0 stroke-[1.8]"
        />
        {copy.blocks.liveNote}
      </p>
    </>
  );
}

const TONE_CLASS = {
  blue: "bg-mark-blue text-mark-blue-foreground",
  gold: "bg-mark-gold text-mark-gold-foreground",
  coral: "bg-mark-coral text-mark-coral-foreground",
  sage: "bg-mark-sage text-mark-sage-foreground",
  crimson: "bg-mark-crimson text-mark-crimson-foreground",
  violet: "bg-mark-violet text-mark-violet-foreground",
} as const;

export interface DocArticleSurfaceProps {
  readonly copy: DocPageCopy;
  readonly title: string;
  readonly chips: readonly DocChip[];
  /** Why the viewer sees the page (an admin seeing a private department). */
  readonly why: string | null;
  readonly blocks: readonly DocBlockView[];
  readonly readOnly?: boolean;
  /** Version compare: struck-through checked items are not struck. */
  readonly compare?: boolean;
  readonly overlay: DocOverlay;
  readonly mentionTasks: readonly DocTaskRef[];
  readonly callbacks?: DocPageCallbacks;
}

/** The page sheet (Final-Docs `.doc`): title, chips and the block flow. */
export function DocArticleSurface({
  copy,
  title,
  chips,
  why,
  blocks,
  readOnly = false,
  compare = false,
  overlay,
  mentionTasks,
  callbacks,
}: DocArticleSurfaceProps) {
  const titleRef = useRef<HTMLHeadingElement | null>(null);
  const common = { copy, callbacks, readOnly };

  const activeBlock = (blockId: string) =>
    (overlay.kind === "slash" || overlay.kind === "mention") &&
    overlay.blockId === blockId
      ? ""
      : undefined;

  const popupFor = (blockId: string, mentionClass: string) => {
    if (overlay.kind === "slash" && overlay.blockId === blockId) {
      return (
        <DocSlashMenuSurface
          copy={copy}
          query={overlay.query}
          className="top-9 left-0"
          onSelect={(type: BlockType) =>
            callbacks?.onSlashSelect?.(blockId, type)
          }
          onClose={callbacks?.onBlockTriggerClose}
        />
      );
    }
    if (overlay.kind === "mention" && overlay.blockId === blockId) {
      return (
        <DocMentionMenuSurface
          copy={copy}
          query={overlay.query}
          tasks={mentionTasks}
          className={mentionClass}
          onSelect={(taskId) => callbacks?.onMentionSelect?.(blockId, taskId)}
          onClose={callbacks?.onBlockTriggerClose}
        />
      );
    }
    return null;
  };

  const rendered: ReactNode[] = [];
  for (let index = 0; index < blocks.length;) {
    const block = blocks[index];
    if (!block) break;
    const kind = block.kind;

    if (block.kind === "numbered" || block.kind === "bulleted") {
      const run: DocTextBlock[] = [];
      while (index < blocks.length) {
        const next = blocks[index];
        if (!next || next.kind !== block.kind) break;
        run.push(next);
        index += 1;
      }
      const List = block.kind === "numbered" ? "ol" : "ul";
      rendered.push(
        <List
          key={`${run[0]?.id}:${kind}`}
          className={cn(
            "mb-3.5 pl-[22px] text-doc-list text-doc-body",
            block.kind === "numbered" ? "list-decimal" : "list-disc",
          )}
        >
          {run.map((item) => (
            <li
              key={item.id}
              data-block={item.id}
              data-active-block={activeBlock(item.id)}
              className={cn(
                "relative mb-1 pl-1",
                item.inserted && "bg-doc-chip-done",
              )}
            >
              <DocEditable
                as="span"
                blockId={item.id}
                segments={item.segments}
                label={copy.blockPlaceholder}
                placeholder={copy.blockPlaceholder}
                {...common}
              />
              {popupFor(item.id, "top-[34px] left-[280px]")}
            </li>
          ))}
        </List>,
      );
      continue;
    }

    index += 1;
    switch (block.kind) {
      case "heading": {
        const Heading = block.level === 3 ? "h3" : "h2";
        rendered.push(
          <div
            key={block.id}
            data-block={block.id}
            data-active-block={activeBlock(block.id)}
            className="relative"
          >
            <DocEditable
              as={Heading}
              blockId={block.id}
              segments={[{ kind: "text", text: block.text }]}
              label={block.text || copy.blockPlaceholder}
              placeholder={copy.blockPlaceholder}
              className={cn(
                "mt-7 mb-2.5 text-foreground",
                block.level === 1
                  ? "text-page-title"
                  : block.level === 3
                    ? "text-subheading font-semibold"
                    : "text-doc-heading",
              )}
              {...common}
            />
            {popupFor(block.id, "top-[34px] left-[280px]")}
          </div>,
        );
        break;
      }
      case "paragraph":
      case "quote": {
        rendered.push(
          <div
            key={block.id}
            data-block={block.id}
            data-active-block={activeBlock(block.id)}
            className="relative"
          >
            <DocEditable
              as="p"
              blockId={block.id}
              segments={block.segments}
              label={copy.blockPlaceholder}
              placeholder={copy.blockPlaceholder}
              className={cn(
                "mb-3.5 max-w-[68ch] text-doc-prose text-pretty text-doc-body",
                block.kind === "quote" &&
                  "border-l-2 border-line-strong pl-4 text-muted-foreground italic",
                block.format?.bold && "font-bold",
                block.format?.italic && "italic",
                block.format?.underline && "underline",
                block.format?.strike && "line-through",
                block.format?.align === "center" && "text-center",
                block.format?.align === "right" && "text-right",
                block.inserted && "bg-doc-chip-done",
              )}
              {...common}
            />
            {popupFor(block.id, "top-[34px] left-[280px]")}
          </div>,
        );
        break;
      }
      case "todo": {
        rendered.push(
          <div
            key={block.id}
            data-block={block.id}
            className={cn(
              "group relative -mx-2 flex min-h-9 items-center gap-2.5 rounded-[8px] px-2 text-doc-check text-doc-body hover:bg-state-hover focus-within:bg-state-hover",
              block.inserted && "bg-doc-chip-done hover:bg-doc-chip-done",
            )}
          >
            {compare && block.inserted ? (
              <span className="sr-only">{copy.blocks.addedSince}</span>
            ) : null}
            <button
              type="button"
              role="checkbox"
              aria-checked={block.checked}
              aria-label={fillDocCopy(copy.blocks.checkbox, {
                name: block.text,
              })}
              aria-disabled={readOnly || undefined}
              onClick={
                readOnly
                  ? undefined
                  : () => callbacks?.onTodoToggle?.(block.id, !block.checked)
              }
              className={cn(
                "box-border grid size-[17px] shrink-0 cursor-pointer place-items-center rounded-[5px] border-[1.5px] border-line-strong bg-transparent p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                block.checked &&
                  "border-work-status-done bg-work-status-done text-white",
              )}
            >
              {block.checked ? (
                <Check aria-hidden="true" className="size-[11px] stroke-3" />
              ) : null}
            </button>
            <DocEditable
              as="span"
              blockId={block.id}
              segments={[{ kind: "text", text: block.text }]}
              label={block.text || copy.blockPlaceholder}
              placeholder={copy.blockPlaceholder}
              className={cn(
                "min-w-0",
                block.checked &&
                  !compare &&
                  "text-meta-foreground line-through",
                block.inserted &&
                  "underline decoration-work-status-done decoration-[1.5px] underline-offset-3",
              )}
              {...common}
            />
            {block.checked ? null : (
              <button
                type="button"
                aria-label={fillDocCopy(copy.blocks.convertToTaskNamed, {
                  name: block.text,
                })}
                aria-disabled={callbacks?.onTodoConvert ? undefined : true}
                title={
                  callbacks?.onTodoConvert
                    ? undefined
                    : copy.blocks.convertUnavailable
                }
                onClick={
                  callbacks?.onTodoConvert
                    ? () => callbacks.onTodoConvert?.(block.id)
                    : undefined
                }
                className="ml-auto inline-flex h-7 items-center gap-[5px] rounded-[7px] border border-line-strong bg-doc-panel px-2.5 text-crumb leading-[1.45] font-semibold text-brand-subtle-foreground opacity-0 outline-none group-focus-within:opacity-100 group-hover:opacity-100 focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default"
              >
                <svg
                  aria-hidden="true"
                  viewBox="0 0 24 24"
                  className={cn(
                    ICON_CLASS,
                    "size-[13px] fill-none stroke-current",
                  )}
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <path d="M9 11l3 3 8-8" />
                  <path d="M20 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
                </svg>
                {copy.blocks.convertToTask}
              </button>
            )}
          </div>,
        );
        break;
      }
      case "callout": {
        rendered.push(
          <div
            key={block.id}
            data-block={block.id}
            className="mt-1.5 mb-[18px] flex gap-3 rounded-[10px] bg-doc-notice px-4 py-3.5 text-doc-callout text-doc-body"
          >
            <CircleAlert
              aria-hidden="true"
              className="mt-[3px] size-[17px] shrink-0 stroke-[1.8] text-brand-solid"
            />
            <DocEditable
              as="div"
              blockId={block.id}
              segments={[{ kind: "text", text: block.text }]}
              label={copy.blockPlaceholder}
              placeholder={copy.blockPlaceholder}
              className="min-w-0 flex-1"
              {...common}
            />
          </div>,
        );
        break;
      }
      case "code": {
        rendered.push(
          <div key={block.id} data-block={block.id} className="mb-3.5">
            <DocEditable
              as="div"
              blockId={block.id}
              segments={[{ kind: "text", text: block.text }]}
              label={copy.blockPlaceholder}
              placeholder={copy.blockPlaceholder}
              className="rounded-[8px] bg-state-hover px-3 py-2.5 font-mono text-compact text-foreground leading-[1.45]"
              {...common}
            />
          </div>,
        );
        break;
      }
      case "divider": {
        rendered.push(<hr key={block.id} className="my-4 border-border" />);
        break;
      }
      case "liveTable": {
        rendered.push(
          <DocLiveTable key={block.id} block={block} copy={copy} />,
        );
        break;
      }
    }
  }

  return (
    <article className="relative w-full max-w-[700px] self-start">
      <h1
        ref={titleRef}
        aria-label={copy.titlePlaceholder}
        contentEditable={readOnly ? false : "plaintext-only"}
        suppressContentEditableWarning
        spellCheck={false}
        data-placeholder={copy.titlePlaceholder}
        onKeyDown={(event) => {
          if (event.key === "Enter") {
            event.preventDefault();
            event.currentTarget.blur();
          }
        }}
        onBlur={(event) => {
          const next = (event.currentTarget.textContent ?? "").trim();
          if (next && next !== title) callbacks?.onTitleCommit?.(next);
          else event.currentTarget.textContent = title;
        }}
        className="text-doc-title text-balance text-foreground outline-none empty:before:text-meta-foreground empty:before:content-[attr(data-placeholder)]"
      >
        {title}
      </h1>
      <div className="mt-3 mb-[26px] flex flex-wrap gap-2">
        {chips.map((chip) =>
          chip.href ? (
            <a key={chip.id} href={chip.href} className={CHIP}>
              <ChipIcon kind={chip.kind} />
              {chip.label}
            </a>
          ) : (
            <span key={chip.id} className={CHIP}>
              <ChipIcon kind={chip.kind} />
              {chip.label}
            </span>
          ),
        )}
        {why ? (
          <span className="self-center text-crumb text-meta-foreground">
            {why}
          </span>
        ) : null}
      </div>
      {blocks.length === 0 ? (
        <button
          type="button"
          onClick={() => callbacks?.onAddBlock?.("paragraph")}
          className="mb-3.5 border-0 bg-transparent p-0 text-left text-doc-prose text-meta-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
        >
          {copy.blocks.emptyDoc}
        </button>
      ) : null}
      {rendered}
    </article>
  );
}

function ChipIcon({ kind }: { kind: DocChip["kind"] }) {
  if (kind === "date") return null;
  return kind === "scope" ? (
    <Lock aria-hidden="true" className="size-[13px] shrink-0 stroke-[1.8]" />
  ) : (
    <Table2 aria-hidden="true" className="size-[13px] shrink-0 stroke-[1.8]" />
  );
}
