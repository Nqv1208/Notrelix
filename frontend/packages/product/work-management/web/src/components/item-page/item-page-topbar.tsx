import {
  ArrowLeft,
  ChevronDown,
  ChevronUp,
  Link2,
  Minimize2,
  MoreHorizontal,
} from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
  cn,
} from "@notrelix/ui-web";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import type {
  TaskDetailCallbacks,
  TaskDetailCapabilities,
} from "../card-detail/task-detail-types";
import type { ItemPageCopy } from "./item-page.copy";
import {
  DisabledControl,
  ICON_BUTTON_CLASS,
  MARK_TONE_CLASS,
  PageLink,
} from "./item-page-parts";
import type { ItemPageNavigation, ItemPageSpace } from "./item-page-types";

const CRUMB_LINK_CLASS = "hover:text-foreground hover:underline";

/**
 * The page's own bar: back to the board, where the item sits, K/J stepping,
 * the way back to the side sheet, copy link and the item menu.
 */
export function ItemPageTopbar({
  board,
  card,
  space,
  navigation,
  capabilities,
  callbacks,
  copy,
  approvers = [],
}: {
  board: Board;
  card: CardDetail;
  space?: ItemPageSpace;
  navigation: ItemPageNavigation;
  capabilities: TaskDetailCapabilities;
  callbacks: TaskDetailCallbacks;
  copy: ItemPageCopy;
  /** The people who may be asked to approve the item; no entry in the menu without any. */
  approvers?: readonly { id: string; label: string }[];
}) {
  const t = copy.topbar;
  const { position } = navigation;
  const lane = position?.laneName ?? "";
  const canAsk =
    capabilities.canEditFields &&
    callbacks.onRequestApproval !== undefined &&
    approvers.length > 0;
  const hasMenu =
    capabilities.canCreateCard ||
    capabilities.canDelete ||
    canAsk ||
    Boolean(callbacks.onMoveToBoard);

  return (
    <header className="flex h-13.25 shrink-0 items-center gap-1.5 border-b border-line bg-background pr-4 pl-3 sm:pl-5">
      <PageLink
        href={navigation.boardHref}
        onNavigate={navigation.onBack}
        aria-label={t.back}
        className={ICON_BUTTON_CLASS}
      >
        <ArrowLeft aria-hidden="true" />
      </PageLink>
      <nav
        aria-label={t.crumb}
        className="flex min-w-0 items-center gap-2 text-item-small text-meta-foreground"
      >
        {space ? (
          <>
            <span
              aria-hidden="true"
              className={cn(
                "grid size-4.5 shrink-0 place-items-center rounded-[6px] text-mark-3xs",
                MARK_TONE_CLASS[space.mark.tone],
              )}
            >
              {space.mark.initials}
            </span>
            <PageLink
              href={space.href}
              onNavigate={navigation.onOpenSpace}
              className={cn("truncate", CRUMB_LINK_CLASS)}
            >
              {space.name}
            </PageLink>
            <span aria-hidden="true" className="text-line-strong">
              /
            </span>
          </>
        ) : null}
        <PageLink
          href={navigation.boardHref}
          onNavigate={navigation.onOpenBoard ?? navigation.onBack}
          className={cn("truncate", CRUMB_LINK_CLASS)}
        >
          {board.title}
        </PageLink>
        {card.itemKey ? (
          <>
            <span aria-hidden="true" className="text-line-strong">
              /
            </span>
            <span
              aria-current="page"
              className="shrink-0 font-mono font-medium text-foreground"
            >
              {card.itemKey}
            </span>
          </>
        ) : null}
      </nav>
      <span className="flex-1" aria-hidden="true" />

      {position ? (
        <span className="mr-1 hidden items-center gap-[5px] text-item-note text-meta-foreground md:inline-flex">
          {fillSheetCopy(t.position, {
            index: position.index,
            total: position.total,
            lane,
          })}
        </span>
      ) : null}
      <StepButton
        label={
          callbacks.onPrevious
            ? t.previous
            : fillSheetCopy(t.previousNone, { lane })
        }
        onStep={callbacks.onPrevious}
      >
        <ChevronUp aria-hidden="true" />
      </StepButton>
      <StepButton
        label={callbacks.onNext ? t.next : fillSheetCopy(t.nextNone, { lane })}
        onStep={callbacks.onNext}
      >
        <ChevronDown aria-hidden="true" />
      </StepButton>
      <span aria-hidden="true" className="mx-1.5 h-5 w-px bg-line" />
      {navigation.onCollapse ? (
        <button
          type="button"
          aria-label={t.collapse}
          title={t.collapse}
          onClick={navigation.onCollapse}
          className={ICON_BUTTON_CLASS}
        >
          <Minimize2 aria-hidden="true" />
        </button>
      ) : null}
      {callbacks.onCopyLink ? (
        <button
          type="button"
          aria-label={t.copyLink}
          title={t.copyLink}
          onClick={() => callbacks.onCopyLink?.(card.id)}
          className={ICON_BUTTON_CLASS}
        >
          <Link2 aria-hidden="true" />
        </button>
      ) : (
        <DisabledControl reason={t.copyLinkUnavailable}>
          <button
            type="button"
            aria-disabled="true"
            aria-label={t.copyLink}
            className={ICON_BUTTON_CLASS}
          >
            <Link2 aria-hidden="true" />
          </button>
        </DisabledControl>
      )}
      {hasMenu ? (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              type="button"
              aria-haspopup="menu"
              aria-label={t.more}
              className={ICON_BUTTON_CLASS}
            >
              <MoreHorizontal aria-hidden="true" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {capabilities.canCreateCard ? (
              <DropdownMenuItem onSelect={() => callbacks.onDuplicate(card.id)}>
                {t.duplicate}
              </DropdownMenuItem>
            ) : null}
            {canAsk ? (
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  {copy.approval.request}
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent
                  aria-label={copy.approval.searchLabel}
                  className="max-h-72 overflow-y-auto"
                >
                  {approvers.map((person) => (
                    <DropdownMenuItem
                      key={person.id}
                      onSelect={() => callbacks.onRequestApproval?.(person.id)}
                    >
                      {person.label}
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuSubContent>
              </DropdownMenuSub>
            ) : null}
            {capabilities.canMoveCard && callbacks.onMoveToBoard ? (
              <DropdownMenuItem
                onSelect={() => callbacks.onMoveToBoard?.(card.id)}
              >
                {t.moveToBoard}
              </DropdownMenuItem>
            ) : null}
            {capabilities.canDelete ? (
              <DropdownMenuItem onSelect={() => callbacks.onArchive(card.id)}>
                {t.archive}
              </DropdownMenuItem>
            ) : null}
            {capabilities.canDelete && callbacks.onDelete ? (
              <DropdownMenuItem
                variant="destructive"
                onSelect={() => callbacks.onDelete?.(card.id)}
              >
                {t.delete}
              </DropdownMenuItem>
            ) : null}
          </DropdownMenuContent>
        </DropdownMenu>
      ) : null}
    </header>
  );
}

function StepButton({
  label,
  onStep,
  children,
}: {
  label: string;
  onStep?: () => void;
  children: React.ReactNode;
}) {
  const button = (
    <button
      type="button"
      aria-label={label}
      title={label}
      onClick={onStep}
      className={ICON_BUTTON_CLASS}
    >
      {children}
    </button>
  );
  if (onStep) return button;
  return <DisabledControl reason={label}>{button}</DisabledControl>;
}
