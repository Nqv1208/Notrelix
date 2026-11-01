import * as React from "react";
import { Home, Lock, Search } from "lucide-react";
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FieldError,
  Input,
  RadioGroup,
  RadioGroupItem,
  cn,
} from "@notrelix/ui-web";
import { libraryWindow } from "../../core/model/library";
import {
  buildSpaceIndex,
  filterSpaceIndex,
  spaceDescendantIds,
  spacePathLabel,
  subsetSpaceIndex,
  visibleSpaceRows,
  type MoveTargets,
  type SpaceIndex,
  type SpaceNode,
  type SpaceTreeRow,
} from "../../core/model/spaces";
import { formatCopy } from "../shell/shell-models";
import { SPACE_TYPE_ICON } from "./space-tree-nav";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

/** A Board or a root Doc being moved between Spaces. */
export interface MoveToSpaceResource {
  readonly id: string;
  readonly name: string;
  /** Its current Space, `null` at the workspace level. */
  readonly spaceId: string | null;
}

/** What is being moved; `null` closes the dialog. */
export type MoveToSpaceSubject =
  | { readonly kind: "space"; readonly space: SpaceNode }
  | ({ readonly kind: "board" } & MoveToSpaceResource)
  | ({ readonly kind: "page" } & MoveToSpaceResource);

export interface MoveToSpaceDialogProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly subject: MoveToSpaceSubject | null;
  readonly workspaceName: string | null;
  /** The visible Space tree. */
  readonly nodes: readonly SpaceNode[];
  /** Where the subject may go (`moveTargets`). */
  readonly targets: MoveTargets;
  readonly pending: boolean;
  readonly error: string | null;
  readonly onOpenChange: (open: boolean) => void;
  /** `null` = the workspace level. */
  readonly onSubmit: (targetSpaceId: string | null) => void;
  /** Picker rows above this count are windowed (stories and tests lower it). */
  readonly windowThreshold?: number;
  /** Tells the host which destination is picked (`undefined` while none is), so it can read what the move changes. */
  readonly onChoose?: (targetSpaceId: string | null | undefined) => void;
  /** What the picked destination changes, shown under the list (a Board's access impact). */
  readonly impact?: React.ReactNode;
  /** Actions before the submit button (a second way to move). */
  readonly footerExtra?: React.ReactNode;
  /** Replaces the submit label. */
  readonly submitLabel?: string;
  /** Holds the submit button while the host is still reading the impact. */
  readonly submitBlocked?: boolean;
}

const ROOT_VALUE = "__root__";
const INDENT = 16;
/** Fixed option height (`h-9`), used by windowing. */
const PICKER_ROW_HEIGHT = 36;
/** Height of the windowed scroll region (`max-h-80`). */
const PICKER_WINDOW_HEIGHT = 320;
/** Vertical padding of the scroll region (`p-1`). */
const PICKER_PADDING = 4;
export const PICKER_WINDOW_THRESHOLD = 150;

/**
 * Picks a destination in the Space tree (OT-5) for a Space, a Board or a
 * root Doc. Places the member may not add the subject to stay listed but
 * disabled, with the reason, so the tree keeps its shape; a Space never
 * moves inside itself (WSP-021).
 */
export function MoveToSpaceDialog({
  copy,
  subject,
  workspaceName,
  nodes,
  targets,
  pending,
  error,
  onOpenChange,
  onSubmit,
  windowThreshold = PICKER_WINDOW_THRESHOLD,
  onChoose,
  impact,
  footerExtra,
  submitLabel,
  submitBlocked = false,
}: MoveToSpaceDialogProps) {
  const [text, setText] = React.useState("");
  const [choice, setChoice] = React.useState<string | null>(null);
  const searchId = React.useId();
  const labelId = React.useId();
  const errorId = React.useId();

  React.useEffect(() => {
    if (subject === null) {
      setText("");
      setChoice(null);
    }
  }, [subject]);

  React.useEffect(() => {
    if (!onChoose) return;
    onChoose(
      choice === null ? undefined : choice === ROOT_VALUE ? null : choice,
    );
  }, [choice, onChoose]);

  const active = React.useMemo(
    () => nodes.filter((node) => !node.isArchived),
    [nodes],
  );
  const index = React.useMemo(() => buildSpaceIndex(active), [active]);
  const filter = React.useMemo(
    () => filterSpaceIndex(index, text),
    [index, text],
  );
  const rows = React.useMemo(() => {
    const scope = filter ? subsetSpaceIndex(index, filter.ids) : index;
    return visibleSpaceRows(scope, new Set(scope.byId.keys()));
  }, [filter, index]);

  if (subject === null) {
    return <Dialog open={false} onOpenChange={onOpenChange} />;
  }

  const currentParent =
    subject.kind === "space" ? subject.space.parentId : subject.spaceId;
  const excluded =
    subject.kind === "space"
      ? new Set([
          subject.space.id,
          ...spaceDescendantIds(buildSpaceIndex(nodes), subject.space.id),
        ])
      : new Set<string>();
  const currentValue = currentParent ?? ROOT_VALUE;
  const selected = choice ?? currentValue;
  const canSubmit =
    !pending && !submitBlocked && choice !== null && choice !== currentValue;
  const name = subject.kind === "space" ? subject.space.name : subject.name;

  const reasonFor = (spaceId: string): string | null => {
    if (spaceId === currentParent) return copy.move.current;
    if (excluded.has(spaceId)) return copy.move.descendant;
    if (!targets.spaceIds.has(spaceId)) return copy.move.unavailable;
    return null;
  };
  const rootReason =
    currentParent === null
      ? copy.move.current
      : targets.root
        ? null
        : copy.move.unavailable;

  return (
    <Dialog open onOpenChange={onOpenChange}>
      <DialogContent className="flex max-h-[85vh] flex-col sm:max-w-lg">
        <form
          noValidate
          onSubmit={(event) => {
            event.preventDefault();
            if (!canSubmit || choice === null) return;
            onSubmit(choice === ROOT_VALUE ? null : choice);
          }}
          className="flex min-h-0 flex-col gap-4"
        >
          <DialogHeader>
            <DialogTitle>
              {formatCopy(
                subject.kind === "space"
                  ? copy.move.spaceTitle
                  : subject.kind === "board"
                    ? copy.move.boardTitle
                    : copy.move.pageTitle,
                { name },
              )}
            </DialogTitle>
            <DialogDescription>
              {subject.kind === "space"
                ? copy.move.spaceDescription
                : subject.kind === "board"
                  ? copy.move.description
                  : copy.move.pageDescription}
            </DialogDescription>
          </DialogHeader>
          <div className="relative">
            <label htmlFor={searchId} className="sr-only">
              {copy.move.searchLabel}
            </label>
            <Search
              aria-hidden="true"
              className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground"
            />
            <Input
              id={searchId}
              type="search"
              value={text}
              placeholder={copy.move.searchPlaceholder}
              onChange={(event) => setText(event.target.value)}
              className="h-8 pl-8 max-md:h-touch"
            />
          </div>
          <p id={labelId} className="sr-only">
            {copy.move.pickerLabel}
          </p>
          <PickerList
            rows={rows}
            index={index}
            selected={selected}
            onSelect={setChoice}
            reasonFor={reasonFor}
            rootReason={rootReason}
            rootLabel={
              subject.kind === "space"
                ? formatCopy(copy.move.root, {
                    workspace: workspaceName ?? "",
                  })
                : copy.move.rootBoard
            }
            copy={copy}
            showNoMatches={rows.length === 0 && filter !== null}
            resetKey={text}
            windowThreshold={windowThreshold}
            labelId={labelId}
            describedBy={error ? errorId : undefined}
          />
          {impact}
          {error ? <FieldError id={errorId}>{error}</FieldError> : null}
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              className="max-md:h-touch"
            >
              {copy.move.cancel}
            </Button>
            <Button
              type="submit"
              variant={footerExtra ? "outline" : "default"}
              disabled={!canSubmit}
              className="max-md:h-touch"
            >
              {pending
                ? copy.move.submitting
                : (submitLabel ?? copy.move.submit)}
            </Button>
            {footerExtra}
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

interface PickerListProps {
  readonly rows: readonly SpaceTreeRow[];
  readonly index: SpaceIndex;
  readonly selected: string;
  readonly onSelect: (value: string) => void;
  readonly reasonFor: (spaceId: string) => string | null;
  readonly rootReason: string | null;
  readonly rootLabel: string;
  readonly copy: WorkspaceSpacesCopy;
  readonly showNoMatches: boolean;
  /** Changing it (the search text) scrolls the list back to the top. */
  readonly resetKey: string;
  readonly windowThreshold: number;
  readonly labelId: string;
  readonly describedBy: string | undefined;
}

/**
 * The radio list of destinations. Entry 0 is the workspace level, entry
 * `n + 1` is `rows[n]`. Above `windowThreshold` rows only the visible window
 * is rendered between exact-height `aria-hidden` spacers; the selected and
 * the focused option always stay mounted so the group keeps its value and
 * keyboard focus is never lost.
 */
function PickerList({
  rows,
  index,
  selected,
  onSelect,
  reasonFor,
  rootReason,
  rootLabel,
  copy,
  showNoMatches,
  resetKey,
  windowThreshold,
  labelId,
  describedBy,
}: PickerListProps) {
  const scrollRef = React.useRef<HTMLDivElement>(null);
  const focusPending = React.useRef<string | null>(null);
  const [focusedValue, setFocusedValue] = React.useState<string | null>(null);
  const [scrollTop, setScrollTop] = React.useState(0);

  const entryCount = rows.length + 1;
  const valueAt = (position: number): string =>
    position === 0 ? ROOT_VALUE : rows[position - 1]!.node.id;
  const reasonAt = (position: number): string | null =>
    position === 0 ? rootReason : reasonFor(valueAt(position));
  const positionByValue = React.useMemo(() => {
    const map = new Map<string, number>([[ROOT_VALUE, 0]]);
    rows.forEach((row, position) => map.set(row.node.id, position + 1));
    return map;
  }, [rows]);

  const windowed = rows.length > windowThreshold;
  const range = windowed
    ? libraryWindow({
        rowCount: entryCount,
        rowHeight: PICKER_ROW_HEIGHT,
        scrollTop,
        viewportHeight: PICKER_WINDOW_HEIGHT,
      })
    : { start: 0, end: entryCount };

  // Positions to mount: the window plus the selected and the focused option.
  const mounted: number[] = [];
  const extras = [selected, focusedValue]
    .map((value) => (value === null ? undefined : positionByValue.get(value)))
    .filter(
      (position): position is number =>
        position !== undefined &&
        (position < range.start || position >= range.end),
    );
  for (const position of Array.from(new Set(extras)).sort((x, y) => x - y)) {
    if (position < range.start) mounted.push(position);
  }
  for (let position = range.start; position < range.end; position += 1) {
    mounted.push(position);
  }
  for (const position of Array.from(new Set(extras)).sort((x, y) => x - y)) {
    if (position >= range.end) mounted.push(position);
  }

  React.useEffect(() => {
    setScrollTop(0);
    if (scrollRef.current) scrollRef.current.scrollTop = 0;
  }, [resetKey]);

  React.useEffect(() => {
    const value = focusPending.current;
    if (!value) return;
    const option = Array.from(
      scrollRef.current?.querySelectorAll<HTMLElement>("[data-picker-value]") ??
        [],
    ).find((element) => element.dataset.pickerValue === value);
    if (option) {
      focusPending.current = null;
      option.focus();
    }
  });

  const focusPosition = (position: number) => {
    const value = valueAt(position);
    setFocusedValue(value);
    onSelect(value);
    focusPending.current = value;
    const region = scrollRef.current;
    if (region) {
      const top = PICKER_PADDING + position * PICKER_ROW_HEIGHT;
      const height = region.clientHeight || PICKER_WINDOW_HEIGHT;
      if (top < region.scrollTop) region.scrollTop = top - PICKER_PADDING;
      else if (top + PICKER_ROW_HEIGHT > region.scrollTop + height) {
        region.scrollTop = top + PICKER_ROW_HEIGHT + PICKER_PADDING - height;
      }
      setScrollTop(region.scrollTop);
    }
  };

  /** Arrow/Home/End across the whole list, mounted or not (windowed only);
   * disabled options are skipped like Radix roving focus does. */
  const onKeyDownCapture = (event: React.KeyboardEvent) => {
    if (!windowed) return;
    const current = (event.target as HTMLElement).dataset.pickerValue;
    if (current === undefined) return;
    const from = positionByValue.get(current) ?? 0;
    let step: 1 | -1;
    let start: number;
    switch (event.key) {
      case "ArrowDown":
      case "ArrowRight":
        step = 1;
        start = from + 1;
        break;
      case "ArrowUp":
      case "ArrowLeft":
        step = -1;
        start = from - 1;
        break;
      case "Home":
        step = 1;
        start = 0;
        break;
      case "End":
        step = -1;
        start = entryCount - 1;
        break;
      default:
        return;
    }
    event.preventDefault();
    event.stopPropagation();
    for (let n = 0; n < entryCount; n += 1) {
      const position =
        (((start + step * n) % entryCount) + entryCount) % entryCount;
      if (reasonAt(position) === null) {
        focusPosition(position);
        return;
      }
    }
  };

  const nodes: React.ReactNode[] = [];
  let previous = -1;
  for (const position of mounted) {
    const gap = position - previous - 1;
    if (windowed && gap > 0) {
      nodes.push(<PickerSpacer key={`gap-${position}`} rows={gap} />);
    }
    previous = position;
    if (position === 0) {
      nodes.push(
        <PickerOption
          key={ROOT_VALUE}
          value={ROOT_VALUE}
          selected={selected === ROOT_VALUE}
          disabledReason={rootReason}
          level={0}
          label={rootLabel}
          icon={<Home aria-hidden="true" className="size-4 text-icon" />}
        />,
      );
      continue;
    }
    const row = rows[position - 1]!;
    const Icon = SPACE_TYPE_ICON[row.node.type];
    nodes.push(
      <PickerOption
        key={row.node.id}
        value={row.node.id}
        selected={selected === row.node.id}
        disabledReason={reasonFor(row.node.id)}
        level={row.level}
        label={row.node.name}
        path={spacePathLabel(index, row.node.id)}
        isPrivate={row.node.visibility === "Private"}
        privateLabel={copy.tree.private}
        icon={<Icon aria-hidden="true" className="size-4 text-icon" />}
      />,
    );
  }
  if (windowed && previous < entryCount - 1) {
    nodes.push(<PickerSpacer key="gap-end" rows={entryCount - 1 - previous} />);
  }

  return (
    <RadioGroup
      ref={scrollRef}
      value={selected}
      onValueChange={onSelect}
      aria-labelledby={labelId}
      aria-describedby={describedBy}
      data-testid={windowed ? "move-to-space-window" : undefined}
      onScroll={
        windowed
          ? (event) => setScrollTop(event.currentTarget.scrollTop)
          : undefined
      }
      onKeyDownCapture={onKeyDownCapture}
      onFocus={(event) => {
        const value = (event.target as HTMLElement).dataset.pickerValue;
        if (value !== undefined) setFocusedValue(value);
      }}
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
          setFocusedValue(null);
        }
      }}
      className={cn(
        "min-h-0 flex-1 gap-0 overflow-y-auto rounded-md border border-border p-1",
        windowed && "max-h-80 flex-none overscroll-contain",
      )}
    >
      {nodes}
      {showNoMatches ? (
        <p className="px-2 py-3 text-metadata text-muted-foreground">
          {copy.move.noMatches}
        </p>
      ) : null}
    </RadioGroup>
  );
}

/** Stands in for unmounted options: exact multiples of the option height. */
function PickerSpacer({ rows }: { rows: number }) {
  return (
    <div
      role="none"
      aria-hidden="true"
      style={{ height: rows * PICKER_ROW_HEIGHT }}
    />
  );
}

function PickerOption({
  value,
  selected,
  disabledReason,
  level,
  label,
  path,
  isPrivate = false,
  privateLabel,
  icon,
}: {
  value: string;
  selected: boolean;
  disabledReason: string | null;
  level: number;
  label: string;
  path?: string;
  isPrivate?: boolean;
  privateLabel?: string;
  icon: React.ReactNode;
}) {
  const id = React.useId();
  const disabled = disabledReason !== null;
  return (
    <label
      htmlFor={id}
      style={{ paddingInlineStart: `${level * INDENT + 8}px` }}
      className={cn(
        "flex h-9 items-center gap-2 rounded-md pr-2 max-md:h-touch",
        disabled
          ? "cursor-not-allowed text-muted-foreground"
          : "cursor-pointer hover:bg-state-hover",
        selected && !disabled && "bg-state-selected",
      )}
    >
      <RadioGroupItem
        id={id}
        value={value}
        data-picker-value={value}
        disabled={disabled}
        aria-describedby={disabledReason || path ? `${id}-note` : undefined}
      />
      {icon}
      <span className="min-w-0 flex-1 truncate text-body">{label}</span>
      {isPrivate ? (
        <Lock
          aria-label={privateLabel}
          role="img"
          className="size-3.5 shrink-0 text-muted-foreground"
        />
      ) : null}
      {disabledReason ? (
        <Badge variant="outline" className="shrink-0 font-normal">
          {disabledReason}
        </Badge>
      ) : null}
      {disabledReason || path ? (
        <span id={`${id}-note`} className="sr-only">
          {[path, disabledReason].filter(Boolean).join(". ")}
        </span>
      ) : null}
    </label>
  );
}
