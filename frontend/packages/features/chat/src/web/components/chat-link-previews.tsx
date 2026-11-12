import { FileText, Link2, Lock, SquareKanban } from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import {
  findProductLinks,
  type ChatProductLink,
} from "../../core/model/link-model";
import type { ChatLinkKind } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";

const CHIP =
  "inline-flex max-w-full min-w-0 items-center gap-1.5 rounded-md border border-border bg-muted/40 px-2 py-1 text-xs";

export function kindLabel(kind: ChatLinkKind, copy: ChatCopy): string {
  switch (kind) {
    case "documents.page":
      return copy.links.page;
    case "work-management.board":
      return copy.links.board;
    case "governance.team":
      return copy.links.team;
    default:
      return copy.links.item;
  }
}

export function KindIcon({ kind }: { readonly kind: ChatLinkKind }) {
  const Icon = kind === "documents.page" ? FileText : SquareKanban;
  return <Icon className="size-3.5 shrink-0" aria-hidden="true" />;
}

/**
 * One link chip (UC-CHT-G05, CHT-023). The preview is resolved on render with the viewer's own access: while it loads a
 * skeleton, with access the title of the Board item or Page, without access a generic chip that names only the kind (a hidden,
 * a trashed and a missing resource look the same), and when the read fails the kind alone. The link itself still leads to the
 * product route, which decides again.
 */
function LinkPreviewChip({
  link,
  workspaceId,
  hooks,
  copy,
}: {
  readonly link: ChatProductLink;
  readonly workspaceId: string;
  readonly hooks: ChatHooks;
  readonly copy: ChatCopy;
}) {
  const preview = hooks.useLinkPreview(workspaceId, link.ref);
  const kind = kindLabel(link.kind, copy);

  if (preview.isPending) {
    return (
      <span
        className={CHIP}
        role="status"
        aria-label={copy.links.loading}
        data-link-preview="loading"
      >
        <Skeleton className="size-3.5 rounded-sm" />
        <Skeleton className="h-3.5 w-32" />
      </span>
    );
  }

  const title = preview.data?.accessible ? preview.data.title : null;
  if (title) {
    return (
      <a
        href={link.href}
        className={cn(
          CHIP,
          "text-foreground hover:bg-muted focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none",
        )}
        aria-label={fillCopy(copy.links.open, { kind, title })}
        data-link-preview="resolved"
      >
        <KindIcon kind={link.kind} />
        <span className="truncate font-medium">{title}</span>
        <span className="shrink-0 text-muted-foreground">{kind}</span>
      </a>
    );
  }

  const denied = preview.isSuccess;
  return (
    <a
      href={link.href}
      className={cn(
        CHIP,
        "text-muted-foreground hover:bg-muted focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none",
      )}
      aria-label={fillCopy(copy.links.openGeneric, { kind })}
      title={denied ? copy.links.noAccessHint : copy.links.unavailableHint}
      data-link-preview={denied ? "no-access" : "unavailable"}
    >
      {denied ? (
        <Lock className="size-3.5 shrink-0" aria-hidden="true" />
      ) : (
        <Link2 className="size-3.5 shrink-0" aria-hidden="true" />
      )}
      <span className="truncate">
        {fillCopy(denied ? copy.links.noAccess : copy.links.unavailable, {
          kind,
        })}
      </span>
    </a>
  );
}

/** The previews of the Board item and Page links in a message (at most a few; the text keeps every link). */
export function MessageLinkPreviews({
  content,
  workspaceId,
  hooks,
  copy,
}: {
  readonly content: string | null;
  readonly workspaceId: string;
  readonly hooks: ChatHooks;
  readonly copy: ChatCopy;
}) {
  const links = content ? findProductLinks(content, workspaceId) : [];
  if (links.length === 0) return null;
  return (
    <ul
      aria-label={copy.links.listLabel}
      className="mt-1 flex flex-col items-start gap-1"
      data-testid="chat-link-previews"
    >
      {links.map((link) => (
        <li key={`${link.kind}:${link.resourceId}`} className="max-w-full">
          <LinkPreviewChip
            link={link}
            workspaceId={workspaceId}
            hooks={hooks}
            copy={copy}
          />
        </li>
      ))}
    </ul>
  );
}
