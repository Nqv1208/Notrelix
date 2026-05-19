"use client"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type { BlockProperties } from "@/features/docs/types"

const fontOptions: Array<{ value: NonNullable<BlockProperties["fontFamily"]>; label: string }> = [
  { value: "inter", label: "Inter" },
  { value: "poppins", label: "Poppins" },
  { value: "serif", label: "Serif" },
  { value: "mono", label: "Mono" },
]

export function FontFamilyDropdown({
  value,
  onValueChange,
}: {
  value?: BlockProperties["fontFamily"]
  onValueChange: (fontFamily: NonNullable<BlockProperties["fontFamily"]>) => void
}) {
  const current = fontOptions.find((option) => option.value === value) ?? fontOptions[0]
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline" size="sm" className="min-w-[104px] justify-start bg-card">
          {current.label}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start">
        {fontOptions.map((option) => (
          <DropdownMenuItem key={option.value} onClick={() => onValueChange(option.value)}>
            {option.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
