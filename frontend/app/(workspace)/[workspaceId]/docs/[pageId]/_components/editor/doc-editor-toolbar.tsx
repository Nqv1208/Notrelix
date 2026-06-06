"use client"

import {
  Bold,
  Bot,
  CheckSquare,
  Code2,
  Italic,
  Link2,
  List,
  ListChecks,
  ListOrdered,
  MoreHorizontal,
  Plus,
  Quote,
  Redo2,
  Strikethrough,
  Underline,
  Undo2,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { Toggle } from "@/components/ui/toggle"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { useDocToolbar } from "@/features/docs/hooks/use-doc-toolbar"
import type { Block, BlockType } from "@/features/docs/types"
import { AlignmentControls } from "./alignment-controls"
import { BlockTypeMenu } from "./block-type-menu"
import { CommentsPopover } from "./comments-popover"
import { FontFamilyDropdown } from "./font-family-dropdown"
import { MentionMenu } from "./mention-menu"
import { TextStyleDropdown } from "./text-style-dropdown"

export function DocEditorToolbar({ pageId, blocks, compact = false }: { pageId: string; blocks: Block[]; compact?: boolean }) {
  const toolbar = useDocToolbar(pageId, blocks)
  const properties = toolbar.properties

  function convert(type: BlockType) {
    toolbar.setBlockType(type)
  }

  return (
    <div className="flex min-h-14 flex-wrap items-center gap-2 bg-background px-4 py-2 sm:px-6">
      <TooltipButton label="Add block">
        <Button size="sm" className="rounded-full" onClick={() => toolbar.addBlock("paragraph")}>
          <Plus className="size-4" />
          {!compact ? <span>Add block</span> : null}
        </Button>
      </TooltipButton>
      <Separator orientation="vertical" className="mx-1 h-7" />
      <ToolbarIcon icon={Undo2} label="Undo" />
      <ToolbarIcon icon={Redo2} label="Redo" />
      <Separator orientation="vertical" className="mx-1 h-7" />
      <BlockTypeMenu value={toolbar.activeBlockType} onValueChange={convert} />
      <FontFamilyDropdown value={properties.fontFamily} onValueChange={(fontFamily) => toolbar.updateProperties({ fontFamily })} />
      <TextStyleDropdown properties={properties} onUpdate={toolbar.updateProperties} />
      <Separator orientation="vertical" className="mx-1 h-7" />
      <Toggle size="sm" pressed={Boolean(properties.bold)} onPressedChange={() => toolbar.toggleProperty("bold")} aria-label="Bold">
        <Bold className="size-4" />
      </Toggle>
      <Toggle size="sm" pressed={Boolean(properties.italic)} onPressedChange={() => toolbar.toggleProperty("italic")} aria-label="Italic">
        <Italic className="size-4" />
      </Toggle>
      <Toggle size="sm" pressed={Boolean(properties.underline)} onPressedChange={() => toolbar.toggleProperty("underline")} aria-label="Underline">
        <Underline className="size-4" />
      </Toggle>
      <Toggle size="sm" pressed={Boolean(properties.strike)} onPressedChange={() => toolbar.toggleProperty("strike")} aria-label="Strikethrough">
        <Strikethrough className="size-4" />
      </Toggle>
      <Separator orientation="vertical" className="mx-1 h-7" />
      <AlignmentControls value={properties.align} onValueChange={(align) => toolbar.updateProperties({ align })} />
      <Separator orientation="vertical" className="mx-1 h-7" />
      <ToolbarIcon icon={List} label="Bullet list" onClick={() => convert("bulleted_list")} />
      <ToolbarIcon icon={ListOrdered} label="Numbered list" onClick={() => convert("numbered_list")} />
      <ToolbarIcon icon={ListChecks} label="Checklist" onClick={() => convert("todo")} />
      <ToolbarIcon icon={Quote} label="Quote" onClick={() => convert("quote")} />
      <ToolbarIcon icon={Code2} label="Code" onClick={() => convert("code")} />
      <ToolbarIcon icon={CheckSquare} label="Callout" onClick={() => convert("callout")} />
      <Separator orientation="vertical" className="mx-1 h-7" />
      <ToolbarIcon icon={Link2} label="Link" />
      <MentionMenu />
      <Button variant="ghost" size="sm" className="rounded-full">
        <Bot className="size-4" />
        AI action
      </Button>
      <CommentsPopover pageId={pageId} blockId={toolbar.activeBlockId ?? undefined} />
      <ToolbarIcon icon={MoreHorizontal} label="More" />
    </div>
  )
}

function ToolbarIcon({ icon: Icon, label, onClick }: { icon: typeof Bold; label: string; onClick?: () => void }) {
  return (
    <TooltipButton label={label}>
      <Button variant="ghost" size="icon-sm" aria-label={label} onClick={onClick}>
        <Icon className="size-4" />
      </Button>
    </TooltipButton>
  )
}

function TooltipButton({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <Tooltip>
      <TooltipTrigger asChild>{children}</TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  )
}
