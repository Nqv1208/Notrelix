import {
  useEffect,
  useId,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type ClipboardEvent,
  type DragEvent,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import {
  Hash,
  Megaphone,
  Mic,
  Paperclip,
  SendHorizontal,
  UserPlus,
  X,
} from "lucide-react";
import { Button, Textarea, cn } from "@notrelix/ui-web";
import {
  checkMessageContent,
  chatTextLength,
  showsCounter,
} from "../../core/model/chat-model";
import {
  activeTrigger,
  buildMentionInputs,
  filterByQuery,
  insertMention,
  mentionWarnings,
  needsBroadcastConfirm,
  typesBroadcast,
  type ActiveTrigger,
  type MentionWarning,
  type TrackedMention,
} from "../../core/model/mention-model";
import type { ChatVoiceRecorder } from "../../core/api/chat-voice-recorder";
import { createBrowserVoiceRecorder } from "../voice/browser-voice-recorder";
import { VoiceRecorderPanel } from "./chat-voice-recorder";
import {
  checkPendingFiles,
  formatFileSize,
  type PendingFileRefusal,
} from "../../core/model/attachment-model";
import {
  CHAT_LIMITS,
  type ChatMentionPayload,
  type ChatMentionables,
} from "../../core/types/chat";
import {
  activeShortcode,
  insertAtSelection,
  insertShortcode,
  shortcodeSuggestions,
  type ShortcodeTrigger,
} from "../../core/model/emoji-model";
import type { EmojiEntry } from "../../core/model/emoji-data";
import type { ChatCopy } from "./chat.copy";
import { rememberEmoji } from "./chat-emoji-recent";
import { EmojiPickerButton } from "./chat-emoji-picker";
import { AttachmentGlyphIcon } from "./chat-attachments";
import { fillCopy } from "./use-chat-copy";
import { formatWait } from "./chat-rate-limit";

/** What the composer needs to mention people and link channels (UC-CHT-G01 to G04); without it @ and # are plain text. */
export interface ChatComposerMentions {
  /** The caller, left out of the list. */
  readonly selfId: string;
  readonly mentionables: ChatMentionables | undefined;
  readonly status: "loading" | "ready" | "error";
  /** Names of the Workspace members by user id; a person without a name is not offered. */
  readonly people: ReadonlyMap<string, string>;
  /** The channels the caller can see (# picker). */
  readonly channels: readonly { readonly id: string; readonly name: string }[];
  /** Keeps the picked mentions of each draft apart while the page is open. */
  readonly draftKey: string;
  /** Adds a person to the channel (offered only when the caller may). */
  readonly onAddMember?: (userId: string) => void;
  readonly addingUserId?: string | null;
  readonly addFailedUserId?: string | null;
}

type PickerItem = {
  readonly id: string;
  readonly group: "people" | "broadcast" | "channels";
  /** The text the draft gets. */
  readonly label: string;
  readonly name: string;
  readonly description: string | null;
  readonly tracked: TrackedMention | null;
};

/** Files can be attached to the message (UC-CHT-G08); without it the composer has no attach control. */
export interface ChatComposerAttachments {
  /** Keeps the pending files of each draft apart while the page is open. */
  readonly draftKey: string;
  /** The voice recorder port (UC-CHT-I04); the browser recorder when the host gives none. Tests pass a fake. */
  readonly voiceRecorder?: ChatVoiceRecorder;
  /** False where a voice message cannot be sent (a thread reply): the composer shows no microphone. */
  readonly voice?: boolean;
}

export interface ChatComposerProps {
  readonly channelName: string;
  readonly value: string;
  /** Why posting is refused here (archived, or not a member); the composer is disabled with it. */
  readonly disabledReason: string | null;
  readonly copy: ChatCopy;
  readonly onChange: (value: string) => void;
  /**
   * The trimmed content; the payload is given only when the content carries mentions and the files only
   * when some are attached (they are sent to the message once it is stored).
   */
  readonly onSend: (
    content: string,
    payload?: ChatMentionPayload,
    files?: readonly File[],
  ) => void;
  readonly mentions?: ChatComposerMentions;
  readonly attachments?: ChatComposerAttachments;
  /** Above the text: the reply chip of a quote reply. */
  readonly leading?: ReactNode;
  /** In the footer before Send: the thread composer's "also send to channel". */
  readonly footerExtra?: ReactNode;
  /** The accessible name and placeholder; the channel composer names the channel by default. */
  readonly labelOverride?: string;
  /** Sends are refused while a send is in flight. */
  readonly busy?: boolean;
  /** Called on each edit of a non-empty draft (the typing signal; the host throttles it). */
  readonly onTyping?: () => void;
  /** The channel's slow mode in seconds (UC-CHT-H08); above 0 the hint line says so. */
  readonly slowModeSeconds?: number;
}

/**
 * The message composer (UC-CHT-B01). Enter sends, Shift+Enter starts a new line,
 * the counter appears near the 4,000 character limit, and the composer is
 * disabled with the reason in an archived channel or when the caller is not in it.
 */
export function ChatComposer({
  channelName,
  value,
  disabledReason,
  copy,
  onChange,
  onSend,
  leading,
  footerExtra,
  labelOverride,
  busy = false,
  mentions,
  onTyping,
  attachments,
  slowModeSeconds = 0,
}: ChatComposerProps) {
  const hintId = useId();
  const listId = useId();
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const [caret, setCaret] = useState(0);
  const [activeIndex, setActiveIndex] = useState(0);
  /** The trigger the user closed (Escape, or a pick): it stays closed until its text changes. */
  const [dismissed, setDismissed] = useState<{
    readonly start: number;
    readonly query: string;
  } | null>(null);
  /** The `:shortcode` the user closed: it stays closed until its text changes. */
  const [dismissedCode, setDismissedCode] = useState<ShortcodeTrigger | null>(
    null,
  );
  const [trackedByDraft, setTrackedByDraft] = useState<
    Readonly<Record<string, readonly TrackedMention[]>>
  >({});
  const [confirming, setConfirming] = useState<{
    readonly content: string;
    readonly payload: ChatMentionPayload;
    readonly token: string;
  } | null>(null);
  const pendingCaret = useRef<number | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [filesByDraft, setFilesByDraft] = useState<
    Readonly<Record<string, readonly File[]>>
  >({});
  const [refusals, setRefusals] = useState<
    readonly { readonly name: string; readonly reason: PendingFileRefusal }[]
  >([]);
  const [dragging, setDragging] = useState(false);
  const filesKey = attachments?.draftKey ?? "";
  const files = filesByDraft[filesKey] ?? [];
  const [voiceOpen, setVoiceOpen] = useState(false);
  const browserRecorder = useMemo(() => createBrowserVoiceRecorder(), []);
  const voiceRecorder = attachments?.voiceRecorder ?? browserRecorder;
  const draftKey = mentions?.draftKey ?? "";
  const tracked = trackedByDraft[draftKey] ?? [];
  const length = chatTextLength(value);
  const check = checkMessageContent(value);
  const tooLong = length > CHAT_LIMITS.messageMaxLength;
  const disabled = disabledReason !== null;
  // The microphone shows while the input is empty and there is no pending file; with text it is the send button.
  const showMic =
    attachments !== undefined &&
    attachments.voice !== false &&
    !check.ok &&
    files.length === 0;
  const label =
    labelOverride ?? fillCopy(copy.composer.label, { channel: channelName });

  // A closed confirmation does not outlive the text it was for.
  useEffect(() => {
    setConfirming(null);
  }, [value]);

  useLayoutEffect(() => {
    if (pendingCaret.current === null) return;
    textareaRef.current?.setSelectionRange(
      pendingCaret.current,
      pendingCaret.current,
    );
    pendingCaret.current = null;
  }, [value]);

  const trigger: ActiveTrigger | null = useMemo(() => {
    if (!mentions || disabled) return null;
    const found = activeTrigger(value, caret);
    if (!found) return null;
    return dismissed &&
      dismissed.start === found.start &&
      dismissed.query === found.query
      ? null
      : found;
  }, [mentions, disabled, value, caret, dismissed]);

  const items: readonly PickerItem[] = useMemo(() => {
    if (!mentions || !trigger) return [];
    return buildItems(trigger, mentions, copy);
  }, [mentions, trigger, copy]);
  const open = trigger !== null && items.length > 0;
  const status =
    trigger?.trigger === "@" && mentions ? mentions.status : "ready";

  // :shortcode and @ / # are exclusive: the emoji list opens only while the mention list shows nothing.
  const shortcode: ShortcodeTrigger | null = useMemo(() => {
    if (disabled || open) return null;
    if (
      trigger !== null &&
      !(status === "ready" && trigger.query.includes(" "))
    ) {
      return null;
    }
    const found = activeShortcode(value, caret);
    if (!found) return null;
    return dismissedCode &&
      dismissedCode.start === found.start &&
      dismissedCode.query === found.query
      ? null
      : found;
  }, [disabled, open, trigger, status, value, caret, dismissedCode]);
  const emojiItems: readonly EmojiEntry[] = useMemo(
    () => (shortcode ? shortcodeSuggestions(shortcode.query) : []),
    [shortcode],
  );
  const emojiOpen = emojiItems.length > 0;
  const listCount = open ? items.length : emojiItems.length;
  const safeIndex = Math.min(activeIndex, Math.max(listCount - 1, 0));
  const optionId = (index: number) => `${listId}-option-${index}`;

  const trimmed = value.trim();
  const sendCheck = check.ok ? check.content : "";
  const inputs = useMemo(
    () =>
      mentions && sendCheck !== ""
        ? buildMentionInputs(sendCheck, tracked, {
            canBroadcast: mentions.mentionables?.canBroadcast ?? false,
          })
        : [],
    [mentions, sendCheck, tracked],
  );
  const warnings = useMemo(
    () =>
      mentions
        ? mentionWarnings(
            inputs,
            tracked,
            mentions.mentionables,
            sendCheck,
            typesBroadcast(trimmed),
          )
        : [],
    [mentions, inputs, tracked, sendCheck, trimmed],
  );

  function pick(item: PickerItem) {
    if (!trigger) return;
    const next = insertMention(value, trigger, caret, item.label);
    if (item.tracked) {
      const entry = item.tracked;
      setTrackedByDraft((prev) => {
        const current = prev[draftKey] ?? [];
        const known = current.some(
          (known) =>
            known.label === entry.label && known.targetId === entry.targetId,
        );
        return known ? prev : { ...prev, [draftKey]: [...current, entry] };
      });
    }
    pendingCaret.current = next.caret;
    setCaret(next.caret);
    const after = activeTrigger(next.text, next.caret);
    setDismissed(after ? { start: after.start, query: after.query } : null);
    setActiveIndex(0);
    onChange(next.text);
  }

  function pickShortcode(entry: EmojiEntry) {
    if (!shortcode) return;
    const next = insertShortcode(value, shortcode, caret, entry.char);
    rememberEmoji(entry.char);
    pendingCaret.current = next.caret;
    setCaret(next.caret);
    setDismissedCode(null);
    setActiveIndex(0);
    onChange(next.text);
  }

  /** Puts the emoji from the toolbar picker at the caret (or over the selection). */
  function insertEmoji(char: string) {
    const box = textareaRef.current;
    const next = insertAtSelection(
      value,
      box?.selectionStart ?? value.length,
      box?.selectionEnd ?? value.length,
      char,
    );
    pendingCaret.current = next.caret;
    setCaret(next.caret);
    setActiveIndex(0);
    onChange(next.text);
    if (next.text.trim().length > 0) onTyping?.();
  }

  function addFiles(incoming: readonly File[]) {
    if (!attachments || disabled || incoming.length === 0) return;
    const result = checkPendingFiles(files.length, incoming);
    setRefusals(result.refused);
    if (result.accepted.length > 0) {
      setFilesByDraft((prev) => ({
        ...prev,
        [filesKey]: [...(prev[filesKey] ?? []), ...result.accepted],
      }));
    }
  }

  function removeFile(index: number) {
    setRefusals([]);
    setFilesByDraft((prev) => ({
      ...prev,
      [filesKey]: (prev[filesKey] ?? []).filter((_, at) => at !== index),
    }));
  }

  /** Sends the text with the pending files (when there are some) and empties the strip. */
  function emit(content: string, payload?: ChatMentionPayload) {
    const carries = (payload?.mentions?.length ?? 0) > 0;
    const held = files;
    if (held.length > 0) {
      onSend(content, carries ? payload : undefined, held);
      setFilesByDraft((prev) => {
        const next = { ...prev };
        delete next[filesKey];
        return next;
      });
      setRefusals([]);
    } else if (carries) onSend(content, payload);
    else onSend(content);
  }

  function dispatch(content: string, payload: ChatMentionPayload) {
    emit(content, payload);
    setTrackedByDraft((prev) => {
      if (!(draftKey in prev)) return prev;
      const next = { ...prev };
      delete next[draftKey];
      return next;
    });
  }

  function submit() {
    if (disabled || busy) return;
    const result = checkMessageContent(value);
    if (!result.ok) return;
    if (!mentions || inputs.length === 0) {
      emit(result.content);
      return;
    }
    if (needsBroadcastConfirm(inputs, mentions.mentionables) && !confirming) {
      const first = inputs.find(
        (input) =>
          input.kind === "broadcast-channel" || input.kind === "broadcast-here",
      );
      setConfirming({
        content: result.content,
        payload: { mentions: inputs, confirmBroadcast: true },
        token: first
          ? result.content.slice(first.start, first.start + first.length)
          : "@channel",
      });
      return;
    }
    // The confirmation travels only when the user gave it: a channel that grew past the limit is refused, not bypassed.
    dispatch(result.content, { mentions: inputs });
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.nativeEvent.isComposing) return;
    if (emojiOpen) {
      if (event.key === "ArrowDown" || event.key === "ArrowUp") {
        event.preventDefault();
        const step = event.key === "ArrowDown" ? 1 : -1;
        setActiveIndex(
          (safeIndex + step + emojiItems.length) % emojiItems.length,
        );
        return;
      }
      if ((event.key === "Enter" || event.key === "Tab") && !event.shiftKey) {
        event.preventDefault();
        const entry = emojiItems[safeIndex];
        if (entry) pickShortcode(entry);
        return;
      }
      if (event.key === "Escape") {
        event.preventDefault();
        if (shortcode) setDismissedCode(shortcode);
        return;
      }
    }
    if (open) {
      if (event.key === "ArrowDown" || event.key === "ArrowUp") {
        event.preventDefault();
        const step = event.key === "ArrowDown" ? 1 : -1;
        setActiveIndex((safeIndex + step + items.length) % items.length);
        return;
      }
      if (
        (event.key === "Enter" && !event.shiftKey) ||
        (event.key === "Tab" && !event.shiftKey)
      ) {
        event.preventDefault();
        const item = items[safeIndex];
        if (item) pick(item);
        return;
      }
      if (event.key === "Escape") {
        event.preventDefault();
        if (trigger) {
          setDismissed({ start: trigger.start, query: trigger.query });
        }
        return;
      }
    }
    if (event.key !== "Enter" || event.shiftKey) return;
    event.preventDefault();
    submit();
  }

  function syncCaret(target: HTMLTextAreaElement) {
    setCaret(target.selectionStart);
    setActiveIndex(0);
  }

  let message: string | null = null;
  if (disabledReason !== null) message = disabledReason;
  else if (tooLong)
    message = fillCopy(copy.composer.tooLong, {
      max: CHAT_LIMITS.messageMaxLength,
    });
  // The backend needs text on a message: files alone are not sent (the text is the message they belong to).
  else if (files.length > 0 && !check.ok) message = copy.attachments.needsText;

  function handleDragOver(event: DragEvent<HTMLElement>) {
    if (!attachments || disabled) return;
    if (!Array.from(event.dataTransfer.types).includes("Files")) return;
    event.preventDefault();
    setDragging(true);
  }

  function handleDrop(event: DragEvent<HTMLElement>) {
    if (!attachments || disabled) return;
    if (!Array.from(event.dataTransfer.types).includes("Files")) return;
    event.preventDefault();
    setDragging(false);
    addFiles(Array.from(event.dataTransfer.files));
  }

  function handlePaste(event: ClipboardEvent<HTMLTextAreaElement>) {
    if (!attachments) return;
    const pasted = Array.from(event.clipboardData.files);
    if (pasted.length === 0) return;
    event.preventDefault();
    addFiles(pasted);
  }

  return (
    <div className="shrink-0 bg-background px-2.5 pb-2.5 md:px-5 md:pb-4 xl:pb-1">
      <div className="relative mx-auto w-full max-w-[720px]">
        {trigger ? (
          <MentionPopover
            id={listId}
            items={items}
            activeIndex={safeIndex}
            optionId={optionId}
            status={status}
            trigger={trigger}
            copy={copy}
            onPick={pick}
            onHover={setActiveIndex}
          />
        ) : null}
        {emojiOpen ? (
          <ShortcodePopover
            id={listId}
            items={emojiItems}
            activeIndex={safeIndex}
            optionId={optionId}
            copy={copy}
            onPick={pickShortcode}
            onHover={setActiveIndex}
          />
        ) : null}
        {voiceOpen && attachments ? (
          <div className="flex items-center overflow-hidden rounded-3xl border border-input bg-card shadow-xs">
            <VoiceRecorderPanel
              recorder={voiceRecorder}
              copy={copy}
              onSend={(file) => onSend("", undefined, [file])}
              onClose={() => {
                setVoiceOpen(false);
                textareaRef.current?.focus();
              }}
            />
          </div>
        ) : (
          <form
            className={cn(
              "flex flex-col overflow-hidden rounded-[26px] border border-input bg-card shadow-xs transition-colors focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/30 motion-reduce:transition-none",
              disabled && "opacity-60",
              dragging && "border-ring ring-[3px] ring-ring/30",
            )}
            onDragOver={handleDragOver}
            onDragLeave={() => setDragging(false)}
            onDrop={handleDrop}
            onSubmit={(event) => {
              event.preventDefault();
              submit();
            }}
          >
            {leading}
            <MentionWarnings
              warnings={warnings}
              mentions={mentions}
              copy={copy}
            />
            {confirming ? (
              <BroadcastConfirm
                count={mentions?.mentionables?.memberCount ?? 0}
                token={confirming.token}
                copy={copy}
                onConfirm={() =>
                  dispatch(confirming.content, confirming.payload)
                }
                onCancel={() => {
                  setConfirming(null);
                  textareaRef.current?.focus();
                }}
              />
            ) : null}
            {attachments && (files.length > 0 || refusals.length > 0) ? (
              <PendingFiles
                files={files}
                refusals={refusals}
                copy={copy}
                onRemove={removeFile}
              />
            ) : null}
            {dragging ? (
              <p
                role="status"
                className="border-b border-border px-3 py-2 text-xs text-muted-foreground"
              >
                {copy.attachments.dropHint}
              </p>
            ) : null}
            <div className="flex items-end gap-0.5 p-[5px] ps-1.5 pe-1.5">
              {attachments ? (
                <>
                  <input
                    ref={fileInputRef}
                    type="file"
                    multiple
                    hidden
                    tabIndex={-1}
                    aria-hidden="true"
                    data-testid="chat-file-input"
                    onChange={(event) => {
                      addFiles(Array.from(event.target.files ?? []));
                      event.target.value = "";
                    }}
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon-sm"
                    className="size-10 rounded-full text-muted-foreground hover:text-foreground"
                    disabled={disabled}
                    aria-label={copy.attachments.attach}
                    title={copy.attachments.attach}
                    onClick={() => fileInputRef.current?.click()}
                  >
                    <Paperclip aria-hidden="true" />
                  </Button>
                </>
              ) : null}
              <EmojiPickerButton
                copy={copy}
                disabled={disabled}
                onPick={insertEmoji}
                onClosed={() => textareaRef.current?.focus()}
              />
              <Textarea
                ref={textareaRef}
                role={mentions ? "combobox" : undefined}
                aria-expanded={mentions ? open || emojiOpen : undefined}
                aria-controls={
                  mentions && (open || emojiOpen) ? listId : undefined
                }
                aria-activedescendant={
                  mentions && (open || emojiOpen)
                    ? optionId(safeIndex)
                    : undefined
                }
                aria-autocomplete={mentions ? "list" : undefined}
                aria-haspopup={mentions ? "listbox" : undefined}
                aria-label={label}
                aria-describedby={hintId}
                aria-keyshortcuts="Enter Shift+Enter"
                aria-invalid={tooLong || undefined}
                placeholder={label}
                value={value}
                disabled={disabled}
                rows={1}
                onChange={(event) => {
                  onChange(event.target.value);
                  if (event.target.value.trim().length > 0) onTyping?.();
                  syncCaret(event.target);
                }}
                onSelect={(event) => syncCaret(event.currentTarget)}
                onKeyDown={handleKeyDown}
                onPaste={handlePaste}
                className="max-h-36 min-h-10 flex-1 resize-none self-center rounded-none border-0 bg-transparent px-2 py-2.5 leading-5 shadow-none focus-visible:border-0 focus-visible:ring-0 md:text-sm dark:bg-transparent"
              />
              {showMic ? (
                <Button
                  type="button"
                  size="icon"
                  variant="ghost"
                  className="size-10 shrink-0 rounded-full text-muted-foreground hover:text-foreground"
                  aria-label={copy.voice.record}
                  title={copy.voice.record}
                  disabled={disabled || busy}
                  onClick={() => setVoiceOpen(true)}
                >
                  <Mic aria-hidden="true" />
                </Button>
              ) : (
                <Button
                  type="submit"
                  size="icon"
                  className="size-10 shrink-0 rounded-full"
                  aria-label={copy.composer.send}
                  title={`${copy.composer.send} (Enter)`}
                  disabled={disabled || busy || tooLong || !check.ok}
                  data-has-content={check.ok ? "true" : "false"}
                >
                  <SendHorizontal aria-hidden="true" />
                </Button>
              )}
            </div>
          </form>
        )}
        <div
          id={hintId}
          className={cn(
            "flex min-h-5 min-w-0 flex-wrap items-center gap-x-3 gap-y-1 px-4 pt-1 text-xs empty:hidden",
            tooLong ? "text-destructive" : "text-muted-foreground",
          )}
        >
          {footerExtra}
          {(message ?? slowModeSeconds > 0) ? (
            <span
              role={message ? "status" : undefined}
              className="min-w-0 truncate"
            >
              {message ??
                fillCopy(copy.composer.slowMode, {
                  wait: formatWait(copy, slowModeSeconds),
                })}
            </span>
          ) : null}
          {showsCounter(value) ? (
            <span className="shrink-0 tabular-nums" aria-live="off">
              {fillCopy(copy.composer.counter, {
                count: length,
                max: CHAT_LIMITS.messageMaxLength,
              })}
            </span>
          ) : null}
        </div>
      </div>
    </div>
  );
}

/** The files waiting for Send, and the ones the pre-check left out with the reason. */
function PendingFiles({
  files,
  refusals,
  copy,
  onRemove,
}: {
  readonly files: readonly File[];
  readonly refusals: readonly {
    readonly name: string;
    readonly reason: PendingFileRefusal;
  }[];
  readonly copy: ChatCopy;
  readonly onRemove: (index: number) => void;
}) {
  return (
    <div className="flex flex-col gap-1.5 border-b border-border px-3 py-2">
      {files.length > 0 ? (
        <ul
          aria-label={fillCopy(copy.attachments.pendingLabel, {
            count: files.length,
            max: CHAT_LIMITS.attachmentsPerMessageMax,
          })}
          className="flex flex-wrap gap-1.5"
          data-testid="chat-pending-files"
        >
          {files.map((file, index) => (
            <li
              key={`${file.name}-${file.size}-${file.lastModified}-${index}`}
              className="flex max-w-full items-center gap-1.5 rounded-md border border-border bg-muted/50 py-0.5 ps-2 pe-0.5 text-xs"
            >
              <AttachmentGlyphIcon
                contentType={file.type}
                fileName={file.name}
                className="text-muted-foreground"
              />
              <span className="max-w-40 truncate font-medium text-foreground">
                {file.name}
              </span>
              <span className="tabular-nums text-muted-foreground">
                {formatFileSize(file.size)}
              </span>
              <Button
                type="button"
                variant="ghost"
                size="icon-xs"
                aria-label={fillCopy(copy.attachments.removePending, {
                  name: file.name,
                })}
                title={copy.attachments.removeShort}
                onClick={() => onRemove(index)}
              >
                <X aria-hidden="true" />
              </Button>
            </li>
          ))}
        </ul>
      ) : null}
      {refusals.length > 0 ? (
        <ul
          role="alert"
          className="flex flex-col gap-0.5 text-xs text-destructive"
        >
          {refusals.map((refusal, index) => (
            <li key={`${refusal.name}-${index}`}>
              {fillCopy(copy.attachments.refused[refusal.reason], {
                name: refusal.name,
                max:
                  refusal.reason === "too-large"
                    ? formatFileSize(CHAT_LIMITS.attachmentMaxBytes)
                    : CHAT_LIMITS.attachmentsPerMessageMax,
              })}
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

const MAX_ITEMS = 20;

/** The entries the open trigger offers: @ lists @channel, @here and people, # lists channels. */
function buildItems(
  trigger: ActiveTrigger,
  mentions: ChatComposerMentions,
  copy: ChatCopy,
): PickerItem[] {
  if (trigger.trigger === "#") {
    return filterByQuery(mentions.channels, trigger.query, (c) => c.name)
      .slice(0, MAX_ITEMS)
      .map((channel) => ({
        id: channel.id,
        group: "channels",
        label: `#${channel.name}`,
        name: channel.name,
        description: null,
        tracked: {
          kind: "channel-link",
          label: `#${channel.name}`,
          targetId: channel.id,
        },
      }));
  }
  const result: PickerItem[] = [];
  const query = trigger.query.trim().toLowerCase();
  if (mentions.mentionables?.canBroadcast) {
    for (const token of ["channel", "here"] as const) {
      if (query === "" || token.startsWith(query)) {
        result.push({
          id: token,
          group: "broadcast",
          label: `@${token}`,
          name: `@${token}`,
          description:
            token === "channel"
              ? copy.mentions.channelDescription
              : copy.mentions.hereDescription,
          tracked: null,
        });
      }
    }
  }
  const named = (mentions.mentionables?.people ?? [])
    .filter((person) => person.userId !== mentions.selfId)
    .flatMap((person) => {
      const name = mentions.people.get(person.userId);
      return name ? [{ ...person, name }] : [];
    });
  const matches = filterByQuery(named, trigger.query, (person) => person.name);
  const ordered = [
    ...matches.filter((person) => person.inSpace),
    ...matches.filter((person) => !person.inSpace),
  ];
  for (const person of ordered) {
    if (result.length >= MAX_ITEMS) break;
    result.push({
      id: person.userId,
      group: "people",
      label: `@${person.name}`,
      name: person.name,
      description: person.inSpace ? null : copy.mentions.notInSpace,
      tracked: {
        kind: "user",
        label: `@${person.name}`,
        targetId: person.userId,
      },
    });
  }
  return result;
}

function MentionPopover({
  id,
  items,
  activeIndex,
  optionId,
  status,
  trigger,
  copy,
  onPick,
  onHover,
}: {
  readonly id: string;
  readonly items: readonly PickerItem[];
  readonly activeIndex: number;
  readonly optionId: (index: number) => string;
  readonly status: "loading" | "ready" | "error";
  readonly trigger: ActiveTrigger;
  readonly copy: ChatCopy;
  readonly onPick: (item: PickerItem) => void;
  readonly onHover: (index: number) => void;
}) {
  const shell =
    "absolute inset-x-0 bottom-full z-20 mb-2 max-h-64 overflow-y-auto rounded-2xl border border-border bg-popover p-1.5 text-popover-foreground shadow-md md:max-w-sm";
  if (items.length === 0) {
    const text =
      status === "loading"
        ? copy.mentions.loading
        : status === "error"
          ? copy.mentions.error
          : trigger.query !== "" && !trigger.query.includes(" ")
            ? copy.mentions.noMatch
            : null;
    if (text === null) return null;
    return (
      <div
        role="status"
        className={cn(shell, "px-3 py-2 text-sm text-muted-foreground")}
      >
        {text}
      </div>
    );
  }
  const groups: readonly PickerItem["group"][] = [
    "broadcast",
    "people",
    "channels",
  ];
  const headings: Record<PickerItem["group"], string> = {
    broadcast: copy.mentions.broadcastHeading,
    people: copy.mentions.peopleHeading,
    channels: copy.mentions.channelsHeading,
  };
  return (
    <div
      id={id}
      role="listbox"
      aria-label={copy.mentions.listLabel}
      className={shell}
    >
      {groups.map((group) => {
        const inGroup = items.filter((item) => item.group === group);
        if (inGroup.length === 0) return null;
        return (
          <div
            key={group}
            role="group"
            aria-labelledby={`${id}-heading-${group}`}
          >
            <div
              id={`${id}-heading-${group}`}
              role="presentation"
              className="px-2 py-1 text-xs font-semibold text-muted-foreground"
            >
              {headings[group]}
            </div>
            {inGroup.map((item) => {
              const index = items.indexOf(item);
              const active = index === activeIndex;
              return (
                <div
                  key={`${item.group}-${item.id}`}
                  id={optionId(index)}
                  role="option"
                  aria-selected={active}
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => onPick(item)}
                  onMouseEnter={() => onHover(index)}
                  className={cn(
                    "flex cursor-pointer items-center gap-2 rounded-md px-2 py-1.5 text-sm",
                    active && "bg-accent text-accent-foreground",
                  )}
                >
                  {item.group === "broadcast" ? (
                    <Megaphone
                      className="size-4 shrink-0 text-muted-foreground"
                      aria-hidden="true"
                    />
                  ) : item.group === "channels" ? (
                    <Hash
                      className="size-4 shrink-0 text-muted-foreground"
                      aria-hidden="true"
                    />
                  ) : null}
                  <span className="min-w-0 flex-1 truncate font-medium">
                    {item.name}
                  </span>
                  {item.description ? (
                    <span
                      className={cn(
                        "shrink-0 text-xs",
                        item.group === "people"
                          ? "rounded-full bg-warning-subtle px-2 py-0.5 text-warning-subtle-foreground"
                          : "text-muted-foreground",
                      )}
                    >
                      {item.description}
                    </span>
                  ) : null}
                </div>
              );
            })}
          </div>
        );
      })}
    </div>
  );
}

/** The `:shortcode` suggestions above the composer: the same listbox pattern as the mention list. */
function ShortcodePopover({
  id,
  items,
  activeIndex,
  optionId,
  copy,
  onPick,
  onHover,
}: {
  readonly id: string;
  readonly items: readonly EmojiEntry[];
  readonly activeIndex: number;
  readonly optionId: (index: number) => string;
  readonly copy: ChatCopy;
  readonly onPick: (entry: EmojiEntry) => void;
  readonly onHover: (index: number) => void;
}) {
  return (
    <div
      id={id}
      role="listbox"
      aria-label={copy.emoji.shortcodeLabel}
      className="absolute inset-x-0 bottom-full z-20 mb-2 max-h-64 overflow-y-auto rounded-2xl border border-border bg-popover p-1.5 text-popover-foreground shadow-md md:max-w-sm"
    >
      {items.map((entry, index) => (
        <div
          key={entry.id}
          id={optionId(index)}
          role="option"
          aria-selected={index === activeIndex}
          onMouseDown={(event) => event.preventDefault()}
          onClick={() => onPick(entry)}
          onMouseEnter={() => onHover(index)}
          className={cn(
            "flex cursor-pointer items-center gap-2 rounded-md px-2 py-1.5 text-sm",
            index === activeIndex && "bg-accent text-accent-foreground",
          )}
        >
          <span aria-hidden="true" className="text-base">
            {entry.char}
          </span>
          <span className="min-w-0 flex-1 truncate font-medium">
            :{entry.id}:
          </span>
        </div>
      ))}
    </div>
  );
}

function MentionWarnings({
  warnings,
  mentions,
  copy,
}: {
  readonly warnings: readonly MentionWarning[];
  readonly mentions: ChatComposerMentions | undefined;
  readonly copy: ChatCopy;
}) {
  if (warnings.length === 0) return null;
  return (
    <ul
      className="flex flex-col border-b border-border"
      data-testid="chat-mention-warnings"
    >
      {warnings.map((warning) => {
        const name = warning.label.replace(/^@/, "");
        const text =
          warning.code === "cannot-view"
            ? fillCopy(copy.mentions.cannotView, { name })
            : warning.code === "no-audience"
              ? fillCopy(copy.mentions.noAudience, { token: warning.label })
              : fillCopy(copy.mentions.broadcastNotAllowed, {
                  token: warning.label,
                });
        const canAdd =
          warning.code === "cannot-view" &&
          warning.userId !== null &&
          mentions?.onAddMember !== undefined;
        const adding = mentions?.addingUserId === warning.userId;
        const failed =
          warning.userId !== null &&
          mentions?.addFailedUserId === warning.userId;
        return (
          <li
            key={`${warning.code}-${warning.userId ?? warning.label}`}
            role="status"
            className="flex flex-wrap items-center gap-x-2 gap-y-1 bg-warning-subtle px-3 py-1.5 text-xs text-warning-subtle-foreground"
          >
            <span className="min-w-0 flex-1">
              {text}
              {failed ? (
                <span role="alert" className="ms-1 text-destructive">
                  {fillCopy(copy.mentions.addFailed, { name })}
                </span>
              ) : null}
            </span>
            {canAdd ? (
              <Button
                type="button"
                size="xs"
                variant="outline"
                disabled={adding}
                onClick={() => mentions?.onAddMember?.(warning.userId!)}
              >
                <UserPlus aria-hidden="true" />
                {adding
                  ? copy.mentions.adding
                  : fillCopy(copy.mentions.addToChannel, { name })}
              </Button>
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}

function BroadcastConfirm({
  count,
  token,
  copy,
  onConfirm,
  onCancel,
}: {
  readonly count: number;
  readonly token: string;
  readonly copy: ChatCopy;
  readonly onConfirm: () => void;
  readonly onCancel: () => void;
}) {
  const titleId = useId();
  const bodyId = useId();
  const cancelRef = useRef<HTMLButtonElement | null>(null);
  // The safe choice has the focus: a second Enter does not notify a whole channel.
  useEffect(() => {
    cancelRef.current?.focus();
  }, []);
  return (
    <div
      role="alertdialog"
      aria-labelledby={titleId}
      aria-describedby={bodyId}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          event.preventDefault();
          onCancel();
        }
      }}
      className="flex flex-col gap-2 border-b border-border bg-warning-subtle px-3 py-2 text-warning-subtle-foreground"
    >
      <p id={titleId} className="text-sm font-semibold">
        {fillCopy(copy.mentions.confirmTitle, { count })}
      </p>
      <p id={bodyId} className="text-xs">
        {fillCopy(copy.mentions.confirmBody, { token })}
      </p>
      <div className="flex flex-wrap gap-2">
        <Button type="button" size="sm" onClick={onConfirm}>
          {copy.mentions.confirmSend}
        </Button>
        <Button
          ref={cancelRef}
          type="button"
          size="sm"
          variant="outline"
          onClick={onCancel}
        >
          {copy.mentions.confirmCancel}
        </Button>
      </div>
    </div>
  );
}
