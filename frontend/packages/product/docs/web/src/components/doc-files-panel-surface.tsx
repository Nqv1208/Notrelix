import { useState, type FormEvent } from "react";
import {
  Download,
  ExternalLink,
  File as FileIcon,
  Link2,
  Trash2,
} from "lucide-react";
import { isWebUrl } from "@notrelix/docs-core";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type {
  DocFileEntry,
  DocFilesPanel,
  DocLinkFailure,
} from "./doc-page-models";

const ICON_ACTION =
  "inline-grid size-7 shrink-0 place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring disabled:opacity-50";
const TEXT_ACTION =
  "h-6 rounded-[6px] border border-line-strong bg-transparent px-2 text-crumb font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";
const FIELD =
  "box-border h-8 w-full rounded-[8px] border border-line-strong bg-doc-panel px-2.5 text-compact text-foreground outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring";
const SECTION_TITLE =
  "mb-2 flex items-center gap-1.5 text-crumb leading-[1.45] font-semibold text-muted-foreground";

/** A link's name; only an absolute http or https address becomes an anchor. */
function FileName({ entry, copy }: { entry: DocFileEntry; copy: DocPageCopy }) {
  const name = (
    <b className="block truncate text-compact leading-[1.4] font-medium text-foreground">
      {entry.name}
    </b>
  );
  if (entry.kind === "link" && entry.href && isWebUrl(entry.href)) {
    return (
      <a
        href={entry.href}
        target="_blank"
        rel="noopener noreferrer"
        aria-label={fillDocCopy(copy.files.openLink, { name: entry.name })}
        className="block min-w-0 rounded-[4px] no-underline outline-none hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring"
      >
        {name}
      </a>
    );
  }
  return name;
}

function FileRow({
  entry,
  copy,
  canEdit,
  confirming,
  busy,
  onConfirm,
  onDownload,
  onDelete,
}: {
  entry: DocFileEntry;
  copy: DocPageCopy;
  canEdit: boolean;
  confirming: boolean;
  busy: boolean;
  onConfirm: (id: string | null) => void;
  onDownload: () => void;
  onDelete: () => void;
}) {
  const f = copy.files;
  const Icon = entry.kind === "link" ? Link2 : FileIcon;
  return (
    <li className="-mx-2 flex flex-col gap-1.5 rounded-[8px] p-2 hover:bg-state-hover">
      <div className="flex items-start gap-[9px]">
        <Icon
          aria-hidden="true"
          className="mt-0.5 size-4 shrink-0 stroke-[1.8] text-muted-foreground"
        />
        <div className="min-w-0 flex-1">
          <FileName entry={entry} copy={copy} />
          <small className="mt-0.5 block text-metadata leading-[1.45] text-meta-foreground tracking-normal">
            {entry.detail}
          </small>
        </div>
        {entry.kind === "file" ? (
          <button
            type="button"
            disabled={busy}
            aria-label={fillDocCopy(f.download, { name: entry.name })}
            title={fillDocCopy(f.download, { name: entry.name })}
            onClick={onDownload}
            className={ICON_ACTION}
          >
            <Download aria-hidden="true" className="size-4" />
          </button>
        ) : entry.href && isWebUrl(entry.href) ? (
          <a
            href={entry.href}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={fillDocCopy(f.openLink, { name: entry.name })}
            title={fillDocCopy(f.openLink, { name: entry.name })}
            className={ICON_ACTION}
          >
            <ExternalLink aria-hidden="true" className="size-4" />
          </a>
        ) : null}
        {canEdit ? (
          <button
            type="button"
            disabled={busy}
            aria-label={fillDocCopy(f.deleteAction, { name: entry.name })}
            title={fillDocCopy(f.deleteAction, { name: entry.name })}
            onClick={() => onConfirm(entry.id)}
            className={ICON_ACTION}
          >
            <Trash2 aria-hidden="true" className="size-4" />
          </button>
        ) : null}
      </div>
      {confirming ? (
        <div
          role="group"
          aria-label={fillDocCopy(f.deleteConfirm, { name: entry.name })}
          className="flex flex-wrap items-center gap-1.5"
        >
          <span className="text-crumb leading-[1.45] text-foreground">
            {fillDocCopy(f.deleteConfirm, { name: entry.name })}
          </span>
          <button
            type="button"
            onClick={onDelete}
            className="h-6 rounded-[6px] border-0 bg-danger-subtle px-2 text-crumb font-medium text-danger-text outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {f.delete}
          </button>
          <button
            type="button"
            onClick={() => onConfirm(null)}
            className={TEXT_ACTION}
          >
            {f.deleteKeep}
          </button>
        </div>
      ) : null}
    </li>
  );
}

function AddLinkForm({
  copy,
  onAddLink,
}: {
  copy: DocPageCopy;
  onAddLink: DocFilesPanel["onAddLink"];
}) {
  const a = copy.files.addLink;
  const [url, setUrl] = useState("");
  const [name, setName] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<DocLinkFailure | null>(null);
  const [added, setAdded] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (pending) return;
    setAdded(false);
    if (!isWebUrl(url)) {
      setError("invalid");
      return;
    }
    setError(null);
    setPending(true);
    const failure = await onAddLink({ url: url.trim(), name: name.trim() });
    setPending(false);
    if (failure) {
      setError(failure);
      return;
    }
    setUrl("");
    setName("");
    setAdded(true);
  };
  // After a lost connection or a server fault the same button sends the same link again.
  const retryable = error === "network" || error === "failed";
  return (
    <form
      aria-label={a.heading}
      onSubmit={(event) => void submit(event)}
      className="flex flex-col gap-1.5"
      noValidate
    >
      <h3 className={SECTION_TITLE}>{a.heading}</h3>
      <input
        type="url"
        inputMode="url"
        aria-label={a.url}
        placeholder={a.urlPlaceholder}
        value={url}
        aria-invalid={error === "invalid" ? true : undefined}
        disabled={pending}
        onChange={(event) => {
          setUrl(event.target.value);
          setAdded(false);
          if (error === "invalid") setError(null);
        }}
        className={FIELD}
      />
      <input
        type="text"
        aria-label={a.name}
        placeholder={a.namePlaceholder}
        maxLength={255}
        value={name}
        disabled={pending}
        onChange={(event) => setName(event.target.value)}
        className={FIELD}
      />
      {error ? (
        <span
          role="alert"
          className="text-crumb leading-[1.45] text-danger-text"
        >
          {a.errors[error]}
        </span>
      ) : null}
      {added ? (
        <span
          role="status"
          className="text-crumb leading-[1.45] text-meta-foreground"
        >
          {a.added}
        </span>
      ) : null}
      <button
        type="submit"
        disabled={pending || url.trim() === ""}
        className="inline-flex h-8 items-center justify-center self-start rounded-[7px] border border-primary bg-primary px-3 text-compact leading-none font-semibold text-primary-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:opacity-50"
      >
        {pending ? a.pending : retryable ? a.retry : a.submit}
      </button>
    </form>
  );
}

/** The Files tab of the side panel: the page's files and links, and the link form for editors. */
export function DocFilesPanelSurface({
  copy,
  files,
}: {
  copy: DocPageCopy;
  files: DocFilesPanel;
}) {
  const f = copy.files;
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const run = (id: string, action: () => Promise<void>) => {
    setBusyId(id);
    void action().finally(() => setBusyId(null));
  };

  let body;
  if (files.status === "loading") {
    body = (
      <div
        role="status"
        aria-label={f.loading}
        className="h-14 rounded-[10px] bg-state-hover motion-safe:animate-pulse"
      />
    );
  } else if (files.status === "denied" || files.status === "notFound") {
    body = (
      <p
        role="alert"
        className="m-0 text-compact leading-[1.55] text-meta-foreground"
      >
        {files.status === "denied" ? f.denied : f.notFound}
      </p>
    );
  } else if (files.status === "error") {
    body = (
      <div
        role="alert"
        className="flex items-center gap-2.5 text-compact leading-[1.45] text-meta-foreground"
      >
        <span className="min-w-0 flex-1">{f.error}</span>
        <button type="button" onClick={files.onRetry} className={TEXT_ACTION}>
          {f.retry}
        </button>
      </div>
    );
  } else if (files.entries.length === 0) {
    body = (
      <p className="m-0 text-compact leading-[1.45] text-meta-foreground">
        {f.empty}
      </p>
    );
  } else {
    body = (
      <ul className="m-0 flex list-none flex-col p-0">
        {files.entries.map((entry) => (
          <FileRow
            key={entry.id}
            entry={entry}
            copy={copy}
            canEdit={files.canEdit}
            confirming={confirmingId === entry.id}
            busy={busyId === entry.id}
            onConfirm={setConfirmingId}
            onDownload={() => run(entry.id, () => files.onDownload(entry.id))}
            onDelete={() => {
              setConfirmingId(null);
              run(entry.id, () => files.onDelete(entry.id));
            }}
          />
        ))}
      </ul>
    );
  }

  return (
    <>
      <section>
        <h3 className={SECTION_TITLE}>
          {f.heading}
          {files.status === "ready" ? (
            <span className="font-normal text-meta-foreground">
              {files.entries.length}
            </span>
          ) : null}
        </h3>
        {body}
      </section>
      {files.canEdit ? (
        files.status === "denied" || files.status === "notFound" ? null : (
          <AddLinkForm copy={copy} onAddLink={files.onAddLink} />
        )
      ) : files.status === "ready" ? (
        <p className="m-0 text-crumb leading-[1.45] text-meta-foreground">
          {f.readOnly}
        </p>
      ) : null}
    </>
  );
}
