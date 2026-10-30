import { useRef, useState } from "react";
import { Check, ImagePlus, Image as ImageIcon } from "lucide-react";
import {
  Button,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
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
        "grid h-16 w-24 place-items-center overflow-hidden rounded-md border border-border bg-accent-subtle",
        selected && "ring-2 ring-primary ring-offset-2 ring-offset-popover",
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
    <Popover>
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
      <PopoverContent align="end" className="flex w-82 flex-col gap-2 p-3">
        <h2 className="text-nav font-semibold">{copy.cover.title}</h2>
        <p className="text-micro font-semibold text-meta-foreground">
          {copy.cover.showAs}
        </p>
        <div className="flex gap-2">
          {(["strip", "full"] as const).map((option) => (
            <button
              key={option}
              type="button"
              aria-pressed={style === option}
              onClick={() => pickStyle(option)}
              className={cn(
                "flex flex-1 flex-col gap-1.5 rounded-lg border border-border bg-popover p-1.5",
                style === option && "border-primary ring-2 ring-primary/20",
              )}
            >
              <span className="block rounded-md bg-background">
                {option === "strip" ? (
                  <>
                    <span className="block h-9.5 rounded-t-[5px] bg-border" />
                    <span className="block h-8.5" />
                  </>
                ) : (
                  <span className="block h-18 rounded-[5px] bg-border" />
                )}
              </span>
              <span className="text-center text-micro font-semibold">
                {copy.cover[option]}
              </span>
            </button>
          ))}
        </div>
        <p
          id={`${card.id}-cover-colors`}
          className="text-micro font-semibold text-meta-foreground"
        >
          {copy.cover.colors}
        </p>
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
                  "grid h-8 place-items-center rounded-md text-primary-foreground",
                  COVER_COLOR_CLASS[color],
                  selected &&
                    "ring-2 ring-primary ring-offset-2 ring-offset-popover",
                )}
              >
                {selected ? (
                  <Check className="size-4" aria-hidden="true" />
                ) : null}
              </button>
            );
          })}
        </div>
        <p className="text-micro font-semibold text-meta-foreground">
          {copy.cover.images}
        </p>
        {images.length === 0 ? (
          <p className="text-micro text-meta-foreground">
            {copy.cover.noImages}
          </p>
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
        <Button
          type="button"
          variant="outline"
          disabled={uploading}
          onClick={() => input.current?.click()}
        >
          <ImagePlus aria-hidden="true" />
          {uploading ? copy.cover.uploading : copy.cover.upload}
        </Button>
        <p className="text-micro text-meta-foreground">
          {copy.cover.uploadNote}
        </p>
        {cover ? (
          <Button type="button" variant="ghost" onClick={() => void save(null)}>
            {copy.cover.remove}
          </Button>
        ) : null}
        {message ? (
          <p role="alert" className="text-micro text-danger-subtle-foreground">
            {message}
          </p>
        ) : null}
      </PopoverContent>
    </Popover>
  );
}
