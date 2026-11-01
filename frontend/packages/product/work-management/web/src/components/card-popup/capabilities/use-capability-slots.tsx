import { useMemo } from "react";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import type { CardPopupSlots } from "../card-popup-context";
import { CardCover } from "./card-cover";
import { ChecklistCopyFrom, ClearAllLinks } from "./checklist-links-controls";
import {
  ColourBlindToggle,
  useColourBlindLabelsControl,
} from "./colour-blind-label-mark";
import { CoverPicker } from "./cover-picker";
import { RepeatSelect, ReminderSelect } from "./repeat-reminder-select";
import { ViewersChip, VoteChip } from "./vote-viewers-chips";

/** The parts of the popup's mount slots the capabilities fill. */
export interface CapabilitySlots {
  readonly cover?: React.ReactNode;
  readonly coverBanner?: React.ReactNode;
  readonly repeat?: React.ReactNode;
  readonly reminder?: React.ReactNode;
  readonly chips?: React.ReactNode;
  readonly labelMarks?: React.ReactNode;
  readonly checklistExtras?: CardPopupSlots["checklistExtras"];
  readonly linksExtras?: React.ReactNode;
}

const NONE: CapabilitySlots = {};

/**
 * The controls of an existing card for the popup's slots. A card that does not exist yet (the create
 * form) has no item to write to, so it gets none. `canEdit` is the right to change the card, `canVote`
 * the right to comment.
 */
export function useCapabilitySlots({
  card,
  board,
  exists,
  canEdit,
  canVote,
}: {
  card: CardDetail;
  board: Board;
  exists: boolean;
  canEdit: boolean;
  canVote: boolean;
}): CapabilitySlots {
  const colourBlind = useColourBlindLabelsControl();
  return useMemo(() => {
    if (!exists) return NONE;
    const voteFields = board.fieldDefinitions.filter(
      (field) => field.fieldType === "vote" && !field.isHidden,
    );
    return {
      cover: <CoverPicker card={card} disabled={!canEdit} />,
      coverBanner:
        card.cover && card.cover.style === "strip" ? (
          <CardCover
            cover={card.cover}
            cardId={card.id}
            className="h-30 w-full"
          />
        ) : null,
      repeat: <RepeatSelect card={card} disabled={!canEdit} />,
      reminder: <ReminderSelect card={card} disabled={!canEdit} />,
      chips: (
        <>
          {voteFields.map((field) => (
            <VoteChip
              key={field.id}
              card={card}
              field={field}
              canVote={canVote}
            />
          ))}
          <ViewersChip card={card} />
        </>
      ),
      labelMarks: colourBlind.setEnabled ? (
        <ColourBlindToggle
          enabled={colourBlind.enabled}
          onToggle={colourBlind.setEnabled}
        />
      ) : null,
      checklistExtras: canEdit
        ? ({ source, onSourceChange, failed }) => (
            <ChecklistCopyFrom
              card={card}
              value={source}
              onChange={onSourceChange}
              error={failed}
            />
          )
        : undefined,
      linksExtras: canEdit ? <ClearAllLinks card={card} /> : null,
    };
  }, [board.fieldDefinitions, canEdit, canVote, card, colourBlind, exists]);
}
