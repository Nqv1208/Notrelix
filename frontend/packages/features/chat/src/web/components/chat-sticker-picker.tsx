import { useState } from "react";
import { Sticker } from "lucide-react";
import {
  Button,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import { STICKERS } from "../../core/model/sticker-model";
import type { ChatCopy } from "./chat.copy";
import { stickerLabel } from "./chat-sticker";

export interface StickerPickerButtonProps {
  readonly copy: ChatCopy;
  readonly disabled?: boolean;
  /** Called with the id of the chosen sticker; it sends at once, as the server stores a sticker alone. */
  readonly onPick: (stickerId: string) => void;
  /** Called when the popover has closed; the composer gives the textarea its focus back here. */
  readonly onClosed: () => void;
}

/** The composer toolbar's sticker button and its popover (UC-CHT-I06): the Notrelix stickers in a grid, one pick sends. */
export function StickerPickerButton({
  copy,
  disabled,
  onPick,
  onClosed,
}: StickerPickerButtonProps) {
  const [open, setOpen] = useState(false);
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="size-10 rounded-full text-muted-foreground hover:text-foreground"
          disabled={disabled}
          aria-label={copy.stickers.open}
          title={copy.stickers.open}
        >
          <Sticker aria-hidden="true" />
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        side="top"
        className="w-72 gap-2 p-2"
        aria-label={copy.stickers.pickerLabel}
        onCloseAutoFocus={(event) => {
          event.preventDefault();
          onClosed();
        }}
      >
        <div
          role="group"
          aria-label={copy.stickers.gridLabel}
          className="grid grid-cols-4 gap-1"
        >
          {STICKERS.map((sticker) => (
            <button
              key={sticker.id}
              type="button"
              aria-label={stickerLabel(copy, sticker.id, null)}
              title={stickerLabel(copy, sticker.id, null)}
              className="flex aspect-square items-center justify-center rounded-xl text-3xl transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none motion-reduce:transition-none"
              onClick={() => {
                onPick(sticker.id);
                setOpen(false);
              }}
            >
              <span aria-hidden="true">{sticker.glyph}</span>
            </button>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
}
