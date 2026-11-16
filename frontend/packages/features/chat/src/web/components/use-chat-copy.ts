import { useLocale } from "@notrelix/i18n";
import { en, vi, type ChatCopy } from "./chat.copy";

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
  return copy.list.systemText[text] ?? text;
}
