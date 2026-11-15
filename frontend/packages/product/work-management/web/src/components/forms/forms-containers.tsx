import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import { useLocale } from "@notrelix/i18n";
import {
  useBoardAccess,
  useBoardForms,
  useBoardLabels,
  useCreateItemWithDetails,
  useDeleteCard,
  useForm,
  useFormSubmissionCounts,
  useFormSubmissions,
  useFormWrites,
  useFullBoard,
  useSubmitForm,
} from "@notrelix/work-management-state";
import type {
  FormSubmission,
  FormWriteResult,
  FormSubmissionStatus,
} from "@notrelix/work-management-core";
import { findItemFields } from "../card-detail/item-field-editors";
import {
  ResponseItemDialog,
  type ResponseItemValues,
} from "./response-item-dialog";
import { FormBuilderSurface } from "./form-builder-surface";
import { FormFillSurface } from "./form-fill-surface";
import { FormInboxSurface, whenLong } from "./form-inbox-surface";
import type { BoardFormsPanelProps } from "./forms-list-panel";
import { fillFormsCopy } from "./forms.copy";
import { boardFormFields, prefillFromResponse } from "./form-model";
import { useFormsCopy } from "./use-forms-copy";
import { toastDecisionWithUndo } from "./form-decision-toast";

const UNDO_MS = 8000;

/** The member link of a form: in the app, signed in, on the form's board. */
export function formLink(
  workspaceId: string,
  boardId: string,
  formId: string,
): string {
  return `${window.location.origin}/workspaces/${workspaceId}/boards/${encodeURIComponent(boardId)}/forms/${encodeURIComponent(formId)}`;
}

async function copyText(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch {
    return false;
  }
}

function readStatus(error: unknown): number | undefined {
  return (error as { status?: number } | null)?.status;
}

/** The "Biểu mẫu" section of the board settings: the forms, their counts and every write on them. */
export function useBoardFormsPanel(
  boardId: string,
  workspaceId: string,
  nav: {
    readonly onOpenBuilder: (formId: string) => void;
    readonly onOpenResponses: (formId: string) => void;
  },
): BoardFormsPanelProps {
  const copy = useFormsCopy();
  const { locale } = useLocale();
  const list = useBoardForms(boardId, workspaceId);
  const writes = useFormWrites(boardId, workspaceId);
  const { board } = useFullBoard(boardId, workspaceId);
  // A stale write leaves the list as it was (the builder shows what changed); the list has no such view, so it
  // reads the form again and the person tries again on the version it now holds.
  const settled = async (
    formId: string,
    send: Promise<FormWriteResult>,
  ): Promise<FormWriteResult> => {
    const result = await send;
    if (!result.ok && result.reason === "conflict") await writes.reload(formId);
    return result;
  };

  return {
    archived: board?.isArchived === true,
    copy,
    locale,
    status: list.isError ? "error" : list.isLoading ? "loading" : "ready",
    forms: list.data ?? [],
    onReload: () => void list.refetch(),
    onCreate: async (title) => {
      const result = await writes.create(title);
      if (result.ok && result.id) nav.onOpenBuilder(result.id);
      return result;
    },
    onPublish: (formId) => settled(formId, writes.publish(formId)),
    onClose: (formId) => settled(formId, writes.close(formId)),
    onDelete: async (form) => {
      const result = await settled(form.id, writes.remove(form.id));
      if (result.ok)
        toast(fillFormsCopy(copy.list.removed, { name: form.title }), {
          duration: UNDO_MS,
          action: {
            label: copy.common.undo,
            onClick: () =>
              void writes.restore(form.id).then((r) => {
                if (!r.ok)
                  toast.error(
                    fillFormsCopy(copy.list.restoreFailed, {
                      name: form.title,
                    }),
                  );
              }),
          },
        });
      return result;
    },
    onOpenBuilder: nav.onOpenBuilder,
    onOpenResponses: nav.onOpenResponses,
    onCopyLink: (formId) => copyText(formLink(workspaceId, boardId, formId)),
  };
}

/** The manager reads the access level of the board; the builder is theirs only. */
function useIsManager(boardId: string, workspaceId: string) {
  const { access, isLoading, error } = useBoardAccess(boardId, workspaceId);
  return {
    manager: access?.myLevel === "manager",
    pending: isLoading && !error,
  };
}

/**
 * Handling the responses (read, process, reject, mark as spam, delete) needs edit rights on the board
 * (UpdateItem), which an editor has as well as a manager; the builder stays the manager's.
 */
function useCanHandleResponses(boardId: string, workspaceId: string) {
  const { access, isLoading, error } = useBoardAccess(boardId, workspaceId);
  return {
    allowed: access?.myLevel === "manager" || access?.myLevel === "editor",
    manager: access?.myLevel === "manager",
    pending: isLoading && !error,
  };
}

export function FormBuilderPage({
  workspaceId,
  boardId,
  formId,
  selectedId,
  onSelect,
  preview,
  onPreview,
  user,
  onBack,
}: {
  readonly workspaceId: string;
  readonly boardId: string;
  readonly formId: string;
  readonly user?: { readonly name: string; readonly email?: string };
  readonly selectedId: string | null;
  readonly onSelect: (questionId: string | null) => void;
  readonly preview: boolean;
  readonly onPreview: (on: boolean) => void;
  readonly onBack: () => void;
}) {
  const copy = useFormsCopy();
  const { locale } = useLocale();
  const query = useForm(formId, workspaceId);
  const writes = useFormWrites(boardId, workspaceId);
  const { board, fieldDefinitions } = useFullBoard(boardId, workspaceId);
  const { manager, pending } = useIsManager(boardId, workspaceId);
  const fields = useMemo(
    () => boardFormFields(fieldDefinitions),
    [fieldDefinitions],
  );
  const missing =
    [403, 404].includes(readStatus(query.error) ?? 0) || (!pending && !manager);
  const status = missing
    ? "missing"
    : query.isError
      ? "error"
      : query.isLoading || pending
        ? "loading"
        : "ready";
  const form = query.data ?? null;

  return (
    <FormBuilderSurface
      copy={copy}
      status={status}
      form={form}
      fields={fields}
      boardTitle={board?.title ?? ""}
      selectedId={selectedId}
      onSelect={onSelect}
      savedAt={
        query.dataUpdatedAt
          ? new Intl.DateTimeFormat(locale === "vi" ? "vi-VN" : "en-GB", {
              hour: "2-digit",
              minute: "2-digit",
            }).format(query.dataUpdatedAt)
          : undefined
      }
      preview={preview}
      user={user}
      archived={board?.isArchived === true}
      onPreview={onPreview}
      linkUrl={formLink(workspaceId, boardId, formId)}
      todayLabel={todayLabel(locale)}
      onUpdateDetails={(title, description) =>
        writes.updateDetails(formId, title, description)
      }
      onAddQuestion={(question) => writes.addQuestion(formId, question)}
      onUpdateQuestion={(question, changes) =>
        writes.updateQuestion(formId, question.id, changes)
      }
      onRemoveQuestion={(question) =>
        writes.removeQuestion(formId, question.id)
      }
      onPublish={() => writes.publish(formId)}
      onClose={() => writes.close(formId)}
      onCopyLink={() => copyText(formLink(workspaceId, boardId, formId))}
      onReload={() => writes.reload(formId)}
      onReadLatest={() => writes.readLatest(formId)}
      onBack={onBack}
    />
  );
}

function todayLabel(locale: "vi" | "en"): string {
  return new Intl.DateTimeFormat(locale === "vi" ? "vi-VN" : "en-GB", {
    weekday: "long",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  })
    .format(new Date())
    .replace(",", "");
}

/** The page a member opens from the link: the form, filled and sent to the inbox. */
export function FormFillPage({
  workspaceId,
  boardId,
  formId,
  user,
  onBack,
  onHome,
}: {
  readonly workspaceId: string;
  readonly boardId: string;
  readonly formId: string;
  readonly user?: { readonly name: string; readonly email?: string };
  readonly onBack: () => void;
  readonly onHome: () => void;
}) {
  const copy = useFormsCopy();
  const { locale } = useLocale();
  const query = useForm(formId, workspaceId);
  const submit = useSubmitForm(formId, workspaceId);
  const { board, fieldDefinitions } = useFullBoard(boardId, workspaceId);
  const { access } = useBoardAccess(boardId, workspaceId);
  const fields = useMemo(
    () => boardFormFields(fieldDefinitions),
    [fieldDefinitions],
  );
  const missing = [403, 404].includes(readStatus(query.error) ?? 0);
  const status = missing
    ? "notFound"
    : query.isError
      ? "error"
      : query.isLoading
        ? "loading"
        : "ready";
  const level = access?.myLevel;

  return (
    <FormFillSurface
      copy={copy}
      status={status}
      form={query.data ?? null}
      fields={fields}
      boardTitle={board?.title ?? ""}
      user={user}
      todayLabel={todayLabel(locale)}
      archived={board?.isArchived === true}
      canManage={level === "manager"}
      canSubmit={
        level === undefined
          ? undefined
          : level === "editor" || level === "manager"
      }
      onBack={onBack}
      onHome={onHome}
      onReload={() => void query.refetch()}
      onCopyText={copyText}
      onSubmit={submit}
    />
  );
}

/** The inbox of a form: every decision on a response is written at once. */
export function FormInboxPage({
  workspaceId,
  boardId,
  formId,
  currentUserId,
  deciderName,
  now,
  onOpenItem,
  onBack,
  onBackToBoard,
}: {
  readonly workspaceId: string;
  readonly boardId: string;
  readonly formId: string;
  readonly currentUserId?: string | null;
  readonly deciderName?: string;
  readonly now?: Date;
  readonly onOpenItem: (itemId: string) => void;
  /** Back to the forms list in the board settings, where a manager came from. */
  readonly onBack: () => void;
  /** Back to the board, for an editor who reaches the inbox from the board menu. */
  readonly onBackToBoard: () => void;
}) {
  const copy = useFormsCopy();
  const { locale } = useLocale();
  const formQuery = useForm(formId, workspaceId);
  const submissions = useFormSubmissions(formId, workspaceId);
  const totals = useFormSubmissionCounts(formId, workspaceId);
  const writes = useFormWrites(boardId, workspaceId);
  const { board, groups, fieldDefinitions } = useFullBoard(
    boardId,
    workspaceId,
  );
  const { allowed, manager, pending } = useCanHandleResponses(
    boardId,
    workspaceId,
  );
  const fields = useMemo(
    () => boardFormFields(fieldDefinitions),
    [fieldDefinitions],
  );
  const [tab, setTab] = useState<FormSubmissionStatus>("new");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [conflict, setConflict] = useState(false);
  const [failed, setFailed] = useState<{ retry: () => void } | null>(null);
  const [creating, setCreating] = useState<FormSubmission | null>(null);
  const [createFailed, setCreateFailed] = useState(false);
  const createItem = useCreateItemWithDetails(boardId, workspaceId);
  const deleteItem = useDeleteCard(boardId, workspaceId);
  const createPending = createItem.isPending;
  const boardLabels = useBoardLabels(boardId, workspaceId, Boolean(creating));

  /**
   * "Tạo việc từ phản hồi": creates the item, marks the response processed, and offers an undo that removes the
   * item and returns the response to New.
   */
  async function createFromResponse(
    response: FormSubmission,
    values: ResponseItemValues,
  ) {
    if (!board) return;
    setCreateFailed(false);
    const fieldsOfBoard = findItemFields(board);
    let created;
    try {
      created = await createItem.mutateAsync({
        listId: values.groupId,
        title: values.title.trim(),
        descriptionMd: values.description.trim() || undefined,
        startDate: values.startDay
          ? `${values.startDay}T00:00:00.000Z`
          : undefined,
        assigneeIds: values.assigneeIds,
        labelIds: values.labelIds,
        dueDate: values.dueDay ? `${values.dueDay}T00:00:00.000Z` : undefined,
        statusOptionId:
          values.statusOptionId ?? fieldsOfBoard.status?.options[0]?.id ?? null,
        priority:
          fieldsOfBoard.priority && values.priorityOptionId
            ? {
                fieldDefinitionId: fieldsOfBoard.priority.id,
                value: values.priorityOptionId,
              }
            : null,
      });
    } catch {
      setCreateFailed(true);
      return;
    }
    setCreating(null);
    if (!created.itemId) return;
    const itemId = created.itemId;
    const result = await writes.process(formId, response.id, itemId);
    if (!result.ok) {
      if (result.reason === "conflict") setConflict(true);
      else toast.error(copy.inbox.createFailedProcess);
      return;
    }
    setSelectedId(null);
    toastDecisionWithUndo(
      copy.inbox.createdToast,
      {
        undo: copy.common.undo,
        undoFailed: copy.createFromResponse.undoFailed,
      },
      async () => {
        const reopened = await writes.reopenSubmission(
          formId,
          response.id,
          true,
        );
        if (!reopened.ok) return reopened;
        try {
          await deleteItem.mutateAsync({
            cardId: itemId,
            expectedVersion: created.version,
          });
        } catch {
          return { ok: false, reason: "failed" } as FormWriteResult;
        }
        toast(copy.createFromResponse.undone);
        return reopened;
      },
    );
  }
  const people = useMemo(
    () =>
      Object.fromEntries((board?.members ?? []).map((m) => [m.userId, m.name])),
    [board?.members],
  );

  /**
   * A decision on a response is written at once; a refusal leaves the response where it was. A reject, a spam
   * mark or a delete says so with an undo for a few seconds: the undo returns the response to the New tab.
   */
  const decide = useCallback(
    async (
      message: string,
      send: () => Promise<FormWriteResult>,
      undoable?: FormSubmission,
    ) => {
      setConflict(false);
      setFailed(null);
      const result = await send();
      if (result.ok) {
        setSelectedId(null);
        if (undoable)
          toastDecisionWithUndo(
            message,
            { undo: copy.common.undo, undoFailed: copy.inbox.undoFailed },
            () => writes.reopenSubmission(undoable.formId, undoable.id),
          );
        else toast(message);
      } else if (result.reason === "conflict") setConflict(true);
      else
        setFailed({
          retry: () => void decide(message, send, undoable),
        });
      return result;
    },
    [copy, writes],
  );

  const status =
    [403, 404].includes(readStatus(formQuery.error) ?? 0) ||
    (!pending && !allowed)
      ? "missing"
      : formQuery.isError
        ? "error"
        : formQuery.isLoading || pending
          ? "loading"
          : submissions.isError
            ? "error"
            : submissions.isLoading
              ? "loading"
              : "ready";
  const visible = submissions.data ?? [];
  const form = formQuery.data ?? null;
  const nameOf = (s: FormSubmission) =>
    (s.submitterUserId ? people[s.submitterUserId] : undefined) ??
    s.submitterEmail ??
    copy.inbox.unknownPerson;

  const prefill =
    creating && form
      ? prefillFromResponse(
          form.questions,
          creating.answers,
          fields,
          board ? (findItemFields(board).priority?.id ?? null) : null,
        )
      : null;
  const tz = "Asia/Ho_Chi_Minh";

  return (
    <>
      <FormInboxSurface
        copy={copy}
        locale={locale}
        timeZone={tz}
        now={now ?? new Date()}
        status={status}
        form={form}
        fields={fields}
        submissions={visible}
        counts={totals.data}
        hasMore={submissions.hasNextPage}
        loadingMore={submissions.isFetchingNextPage}
        onLoadMore={() => void submissions.fetchNextPage()}
        people={people}
        deciderName={deciderName}
        tab={tab}
        onTab={(next) => {
          setTab(next);
          setSelectedId(null);
        }}
        selectedId={selectedId}
        onSelect={setSelectedId}
        conflict={conflict}
        failed={failed}
        archived={board?.isArchived === true}
        onCreateItem={(response) => {
          setConflict(false);
          setFailed(null);
          setCreating(response);
        }}
        onReject={(response, reason) =>
          decide(
            copy.inbox.rejected,
            () => writes.reject(formId, response.id, reason),
            response,
          )
        }
        onSpam={(response) =>
          decide(
            copy.inbox.spammed,
            () => writes.markSpam(formId, response.id),
            response,
          )
        }
        onRemove={(response) =>
          decide(
            copy.inbox.removed,
            () => writes.removeSubmission(formId, response.id),
            response,
          )
        }
        onOpenItem={onOpenItem}
        onReload={() => {
          void formQuery.refetch();
          void submissions.refetch();
        }}
        onBack={manager ? onBack : onBackToBoard}
        backLabel={manager ? undefined : copy.inbox.backToBoard}
      />
      {board && creating && form && prefill ? (
        <ResponseItemDialog
          copy={copy}
          boardTitle={board.title}
          notice={fillFormsCopy(copy.createFromResponse.prefilled, {
            name: nameOf(creating),
            when: whenLong(creating.submittedAt, locale, tz),
          })}
          initial={{
            title: prefill.title,
            description: prefill.description,
            groupId: groups[0]?.id ?? "",
            priorityOptionId: prefill.priorityOptionId,
            statusOptionId:
              findItemFields(board).status?.options[0]?.id ?? null,
            startDay: "",
            dueDay: prefill.dueDay,
            assigneeIds: [],
            labelIds: [],
          }}
          statuses={findItemFields(board).status?.options ?? []}
          members={board.members ?? []}
          currentUserId={currentUserId}
          labels={boardLabels.data ?? []}
          today={new Intl.DateTimeFormat("en-CA", { timeZone: tz }).format(
            now ?? new Date(),
          )}
          from={prefill.from}
          groups={groups}
          priorities={findItemFields(board).priority?.options ?? []}
          left={prefill.left.map((entry) =>
            fillFormsCopy(copy.createFromResponse.unmapped, entry),
          )}
          pending={createPending}
          failed={createFailed}
          onClose={() => {
            setCreateFailed(false);
            setCreating(null);
          }}
          onSubmit={(values) => void createFromResponse(creating, values)}
        />
      ) : null}
    </>
  );
}
