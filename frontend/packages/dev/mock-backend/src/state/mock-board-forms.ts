/**
 * The forms of the mock world's boards (the producer's `Form`, `FormQuestion`
 * and `FormSubmission`): a manager builds a form, members of the board submit
 * answers to a published one, the manager decides each response. Deleting a
 * form is soft; a deleted form can be restored.
 */
import type { MockStore } from "./mock-store";

export type MockFormStatus = "Draft" | "Published" | "Closed";
export type MockSubmissionStatus = "Accepted" | "Rejected" | "Spam" | "Deleted";

export interface MockFormQuestion {
  id: string;
  questionKey: string;
  label: string;
  /** The producer's enum name ("ShortText", "Select", ...). */
  questionType: string;
  isRequired: boolean;
  boardFieldId: string | null;
  position: string;
  placeholder: string | null;
  helpText: string | null;
  maxLength: number | null;
  minValue: number | null;
  maxValue: number | null;
  options: { key: string; label: string }[] | null;
}

export interface MockForm {
  id: string;
  boardId: string;
  name: string;
  slug: string;
  status: MockFormStatus;
  description: string;
  version: number;
  createdAt: string;
  deleted: boolean;
  /** The whole public link (`/f/<key>`) while the form is public; null while it is private. */
  publicKey: string | null;
  questions: MockFormQuestion[];
}

export interface MockFormSubmission {
  id: string;
  formId: string;
  boardId: string;
  createdItemId: string | null;
  submitterUserId: string | null;
  submitterEmail: string | null;
  status: MockSubmissionStatus;
  submittedAt: string;
  processedAt: string | null;
  payloadJson: string;
  reason: string | null;
  /** The idempotency key of the submit that made it: a retry replays it. */
  key: string | null;
}

export class MockBoardForms {
  private readonly formsById = new Map<string, MockForm>();
  private readonly submissionsById = new Map<string, MockFormSubmission>();
  /** Anonymous sends by public key, as virtual seconds (the rate limit counts the last minute). */
  readonly publicHits = new Map<string, number[]>();

  constructor(private readonly store: () => MockStore) {}

  clear(): void {
    this.formsById.clear();
    this.submissionsById.clear();
    this.publicHits.clear();
  }

  /** The form a public key belongs to, whatever its state; null for a key no form holds. */
  findByPublicKey(key: string): MockForm | undefined {
    for (const form of this.formsById.values())
      if (form.publicKey !== null && form.publicKey === key) return form;
    return undefined;
  }

  /** A key no earlier link had: a form made private and public again gets a new one. */
  mintPublicKey(): string {
    return `mockpk-${String(this.store().nextSequence("form-public-key")).padStart(6, "0")}`;
  }

  /** The live forms of a board, newest first. */
  list(boardId: string): MockForm[] {
    return [...this.formsById.values()]
      .filter((form) => form.boardId === boardId && !form.deleted)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  }

  find(formId: string): MockForm | undefined {
    return this.formsById.get(formId);
  }

  put(form: MockForm): void {
    this.formsById.set(form.id, form);
  }

  add(boardId: string, name: string): MockForm {
    const form: MockForm = {
      id: this.store().nextId("form"),
      boardId,
      name,
      slug: name.toLowerCase().replace(/[^a-z0-9]+/g, "-"),
      status: "Draft",
      description: "",
      version: 1,
      createdAt: this.store().now(),
      deleted: false,
      publicKey: null,
      questions: [],
    };
    this.formsById.set(form.id, form);
    return form;
  }

  submissions(formId: string): MockFormSubmission[] {
    return [...this.submissionsById.values()]
      .filter((s) => s.formId === formId && s.status !== "Deleted")
      .sort((a, b) => b.submittedAt.localeCompare(a.submittedAt));
  }

  findSubmission(submissionId: string): MockFormSubmission | undefined {
    return this.submissionsById.get(submissionId);
  }

  putSubmission(submission: MockFormSubmission): void {
    this.submissionsById.set(submission.id, submission);
  }

  addSubmission(
    form: MockForm,
    input: Pick<
      MockFormSubmission,
      "submitterUserId" | "submitterEmail" | "payloadJson" | "key"
    >,
  ): MockFormSubmission {
    const submission: MockFormSubmission = {
      ...input,
      id: this.store().nextId("fsub"),
      formId: form.id,
      boardId: form.boardId,
      createdItemId: null,
      status: "Accepted",
      submittedAt: this.store().now(),
      processedAt: null,
      reason: null,
    };
    this.submissionsById.set(submission.id, submission);
    return submission;
  }
}
