"use client"

import { Palette } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type { BlockProperties } from "@/features/docs/types"

const sizes: Array<{ value: NonNullable<BlockProperties["fontSize"]>; label: string }> = [
  { value: "sm", label: "Small" },
  { value: "base", label: "Body" },
  { value: "lg", label: "Large" },
  { value: "xl", label: "XL" },
]

const textColors: Array<{ value: NonNullable<BlockProperties["textColor"]>; label: string; className: string }> = [
  { value: "default", label: "Default", className: "bg-foreground" },
  { value: "muted", label: "Muted", className: "bg-muted-foreground" },
  { value: "primary", label: "Primary", className: "bg-primary" },
  { value: "accent", label: "Accent", className: "bg-accent-foreground" },
  { value: "destructive", label: "Danger", className: "bg-destructive" },
]

const highlights: Array<{ value: NonNullable<BlockProperties["highlight"]>; label: string; className: string }> = [
  { value: "none", label: "No highlight", className: "bg-background" },
  { value: "muted", label: "Muted", className: "bg-muted" },
  { value: "accent", label: "Accent", className: "bg-accent" },
  { value: "primary", label: "Primary", className: "bg-primary/20" },
]

export function TextStyleDropdown({
  properties,
  onUpdate,
}: {
  properties: BlockProperties
  onUpdate: (properties: BlockProperties) => void
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline" size="sm" className="bg-card">
          <Palette className="size-4" />
          Style
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-60">
        <DropdownMenuLabel>Font size</DropdownMenuLabel>
        <DropdownMenuGroup>
          {sizes.map((size) => (
            <DropdownMenuItem key={size.value} onClick={() => onUpdate({ fontSize: size.value })}>
              {size.label}
              {properties.fontSize === size.value ? <span className="ml-auto text-xs text-muted-foreground">Active</span> : null}
            </DropdownMenuItem>
          ))}
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuLabel>Text color</DropdownMenuLabel>
        {textColors.map((color) => (
          <DropdownMenuItem key={color.value} onClick={() => onUpdate({ textColor: color.value })}>
            <span className={`size-3 rounded-full ${color.className}`} />
            {color.label}
          </DropdownMenuItem>
        ))}
        <DropdownMenuSeparator />
        <DropdownMenuLabel>Highlight</DropdownMenuLabel>
        {highlights.map((highlight) => (
          <DropdownMenuItem key={highlight.value} onClick={() => onUpdate({ highlight: highlight.value })}>
            <span className={`size-3 rounded-sm border border-border ${highlight.className}`} />
            {highlight.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
