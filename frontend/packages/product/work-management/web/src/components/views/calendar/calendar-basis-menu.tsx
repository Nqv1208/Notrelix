import { CalendarDays, ChevronDown } from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import type { CalendarCopy } from "./calendar.copy";
import type { CalendarDateBasis } from "./calendar-model";

export interface CalendarBasisMenuProps {
  copy: CalendarCopy;
  dateBasis: CalendarDateBasis;
  onDateBasisChange: (basis: CalendarDateBasis) => void;
  dateFields?: readonly { id: string; name: string }[];
}

/** The date field the board places items by, as a 44 px button of the touch tray. */
export function CalendarBasisMenu({
  copy,
  dateBasis,
  onDateBasisChange,
  dateFields = [],
}: CalendarBasisMenuProps) {
  const label =
    dateBasis === "span" || dateBasis === "due"
      ? copy.basis[dateBasis]
      : (dateFields.find((field) => field.id === dateBasis)?.name ??
        copy.basis.span);
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          className="h-11 w-full justify-start gap-2 rounded-[10px] border-line-strong bg-popover px-3 text-sm font-normal text-muted-foreground shadow-none dark:border-line-strong dark:bg-card"
        >
          <CalendarDays className="size-4" aria-hidden="true" />
          <span className="truncate">
            {copy.basis.label}{" "}
            <b className="font-semibold text-foreground">{label}</b>
          </span>
          <ChevronDown className="ml-auto size-3.5" aria-hidden="true" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-56">
        <DropdownMenuRadioGroup
          value={dateBasis}
          onValueChange={(value) => {
            if (value !== dateBasis) onDateBasisChange(value);
          }}
        >
          {(["span", "due"] as const).map((value) => (
            <DropdownMenuRadioItem key={value} value={value}>
              {copy.basis[value]}
            </DropdownMenuRadioItem>
          ))}
          {dateFields.map((field) => (
            <DropdownMenuRadioItem key={field.id} value={field.id}>
              {field.name}
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
