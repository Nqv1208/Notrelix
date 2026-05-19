import type { Block, BlockProperties, BlockType } from "@/features/docs/types"
import { cn } from "@/lib/utils"

export const textColorClass: Record<NonNullable<BlockProperties["textColor"]>, string> = {
  default: "text-foreground",
  muted: "text-muted-foreground",
  primary: "text-primary",
  accent: "text-accent-foreground",
  destructive: "text-destructive",
}

export const highlightClass: Record<NonNullable<BlockProperties["highlight"]>, string> = {
  none: "",
  muted: "bg-muted",
  accent: "bg-accent",
  primary: "bg-primary/10",
}

export const blockTypeLabels: Record<BlockType, string> = {
  paragraph: "Text",
  heading_1: "Heading 1",
  heading_2: "Heading 2",
  heading_3: "Heading 3",
  bulleted_list: "Bullet list",
  numbered_list: "Numbered list",
  todo: "Checklist",
  quote: "Quote",
  divider: "Divider",
  code: "Code",
  callout: "Callout",
  toggle: "Toggle",
  image: "Image",
  embed: "Embed",
  table: "Table",
  board_reference: "Board reference",
  page_reference: "Page reference",
}

export function getBlockTextClass(block: Block) {
  const properties = block.properties
  return cn(
    "leading-7",
    textColorClass[properties.textColor ?? "default"],
    properties.bold && "font-semibold",
    properties.italic && "italic",
    properties.underline && "underline underline-offset-4",
    properties.strike && "line-through",
    properties.fontFamily === "poppins" && "font-[var(--font-display)]",
    properties.fontFamily === "serif" && "font-serif",
    properties.fontFamily === "mono" && "font-mono",
    properties.fontSize === "sm" && "text-sm",
    (properties.fontSize === "base" || !properties.fontSize) && "text-base",
    properties.fontSize === "lg" && "text-lg",
    properties.fontSize === "xl" && "text-xl",
    properties.align === "center" && "text-center",
    properties.align === "right" && "text-right",
    (!properties.align || properties.align === "left") && "text-left"
  )
}

export function getHighlightClass(properties: BlockProperties) {
  return highlightClass[properties.highlight ?? "none"]
}
