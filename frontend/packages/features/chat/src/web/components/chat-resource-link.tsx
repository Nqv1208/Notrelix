import { useState, type FormEvent } from "react";
import { Link2, Lock } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import {
  CHAT_LINKABLE_KINDS,
  parseLinkInput,
  resourceHref,
  type ChatLinkableKind,
} from "../../core/model/link-model";
import type { ChatChannelLink, ChatLinkKind } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { KindIcon, kindLabel } from "./chat-link-previews";
import { fillCopy } from "./use-chat-copy";

const CHIP =
  "inline-flex max-w-48 min-w-0 items-center gap-1.5 rounded-md border border-border bg-muted/40 px-2 py-1 text-xs focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none";

/**
 * The link of a channel in its header (UC-CHT-H10). With a title the chip opens the resource; without access it is a generic,
 * non-clickable chip that names only the kind (a hidden, a deleted and a missing resource look the same); a kind with no route
 * here (a Team) is the same generic chip. The resource's own page decides access again when it is opened.
 */
export function ChatLinkChip({
  link,
  workspaceId,
  copy,
}: {
  readonly link: ChatChannelLink;
  readonly workspaceId: string;
  readonly copy: ChatCopy;
}) {
  const kind = kindLabel(link.kind, copy);
  const href = resourceHref(link.kind, workspaceId, link.resourceId);

  if (link.accessible && link.title && href) {
    return (
      <a
        href={href}
        className={cn(CHIP, "text-foreground hover:bg-muted")}
        aria-label={fillCopy(copy.resourceLink.chipOpen, {
          kind,
          title: link.title,
        })}
        data-channel-link="resolved"
      >
        <KindIcon kind={link.kind} />
        <span className="truncate font-medium">{link.title}</span>
        <span className="shrink-0 text-muted-foreground">{kind}</span>
      </a>
    );
  }

  const denied = !link.accessible;
  return (
    <span
      className={cn(CHIP, "text-muted-foreground")}
      title={
        denied
          ? copy.resourceLink.noAccessHint
          : copy.resourceLink.unavailableHint
      }
      data-channel-link={denied ? "no-access" : "unavailable"}
    >
      {denied ? (
        <Lock className="size-3.5 shrink-0" aria-hidden="true" />
      ) : (
        <Link2 className="size-3.5 shrink-0" aria-hidden="true" />
      )}
      <span className="truncate">
        {fillCopy(
          denied ? copy.resourceLink.noAccess : copy.resourceLink.unavailable,
          { kind },
        )}
      </span>
    </span>
  );
}

export interface ChatLinkDialogProps {
  readonly open: boolean;
  readonly copy: ChatCopy;
  readonly workspaceId: string;
  readonly hooks: ChatHooks;
  readonly busy: boolean;
  /** An error from the last save, already worded for the user. */
  readonly error: string | null;
  readonly onOpenChange: (open: boolean) => void;
  readonly onConfirm: (target: {
    readonly kind: ChatLinkKind;
    readonly resourceId: string;
  }) => void;
}

const KIND_FIELD = "chat-link-kind";

/**
 * "Link a resource" in the channel settings (UC-CHT-H10): the manager pastes the address of a Board or Page of this Workspace (or
 * an id and says which kind it is), checks it, sees what it resolves to with their own access, and only then confirms. A resource
 * the manager cannot view cannot be linked: the preview says so and the confirm stays off.
 */
export function ChatLinkDialog({
  open,
  copy,
  workspaceId,
  hooks,
  busy,
  error,
  onOpenChange,
  onConfirm,
}: ChatLinkDialogProps) {
  const text = copy.resourceLink.dialog;
  const [input, setInput] = useState("");
  const [fallbackKind, setFallbackKind] = useState<ChatLinkableKind>(
    "work-management.board",
  );
  const [checked, setChecked] = useState<{
    readonly kind: ChatLinkableKind;
    readonly resourceId: string;
  } | null>(null);
  const [invalid, setInvalid] = useState(false);
  const preview = hooks.useLinkPreview(
    workspaceId,
    checked ?? { kind: fallbackKind, resourceId: "none" },
    checked !== null,
  );
  const canConfirm =
    checked !== null && preview.isSuccess && preview.data.accessible && !busy;

  function handleCheck(event: FormEvent) {
    event.preventDefault();
    const parsed = parseLinkInput(input, workspaceId, fallbackKind);
    setInvalid(parsed === null);
    setChecked(parsed);
  }

  function handleConfirm() {
    if (!checked || !canConfirm) return;
    onConfirm(checked);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleCheck} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>{text.title}</DialogTitle>
            <DialogDescription>{text.description}</DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="chat-link-input">{text.inputLabel}</Label>
            <Input
              id="chat-link-input"
              value={input}
              aria-describedby="chat-link-input-help"
              aria-invalid={invalid}
              onChange={(event) => {
                setInput(event.target.value);
                setChecked(null);
                setInvalid(false);
              }}
              autoFocus
            />
            <p
              id="chat-link-input-help"
              className="text-xs text-muted-foreground"
            >
              {text.inputHelp}
            </p>
          </div>
          <fieldset className="flex flex-col gap-1.5">
            <legend className="text-sm font-medium">{text.kindLabel}</legend>
            <div className="flex flex-wrap gap-4">
              {CHAT_LINKABLE_KINDS.map((kind) => (
                <label key={kind} className="flex items-center gap-2 text-sm">
                  <input
                    type="radio"
                    name={KIND_FIELD}
                    value={kind}
                    checked={fallbackKind === kind}
                    onChange={() => {
                      setFallbackKind(kind);
                      setChecked(null);
                    }}
                  />
                  {kindLabel(kind, copy)}
                </label>
              ))}
            </div>
          </fieldset>
          <div>
            <Button
              type="submit"
              variant="outline"
              size="sm"
              disabled={input.trim().length === 0 || busy}
            >
              {text.check}
            </Button>
          </div>

          <div aria-live="polite" className="min-h-10 text-sm">
            {invalid ? (
              <p role="alert" className="text-destructive">
                {text.invalid}
              </p>
            ) : checked && preview.isPending ? (
              <div role="status" aria-label={text.checking}>
                <Skeleton className="h-8 w-full" />
              </div>
            ) : checked && preview.isError ? (
              <p role="alert" className="text-destructive">
                {text.previewFailed}
              </p>
            ) : checked && preview.isSuccess ? (
              preview.data.accessible && preview.data.title ? (
                <p
                  className="flex items-center gap-2 rounded-md border border-border bg-muted/40 px-3 py-2"
                  data-testid="chat-link-preview"
                >
                  <KindIcon kind={checked.kind} />
                  <span className="sr-only">{text.previewLabel}: </span>
                  <span className="truncate font-medium">
                    {preview.data.title}
                  </span>
                  <span className="shrink-0 text-muted-foreground">
                    {kindLabel(checked.kind, copy)}
                  </span>
                </p>
              ) : (
                <p role="alert" className="text-destructive">
                  {text.previewNoAccess}
                </p>
              )
            ) : null}
          </div>
          {error ? (
            <p role="alert" className="text-sm text-destructive">
              {error}
            </p>
          ) : null}
          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => onOpenChange(false)}
            >
              {text.cancel}
            </Button>
            <Button
              type="button"
              disabled={!canConfirm}
              onClick={handleConfirm}
            >
              {text.confirm}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
