import { useCallback, useMemo, useRef, useState } from "react";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import { findItemFields } from "../card-detail/item-field-editors";
import type {
  ItemSheetGroup,
  TaskDetailCallbacks,
  TaskDetailCapabilities,
  TaskDetailData,
} from "../card-detail/task-detail-types";
import {
  type CardPopupActions,
  type CardPopupMode,
  type CardPopupReadOnly,
  type CardPopupModel,
  type CardPopupPicker,
  type CardPopupSlots,
  type FieldSave,
} from "./card-popup-context";
import { useCapabilitySlots } from "./capabilities/use-capability-slots";
import { useCardPopupCopy } from "./use-card-popup-copy";

export interface CardPopupModelInput {
  readonly mode: CardPopupMode;
  readonly board: Board;
  readonly card: CardDetail;
  readonly capabilities: TaskDetailCapabilities;
  readonly data: TaskDetailData;
  readonly callbacks: TaskDetailCallbacks;
  readonly actions?: CardPopupActions;
  readonly groups?: readonly ItemSheetGroup[];
  readonly now: Date;
  readonly timeZone?: string;
  readonly slots?: CardPopupSlots;
  /** Forces the read-only reason (create mode while the request is out). */
  readonly readOnly?: CardPopupReadOnly;
}

const NO_ACTIONS: CardPopupActions = {};
const NO_SLOTS: CardPopupSlots = {};
const NO_GROUPS: readonly ItemSheetGroup[] = [];

/**
 * Builds the model every part of the popup reads: the card and its writes, the
 * words, the read-only reason, the open picker and the per-field save state.
 * The view container and the create host both use it.
 */
export function useCardPopupModel(input: CardPopupModelInput): CardPopupModel {
  const copy = useCardPopupCopy();
  const [picker, setPicker] = useState<CardPopupPicker | null>(null);
  const [saves, setSaves] = useState<Record<string, FieldSave | undefined>>({});
  const [conflict, setConflict] = useState(false);
  const mounted = useRef(true);

  const track = useCallback((key: string, run: () => Promise<unknown>) => {
    setSaves((current) => ({ ...current, [key]: { status: "saving" } }));
    const attempt = () => {
      setSaves((current) => ({ ...current, [key]: { status: "saving" } }));
      run().then(
        () => {
          if (mounted.current)
            setSaves((current) => ({ ...current, [key]: undefined }));
        },
        (error: unknown) => {
          // A refused version is not a retry: the fields are re-read and the notice shows.
          if ((error as { kind?: string } | null)?.kind === "conflict") {
            if (mounted.current) {
              setConflict(true);
              setSaves((current) => ({ ...current, [key]: undefined }));
            }
            return;
          }
          if (mounted.current)
            setSaves((current) => ({
              ...current,
              [key]: { status: "error", retry: attempt },
            }));
        },
      );
    };
    attempt();
  }, []);

  const readOnly =
    input.readOnly ??
    (input.capabilities.frozen
      ? "archived"
      : input.capabilities.canEditFields
        ? null
        : "permission");
  const fields = useMemo(() => findItemFields(input.board), [input.board]);
  const capabilitySlots = useCapabilitySlots({
    card: input.card,
    board: input.board,
    exists: input.mode === "view",
    canEdit: readOnly === null,
    canVote: input.capabilities.canComment ?? input.capabilities.canEditFields,
  });
  const slots = useMemo(
    () => ({ ...capabilitySlots, ...(input.slots ?? NO_SLOTS) }),
    [capabilitySlots, input.slots],
  );

  return {
    mode: input.mode,
    board: input.board,
    card: input.card,
    capabilities: input.capabilities,
    readOnly,
    data: input.data,
    callbacks: input.callbacks,
    actions: input.actions ?? NO_ACTIONS,
    groups: input.groups ?? NO_GROUPS,
    fields,
    copy,
    now: input.now,
    timeZone: input.timeZone,
    slots,
    saves,
    track,
    conflict,
    clearConflict: () => setConflict(false),
    picker,
    openPicker: setPicker,
  };
}
