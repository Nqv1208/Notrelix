import type { ReactNode } from "react";
import {
  Ellipsis,
  Info,
  MessagesSquare,
  Phone,
  Pin,
  Search,
  Video,
} from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import type { ChatCopy } from "./chat.copy";
import type { ChatInfoSectionKey } from "./chat-info-panel";

export type ChatCallKind = "voice" | "video";

export interface ChatHeaderActionsProps {
  readonly copy: ChatCopy;
  /** Calls are not shipped yet: the two call buttons exist only when this is true and a handler is given. */
  readonly callsEnabled?: boolean;
  readonly onStartCall?: (kind: ChatCallKind) => void;
  readonly onSearch?: () => void;
  readonly infoOpen: boolean;
  readonly onToggleInfo: () => void;
  /** The name of the info button; a channel says "Channel info", a conversation "Conversation details". */
  readonly infoLabel?: string;
  /** Opens the info panel at a section, expanded. */
  readonly onOpenSection: (key: ChatInfoSectionKey) => void;
  /** More entries of the "…" menu, under Pinned and Following. */
  readonly menu?: ReactNode;
}

/**
 * The icon buttons at the right of a conversation or channel header (Chat-Info boards): voice call, video call, search,
 * the info toggle and a "…" menu that holds the rest. Every button names itself by label and tooltip; none shows a key hint.
 */
export function ChatHeaderActions({
  copy,
  callsEnabled = false,
  onStartCall,
  onSearch,
  infoOpen,
  onToggleInfo,
  infoLabel,
  onOpenSection,
  menu,
}: ChatHeaderActionsProps) {
  const calls = callsEnabled && onStartCall !== undefined;
  return (
    <div className="flex shrink-0 items-center gap-1">
      {calls ? (
        <>
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={copy.panel.voiceCall}
            title={copy.panel.voiceCall}
            onClick={() => onStartCall("voice")}
          >
            <Phone aria-hidden="true" />
          </Button>
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={copy.panel.videoCall}
            title={copy.panel.videoCall}
            onClick={() => onStartCall("video")}
          >
            <Video aria-hidden="true" />
          </Button>
        </>
      ) : null}
      {onSearch ? (
        <Button
          variant="ghost"
          size="icon-sm"
          aria-label={copy.search.open}
          title={copy.search.open}
          onClick={onSearch}
        >
          <Search aria-hidden="true" />
        </Button>
      ) : null}
      <Button
        variant={infoOpen ? "secondary" : "ghost"}
        size="icon-sm"
        aria-label={infoLabel ?? copy.dm.view.details}
        title={infoLabel ?? copy.dm.view.details}
        aria-pressed={infoOpen}
        onClick={onToggleInfo}
      >
        <Info aria-hidden="true" />
      </Button>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={copy.panel.more}
            title={copy.panel.more}
          >
            <Ellipsis aria-hidden="true" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="end"
          className="w-[248px] [&_[role^=menuitem]]:min-h-9"
        >
          <DropdownMenuItem onSelect={() => onOpenSection("pins")}>
            <Pin aria-hidden="true" />
            {copy.pins.title}
          </DropdownMenuItem>
          <DropdownMenuItem onSelect={() => onOpenSection("following")}>
            <MessagesSquare aria-hidden="true" />
            {copy.thread.followedTab}
          </DropdownMenuItem>
          {menu ? (
            <>
              <DropdownMenuSeparator />
              {menu}
            </>
          ) : null}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
