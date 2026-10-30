import { useState, type ReactNode } from "react";
import {
  Download,
  ExternalLink,
  File as FileIcon,
  FileText,
  Image as ImageIcon,
  Link2,
  Lock,
  MoreHorizontal,
  Plus,
  X,
} from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  cn,
} from "@notrelix/ui-web";
import {
  useItemLinkedPages,
  type LinkedPageDto,
} from "@notrelix/work-management-state";
import type { CardFile } from "@notrelix/work-management-core";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup, useCanEdit } from "../card-popup-context";
import {
  formatDateTime,
  formatFileSize,
} from "../../item-page/item-page-format";
import { useItemPageCopy } from "../../item-page/use-item-page-copy";
import {
  IconBtn,
  PopSection,
  SECTION_ICONS,
  SectionHead,
  SmallBtn,
} from "../popup-ui";

const THUMB =
  "box-border grid h-12 w-[72px] shrink-0 place-items-center overflow-hidden rounded-[6px] text-[11px] font-bold";

function extensionOf(name: string): string {
  const dot = name.lastIndexOf(".");
  return dot > 0 ? name.slice(dot + 1, dot + 5).toUpperCase() : "";
}

/** The tile of a file: its extension on a tone of its kind. */
function thumbOf(file: CardFile): { tone: string; node: ReactNode } {
  if (file.contentType.startsWith("image/"))
    return {
      tone: "bg-label-data text-label-data-foreground",
      node: <ImageIcon aria-hidden="true" className="size-5" />,
    };
  const ext = extensionOf(file.name);
  if (/^(XLS|XLSX|CSV)$/.test(ext))
    return {
      tone: "bg-label-training text-label-training-foreground",
      node: ext,
    };
  if (/^(DOC|DOCX|PDF|PPT|PPTX)$/.test(ext))
    return {
      tone: "bg-label-security text-label-security-foreground",
      node: ext,
    };
  return {
    tone: "bg-state-hover-strong text-muted-foreground",
    node: ext || <FileIcon aria-hidden="true" className="size-5" />,
  };
}

function hostOf(url: string): string {
  try {
    return new URL(url).host;
  } catch {
    return url;
  }
}

interface Confirm {
  readonly title: string;
  readonly body: string;
  readonly action: string;
  readonly run: () => Promise<boolean>;
}

function Row({
  tile,
  tone,
  name,
  nameHref,
  meta,
  actions,
  busy,
}: {
  tile: ReactNode;
  tone: string;
  name: string;
  nameHref?: string;
  meta: ReactNode;
  actions?: ReactNode;
  busy?: boolean;
}) {
  return (
    <div
      aria-busy={busy || undefined}
      className="box-border flex min-h-[60px] items-center gap-3 rounded-[8px] px-2 py-1.5 hover:bg-state-hover-strong"
    >
      <span className={cn(THUMB, tone)}>{tile}</span>
      <span className="min-w-0 flex-1">
        {nameHref ? (
          <a
            href={nameHref}
            target="_blank"
            rel="noreferrer noopener"
            className="block truncate text-[14px] font-semibold text-foreground hover:underline focus-visible:outline-2 focus-visible:outline-primary"
          >
            {name}
          </a>
        ) : (
          <span className="block truncate text-[14px] font-semibold">
            {name}
          </span>
        )}
        <span className="block truncate text-[12.5px] text-meta-foreground">
          {meta}
        </span>
      </span>
      {actions}
    </div>
  );
}

/** Pages of Documents linked to the card (view mode only: a draft has no id yet). */
function LinkedPageRows({
  canEdit,
  onConfirm,
}: {
  canEdit: boolean;
  onConfirm: (c: Confirm) => void;
}) {
  const { card, copy, timeZone, now } = useCardPopup();
  const t = copy.related.attachments;
  const linked = useItemLinkedPages(card.id, card.workspaceId);
  void now;
  return (
    <>
      {linked.isError ? (
        <p
          role="alert"
          className="m-0 flex items-center gap-2 rounded-[8px] bg-danger-subtle px-3 py-2 text-[13px]"
        >
          <span className="flex-1">{t.pagesError}</span>
          <SmallBtn onClick={() => void linked.refetch()}>{t.retry}</SmallBtn>
        </p>
      ) : null}
      {linked.pages.map((page: LinkedPageDto) => {
        const title = page.title || t.untitled;
        const href = `/workspaces/${card.workspaceId}/docs/${page.pageId}`;
        return (
          <Row
            key={page.pageId}
            tile={<FileText aria-hidden="true" className="size-5" />}
            tone="bg-brand-subtle text-brand-subtle-foreground"
            name={title}
            nameHref={href}
            meta={fillPopupCopy(t.pageMeta, {
              date:
                formatDateTime(page.linkedAt, timeZone)?.split(",")[0] ?? "",
            })}
            actions={
              <OptionsMenu label={fillPopupCopy(t.options, { name: title })}>
                <DropdownMenuItem asChild>
                  <a href={href} target="_blank" rel="noreferrer noopener">
                    {t.openPage}
                  </a>
                </DropdownMenuItem>
                {canEdit ? (
                  <DropdownMenuItem
                    onSelect={() =>
                      onConfirm({
                        title: fillPopupCopy(t.unlinkTitle, { name: title }),
                        body: t.unlinkBody,
                        action: t.unlinkConfirm,
                        run: () =>
                          linked.unlink.mutateAsync(page.pageId).then(
                            () => true,
                            () => false,
                          ),
                      })
                    }
                  >
                    {t.unlinkPage}
                  </DropdownMenuItem>
                ) : null}
              </OptionsMenu>
            }
          />
        );
      })}
      {Array.from({ length: linked.hiddenCount }, (_, index) => (
        <Row
          key={`hidden-${index}`}
          tile={<Lock aria-hidden="true" className="size-5" />}
          tone="bg-state-hover-strong text-muted-foreground"
          name={t.restricted}
          meta={t.restrictedBody}
        />
      ))}
    </>
  );
}

function OptionsMenu({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <IconBtn small aria-label={label}>
          <MoreHorizontal aria-hidden="true" className="size-4" />
        </IconBtn>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">{children}</DropdownMenuContent>
    </DropdownMenu>
  );
}

/** The attachments of the card: files, web links, linked Documents pages, uploads on their way. */
export function AttachmentsSection() {
  const { data, callbacks, copy, mode, openPicker, timeZone } = useCardPopup();
  const t = copy.related.attachments;
  const pageCopy = useItemPageCopy();
  const canEdit = useCanEdit();
  const [confirm, setConfirm] = useState<Confirm | null>(null);
  const [failed, setFailed] = useState(false);
  const [working, setWorking] = useState(false);
  const files = data.files;
  const uploads = (data.uploads ?? []).filter(
    (upload) => upload.status !== "done",
  );
  const locale = copy.locale;
  const date = (value: string) =>
    formatDateTime(value, timeZone)?.replace(",", "") ?? "";

  return (
    <PopSection label={t.title} icon={SECTION_ICONS.attachments}>
      <SectionHead title={t.title}>
        {canEdit ? (
          <SmallBtn
            className="max-md:h-11"
            aria-haspopup="dialog"
            aria-label={t.addLabel}
            onClick={(event) => openPicker("attach", event.currentTarget)}
          >
            <Plus aria-hidden="true" className="size-[13px]" />
            {t.add}
          </SmallBtn>
        ) : null}
      </SectionHead>
      <div className="mt-2">
        {data.filesLoading && files.length === 0 ? (
          <div
            role="status"
            aria-label={t.loading}
            className="flex flex-col gap-1.5"
          >
            <span className="block h-[60px] rounded-[8px] bg-state-hover-strong" />
          </div>
        ) : null}
        {data.fileNotice ? (
          <p
            role="alert"
            className="m-0 mb-1.5 rounded-[8px] bg-danger-subtle px-3 py-2 text-[13px]"
          >
            {data.fileNotice}
          </p>
        ) : null}
        {uploads.map((upload) => (
          <Row
            key={upload.id}
            busy={upload.status === "uploading"}
            tile={<FileIcon aria-hidden="true" className="size-5" />}
            tone="bg-state-hover-strong text-muted-foreground"
            name={upload.name}
            meta={
              upload.status === "error"
                ? (
                    pageCopy.files[upload.error ?? "failed"] ?? t.uploadFailed
                  ).replace("{{limit}}", upload.limit ?? "")
                : upload.progress == null
                  ? t.uploadingPlain
                  : fillPopupCopy(t.uploading, {
                      percent: Math.round(upload.progress),
                    })
            }
            actions={
              <>
                {upload.status === "error" && upload.retryable ? (
                  <SmallBtn
                    onClick={() => callbacks.onRetryUpload?.(upload.id)}
                  >
                    {t.retry}
                  </SmallBtn>
                ) : null}
                {upload.status === "error" ? (
                  <IconBtn
                    small
                    aria-label={fillPopupCopy(t.cancelUpload, {
                      name: upload.name,
                    })}
                    onClick={() => callbacks.onDismissUpload?.(upload.id)}
                  >
                    <X aria-hidden="true" className="size-4" />
                  </IconBtn>
                ) : null}
              </>
            }
          />
        ))}
        {files.map((file) => {
          const isLink = file.kind === "link" || file.source === "link";
          const tile = isLink
            ? {
                tone: "bg-state-hover-strong text-muted-foreground",
                node: <Link2 aria-hidden="true" className="size-5" />,
              }
            : thumbOf(file);
          const author = file.createdBy.name;
          return (
            <Row
              key={file.id}
              tile={tile.node}
              tone={tile.tone}
              name={file.name}
              nameHref={isLink ? file.url : undefined}
              meta={
                isLink
                  ? fillPopupCopy(t.linkMeta, {
                      host: hostOf(file.url),
                      date: date(file.createdAt).split(" ")[0] ?? "",
                      author,
                    })
                  : fillPopupCopy(t.addedBy, {
                      date: date(file.createdAt),
                      size: formatFileSize(file.size, locale),
                      author,
                    })
              }
              actions={
                <>
                  {!isLink && callbacks.onDownloadFile ? (
                    <IconBtn
                      small
                      aria-label={fillPopupCopy(t.downloadFile, {
                        name: file.name,
                      })}
                      title={t.download}
                      onClick={() => callbacks.onDownloadFile?.(file)}
                    >
                      <Download aria-hidden="true" className="size-4" />
                    </IconBtn>
                  ) : null}
                  {isLink || (canEdit && callbacks.onDeleteFile) ? (
                    <OptionsMenu
                      label={fillPopupCopy(t.options, { name: file.name })}
                    >
                      {isLink ? (
                        <DropdownMenuItem asChild>
                          <a
                            href={file.url}
                            target="_blank"
                            rel="noreferrer noopener"
                          >
                            <ExternalLink aria-hidden="true" />
                            {t.openLink}
                          </a>
                        </DropdownMenuItem>
                      ) : null}
                      {canEdit && callbacks.onDeleteFile ? (
                        <DropdownMenuItem
                          variant="destructive"
                          onSelect={() =>
                            setConfirm({
                              title: fillPopupCopy(t.deleteTitle, {
                                name: file.name,
                              }),
                              body: t.deleteBody,
                              action: t.deleteConfirm,
                              run: () => callbacks.onDeleteFile!(file),
                            })
                          }
                        >
                          {t.deleteFile}
                        </DropdownMenuItem>
                      ) : null}
                    </OptionsMenu>
                  ) : null}
                </>
              }
            />
          );
        })}
        {mode === "view" ? (
          <LinkedPageRows
            canEdit={canEdit}
            onConfirm={(c) => {
              setFailed(false);
              setConfirm(c);
            }}
          />
        ) : null}
        {!data.filesLoading && files.length === 0 && uploads.length === 0 ? (
          <p className="m-0 text-[13.5px] text-meta-foreground">{t.empty}</p>
        ) : null}
      </div>
      <AlertDialog
        open={confirm !== null}
        onOpenChange={(next) => {
          if (!next && !working) setConfirm(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{confirm?.title}</AlertDialogTitle>
            <AlertDialogDescription>{confirm?.body}</AlertDialogDescription>
          </AlertDialogHeader>
          {failed ? (
            <p role="alert" className="m-0 text-[13.5px] text-danger-text">
              {t.deleteFailed}
            </p>
          ) : null}
          <AlertDialogFooter>
            <AlertDialogCancel disabled={working}>
              {t.deleteKeep}
            </AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={working}
              onClick={(event) => {
                event.preventDefault();
                if (!confirm) return;
                setWorking(true);
                void confirm.run().then((ok) => {
                  setWorking(false);
                  setFailed(!ok);
                  if (ok) setConfirm(null);
                });
              }}
            >
              {confirm?.action}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </PopSection>
  );
}
