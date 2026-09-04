import { MoreHorizontal, Palette, Pen, Trash2 } from "lucide-react";
import { Button } from "@notrelix/ui-web";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";

const COLOR_OPTIONS = [
  { name: "Mint", value: "#bcfe90" },
  { name: "Lavender", value: "#eddff7" },
  { name: "Sky", value: "#abf0ff" },
  { name: "Sunset", value: "#ff8940" },
  { name: "Pale Blue", value: "#e7ecff" },
  { name: "Ocean", value: "#93beff" },
  { name: "Ice", value: "#d1faff" },
  { name: "Fuchsia", value: "#ff83dd" },
  { name: "Gold", value: "#ffc95e" },
  { name: "Teal", value: "#2a9d99" },
  { name: "Coral", value: "#ff8a33" },
  { name: "Grape", value: "#ad6ded" },
];

export function KanbanColumnMenu({
  onRename,
  onColorChange,
  onDelete,
}: {
  onRename: () => void;
  onColorChange: (color: string) => void;
  onDelete: () => void;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon-sm" aria-label="Column menu">
          <MoreHorizontal className="size-4" aria-hidden="true" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-48">
        <DropdownMenuItem onClick={onRename}>
          <Pen className="mr-2 size-4 text-muted-foreground" />
          Rename column
        </DropdownMenuItem>

        <DropdownMenuSub>
          <DropdownMenuSubTrigger>
            <Palette className="mr-2 size-4 text-muted-foreground" />
            Change color
          </DropdownMenuSubTrigger>
          <DropdownMenuSubContent className="w-44">
            <div className="grid grid-cols-4 gap-1.5 p-2">
              {COLOR_OPTIONS.map((color) => (
                <button
                  type="button"
                  key={color.name}
                  className="size-6 rounded-full border border-border/30 transition-transform hover:scale-110 active:scale-95"
                  style={{ backgroundColor: color.value }}
                  aria-label={`Set column color to ${color.name}`}
                  title={color.name}
                  onClick={() => onColorChange(color.value)}
                />
              ))}
            </div>
          </DropdownMenuSubContent>
        </DropdownMenuSub>

        <DropdownMenuSeparator />
        <DropdownMenuItem
          className="text-destructive focus:bg-destructive/10 focus:text-destructive"
          onClick={onDelete}
        >
          <Trash2 className="mr-2 size-4" />
          Delete column
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
