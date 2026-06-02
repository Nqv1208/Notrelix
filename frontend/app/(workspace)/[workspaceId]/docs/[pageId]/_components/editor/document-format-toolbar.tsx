"use client"

import type { ComponentType } from "react"
import { AlignCenter, AlignLeft, AlignRight, Heading1, Heading2, Heading3, List, ListChecks, Palette, Pilcrow, Quote } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Separator } from "@/components/ui/separator"
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"
import { useUpdateBlock } from "@/features/docs/hooks/use-update-block"
import { useDocsEditorStore } from "@/features/docs/store/editor-store"
import type { Block, BlockType } from "@/features/docs/types"

interface DocumentFormatToolbarProps {
  pageId: string
  blocks: Block[]
}

const typeOptions: Array<{ type: BlockType; icon: ComponentType<{ className?: string }>; label: string }> = [
  { type: "paragraph", icon: Pilcrow, label: "Text" },
  { type: "heading_1", icon: Heading1, label: "H1" },
  { type: "heading_2", icon: Heading2, label: "H2" },
  { type: "heading_3", icon: Heading3, label: "H3" },
  { type: "bulleted_list", icon: List, label: "Bullet" },
  { type: "todo", icon: ListChecks, label: "Todo" },
  { type: "quote", icon: Quote, label: "Quote" },
]

const colors = [
  { value: "default", label: "Default", className: "bg-foreground" },
  { value: "muted", label: "Muted", className: "bg-muted-foreground" },
  { value: "primary", label: "Primary", className: "bg-primary" },
  { value: "accent", label: "Accent", className: "bg-accent-foreground" },
  { value: "destructive", label: "Danger", className: "bg-destructive" },
] as const

export function DocumentFormatToolbar({ pageId, blocks }: DocumentFormatToolbarProps) {
  const focusedBlockId = useDocsEditorStore((state) => state.focusedBlockId)
  const updateBlock = useUpdateBlock(pageId)
  const block = blocks.find((item) => item.id === focusedBlockId) ?? blocks[0]

  function update(payload: Parameters<typeof updateBlock.mutate>[0]["payload"]) {
    if (!block) return
    updateBlock.mutate({ blockId: block.id, payload })
  }

  return (
    <div className="sticky top-28 z-30 mb-6 rounded-2xl border border-border bg-card/90 p-2 shadow-[rgba(0,0,0,0.08)_0px_8px_28px] backdrop-blur-xl">
      <div className="flex flex-wrap items-center gap-1">
        <Select value={block?.type ?? "paragraph"} onValueChange={(value) => update({ type: value as BlockType })}>
          <SelectTrigger size="sm" className="w-[132px] bg-card">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {typeOptions.map((option) => (
              <SelectItem key={option.type} value={option.type}>
                <span className="flex items-center gap-2"><option.icon className="size-4" /> {option.label}</span>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select value={block?.properties.fontFamily ?? "inter"} onValueChange={(fontFamily) => update({ properties: { fontFamily: fontFamily as "inter" | "poppins" | "serif" | "mono" } })}>
          <SelectTrigger size="sm" className="w-[120px] bg-card"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="inter">Inter</SelectItem>
            <SelectItem value="poppins">Poppins</SelectItem>
            <SelectItem value="serif">Serif</SelectItem>
            <SelectItem value="mono">Mono</SelectItem>
          </SelectContent>
        </Select>

        <Select value={block?.properties.fontSize ?? "base"} onValueChange={(fontSize) => update({ properties: { fontSize: fontSize as "sm" | "base" | "lg" | "xl" } })}>
          <SelectTrigger size="sm" className="w-[96px] bg-card"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="sm">Small</SelectItem>
            <SelectItem value="base">Body</SelectItem>
            <SelectItem value="lg">Large</SelectItem>
            <SelectItem value="xl">XL</SelectItem>
          </SelectContent>
        </Select>

        <Separator orientation="vertical" className="mx-1 h-7" />
        <ToggleGroup
          type="single"
          value={block?.properties.align ?? "left"}
          onValueChange={(align) => align && update({ properties: { align: align as "left" | "center" | "right" } })}
        >
          <ToggleGroupItem value="left" size="sm" aria-label="Align left"><AlignLeft className="size-4" /></ToggleGroupItem>
          <ToggleGroupItem value="center" size="sm" aria-label="Align center"><AlignCenter className="size-4" /></ToggleGroupItem>
          <ToggleGroupItem value="right" size="sm" aria-label="Align right"><AlignRight className="size-4" /></ToggleGroupItem>
        </ToggleGroup>

        <Separator orientation="vertical" className="mx-1 h-7" />
        <div className="flex items-center gap-1">
          <Palette className="mx-1 size-4 text-muted-foreground" />
          {colors.map((color) => (
            <Button
              key={color.value}
              variant="ghost"
              size="icon-xs"
              className="rounded-full"
              onClick={() => update({ properties: { textColor: color.value } })}
              aria-label={`Set color ${color.label}`}
            >
              <span className={`size-4 rounded-full border border-border ${color.className}`} />
            </Button>
          ))}
        </div>
      </div>
    </div>
  )
}
