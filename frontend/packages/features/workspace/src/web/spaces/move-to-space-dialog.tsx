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
import {
  buildSpaceIndex,
  filterSpaceIndex,
  spaceDescendantIds,
  spacePathLabel,
  subsetSpaceIndex,
  visibleSpaceRows,
  type MoveTargets,
  type SpaceNode,
} from "../../core/model/spaces";
import { formatCopy } from "../shell/shell-models";
import { SPACE_TYPE_ICON } from "./space-tree-nav";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

/** What is being moved; `null` closes the dialog. */
export type MoveToSpaceSubject =
  | { readonly kind: "space"; readonly space: SpaceNode }
  | {
      readonly kind: "board";
      readonly id: string;
      readonly name: string;
      /** Its current Space, `null` at the workspace level. */
      readonly spaceId: string | null;
    };

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
}

const ROOT_VALUE = "__root__";
const INDENT = 16;

/**
 * Picks a destination in the Space tree (OT-5). Places the member may not
 * add the subject to stay listed but disabled, with the reason, so the tree
 * keeps its shape; a Space never moves inside itself (WSP-021).
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
  const canSubmit = !pending && choice !== null && choice !== currentValue;
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
                  : copy.move.boardTitle,
                { name },
              )}
            </DialogTitle>
            <DialogDescription>
              {subject.kind === "space"
                ? copy.move.spaceDescription
                : copy.move.description}
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
              className="h-8 pl-8"
            />
          </div>
          <p id={labelId} className="sr-only">
            {copy.move.pickerLabel}
          </p>
          <RadioGroup
            value={selected}
            onValueChange={setChoice}
            aria-labelledby={labelId}
            aria-describedby={error ? errorId : undefined}
            className="min-h-0 flex-1 gap-0.5 overflow-y-auto rounded-md border border-border p-1"
          >
            <PickerOption
              value={ROOT_VALUE}
              selected={selected === ROOT_VALUE}
              disabledReason={rootReason}
              level={0}
              label={
                subject.kind === "space"
                  ? formatCopy(copy.move.root, {
                      workspace: workspaceName ?? "",
                    })
                  : copy.move.rootBoard
              }
              icon={<Home aria-hidden="true" className="size-4 text-icon" />}
            />
            {rows.map((row) => {
              const Icon = SPACE_TYPE_ICON[row.node.type];
              return (
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
                  icon={
                    <Icon aria-hidden="true" className="size-4 text-icon" />
                  }
                />
              );
            })}
            {rows.length === 0 && filter ? (
              <p className="px-2 py-3 text-metadata text-muted-foreground">
                {copy.move.noMatches}
              </p>
            ) : null}
          </RadioGroup>
          {error ? <FieldError id={errorId}>{error}</FieldError> : null}
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
            >
              {copy.move.cancel}
            </Button>
            <Button type="submit" disabled={!canSubmit}>
              {pending ? copy.move.submitting : copy.move.submit}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
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
        "flex min-h-9 items-center gap-2 rounded-md py-1.5 pr-2",
        disabled
          ? "cursor-not-allowed text-muted-foreground"
          : "cursor-pointer hover:bg-state-hover",
        selected && !disabled && "bg-state-selected",
      )}
    >
      <RadioGroupItem
        id={id}
        value={value}
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
