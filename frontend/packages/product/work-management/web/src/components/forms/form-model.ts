import {
  FORM_DESCRIPTION_KEY,
  FORM_DUE_KEY,
  FORM_TITLE_KEY,
  type FormAnswerErrors,
  type FormQuestion,
  type FormQuestionType,
} from "@notrelix/work-management-core";
import { fillFormsCopy, type FormsCopy } from "./forms.copy";

/** A field of the board a question can map to (id, name, type of the board field). */
export interface FormBoardField {
  readonly id: string;
  readonly name: string;
  readonly type:
    "text" | "number" | "date" | "checkbox" | "select" | "multiselect";
  readonly options: readonly { readonly id: string; readonly name: string }[];
}

/** Which board field types take the answer of a question type. */
export function compatibleFieldTypes(
  type: FormQuestionType,
): readonly FormBoardField["type"][] {
  switch (type) {
    case "text":
    case "email":
    case "phone":
      return ["text"];
    case "longtext":
      return ["text"];
    case "number":
      return ["number"];
    case "date":
      return ["date"];
    case "checkbox":
      return ["checkbox"];
    case "select":
      return ["select"];
    case "multiselect":
      return ["multiselect"];
    default:
      return [];
  }
}

/** The built-in item property a question key fills, if any (an item's name, description and due date are not board fields). */
export type BuiltInTarget = "title" | "description" | "due";

export function builtInTarget(question: FormQuestion): BuiltInTarget | null {
  const key = question.key;
  return key === FORM_TITLE_KEY
    ? "title"
    : key === FORM_DESCRIPTION_KEY
      ? "description"
      : key === FORM_DUE_KEY
        ? "due"
        : null;
}

/** Where a question's answer goes: the item property, the board field's name, or nothing. */
export function targetName(
  question: FormQuestion,
  fields: readonly FormBoardField[],
  copy: FormsCopy,
): string | null {
  const built = builtInTarget(question);
  if (built === "title") return copy.builder.mapTitle;
  if (built === "description") return copy.builder.mapDescription;
  if (built === "due") return copy.builder.mapDue;
  return (
    fields.find((field) => field.id === question.boardFieldId)?.name ?? null
  );
}

/** The options a select question offers: the mapped board field's, else its own. */
export function questionOptions(
  question: FormQuestion,
  fields: readonly FormBoardField[],
): readonly { readonly key: string; readonly label: string }[] {
  if (question.boardFieldId) {
    const field = fields.find((entry) => entry.id === question.boardFieldId);
    if (field) return field.options.map((o) => ({ key: o.id, label: o.name }));
  }
  return question.options;
}

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** The first problem of every answer, by question key (the member sees them before the server does). */
export function validateAnswers(
  questions: readonly FormQuestion[],
  answers: Readonly<Record<string, unknown>>,
  copy: FormsCopy,
): FormAnswerErrors {
  const errors: Record<string, string> = {};
  for (const q of questions) {
    const value = answers[q.key];
    const blank =
      value === undefined ||
      value === null ||
      (typeof value === "string" && value.trim() === "") ||
      (Array.isArray(value) && value.length === 0);
    if (blank) {
      if (q.required)
        errors[q.key] =
          q.type === "select" || q.type === "multiselect"
            ? copy.fill.errChoose
            : copy.fill.errRequired;
      continue;
    }
    if (q.type === "email" && !EMAIL.test(String(value)))
      errors[q.key] = copy.fill.errEmail;
    else if (
      typeof value === "string" &&
      q.maxLength !== null &&
      value.length > q.maxLength
    )
      errors[q.key] = fillFormsCopy(copy.fill.errTooLong, { max: q.maxLength });
    else if (q.type === "number") {
      const n = Number(value);
      if (Number.isNaN(n)) errors[q.key] = copy.fill.errNumber;
      else if (q.minValue !== null && n < q.minValue)
        errors[q.key] = fillFormsCopy(copy.fill.errMin, { min: q.minValue });
      else if (q.maxValue !== null && n > q.maxValue)
        errors[q.key] = fillFormsCopy(copy.fill.errMax, { max: q.maxValue });
    }
  }
  return errors;
}

/** What the answers of a response put into the "Add item" dialog. */
export interface ResponsePrefill {
  readonly title: string;
  readonly description: string;
  readonly priorityOptionId: string | null;
  /** `YYYY-MM-DD` or "". */
  readonly dueDay: string;
  /** The question each prefilled value came from, for the "Từ: ..." notes. */
  readonly from: {
    readonly title?: string;
    readonly description?: string;
    readonly priority?: string;
    readonly due?: string;
  };
  /** Answers that map to nothing in the item (they stay on the response). */
  readonly left: readonly { readonly label: string; readonly value: string }[];
}

export function answerText(
  question: FormQuestion,
  value: unknown,
  fields: readonly FormBoardField[],
): string {
  if (value === undefined || value === null || value === "") return "";
  if (Array.isArray(value))
    return value
      .map((entry) => optionLabel(question, entry, fields))
      .join(", ");
  if (typeof value === "boolean") return value ? "✓" : "";
  if (question.type === "date") return dayText(String(value));
  return optionLabel(question, value, fields);
}

function optionLabel(
  question: FormQuestion,
  value: unknown,
  fields: readonly FormBoardField[],
): string {
  const key = String(value);
  if (question.type === "select" || question.type === "multiselect")
    return (
      questionOptions(question, fields).find((o) => o.key === key)?.label ?? key
    );
  return key;
}

/** `2026-10-16` as `16/10/2026`. */
export function dayText(day: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(day);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : day;
}

export function prefillFromResponse(
  questions: readonly FormQuestion[],
  answers: Readonly<Record<string, unknown>>,
  fields: readonly FormBoardField[],
  priorityFieldId: string | null,
): ResponsePrefill {
  let title = "";
  let description = "";
  let priorityOptionId: string | null = null;
  let dueDay = "";
  const from: { -readonly [K in keyof ResponsePrefill["from"]]: string } = {};
  const left: { label: string; value: string }[] = [];
  for (const q of questions) {
    const value = answers[q.key];
    const text = answerText(q, value, fields);
    if (text === "") continue;
    const built = builtInTarget(q);
    if (built === "title") {
      title = String(value);
      from.title = q.label;
    } else if (built === "description") {
      description = String(value);
      from.description = q.label;
    } else if (built === "due" && /^\d{4}-\d{2}-\d{2}/.test(String(value))) {
      dueDay = String(value).slice(0, 10);
      from.due = q.label;
    } else if (
      priorityFieldId &&
      q.boardFieldId === priorityFieldId &&
      typeof value === "string"
    ) {
      priorityOptionId = value;
      from.priority = q.label;
    } else {
      left.push({ label: q.label, value: text });
    }
  }
  return { title, description, priorityOptionId, dueDay, from, left };
}

/** The board's fields a question can map to, from the board read (the status field and system columns are not offered). */
export function boardFormFields(
  definitions: readonly {
    readonly id: string;
    readonly name: string;
    readonly fieldType: string;
    readonly isSystemField?: boolean;
    readonly options: readonly {
      readonly id: string;
      readonly label: string;
    }[];
  }[],
): FormBoardField[] {
  const types: Readonly<Record<string, FormBoardField["type"]>> = {
    text: "text",
    number: "number",
    date: "date",
    checkbox: "checkbox",
    select: "select",
    multi_select: "multiselect",
  };
  return definitions.flatMap((field) => {
    const type = types[field.fieldType];
    return type
      ? [
          {
            id: field.id,
            name: field.name,
            type,
            options: field.options.map((o) => ({ id: o.id, name: o.label })),
          },
        ]
      : [];
  });
}
