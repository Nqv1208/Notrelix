import { en as shellEn, vi as shellVi } from "./copy/shell.copy";
import { en as pickersEn, vi as pickersVi } from "./copy/pickers.copy";
import { en as sectionsEn, vi as sectionsVi } from "./copy/sections.copy";
import { en as createEn, vi as createVi } from "./copy/create.copy";
import { en as relatedEn, vi as relatedVi } from "./copy/related.copy";
import { en as approvalEn, vi as approvalVi } from "./copy/approval.copy";

/**
 * Copy of the card popup, vi and en, registered as the `card-popup` catalog.
 * One file per area keeps the words next to the parts that use them;
 * placeholders use `{{name}}` and are filled by `fillPopupCopy`.
 */
export const en = {
  locale: "en",
  shell: shellEn,
  pickers: pickersEn,
  sections: sectionsEn,
  create: createEn,
  related: relatedEn,
  approval: approvalEn,
};

export type CardPopupCopy = typeof en;

export const vi: CardPopupCopy = {
  locale: "vi",
  shell: shellVi,
  pickers: pickersVi,
  sections: sectionsVi,
  create: createVi,
  related: relatedVi,
  approval: approvalVi,
};

/** Fills `{{name}}` placeholders. */
export function fillPopupCopy(
  text: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return text.replace(/\{\{(\w+)\}\}/g, (_, key: string) =>
    String(values[key] ?? ""),
  );
}
