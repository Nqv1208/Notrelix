import { Copy, CopyCheck, Trash2 } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";

export function KanbanCardMenu({
  onCopyLink,
  onDuplicate,
  onDelete,
  children,
}: {
  onCopyLink: () => void;
  onDuplicate: () => void;
  onDelete: () => void;
  children: React.ReactNode;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{children}</DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-44">
        <DropdownMenuItem onClick={onCopyLink}>
          <Copy className="mr-2 size-4 text-muted-foreground" />
          Copy link
        </DropdownMenuItem>
        <DropdownMenuItem onClick={onDuplicate}>
          <CopyCheck className="mr-2 size-4 text-muted-foreground" />
          Duplicate card
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          className="text-destructive focus:bg-destructive/10 focus:text-destructive"
          onClick={onDelete}
        >
          <Trash2 className="mr-2 size-4" />
          Archive card
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
