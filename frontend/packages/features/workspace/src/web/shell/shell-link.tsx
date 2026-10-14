import * as React from "react";
import { FileText, SquareKanban } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import {
  identityToneFor,
  initialsFor,
  markInitialsFor,
  NAVIGATION_RESOURCE_KIND,
  type IdentityTone,
  type MarkToneId,
  type NavigationResourceKind,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import {
  IDENTITY_ICON_CLASS,
  IDENTITY_MARK_CLASS,
  MARK_TONE_CLASS,
  isPlainClick,
  type ShellNavigation,
} from "./shell-models";

export interface ShellLinkProps extends Omit<
  React.ComponentProps<"a">,
  "href" | "onClick"
> {
  readonly destination: WorkspaceDestination;
  readonly navigation: ShellNavigation;
  /** Runs after a plain click is handed to the host (e.g. close a drawer). */
  readonly onNavigated?: () => void;
}

/** An anchor with a real href whose plain clicks go through the host router. */
export const ShellLink = React.forwardRef<HTMLAnchorElement, ShellLinkProps>(
  function ShellLink({ destination, navigation, onNavigated, ...props }, ref) {
    return (
      <a
        ref={ref}
        {...props}
        href={navigation.getHref(destination)}
        onClick={(event) => {
          if (!isPlainClick(event)) return;
          event.preventDefault();
          navigation.onNavigate(destination);
          onNavigated?.();
        }}
      />
    );
  },
);

/**
 * Chip colour: an identity tone (members, avatars) or a mark tone
 * (workspace and department initials, WX-DEC-014). Exactly one is given.
 */
export type IdentityMarkToneProps =
  | { readonly tone: IdentityTone; readonly markTone?: never }
  | { readonly markTone: MarkToneId; readonly tone?: never };

export type IdentityMarkProps = IdentityMarkToneProps & {
  readonly name: string;
  readonly size?: "sm" | "md";
  readonly shape?: "square" | "round";
  readonly imageUrl?: string | null;
  readonly className?: string;
};

/** Initials on an identity or mark colour (workspace, Space, member). Decorative. */
export function IdentityMark({
  name,
  tone,
  markTone,
  size = "md",
  shape = "square",
  imageUrl,
  className,
}: IdentityMarkProps) {
  const box = cn(
    "flex shrink-0 items-center justify-center overflow-hidden font-semibold select-none",
    size === "sm" ? "size-5 text-caption" : "size-8 text-label",
    shape === "round" ? "rounded-full" : "rounded-md",
    className,
  );
  if (imageUrl) {
    return (
      <img
        src={imageUrl}
        alt=""
        aria-hidden="true"
        className={cn(box, "object-cover")}
      />
    );
  }
  return (
    <span
      aria-hidden="true"
      className={cn(
        box,
        markTone ? MARK_TONE_CLASS[markTone] : IDENTITY_MARK_CLASS[tone],
      )}
    >
      {markTone ? markInitialsFor(name) : initialsFor(name)}
    </span>
  );
}

export interface ResourceMarkProps {
  readonly kind: NavigationResourceKind;
  /** Resource id; picks a stable identity tone. */
  readonly id: string;
  readonly size?: "sm" | "md";
  readonly className?: string;
}

/**
 * A Board or Doc identity mark: the kind's icon in the resource's identity
 * tone on a neutral tile (identity only, never status — WX-DEC-006; a quiet
 * tile keeps dense lists restrained). Decorative.
 */
export function ResourceMark({
  kind,
  id,
  size = "md",
  className,
}: ResourceMarkProps) {
  const Icon =
    kind === NAVIGATION_RESOURCE_KIND.board ? SquareKanban : FileText;
  return (
    <span
      aria-hidden="true"
      className={cn(
        "flex shrink-0 items-center justify-center rounded-md border border-border bg-surface-raised",
        size === "sm" ? "size-5 [&_svg]:size-3" : "size-7 [&_svg]:size-4",
        IDENTITY_ICON_CLASS[identityToneFor(id)],
        className,
      )}
    >
      <Icon />
    </span>
  );
}
