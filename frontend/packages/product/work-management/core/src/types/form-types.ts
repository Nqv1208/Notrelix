/**
 * Forms of a board: a manager builds a form, members of the board submit it,
 * the manager turns a response into an item. The wire speaks the producer's
 * enum names; these are the words the product uses.
 */

export type FormStatus = "draft" | "published" | "closed";

/** The producer's `FormQuestionType`, in wire order (the request sends the index). */
export const FORM_QUESTION_TYPES = [
  "text",
  "longtext",
  "number",
  "date",
  "select",
  "multiselect",
  "checkbox",
  "email",
  "phone",
  "file",
  "signature",
] as const;
export type FormQuestionType = (typeof FORM_QUESTION_TYPES)[number];

/** Types the builder offers; files and signatures have nowhere to be stored on an item yet. */
export const FORM_SUPPORTED_QUESTION_TYPES: readonly FormQuestionType[] =
  FORM_QUESTION_TYPES.filter((type) => type !== "file" && type !== "signature");

const WIRE_NAMES: Readonly<Record<string, FormQuestionType>> = {
  ShortText: "text",
  LongText: "longtext",
  Number: "number",
  Date: "date",
  Select: "select",
  MultiSelect: "multiselect",
  Checkbox: "checkbox",
  Email: "email",
  Phone: "phone",
  FileUpload: "file",
  Signature: "signature",
};

export function parseFormQuestionType(
  value: string | null | undefined,
): FormQuestionType {
  return WIRE_NAMES[value ?? ""] ?? "text";
}

export function formQuestionTypeIndex(type: FormQuestionType): number {
  return FORM_QUESTION_TYPES.indexOf(type);
}

export function parseFormStatus(value: string | null | undefined): FormStatus {
  return value === "Published"
    ? "published"
    : value === "Closed"
      ? "closed"
      : "draft";
}

export interface FormQuestionOption {
  readonly key: string;
  readonly label: string;
}

/** The question keys whose answer fills the item's own name, description and due date (a board field has no such column). */
export const FORM_TITLE_KEY = "title";
export const FORM_DESCRIPTION_KEY = "description";
export const FORM_DUE_KEY = "due";

export interface FormQuestion {
  readonly id: string;
  /** Fixed when the question is added; the answer of a submission is keyed by it. */
  readonly key: string;
  readonly label: string;
  readonly type: FormQuestionType;
  readonly required: boolean;
  /** The board field the answer goes into when the item is created. */
  readonly boardFieldId: string | null;
  /** Fractional-index order key. */
  readonly position: string;
  readonly placeholder: string | null;
  readonly helpText: string | null;
  readonly maxLength: number | null;
  readonly minValue: number | null;
  readonly maxValue: number | null;
  /** Own choices of an unmapped select; a mapped one reads the field's options. */
  readonly options: readonly FormQuestionOption[];
}

export interface Form {
  readonly id: string;
  readonly boardId: string;
  readonly title: string;
  readonly status: FormStatus;
  readonly createdAt: string;
}

export interface FormDetail extends Form {
  readonly description: string;
  readonly version: number;
  /** In reading order. */
  readonly questions: readonly FormQuestion[];
}

/** Where a response sits in the form's inbox. */
export type FormSubmissionStatus = "new" | "processed" | "rejected" | "spam";

export interface FormSubmission {
  readonly id: string;
  readonly formId: string;
  readonly status: FormSubmissionStatus;
  readonly submitterUserId: string | null;
  readonly submitterEmail: string | null;
  readonly submittedAt: string;
  readonly processedAt: string | null;
  readonly createdItemId: string | null;
  readonly reason: string | null;
  /** Answers by question key. */
  readonly answers: Readonly<Record<string, unknown>>;
}

/** The submission status words of the producer, as the inbox tabs read them. */
export function parseFormSubmissionStatus(
  status: string | null | undefined,
  createdItemId: string | null | undefined,
): FormSubmissionStatus | "deleted" {
  if (status === "Rejected") return "rejected";
  if (status === "Spam") return "spam";
  if (status === "Deleted") return "deleted";
  return createdItemId ? "processed" : "new";
}

export function parseFormAnswers(
  payload: string | null | undefined,
): Record<string, unknown> {
  if (!payload) return {};
  try {
    const value: unknown = JSON.parse(payload);
    return typeof value === "object" && value !== null && !Array.isArray(value)
      ? (value as Record<string, unknown>)
      : {};
  } catch {
    return {};
  }
}

/** `{ key: error }` of a refused submit, keyed by question key. */
export type FormAnswerErrors = Readonly<Record<string, string>>;

/** The questions of a mapped field that two questions share (a field takes one answer). */
export function duplicateFieldMappings(
  questions: readonly FormQuestion[],
): ReadonlyMap<string, readonly FormQuestion[]> {
  const byField = new Map<string, FormQuestion[]>();
  for (const question of questions) {
    if (!question.boardFieldId) continue;
    byField.set(question.boardFieldId, [
      ...(byField.get(question.boardFieldId) ?? []),
      question,
    ]);
  }
  for (const [fieldId, list] of byField)
    if (list.length < 2) byField.delete(fieldId);
  return byField;
}

// --- Fractional index keys (the producer orders questions by these). ---

const DIGITS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
const SMALLEST_INTEGER = `A${"0".repeat(26)}`;

function midpoint(a: string, b: string | null): string {
  if (b !== null && a >= b) throw new Error(`${a} >= ${b}`);
  if (b !== null) {
    let n = 0;
    while ((a[n] ?? "0") === b[n]) n += 1;
    if (n > 0) return b.slice(0, n) + midpoint(a.slice(n), b.slice(n));
  }
  const digitA = a ? DIGITS.indexOf(a[0]!) : 0;
  const digitB = b !== null ? DIGITS.indexOf(b[0]!) : DIGITS.length;
  if (digitB - digitA > 1) return DIGITS[Math.round(0.5 * (digitA + digitB))]!;
  if (b !== null && b.length > 1) return b.slice(0, 1);
  return DIGITS[digitA]! + midpoint(a.slice(1), null);
}

function integerLength(head: string): number {
  if (head >= "a" && head <= "z") return head.charCodeAt(0) - 97 + 2;
  if (head >= "A" && head <= "Z") return 90 - head.charCodeAt(0) + 2;
  throw new Error(`invalid order key head ${head}`);
}

function integerPart(key: string): string {
  return key.slice(0, integerLength(key[0]!));
}

function incrementInteger(x: string): string | null {
  const head = x[0]!;
  const digits = x.slice(1).split("");
  let carry = true;
  for (let i = digits.length - 1; carry && i >= 0; i -= 1) {
    const d = DIGITS.indexOf(digits[i]!) + 1;
    if (d === DIGITS.length) digits[i] = DIGITS[0]!;
    else {
      digits[i] = DIGITS[d]!;
      carry = false;
    }
  }
  if (carry) {
    if (head === "Z") return `a${DIGITS[0]}`;
    if (head === "z") return null;
    const next = String.fromCharCode(head.charCodeAt(0) + 1);
    if (next > "a") digits.push(DIGITS[0]!);
    else digits.pop();
    return next + digits.join("");
  }
  return head + digits.join("");
}

function decrementInteger(x: string): string | null {
  const head = x[0]!;
  const digits = x.slice(1).split("");
  let borrow = true;
  for (let i = digits.length - 1; borrow && i >= 0; i -= 1) {
    const d = DIGITS.indexOf(digits[i]!) - 1;
    if (d === -1) digits[i] = DIGITS[DIGITS.length - 1]!;
    else {
      digits[i] = DIGITS[d]!;
      borrow = false;
    }
  }
  if (borrow) {
    if (head === "a") return `Z${DIGITS[DIGITS.length - 1]}`;
    if (head === "A") return null;
    const next = String.fromCharCode(head.charCodeAt(0) - 1);
    if (next < "Z") digits.push(DIGITS[DIGITS.length - 1]!);
    else digits.pop();
    return next + digits.join("");
  }
  return head + digits.join("");
}

/** An order key strictly between `a` and `b` (either may be null: unbounded). */
export function orderKeyBetween(a: string | null, b: string | null): string {
  if (a !== null && b !== null && a >= b) throw new Error(`${a} >= ${b}`);
  if (a === null && b === null) return `a${DIGITS[0]}`;
  if (a === null) {
    const ib = integerPart(b!);
    const fb = b!.slice(ib.length);
    if (ib === SMALLEST_INTEGER) return ib + midpoint("", fb);
    if (ib < b!) return ib;
    const lower = decrementInteger(ib);
    if (lower === null) throw new Error("cannot decrement any more");
    return lower;
  }
  const ia = integerPart(a);
  const fa = a.slice(ia.length);
  if (b === null) return incrementInteger(ia) ?? ia + midpoint(fa, null);
  const ib = integerPart(b);
  const fb = b.slice(ib.length);
  if (ia === ib) return ia + midpoint(fa, fb);
  const next = incrementInteger(ia);
  if (next === null) throw new Error("cannot increment any more");
  return next < b ? next : ia + midpoint(fa, null);
}
