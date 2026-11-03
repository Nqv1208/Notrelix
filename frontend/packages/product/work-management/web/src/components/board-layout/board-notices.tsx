import { House, Info, Lock } from "lucide-react";
import type { BoardHeaderCopy } from "./board-header.copy";
import {
  BOARD_BAND_BUTTON,
  BoardBand,
  BoardStateBlock,
} from "./board-state-block";

/** A board that is missing or not visible to this person: one answer for both, no name and no card (BC-Shell NP). */
export function BoardGone({
  copy,
  onHome,
}: {
  copy: BoardHeaderCopy;
  onHome: () => void;
}) {
  const t = copy.boardGone;
  return (
    <BoardStateBlock
      // The artboard sits the note 64 px under the top (48 px at 320).
      className="pt-12 pb-12 sm:pt-16 sm:pb-16"
      icon={<Lock />}
      title={t.title}
      description={t.body}
      action={
        <button type="button" className={BOARD_BAND_BUTTON} onClick={onHome}>
          <House className="size-4" aria-hidden="true" />
          {t.home}
        </button>
      }
    />
  );
}

/** The address named a saved view that is gone: the default shows and this says so once (BC-Shell VM). */
export function ViewGoneBanner({
  copy,
  onClose,
}: {
  copy: BoardHeaderCopy;
  onClose: () => void;
}) {
  const t = copy.viewGone;
  return (
    <div className="px-4 pt-3 sm:px-6">
      <BoardBand
        tone="info"
        icon={<Info />}
        title={t.title}
        description={t.body}
        action={
          <button type="button" className={BOARD_BAND_BUTTON} onClick={onClose}>
            {t.close}
          </button>
        }
      />
    </div>
  );
}
