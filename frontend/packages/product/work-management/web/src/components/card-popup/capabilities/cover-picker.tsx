import { useRef, useState } from "react";
import { Check, ImagePlus, Image as ImageIcon } from "lucide-react";
import { Button, PopoverTrigger, cn } from "@notrelix/ui-web";
import {
  CARD_COVER_COLORS,
  type CardCover,
  type CardDetail,
  type CardCoverStyle,
} from "@notrelix/work-management-core";
import {
  useCardFiles,
  useItemSettingsWrites,
  useWorkManagementServices,
} from "@notrelix/work-management-state";
import { PanelBtn, PanelPopover } from "../popup-ui";
import { Caption, Hint, PickerHead } from "../pickers/picker-bits";
import { COVER_COLOR_CLASS, useCoverImageUrl } from "./card-cover";
import { fillCapabilityCopy } from "./capabilities.copy";
import { useCapabilitiesCopy } from "./use-capabilities-copy";

function ImageChoice({
  cardId,
  attachmentId,
  name,
  selected,
  label,
  onPick,
}: {
  cardId: string;
  attachmentId: string;
  name: string;
  selected: boolean;
  label: string;
  onPick: () => void;
}) {
  const url = useCoverImageUrl(cardId, attachmentId);
  return (
    <button
      type="button"
      aria-label={label}
      aria-pressed={selected}
      title={name}
      onClick={onPick}
      className={cn(
        "grid h-16 w-24 place-items-center overflow-hidden rounded-[6px] border border-line-strong bg-brand-subtle focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary",
        selected && "border-primary shadow-[0_0_0_3px_var(--brand-subtle)]",
      )}
    >
      {url ? (
        <img src={url} alt="" className="size-full object-cover" />
      ) : (
        <ImageIcon
          className="size-4 text-muted-foreground"
          aria-hidden="true"
        />
      )}
    </button>
  );
}

/**
 * The cover picker: the display style, ten colours, the images already attached to the item, and an upload
 * (which also adds the file to the item's attachments). Needs edit rights on the board; without them
 * `disabled` keeps it closed. A cover is saved against the item version.
 */
export function CoverPicker({
  card,
  disabled = false,
}: {
  card: CardDetail;
  disabled?: boolean;
}) {
  const copy = useCapabilitiesCopy();
  const { cards } = useWorkManagementServices();
  const scope = {
    itemId: card.id,
    boardId: card.boardId,
    workspaceId: card.workspaceId,
  };
  const writes = useItemSettingsWrites(scope, card.version ?? 0);
  const files = useCardFiles(card.id, card.workspaceId);
  const [style, setStyle] = useState<CardCoverStyle>(
    card.cover?.style ?? "strip",
  );
  const [message, setMessage] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [open, setOpen] = useState(false);
  const input = useRef<HTMLInputElement>(null);
  const cover = card.cover ?? null;
  const images = (files.data ?? []).filter(
    (file) => file.kind !== "link" && file.contentType.startsWith("image/"),
  );

  const save = async (next: CardCover | null) => {
    setMessage(null);
    try {
      await writes.setCover(next);
    } catch {
      setMessage(copy.cover.failed);
    }
  };
  const pickStyle = (next: CardCoverStyle) => {
    setStyle(next);
    if (cover) void save({ ...cover, style: next });
  };
  const upload = async (file: File) => {
    if (!file.type.startsWith("image/")) {
      setMessage(copy.cover.notImage);
      return;
    }
    setUploading(true);
    try {
      const stored = await cards.uploadCardFile(card.id, file, {
        idempotencyKey: crypto.randomUUID(),
      });
      await save({ color: null, attachmentId: stored.id, style });
    } catch {
      setMessage(copy.cover.failed);
    } finally {
      setUploading(false);
    }
  };

  return (
    <PanelPopover
      open={open}
      onOpenChange={setOpen}
      width={328}
      align="end"
      trigger={
        <PopoverTrigger asChild>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            disabled={disabled}
            aria-label={copy.cover.open}
            title={copy.cover.open}
          >
            <ImageIcon aria-hidden="true" />
          </Button>
        </PopoverTrigger>
      }
    >
      <PickerHead title={copy.cover.title} onClose={() => setOpen(false)} />
      <Caption className="mt-0">{copy.cover.showAs}</Caption>
      <div className="flex gap-2">
        {(["strip", "full"] as const).map((option) => (
          <button
            key={option}
            type="button"
            aria-pressed={style === option}
            onClick={() => pickStyle(option)}
            className={cn(
              "box-border flex min-w-0 flex-1 flex-col gap-1.5 rounded-[8px] border border-line-strong bg-popover p-1.5 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary",
              style === option &&
                "border-primary shadow-[0_0_0_3px_var(--brand-subtle)]",
            )}
          >
            <span className="block overflow-hidden rounded-[6px] bg-background">
              {option === "strip" ? (
                <>
                  <span className="block h-[38px] rounded-t-[5px] bg-line-strong" />
                  <span className="flex h-[34px] flex-col gap-[5px] p-1.5">
                    <span className="block h-1.5 w-[70%] rounded-[6px] bg-state-hover-strong" />
                    <span className="block h-1.5 w-[40%] rounded-[6px] bg-state-hover-strong" />
                  </span>
                </>
              ) : (
                <span className="flex h-[72px] flex-col justify-end gap-1 rounded-[5px] bg-line-strong p-2 pb-1.5">
                  <span className="block h-1.5 w-[80%] rounded-[6px] bg-popover" />
                  <span className="block h-1.5 w-[45%] rounded-[6px] bg-popover" />
                </span>
              )}
            </span>
            <span className="text-center text-[12.5px] leading-[1.45] font-semibold">
              {copy.cover[option]}
            </span>
          </button>
        ))}
      </div>
      <Caption id={`${card.id}-cover-colors`}>{copy.cover.colors}</Caption>
      <div
        role="group"
        aria-labelledby={`${card.id}-cover-colors`}
        className="grid grid-cols-5 gap-1.5"
      >
        {CARD_COVER_COLORS.map((color) => {
          const selected = cover?.color === color;
          return (
            <button
              key={color}
              type="button"
              aria-label={copy.cover.colorNames[color]}
              aria-pressed={selected}
              onClick={() => void save({ color, attachmentId: null, style })}
              className={cn(
                "grid h-8 place-items-center rounded-[6px] border-0 p-0 text-primary-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary",
                COVER_COLOR_CLASS[color],
                selected &&
                  "shadow-[0_0_0_2px_var(--popover),0_0_0_4px_var(--primary)]",
              )}
            >
              {selected ? (
                <Check className="size-4" strokeWidth={3} aria-hidden="true" />
              ) : null}
            </button>
          );
        })}
      </div>
      <Caption>{copy.cover.images}</Caption>
      {images.length === 0 ? (
        <Hint>{copy.cover.noImages}</Hint>
      ) : (
        <div className="flex flex-wrap gap-2">
          {images.map((file) => (
            <ImageChoice
              key={file.id}
              cardId={card.id}
              attachmentId={file.id}
              name={file.name}
              selected={cover?.attachmentId === file.id}
              label={fillCapabilityCopy(copy.cover.useImage, {
                name: file.name,
              })}
              onPick={() =>
                void save({ color: null, attachmentId: file.id, style })
              }
            />
          ))}
        </div>
      )}
      <input
        ref={input}
        type="file"
        accept="image/*"
        hidden
        onChange={(event) => {
          const file = event.target.files?.[0];
          event.target.value = "";
          if (file) void upload(file);
        }}
      />
      <PanelBtn
        className="mt-3 w-full"
        disabled={uploading}
        onClick={() => input.current?.click()}
      >
        <ImagePlus aria-hidden="true" className="size-4" />
        {uploading ? copy.cover.uploading : copy.cover.upload}
      </PanelBtn>
      <Hint className="mt-2 px-0.5">{copy.cover.uploadNote}</Hint>
      {cover ? (
        <PanelBtn
          variant="danger"
          className="mt-3 w-full"
          onClick={() => void save(null)}
        >
          {copy.cover.remove}
        </PanelBtn>
      ) : null}
      {message ? (
        <Hint error className="mt-2">
          {message}
        </Hint>
      ) : null}
    </PanelPopover>
  );
}
