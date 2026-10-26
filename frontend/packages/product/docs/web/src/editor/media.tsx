import {
  useCallback,
  useEffect,
  useRef,
  useState,
  useSyncExternalStore,
  type KeyboardEvent,
  type PointerEvent,
} from "react";
import { Extension, Node, mergeAttributes } from "@tiptap/core";
import {
  NodeViewWrapper,
  ReactNodeViewRenderer,
  type NodeViewProps,
} from "@tiptap/react";
import { Plugin, PluginKey } from "@tiptap/pm/state";
import {
  Download,
  File as FileIcon,
  FileArchive,
  FileImage,
  FileText,
  Trash2,
  Type,
  type LucideIcon,
} from "lucide-react";
import {
  IMAGE_MAX_ALT_LENGTH,
  IMAGE_MAX_WIDTH,
  IMAGE_MIN_WIDTH,
  canRetryUpload,
  clampImageWidth,
  formatFileSize,
  type UploadFailure,
} from "@notrelix/docs-core";
import { fillDocCopy } from "../components/doc-page.copy";
import { UPLOAD_PLACEHOLDER } from "./block-mapping";
import { useContextVersion, type ContextHolder } from "./extensions";
import {
  insertMediaFiles,
  removePlaceholder,
  type DocMediaStorage,
} from "./media-commands";
import type { DocMediaHost } from "./media-host";

const ACTION =
  "inline-flex min-h-8 items-center justify-center gap-1.5 rounded-[8px] border border-line-strong bg-transparent px-2.5 text-compact text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring disabled:opacity-50";
const ICON_ACTION =
  "inline-grid size-8 shrink-0 place-items-center rounded-[8px] border-0 bg-transparent text-muted-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring disabled:opacity-50";
const FIELD =
  "box-border min-h-8 w-full rounded-[8px] border border-line-strong bg-transparent px-2.5 text-compact text-foreground outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring";

/** An object address for a blob, revoked when the blob changes or the view goes. */
function useObjectUrl(blob: Blob | null): string | null {
  const [url, setUrl] = useState<string | null>(null);
  useEffect(() => {
    if (!blob) {
      setUrl(null);
      return;
    }
    const next = URL.createObjectURL(blob);
    setUrl(next);
    return () => URL.revokeObjectURL(next);
  }, [blob]);
  return url;
}

function Unavailable({ children }: { children: string }) {
  return (
    <div className="rounded-[9px] border border-dashed border-line-strong px-3 py-2 text-compact text-meta-foreground">
      {children}
    </div>
  );
}

// ─── Image ─────────────────────────────────────────────────────────────────

function ImageContent({
  media,
  context,
  node,
  editor,
  updateAttributes,
  deleteNode,
}: NodeViewProps & { media: DocMediaHost; context: ContextHolder }) {
  const copy = context.current.copy.media.image;
  const attachmentId = (node.attrs.attachmentId as string | null) ?? null;
  const stored = (node.attrs.width as number | null) ?? null;
  const alt = (node.attrs.alt as string | null) ?? "";
  const caption = (node.attrs.caption as string | null) ?? "";
  const editable = editor.isEditable;
  const content = media.useContent(attachmentId);
  const url = useObjectUrl(content.blob);
  const box = useRef<HTMLDivElement | null>(null);
  const [live, setLive] = useState<number | null>(null);
  const drag = useRef<{ x: number; width: number } | null>(null);
  const [captionDraft, setCaptionDraft] = useState(caption);
  const [altDraft, setAltDraft] = useState(alt);
  const [altOpen, setAltOpen] = useState(false);

  useEffect(() => setCaptionDraft(caption), [caption]);
  useEffect(() => setAltDraft(alt), [alt]);

  const width = live ?? stored;
  const commitWidth = useCallback(
    (next: number) => updateAttributes({ width: clampImageWidth(next) }),
    [updateAttributes],
  );
  const shown = (): number =>
    Math.round(width ?? box.current?.getBoundingClientRect().width ?? 0);

  const onPointerDown = (event: PointerEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.currentTarget.setPointerCapture(event.pointerId);
    drag.current = {
      x: event.clientX,
      width: box.current?.getBoundingClientRect().width ?? IMAGE_MIN_WIDTH,
    };
  };
  const onPointerMove = (event: PointerEvent<HTMLButtonElement>) => {
    const start = drag.current;
    if (!start) return;
    setLive(clampImageWidth(start.width + event.clientX - start.x));
  };
  const onPointerUp = () => {
    if (drag.current && live !== null) commitWidth(live);
    drag.current = null;
    setLive(null);
  };
  const onResizeKey = (event: KeyboardEvent<HTMLButtonElement>) => {
    const step = event.shiftKey ? 50 : 10;
    const current = shown() || IMAGE_MIN_WIDTH;
    let next: number | null = null;
    if (event.key === "ArrowRight" || event.key === "ArrowUp")
      next = current + step;
    else if (event.key === "ArrowLeft" || event.key === "ArrowDown")
      next = current - step;
    else if (event.key === "Home") next = IMAGE_MIN_WIDTH;
    else if (event.key === "End") next = IMAGE_MAX_WIDTH;
    if (next === null) return;
    event.preventDefault();
    commitWidth(next);
  };

  let body;
  if (!attachmentId) {
    body = <Unavailable>{copy.unavailable}</Unavailable>;
  } else if (content.status === "error") {
    body = (
      <div
        role="alert"
        className="flex items-center gap-3 rounded-[9px] border border-line-strong px-3 py-3 text-compact text-meta-foreground"
      >
        <span className="min-w-0 flex-1">{copy.error}</span>
        <button type="button" className={ACTION} onClick={content.retry}>
          {copy.retry}
        </button>
      </div>
    );
  } else if (!url) {
    body = (
      <div
        role="status"
        aria-label={copy.loading}
        className="aspect-video w-full rounded-[9px] bg-state-hover motion-safe:animate-pulse"
      />
    );
  } else {
    body = (
      <img
        src={url}
        alt={alt}
        draggable={false}
        className="block h-auto w-full rounded-[9px]"
      />
    );
  }

  return (
    <NodeViewWrapper
      data-block={String(node.attrs.blockId)}
      contentEditable={false}
      className="group/image my-3"
    >
      <figure className="m-0">
        <div
          ref={box}
          data-image-box=""
          style={{
            width: width ? `${width}px` : undefined,
            maxWidth: "100%",
          }}
          className={`relative ${width ? "" : "w-fit"}`}
        >
          {body}
          {editable && url ? (
            <button
              type="button"
              role="slider"
              aria-label={copy.resize}
              aria-orientation="horizontal"
              aria-valuemin={IMAGE_MIN_WIDTH}
              aria-valuemax={IMAGE_MAX_WIDTH}
              aria-valuenow={shown() || IMAGE_MIN_WIDTH}
              aria-valuetext={fillDocCopy(copy.resizeValue, {
                width: String(shown()),
              })}
              onPointerDown={onPointerDown}
              onPointerMove={onPointerMove}
              onPointerUp={onPointerUp}
              onPointerCancel={onPointerUp}
              onKeyDown={onResizeKey}
              className="absolute top-1/2 -right-2 h-12 w-2 -translate-y-1/2 cursor-ew-resize rounded-full border-0 bg-brand-solid p-0 opacity-0 outline-none focus-visible:opacity-100 focus-visible:ring-2 focus-visible:ring-focus-ring group-hover/image:opacity-100 touch-none"
            />
          ) : null}
        </div>
        {editable || caption ? (
          <figcaption className="mt-1.5 flex max-w-[68ch] items-center gap-1.5">
            {editable ? (
              <input
                type="text"
                aria-label={copy.caption}
                placeholder={copy.captionPlaceholder}
                value={captionDraft}
                onChange={(event) => setCaptionDraft(event.target.value)}
                onBlur={() => {
                  if (captionDraft !== caption) {
                    updateAttributes({ caption: captionDraft || null });
                  }
                }}
                onKeyDown={(event) => {
                  if (event.key === "Enter") event.currentTarget.blur();
                }}
                className={`${FIELD} border-transparent text-meta-foreground hover:border-line-strong`}
              />
            ) : (
              <span className="text-compact text-meta-foreground">
                {caption}
              </span>
            )}
            {editable ? (
              <>
                <button
                  type="button"
                  aria-label={copy.altText}
                  title={copy.altText}
                  aria-pressed={altOpen}
                  onClick={() => setAltOpen((open) => !open)}
                  className={ICON_ACTION}
                >
                  <Type aria-hidden="true" className="size-4" />
                </button>
                <button
                  type="button"
                  aria-label={copy.remove}
                  title={copy.remove}
                  onClick={() => deleteNode()}
                  className={ICON_ACTION}
                >
                  <Trash2 aria-hidden="true" className="size-4" />
                </button>
              </>
            ) : null}
          </figcaption>
        ) : null}
        {editable && altOpen ? (
          <input
            type="text"
            aria-label={copy.altText}
            placeholder={copy.altPlaceholder}
            maxLength={IMAGE_MAX_ALT_LENGTH}
            value={altDraft}
            onChange={(event) => setAltDraft(event.target.value)}
            onBlur={() => {
              if (altDraft !== alt) updateAttributes({ alt: altDraft || null });
            }}
            onKeyDown={(event) => {
              if (event.key === "Enter" || event.key === "Escape") {
                event.currentTarget.blur();
                setAltOpen(false);
              }
            }}
            className={`${FIELD} mt-1.5 max-w-[68ch]`}
          />
        ) : null}
      </figure>
    </NodeViewWrapper>
  );
}

function MediaBlockView({
  props,
  context,
  render,
}: {
  props: NodeViewProps;
  context: ContextHolder;
  render: (media: DocMediaHost) => React.ReactNode;
}) {
  useContextVersion(context);
  const media = context.current.media;
  if (!media) {
    return (
      <NodeViewWrapper
        data-block={String(props.node.attrs.blockId)}
        contentEditable={false}
      >
        <Unavailable>{context.current.copy.blocks.unsupported}</Unavailable>
      </NodeViewWrapper>
    );
  }
  return <>{render(media)}</>;
}

// ─── File ──────────────────────────────────────────────────────────────────

function iconFor(contentType: string): LucideIcon {
  if (contentType.startsWith("image/")) return FileImage;
  if (contentType === "application/pdf" || contentType.startsWith("text/")) {
    return FileText;
  }
  if (/zip|compressed|tar|rar|7z/.test(contentType)) return FileArchive;
  return FileIcon;
}

/** The short type label of a file ("PDF", "PNG"): its extension, else its type. */
function typeLabel(fileName: string, contentType: string): string {
  const dot = fileName.lastIndexOf(".");
  if (dot >= 0 && dot < fileName.length - 1) {
    return fileName.slice(dot + 1).toUpperCase();
  }
  return contentType.split("/")[1]?.toUpperCase() ?? "";
}

function FileContent({
  media,
  context,
  node,
  editor,
  deleteNode,
}: NodeViewProps & { media: DocMediaHost; context: ContextHolder }) {
  const copy = context.current.copy.media.file;
  const { files, loading } = media.useFiles();
  const [busy, setBusy] = useState(false);
  const attachmentId = (node.attrs.attachmentId as string | null) ?? null;
  const file = attachmentId ? files.get(attachmentId) : undefined;

  let body;
  if (!attachmentId || (!file && !loading)) {
    body = <Unavailable>{copy.unavailable}</Unavailable>;
  } else if (!file) {
    body = (
      <div
        role="status"
        aria-label={copy.loading}
        className="h-14 max-w-[68ch] rounded-[10px] bg-state-hover motion-safe:animate-pulse"
      />
    );
  } else {
    const Icon = iconFor(file.contentType);
    body = (
      <div className="flex max-w-[68ch] items-center gap-3 rounded-[10px] border border-line-strong bg-doc-panel px-3 py-2.5">
        <Icon
          aria-hidden="true"
          className="size-6 shrink-0 text-muted-foreground"
        />
        <div className="min-w-0 flex-1">
          <span className="block truncate text-compact font-medium text-foreground">
            {file.fileName}
          </span>
          <span className="block text-micro text-meta-foreground">
            {formatFileSize(file.sizeBytes)} ·{" "}
            {typeLabel(file.fileName, file.contentType)}
          </span>
        </div>
        <button
          type="button"
          disabled={busy}
          aria-label={fillDocCopy(copy.download, { name: file.fileName })}
          title={fillDocCopy(copy.download, { name: file.fileName })}
          onClick={() => {
            setBusy(true);
            void media.download(file).finally(() => setBusy(false));
          }}
          className={ICON_ACTION}
        >
          <Download aria-hidden="true" className="size-4" />
        </button>
        {editor.isEditable ? (
          <button
            type="button"
            aria-label={copy.remove}
            title={copy.remove}
            onClick={() => deleteNode()}
            className={ICON_ACTION}
          >
            <Trash2 aria-hidden="true" className="size-4" />
          </button>
        ) : null}
      </div>
    );
  }
  return (
    <NodeViewWrapper
      data-block={String(node.attrs.blockId)}
      contentEditable={false}
      className="my-2"
    >
      {body}
    </NodeViewWrapper>
  );
}

// ─── Upload placeholder ────────────────────────────────────────────────────

function PlaceholderContent({
  media,
  context,
  node,
  editor,
}: NodeViewProps & { media: DocMediaHost; context: ContextHolder }) {
  const copy = context.current.copy.media.upload;
  const uploadId = String(node.attrs.uploadId);
  const uploads = useSyncExternalStore(
    media.uploads.subscribe,
    media.uploads.snapshot,
  );
  const upload = uploads.find((entry) => entry.id === uploadId);
  const gone = upload === undefined;
  useEffect(() => {
    // The upload is no longer tracked (it was dropped): its placeholder goes too.
    if (gone) removePlaceholder(editor as never, uploadId);
  }, [gone, editor, uploadId]);
  if (!upload) return null;

  const percent = upload.progress === null ? null : Math.round(upload.progress);
  const failed = upload.status === "failed";
  const reason = copy.reasons[(upload.failure ?? "failed") as UploadFailure];
  return (
    <NodeViewWrapper
      data-block={String(node.attrs.blockId)}
      data-upload={uploadId}
      contentEditable={false}
      className="my-2"
    >
      <div className="flex max-w-[68ch] items-center gap-3 rounded-[10px] border border-line-strong bg-doc-panel px-3 py-2.5">
        {upload.previewUrl ? (
          <img
            src={upload.previewUrl}
            alt=""
            className="size-10 shrink-0 rounded-[6px] object-cover"
          />
        ) : (
          <FileIcon
            aria-hidden="true"
            className="size-6 shrink-0 text-muted-foreground"
          />
        )}
        <div className="min-w-0 flex-1">
          <span className="block truncate text-compact font-medium text-foreground">
            {upload.fileName}
          </span>
          {failed ? (
            <span role="alert" className="block text-micro text-danger-text">
              {fillDocCopy(copy.failed, { reason })}
            </span>
          ) : (
            <div
              role="progressbar"
              aria-label={fillDocCopy(copy.uploading, {
                name: upload.fileName,
              })}
              aria-valuemin={0}
              aria-valuemax={100}
              {...(percent === null ? {} : { "aria-valuenow": percent })}
              className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-state-hover"
            >
              <div
                className={`h-full rounded-full bg-brand-solid ${percent === null ? "w-2/5 motion-safe:animate-pulse" : ""}`}
                style={percent === null ? undefined : { width: `${percent}%` }}
              />
            </div>
          )}
        </div>
        {failed && canRetryUpload(upload.failure) ? (
          <button
            type="button"
            className={ACTION}
            onClick={() => media.uploads.retry(uploadId)}
          >
            {copy.retry}
          </button>
        ) : null}
        <button
          type="button"
          className={ACTION}
          onClick={() => {
            media.uploads.remove(uploadId);
            removePlaceholder(editor as never, uploadId);
          }}
        >
          {failed ? copy.remove : copy.cancel}
        </button>
      </div>
    </NodeViewWrapper>
  );
}

// ─── Nodes ─────────────────────────────────────────────────────────────────

function mediaNode(
  name: string,
  attributes: readonly string[],
  View: (
    props: NodeViewProps & { media: DocMediaHost; context: ContextHolder },
  ) => React.ReactNode,
  options: { selectable: boolean } = { selectable: true },
) {
  return Node.create<{ context: ContextHolder }>({
    name,
    group: "block",
    atom: true,
    selectable: options.selectable,
    draggable: false,
    addOptions() {
      return { context: undefined as unknown as ContextHolder };
    },
    addAttributes() {
      return Object.fromEntries(
        attributes.map((key) => [key, { default: null, rendered: false }]),
      );
    },
    parseHTML() {
      return [{ tag: `div[data-doc-node="${name}"]` }];
    },
    renderHTML({ HTMLAttributes }) {
      return [
        "div",
        mergeAttributes(HTMLAttributes, { "data-doc-node": name }),
      ];
    },
    addNodeView() {
      const context = this.options.context;
      return ReactNodeViewRenderer((props: NodeViewProps) => (
        <MediaBlockView
          props={props}
          context={context}
          render={(media) => (
            <View {...props} media={media} context={context} />
          )}
        />
      ));
    },
  });
}

export const ImageBlock = mediaNode(
  "imageBlock",
  ["attachmentId", "width", "alt", "caption"],
  ImageContent,
);
export const FileBlock = mediaNode("fileBlock", ["attachmentId"], FileContent);
export const UploadPlaceholder = mediaNode(
  UPLOAD_PLACEHOLDER,
  ["uploadId", "kind", "fileName"],
  PlaceholderContent,
  { selectable: false },
);

const PASTE_KEY = new PluginKey("docMediaPaste");

/**
 * Files pasted or dropped on the page become uploads. Text pasted together with
 * a file (a copied spreadsheet range, a copied web image with its link) keeps
 * the normal paste; a file dropped from the desktop goes where it is dropped.
 */
export const DocMedia = Extension.create<{ context: ContextHolder }>({
  name: "docMedia",
  addOptions() {
    return { context: undefined as unknown as ContextHolder };
  },
  addStorage(): DocMediaStorage {
    const context = this.options.context;
    return { host: () => context.current.media };
  },
  addProseMirrorPlugins() {
    const editor = this.editor;
    const storage = this.storage as DocMediaStorage;
    return [
      new Plugin({
        key: PASTE_KEY,
        props: {
          handlePaste: (view, event) => {
            if (!view.editable || !storage.host()) return false;
            const data = event.clipboardData;
            const files = Array.from(data?.files ?? []);
            if (files.length === 0 || data?.getData("text/plain")) return false;
            event.preventDefault();
            insertMediaFiles(editor, files);
            return true;
          },
          handleDrop: (view, event, _slice, moved) => {
            if (moved || !view.editable || !storage.host()) return false;
            const files = Array.from(event.dataTransfer?.files ?? []);
            if (files.length === 0) return false;
            event.preventDefault();
            const at = view.posAtCoords({
              left: event.clientX,
              top: event.clientY,
            })?.pos;
            insertMediaFiles(editor, files, at);
            return true;
          },
        },
      }),
    ];
  },
});
