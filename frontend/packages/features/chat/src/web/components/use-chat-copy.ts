import { useLocale } from "@notrelix/i18n";
import { en, vi, type ChatCopy } from "./chat.copy";
import { parseStoredCallLine } from "../../core/model/call-model";

/** Chat copy for the active locale (requires the host `I18nProvider`). */
export function useChatCopy(): ChatCopy {
  const { locale } = useLocale();
  return locale === "vi" ? vi : en;
}

/** Fills `{{name}}` placeholders; an unknown placeholder stays visible. */
export function fillCopy(
  message: string,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** Picks the `_one` form for exactly 1 and `_other` otherwise, then fills it. */
export function pluralCopy(
  forms: { readonly one: string; readonly other: string },
  count: number,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return fillCopy(count === 1 ? forms.one : forms.other, {
    count,
    ...values,
  });
}

const RENAMED_PREFIX = "renamed the channel to ";

/** A system message as the viewer reads it: the stored English line through the copy files, the rename keeping its name. */
export function systemTextOf(copy: ChatCopy, stored: string | null): string {
  const text = stored ?? "";
  if (text.startsWith(RENAMED_PREFIX)) {
    return fillCopy(copy.list.systemRenamed, {
      name: text.slice(RENAMED_PREFIX.length),
    });
  }
  const call = parseStoredCallLine(text);
  if (call) {
    const c = copy.calls;
    const values = {
      kind: call.kind === "video" ? c.lineVideo : c.lineVoice,
      duration: call.duration ?? "",
    };
    const line = {
      completed: c.lineCompleted,
      missed: c.lineMissed,
      declined: c.lineDeclined,
      cancelled: c.lineCancelled,
      busy: c.lineBusy,
      failed: c.lineFailed,
    }[call.outcome];
    return fillCopy(line, values);
  }
  return copy.list.systemText[text] ?? text;
}

/** A stored call line as the board draws it: a bold head ("Cuộc gọi video"), a tail ("24 phút", "từ Quân") and whether it is missed. */
export function callLineOf(
  copy: ChatCopy,
  stored: string | null,
  authorName: string,
  isOwn: boolean,
) {
  const call = parseStoredCallLine(stored);
  if (!call) return null;
  const c = copy.calls;
  const kind = call.kind === "video" ? c.kindVideo : c.kindVoice;
  const tail = {
    completed: call.duration ?? "",
    missed: isOwn
      ? c.lineTailMissedOwn
      : fillCopy(c.lineTailMissedFrom, { name: authorName }),
    declined: c.lineTailDeclined,
    cancelled: c.lineTailCancelled,
    busy: c.lineTailBusy,
    failed: c.lineTailFailed,
  }[call.outcome];
  return {
    kind: call.kind,
    head: call.outcome === "missed" ? c.lineHeadMissed : kind,
    tail,
    missed: call.outcome === "missed",
  };
}
