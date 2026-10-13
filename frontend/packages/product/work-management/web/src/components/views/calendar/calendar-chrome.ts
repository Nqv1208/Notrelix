import { cn } from "@notrelix/ui-web";
import { kanbanOutlineButtonClass } from "../kanban/kanban-chrome";

/**
 * The Calendar's 32 px outline button (Hôm nay, Bỏ lọc, Đặt hạn). On the
 * dark panel the mockup keeps the button on the panel tier with the strong
 * line, where the shared outline variant would tint it.
 */
export const calendarOutlineButtonClass = cn(
  kanbanOutlineButtonClass,
  "dark:border-line-strong dark:bg-card dark:hover:bg-state-hover-strong",
);
