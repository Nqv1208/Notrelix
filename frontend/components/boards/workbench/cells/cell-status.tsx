"use client"

import { Badge } from "@/components/ui/badge"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { useUpdateFieldValue } from "@/features/boards/hooks"
import type { Card, FieldDefinition, FieldOption } from "@/features/boards/types"

export function CellStatus({ card, field, value, options }: { card: Card; field: FieldDefinition; value?: string; options: FieldOption[] }) {
  const updateFieldValue = useUpdateFieldValue(card.boardId)
  const option = options.find((item) => item.id === value)

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button type="button" aria-label={`${field.name}: ${option?.label ?? "Empty"}`}>
          {option ? (
            <Badge
              variant="secondary"
              className="w-fit rounded-full border"
              style={{
                backgroundColor: `${option.color}24`,
                borderColor: `${option.color}55`,
                color: option.color,
              }}
            >
              {option.label}
            </Badge>
          ) : (
            <span className="text-sm text-muted-foreground">Empty</span>
          )}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start">
        {options.map((item) => (
          <DropdownMenuItem
            key={item.id}
            onClick={() => updateFieldValue.mutate({ cardId: card.id, fieldDefinitionId: field.id, value: item.id })}
          >
            <span className="size-2 rounded-full" style={{ backgroundColor: item.color }} />
            {item.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
