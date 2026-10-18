import { cloneElement, useId, type MouseEvent, type ReactElement } from "react";
import { Tooltip, TooltipContent, TooltipTrigger } from "@notrelix/ui-web";

/**
 * A control that cannot work yet: it stays in the tab order with
 * `aria-disabled`, says why in a tooltip and to assistive technology, and
 * never acts. The child is the control element.
 */
export function DisabledHint({
  reason,
  children,
}: {
  reason: string;
  children: ReactElement<{
    "aria-disabled"?: boolean | "true" | "false";
    "aria-describedby"?: string;
    onClick?: (event: MouseEvent) => void;
  }>;
}) {
  const reasonId = useId();
  return (
    <>
      <Tooltip>
        <TooltipTrigger asChild>
          {cloneElement(children, {
            "aria-disabled": "true",
            "aria-describedby": reasonId,
            onClick: (event: MouseEvent) => event.preventDefault(),
          })}
        </TooltipTrigger>
        <TooltipContent>{reason}</TooltipContent>
      </Tooltip>
      <span id={reasonId} className="sr-only">
        {reason}
      </span>
    </>
  );
}
