import { useRef, useState } from "react";
import {
  ArrowRight,
  Check,
  ClipboardPaste,
  Lock,
  Mail,
  Plus,
  X,
  CircleAlert,
} from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import type { OnboardingCopy } from "./onboarding.copy";
import { fillOnboarding, pluralOnboarding } from "./fill-copy";
import {
  BUTTON_OUTLINE_CLASS,
  FIELD_INPUT_CLASS,
  FieldLabel,
  Hint,
  NoteCard,
  OPTION_CLASS,
  StepTitle,
} from "./onboarding-frame";
import {
  COMPANY_SIZES,
  DEFAULT_COLOR_CYCLE,
  DEPARTMENT_COLORS,
  DEPARTMENT_TONE_CLASS,
  EMAIL_SHAPE,
  INVITE_ROLES,
  departmentInitials,
  emailsFromText,
  type AddressCheck,
  type BoardTemplateId,
  type BoardTemplateInfo,
  type DepartmentColor,
  type DepartmentDraft,
  type InviteRowDraft,
  type OnboardingCatalog,
  type OnboardingDraftData,
} from "./onboarding-models";

/** Radix items cannot carry an empty value, so "none" is a sentinel. */
const NONE = "__none__";

/** The bodies of the four setup steps (the Onboarding mockup). */

type DraftChange = (next: OnboardingDraftData) => void;

// ─── Step 1: the workspace ───────────────────────────────────────────────────

export function WorkspaceStep({
  copy,
  draft,
  address,
  onDraft,
  onUseSuggestion,
  showNameError,
  locked,
}: {
  copy: OnboardingCopy;
  draft: OnboardingDraftData;
  address: AddressCheck;
  onDraft: DraftChange;
  onUseSuggestion: (slug: string) => void;
  showNameError: boolean;
  /** The workspace exists already: its name and address are no longer written here. */
  locked: boolean;
}) {
  const t = copy.workspace;
  const { workspace } = draft;
  const patch = (next: Partial<OnboardingDraftData["workspace"]>) =>
    onDraft({ ...draft, workspace: { ...workspace, ...next } });
  const slugState =
    address.kind === "free"
      ? "free"
      : address.kind === "taken"
        ? "taken"
        : address.kind === "invalid"
          ? "invalid"
          : "none";
  return (
    <>
      <StepTitle title={t.title} lead={t.lead} />
      {locked ? <Hint>{t.lockedNote}</Hint> : null}
      <div className="flex flex-col gap-1.5">
        <FieldLabel htmlFor="onb-name">{t.nameLabel}</FieldLabel>
        <input
          id="onb-name"
          name="company"
          type="text"
          autoComplete="organization"
          value={workspace.name}
          readOnly={locked}
          placeholder={t.namePlaceholder}
          aria-invalid={
            showNameError && !workspace.name.trim() ? true : undefined
          }
          onChange={(event) => patch({ name: event.target.value })}
          className={FIELD_INPUT_CLASS}
        />
        {showNameError && !workspace.name.trim() ? (
          <ErrorLine>{t.nameRequired}</ErrorLine>
        ) : null}
      </div>
      <div className="flex flex-col gap-1.5">
        <FieldLabel htmlFor="onb-slug">{t.slugLabel}</FieldLabel>
        <div className="flex h-10 items-stretch overflow-hidden rounded-[8px] border border-line-strong bg-popover focus-within:border-focus-ring focus-within:ring-[3px] focus-within:ring-brand-subtle max-md:h-touch has-[[aria-invalid=true]]:border-danger-text has-[[aria-invalid=true]]:ring-1 has-[[aria-invalid=true]]:ring-danger-text">
          <input
            id="onb-slug"
            name="slug"
            type="text"
            autoComplete="off"
            autoCapitalize="none"
            spellCheck={false}
            value={workspace.slug}
            readOnly={locked}
            aria-invalid={
              slugState === "taken" || slugState === "invalid"
                ? true
                : undefined
            }
            aria-describedby="onb-slug-status onb-slug-hint"
            onChange={(event) =>
              patch({ slug: event.target.value.toLowerCase().trim() })
            }
            className="min-w-0 flex-1 border-0 bg-transparent px-3 text-body text-foreground outline-none placeholder:text-meta-foreground"
          />
          <span
            aria-hidden="true"
            className="inline-flex shrink-0 items-center border-l border-line-strong bg-state-hover px-3 text-item-ui text-muted-foreground"
          >
            {t.suffix}
          </span>
        </div>
        <div
          id="onb-slug-status"
          role="status"
          className="flex flex-wrap items-center gap-2 text-crumb leading-[1.45] font-medium"
        >
          {slugState === "free" ? (
            <span className="inline-flex items-center gap-1.5 text-success-text">
              <Check aria-hidden="true" className="size-3.5" />
              {fillOnboarding(t.free, {
                address: `${workspace.slug}${t.suffix}`,
              })}
            </span>
          ) : null}
          {slugState === "taken" ? (
            <>
              <span className="inline-flex items-center gap-1.5 text-danger-text">
                <CircleAlert aria-hidden="true" className="size-3.5" />
                {address.kind === "taken" && address.suggestion
                  ? fillOnboarding(t.taken, {
                      slug: workspace.slug,
                      suggestion: address.suggestion,
                    })
                  : fillOnboarding(t.taken, {
                      slug: workspace.slug,
                      suggestion: "…",
                    })}
              </span>
              {address.kind === "taken" && address.suggestion ? (
                <button
                  type="button"
                  onClick={() => onUseSuggestion(address.suggestion ?? "")}
                  className="inline-flex h-[30px] cursor-pointer items-center rounded-[6px] border border-line-strong bg-popover px-2.5 text-item-small font-medium text-foreground hover:bg-state-hover"
                >
                  {fillOnboarding(t.useSuggestion, {
                    suggestion: address.suggestion,
                  })}
                </button>
              ) : null}
            </>
          ) : null}
          {slugState === "invalid" ? (
            <span className="inline-flex items-center gap-1.5 text-danger-text">
              <CircleAlert aria-hidden="true" className="size-3.5" />
              {t.invalid}
            </span>
          ) : null}
          {address.kind === "checking" ? (
            <span className="text-meta-foreground">{t.checking}</span>
          ) : null}
        </div>
        <Hint id="onb-slug-hint">{t.slugHint}</Hint>
      </div>
      <fieldset className="m-0 flex min-w-0 flex-col gap-2 border-0 p-0">
        <legend className="mb-2 p-0 text-compact leading-[1.45] font-semibold">
          {t.sizeLabel}
        </legend>
        <div className="flex flex-wrap gap-2">
          {COMPANY_SIZES.map((size) => (
            <label key={size} className={OPTION_CLASS}>
              <input
                type="radio"
                name="company-size"
                value={size}
                checked={workspace.size === size}
                disabled={locked}
                onChange={() => patch({ size })}
                className="size-4 accent-primary"
              />
              {t.sizes[size]}
            </label>
          ))}
        </div>
        <Hint>{t.sizeHint}</Hint>
      </fieldset>
      <fieldset className="m-0 flex min-w-0 flex-col gap-2 border-0 p-0">
        <legend className="mb-2 p-0 text-compact leading-[1.45] font-semibold">
          {t.localeLabel}
        </legend>
        <div className="flex flex-wrap gap-2">
          {(["vi", "en"] as const).map((locale) => (
            <label key={locale} className={OPTION_CLASS}>
              <input
                type="radio"
                name="company-locale"
                value={locale}
                checked={workspace.locale === locale}
                disabled={locked}
                onChange={() => patch({ locale })}
                className="size-4 accent-primary"
              />
              {t.locales[locale]}
            </label>
          ))}
        </div>
        <Hint>{t.localeHint}</Hint>
      </fieldset>
    </>
  );
}

function ErrorLine({
  children,
  id,
}: {
  children: React.ReactNode;
  id?: string;
}) {
  return (
    <p
      id={id}
      role="alert"
      className="m-0 flex items-start gap-1.5 text-crumb leading-[1.45] font-medium text-danger-text"
    >
      <CircleAlert aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
      {children}
    </p>
  );
}

// ─── Step 2: the departments ─────────────────────────────────────────────────

let departmentCounter = 0;
const newDepartmentKey = () => `custom-${(departmentCounter += 1)}`;

/** Names of the departments that wear `color`, other than `key`. */
function sharingColor(
  departments: readonly DepartmentDraft[],
  key: string,
  color: DepartmentColor,
): string[] {
  return departments
    .filter((entry) => entry.key !== key && entry.color === color)
    .map((entry) => entry.name);
}

export function DepartmentsStep({
  copy,
  draft,
  catalog,
  sizeLabel,
  onDraft,
  locked,
}: {
  copy: OnboardingCopy;
  draft: OnboardingDraftData;
  catalog: OnboardingCatalog | null;
  sizeLabel: string;
  onDraft: DraftChange;
  /** The departments exist already: they are shown, not written, here. */
  locked: boolean;
}) {
  const t = copy.departments;
  const [pickedKey, setSelectedKey] = useState<string | null>(null);
  // Until one is picked, the third department is the open one (as in the mockup).
  const selectedKey =
    pickedKey && draft.departments.some((entry) => entry.key === pickedKey)
      ? pickedKey
      : (draft.departments[2]?.key ?? draft.departments[0]?.key ?? null);
  const lastAdded = useRef<string | null>(null);
  const selected = draft.departments.find((entry) => entry.key === selectedKey);
  const set = (departments: readonly DepartmentDraft[]) =>
    onDraft({ ...draft, departments });
  const update = (key: string, patch: Partial<DepartmentDraft>) =>
    set(
      draft.departments.map((entry) =>
        entry.key === key ? { ...entry, ...patch } : entry,
      ),
    );
  const colorFor = (index: number): DepartmentColor =>
    DEFAULT_COLOR_CYCLE[index % DEFAULT_COLOR_CYCLE.length] ?? "blue";
  const add = (name: string, key = newDepartmentKey()) => {
    lastAdded.current = key;
    set([
      ...draft.departments,
      { key, name, color: colorFor(draft.departments.length) },
    ]);
    setSelectedKey(key);
  };
  const remove = (key: string) => {
    const next = draft.departments.filter((entry) => entry.key !== key);
    set(next);
    if (selectedKey === key) setSelectedKey(next[0]?.key ?? null);
  };
  const unused = (catalog?.extras ?? []).filter(
    (extra) =>
      !draft.departments.some(
        (entry) => entry.name.toLowerCase() === extra.name.toLowerCase(),
      ),
  );
  const duplicate = draft.departments.find((entry, index) =>
    draft.departments.some(
      (other, otherIndex) =>
        otherIndex < index &&
        other.name.trim().toLowerCase() === entry.name.trim().toLowerCase(),
    ),
  );
  const shared = selected
    ? sharingColor(draft.departments, selected.key, selected.color)
    : [];
  return (
    <>
      <StepTitle
        title={t.title}
        lead={
          catalog ? fillOnboarding(t.lead, { size: sizeLabel }) : t.leadNeutral
        }
      />
      {locked ? <Hint>{t.lockedNote}</Hint> : null}
      <ul
        aria-label={t.listLabel}
        className="m-0 flex list-none flex-wrap gap-2.5 p-0"
      >
        {draft.departments.map((entry) => (
          <li
            key={entry.key}
            className={cn(
              "flex h-11 items-center gap-1.5 rounded-[11px] border bg-popover pr-1 pl-1.5",
              entry.key === selectedKey
                ? "border-primary ring-1 ring-primary"
                : "border-line-strong",
            )}
          >
            <button
              type="button"
              aria-label={fillOnboarding(t.colorTitle, { name: entry.name })}
              onClick={() => setSelectedKey(entry.key)}
              className={cn(
                "grid size-7.5 shrink-0 cursor-pointer place-items-center rounded-[8px] border-0 p-0 text-rail-label font-bold outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                DEPARTMENT_TONE_CLASS[entry.color],
              )}
            >
              {departmentInitials(entry.name)}
            </button>
            <input
              type="text"
              aria-label={t.nameLabel}
              value={entry.name}
              readOnly={locked}
              size={Math.max(4, Math.ceil(entry.name.length * 0.9))}
              autoFocus={lastAdded.current === entry.key}
              onFocus={() => setSelectedKey(entry.key)}
              onChange={(event) =>
                update(entry.key, { name: event.target.value })
              }
              className="min-w-0 rounded-[5px] border-0 bg-transparent px-0.5 py-1 text-body leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover"
            />
            <Lock
              aria-label={t.private}
              role="img"
              className="size-3.5 shrink-0 text-meta-foreground"
            />
            <button
              type="button"
              hidden={locked}
              aria-label={fillOnboarding(t.remove, { name: entry.name })}
              onClick={() => remove(entry.key)}
              className="grid size-7 shrink-0 cursor-pointer place-items-center rounded-[8px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover hover:text-foreground"
            >
              <X aria-hidden="true" className="size-3.5" />
            </button>
          </li>
        ))}
        <li hidden={locked}>
          <button
            type="button"
            onClick={() => add(t.newName)}
            className="flex h-11 cursor-pointer items-center gap-1.5 rounded-[11px] border border-dashed border-line-strong bg-transparent px-3.5 text-item-ui font-medium text-muted-foreground hover:bg-state-hover hover:text-foreground"
          >
            <Plus aria-hidden="true" className="size-4" />
            {t.add}
          </button>
        </li>
      </ul>
      {duplicate ? (
        <ErrorLine>
          {fillOnboarding(t.duplicate, { name: duplicate.name })}
        </ErrorLine>
      ) : null}
      {draft.departments.length === 0 ? <Hint>{t.empty}</Hint> : null}
      {selected ? (
        <fieldset className="m-0 flex flex-col gap-2.5 rounded-[12px] border border-line bg-popover px-3.5 py-3">
          <legend className="float-left m-0 w-full p-0 text-crumb font-semibold text-muted-foreground">
            {fillOnboarding(t.colorTitle, { name: selected.name })}
          </legend>
          <div
            role="radiogroup"
            aria-label={fillOnboarding(t.colorTitle, { name: selected.name })}
            className="clear-both flex flex-wrap gap-1.5"
          >
            {DEPARTMENT_COLORS.map((color) => {
              const on = selected.color === color;
              return (
                <button
                  key={color}
                  type="button"
                  role="radio"
                  aria-checked={on}
                  disabled={locked}
                  onClick={() => update(selected.key, { color })}
                  className={cn(
                    "box-content inline-flex h-[34px] cursor-pointer items-center gap-[7px] rounded-[9px] border pr-2.5 pl-[5px] text-crumb outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                    on
                      ? "border-line-strong bg-state-hover font-semibold text-foreground"
                      : "border-transparent bg-transparent font-medium text-muted-foreground hover:bg-state-hover",
                  )}
                >
                  <span
                    aria-hidden="true"
                    className={cn(
                      "grid size-5.5 shrink-0 place-items-center rounded-[6px]",
                      DEPARTMENT_TONE_CLASS[color],
                    )}
                  >
                    <Check
                      className={cn("size-3.25", !on && "opacity-0")}
                      strokeWidth={3}
                    />
                  </span>
                  {t.colors[color]}
                </button>
              );
            })}
          </div>
          <Hint>
            {t.colorNote}
            {shared.length > 0 ? (
              <>
                {" "}
                <b className="font-semibold text-warning-text">
                  {fillOnboarding(t.sharedColor, { names: shared.join(", ") })}
                </b>
              </>
            ) : null}
          </Hint>
        </fieldset>
      ) : null}
      {unused.length > 0 && !locked ? (
        <div className="flex flex-wrap items-center gap-2 text-crumb text-meta-foreground">
          <span>
            {catalog?.industry
              ? fillOnboarding(t.extrasIndustry, {
                  industry: t.industries[catalog.industry],
                })
              : t.extras}
          </span>
          {unused.map((extra) => (
            <button
              key={extra.key}
              type="button"
              onClick={() => add(extra.name, extra.key)}
              className="inline-flex h-[30px] cursor-pointer items-center gap-[5px] rounded-full border border-line-strong bg-transparent px-[11px] text-crumb text-foreground hover:bg-state-hover"
            >
              <Plus aria-hidden="true" className="size-3" />
              {extra.name}
            </button>
          ))}
        </div>
      ) : null}
      <NoteCard icon={Lock}>
        <b className="font-semibold text-foreground">{t.privateTitle}</b>
        {t.privateBody}
      </NoteCard>
    </>
  );
}

// ─── Step 3: the invitations ─────────────────────────────────────────────────

let rowCounter = 0;
export const newInviteRow = (
  departmentKey: string | null,
  email = "",
): InviteRowDraft => ({
  id: `row-${(rowCounter += 1)}`,
  email,
  departmentKey,
  role: "Member",
});

/** The message under a row: a malformed or repeated email. */
export function inviteRowProblem(
  rows: readonly InviteRowDraft[],
  row: InviteRowDraft,
  index: number,
): "invalid" | "duplicate" | null {
  const email = row.email.trim().toLowerCase();
  if (!email) return null;
  if (!EMAIL_SHAPE.test(email)) return "invalid";
  const repeated = rows.some(
    (other, otherIndex) =>
      otherIndex < index && other.email.trim().toLowerCase() === email,
  );
  return repeated ? "duplicate" : null;
}

export function InviteStep({
  copy,
  draft,
  onDraft,
  showProblems,
}: {
  copy: OnboardingCopy;
  draft: OnboardingDraftData;
  onDraft: DraftChange;
  showProblems: boolean;
}) {
  const t = copy.invite;
  const [pasting, setPasting] = useState(false);
  const [pasted, setPasted] = useState("");
  const firstDepartment = draft.departments[0]?.key ?? null;
  const rows = draft.invites;
  const set = (invites: readonly InviteRowDraft[]) =>
    onDraft({ ...draft, invites });
  const change = (id: string, patch: Partial<InviteRowDraft>) =>
    set(rows.map((row) => (row.id === id ? { ...row, ...patch } : row)));
  const applyPaste = () => {
    const known = new Set(rows.map((row) => row.email.trim().toLowerCase()));
    const incoming = emailsFromText(pasted).filter(
      (email) => !known.has(email),
    );
    // A blank row at the end takes the first pasted email.
    const kept = rows.filter((row) => row.email.trim() || rows.length === 1);
    set([
      ...kept,
      ...incoming.map((email) => newInviteRow(firstDepartment, email)),
    ]);
    setPasted("");
    setPasting(false);
  };
  return (
    <>
      <StepTitle title={t.title} lead={t.lead} />
      <section className="@container flex flex-col gap-2">
        <div
          role="group"
          aria-label={t.listLabel}
          className="flex flex-col gap-3.5 @xl:gap-2"
        >
          <div className="hidden @xl:grid @xl:grid-cols-[minmax(0,1fr)_9.375rem_8.25rem_2rem] items-end gap-2 px-0.5 text-item-note font-semibold text-meta-foreground">
            <span>{t.email}</span>
            <span>{t.department}</span>
            <span>{t.role}</span>
            <span />
          </div>
          {rows.map((row, index) => {
            const problem =
              showProblems || row.email.includes("@")
                ? inviteRowProblem(rows, row, index)
                : null;
            return (
              <div key={row.id} className="flex flex-col gap-1">
                <div className="grid grid-cols-[minmax(0,1fr)_minmax(0,1fr)_2rem] items-center gap-2 @xl:grid-cols-[minmax(0,1fr)_9.375rem_8.25rem_2rem]">
                  <input
                    type="email"
                    aria-label={`${t.email} ${index + 1}`}
                    value={row.email}
                    placeholder={t.placeholder}
                    aria-invalid={problem ? true : undefined}
                    onChange={(event) =>
                      change(row.id, { email: event.target.value })
                    }
                    className={cn(
                      FIELD_INPUT_CLASS,
                      "col-span-3 @xl:col-span-1",
                    )}
                  />
                  <Select
                    value={row.departmentKey ?? NONE}
                    onValueChange={(value) =>
                      change(row.id, {
                        departmentKey: value === NONE ? null : value,
                      })
                    }
                  >
                    <SelectTrigger
                      aria-label={`${t.department} ${index + 1}`}
                      className="w-full rounded-[8px] data-[size=default]:h-10 max-md:data-[size=default]:h-touch"
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {draft.departments.length === 0 ? (
                        <SelectItem value={NONE}>{t.noDepartments}</SelectItem>
                      ) : null}
                      {draft.departments.map((department) => (
                        <SelectItem key={department.key} value={department.key}>
                          {department.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Select
                    value={row.role}
                    onValueChange={(value) =>
                      change(row.id, { role: value as InviteRowDraft["role"] })
                    }
                  >
                    <SelectTrigger
                      aria-label={`${t.role} ${index + 1}`}
                      className="w-full rounded-[8px] data-[size=default]:h-10 max-md:data-[size=default]:h-touch"
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {INVITE_ROLES.map((role) => (
                        <SelectItem key={role} value={role}>
                          {t.roles[role]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {rows.length > 1 ? (
                    <button
                      type="button"
                      aria-label={fillOnboarding(t.removeRow, { n: index + 1 })}
                      onClick={() =>
                        set(rows.filter((other) => other.id !== row.id))
                      }
                      className="grid size-8 cursor-pointer place-items-center rounded-[8px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover hover:text-foreground"
                    >
                      <X aria-hidden="true" className="size-3.5" />
                    </button>
                  ) : (
                    <span />
                  )}
                </div>
                {problem ? (
                  <ErrorLine>
                    {problem === "invalid" ? t.invalidEmail : t.duplicateEmail}
                  </ErrorLine>
                ) : null}
              </div>
            );
          })}
        </div>
        <div className="mt-0.5 flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => set([...rows, newInviteRow(firstDepartment)])}
            className={cn(BUTTON_OUTLINE_CLASS, "h-[34px] px-3 text-compact")}
          >
            <Plus aria-hidden="true" className="size-4" />
            {t.addRow}
          </button>
          <button
            type="button"
            aria-expanded={pasting}
            onClick={() => setPasting((open) => !open)}
            className={cn(BUTTON_OUTLINE_CLASS, "h-[34px] px-3 text-compact")}
          >
            <ClipboardPaste aria-hidden="true" className="size-4" />
            {t.paste}
          </button>
        </div>
      </section>
      {pasting ? (
        <div className="flex flex-col gap-2 rounded-[12px] border border-line p-3.5">
          <label htmlFor="onb-paste" className="text-item-ui font-semibold">
            {t.pasteTitle}
          </label>
          <textarea
            id="onb-paste"
            rows={4}
            value={pasted}
            onChange={(event) => setPasted(event.target.value)}
            className={cn(FIELD_INPUT_CLASS, "h-auto resize-y py-2")}
          />
          <Hint>{t.pasteHint}</Hint>
          <div className="flex gap-2">
            <button
              type="button"
              disabled={emailsFromText(pasted).length === 0}
              onClick={applyPaste}
              className={cn(BUTTON_OUTLINE_CLASS, "h-[34px] px-3 text-compact")}
            >
              {t.pasteApply}
            </button>
            <button
              type="button"
              onClick={() => {
                setPasted("");
                setPasting(false);
              }}
              className="h-9 cursor-pointer border-0 bg-transparent px-2 text-item-ui font-medium text-muted-foreground"
            >
              {t.pasteCancel}
            </button>
          </div>
        </div>
      ) : null}
      <p className="m-0 text-crumb leading-[1.55] text-meta-foreground">
        <b className="font-semibold text-foreground">{t.adminHelp}</b>
        {t.adminBody}
        <b className="font-semibold text-foreground">{t.memberHelp}</b>
        {t.memberBody}
        <b className="font-semibold text-foreground">{t.viewerHelp}</b>
        {t.viewerBody}
      </p>
      <NoteCard icon={Mail}>
        <b className="font-semibold text-foreground">{t.noteTitle}</b>
        {t.noteBody}
      </NoteCard>
    </>
  );
}

// ─── Step 4: the first board ─────────────────────────────────────────────────

/** The small picture of a template: the shell is the same for all five, the inside differs. */
const PREVIEW_SHELL =
  "flex rounded-[8px] border border-line bg-background p-2 text-rail-label leading-[1.2] font-medium text-muted-foreground";

function TemplatePreview({
  template,
  copy,
}: {
  template: BoardTemplateInfo;
  copy: OnboardingCopy;
}) {
  const t = copy.board;
  if (template.id === "project")
    return (
      <span
        aria-hidden="true"
        className={cn(PREVIEW_SHELL, "h-[82px] gap-[5px]")}
      >
        {template.stages.map((stage, index) => (
          <span
            key={stage}
            className={cn(
              "flex min-w-0 flex-1 flex-col gap-1 rounded-[5px] p-[5px]",
              [
                "bg-work-status-todo-lane",
                "bg-work-status-doing-lane",
                "bg-work-status-review-lane",
                "bg-work-status-done-lane",
              ][index],
            )}
          >
            <span className="block truncate">{stage}</span>
            {Array.from({ length: [3, 2, 1, 2][index] ?? 1 }, (_, card) => (
              <i
                key={card}
                className="block h-3 rounded-[3px] bg-popover ring-1 ring-line"
              />
            ))}
          </span>
        ))}
      </span>
    );
  if (template.id === "approval")
    return (
      <span
        aria-hidden="true"
        className={cn(
          PREVIEW_SHELL,
          "min-h-[82px] flex-col justify-center gap-2 overflow-hidden",
        )}
      >
        <span className="flex flex-wrap items-center gap-1">
          {template.stages.map((stage, index) => (
            <span key={stage} className="contents">
              {index > 0 ? (
                <ArrowRight className="size-2.75 shrink-0 text-meta-foreground" />
              ) : null}
              <span
                className={cn(
                  "rounded-full px-[7px] py-1 whitespace-nowrap",
                  index === template.stages.length - 1
                    ? "bg-work-status-done-lane text-foreground"
                    : "bg-popover ring-1 ring-line-strong",
                )}
              >
                {stage}
              </span>
            </span>
          ))}
        </span>
        <span className="flex flex-wrap gap-[5px]">
          {template.fields.map((field) => (
            <em
              key={field}
              className="rounded-[4px] bg-state-hover-strong px-1.5 py-0.5 not-italic"
            >
              {field}
            </em>
          ))}
        </span>
      </span>
    );
  if (template.id === "hiring")
    return (
      <span
        aria-hidden="true"
        className={cn(PREVIEW_SHELL, "h-[92px] flex-col justify-center gap-1")}
      >
        {template.stages.map((stage, index) => (
          <span key={stage} className="flex items-center gap-1.5">
            <span className="w-14.5 shrink-0">{stage}</span>
            <i
              className="block h-2.5 rounded-[3px] bg-work-status-doing opacity-75"
              style={{ width: `${template.bars[index] ?? 50}%` }}
            />
          </span>
        ))}
      </span>
    );
  if (template.id === "support")
    return (
      <span
        aria-hidden="true"
        className={cn(PREVIEW_SHELL, "h-[92px] flex-col overflow-hidden p-0")}
      >
        <span className="grid grid-cols-[1.25fr_.9fr_1fr] bg-state-hover font-semibold text-foreground">
          {template.fields.map((field) => (
            <span key={field} className="truncate p-[5px]">
              {field}
            </span>
          ))}
        </span>
        {template.sample.map((row) => (
          <span
            key={row.join("|")}
            className="grid grid-cols-[1.25fr_.9fr_1fr] border-t border-line"
          >
            {row.map((cell, index) => (
              <span
                key={cell}
                className={cn(
                  "truncate p-[5px]",
                  index === 2 && /^(gấp|urgent)$/i.test(cell)
                    ? "font-semibold text-danger-text"
                    : "",
                )}
              >
                {cell}
              </span>
            ))}
          </span>
        ))}
      </span>
    );
  return (
    <span
      aria-hidden="true"
      className={cn(
        PREVIEW_SHELL,
        "h-[92px] flex-col items-start gap-1.5 border-dashed border-line-strong",
      )}
    >
      <span className="rounded-[4px] bg-state-hover-strong px-2 py-[3px] font-semibold text-foreground">
        {template.fields[0] ?? ""}
      </span>
      <span className="inline-flex items-center gap-[3px] text-meta-foreground">
        <Plus className="size-2.75" />
        {t.blankAddColumn}
      </span>
      <span className="inline-flex items-center gap-[3px] text-meta-foreground">
        <Plus className="size-2.75" />
        {t.blankAddItem}
      </span>
    </span>
  );
}

export function BoardStep({
  copy,
  draft,
  catalog,
  onDraft,
  showNameError,
}: {
  copy: OnboardingCopy;
  draft: OnboardingDraftData;
  catalog: OnboardingCatalog | null;
  onDraft: DraftChange;
  showNameError: boolean;
}) {
  const t = copy.board;
  const { board } = draft;
  const patch = (next: Partial<OnboardingDraftData["board"]>) =>
    onDraft({ ...draft, board: { ...board, ...next } });
  const department = draft.departments.find(
    (entry) => entry.key === board.departmentKey,
  );
  const templates = catalog?.boardTemplates ?? [];
  return (
    <>
      <StepTitle title={t.title} lead={t.lead} />
      <div
        role="radiogroup"
        aria-label={t.templatesLabel}
        className="grid grid-cols-1 gap-3 md:grid-cols-6"
      >
        {templates.map((template, index) => {
          const on = board.templateId === template.id;
          const text = t.templates[template.id as BoardTemplateId];
          return (
            <button
              key={template.id}
              type="button"
              role="radio"
              aria-checked={on}
              onClick={() => patch({ templateId: template.id })}
              className={cn(
                "flex min-w-0 cursor-pointer flex-col gap-2.5 rounded-[12px] border bg-popover p-3 text-left outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring",
                index < 2 ? "md:col-span-3" : "md:col-span-2",
                on
                  ? "border-primary ring-1 ring-primary"
                  : "border-line-strong",
              )}
            >
              <span className="flex items-start gap-[9px]">
                <span
                  aria-hidden="true"
                  className={cn(
                    "mt-0.5 grid size-4 shrink-0 place-items-center rounded-full border",
                    on ? "border-primary" : "border-line-strong",
                  )}
                >
                  {on ? (
                    <span className="size-2 rounded-full bg-primary" />
                  ) : null}
                </span>
                <span className="min-w-0">
                  <b className="block text-body leading-[1.45] font-semibold">
                    {text.name}
                  </b>
                  <span className="mt-px block text-item-note leading-[1.45] text-meta-foreground">
                    {text.summary}
                  </span>
                </span>
              </span>
              <TemplatePreview template={template} copy={copy} />
            </button>
          );
        })}
      </div>
      <div className="grid grid-cols-[1.1fr_1fr] gap-4">
        <div className="flex flex-col gap-1.5">
          <FieldLabel htmlFor="onb-board-name">{t.nameLabel}</FieldLabel>
          <input
            id="onb-board-name"
            type="text"
            value={board.name}
            placeholder={t.namePlaceholder}
            aria-invalid={
              showNameError && !board.name.trim() ? true : undefined
            }
            onChange={(event) => patch({ name: event.target.value })}
            className={FIELD_INPUT_CLASS}
          />
          {showNameError && !board.name.trim() ? (
            <ErrorLine>{t.nameRequired}</ErrorLine>
          ) : null}
        </div>
        <div className="flex flex-col gap-1.5">
          <FieldLabel htmlFor="onb-board-dept">{t.departmentLabel}</FieldLabel>
          <Select
            value={board.departmentKey ?? NONE}
            onValueChange={(value) =>
              patch({ departmentKey: value === NONE ? null : value })
            }
          >
            <SelectTrigger
              id="onb-board-dept"
              className="w-full rounded-[8px] data-[size=default]:h-10 max-md:data-[size=default]:h-touch"
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={NONE}>{t.noDepartment}</SelectItem>
              {draft.departments.map((entry) => (
                <SelectItem key={entry.key} value={entry.key}>
                  {entry.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>
      <p className="-mt-3 text-crumb leading-normal text-meta-foreground">
        {department
          ? fillOnboarding(t.departmentNote, { name: department.name })
          : t.rootNote}
      </p>
    </>
  );
}

export { pluralOnboarding };
