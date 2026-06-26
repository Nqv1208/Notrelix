"use client"

import { useMemo } from "react"
import type { BoardTableColumn, FieldDefinition, FieldType, ViewConfig } from "@/features/work-management/types"

const defaultColumnWidths: Partial<Record<FieldType, number>> = {
  text: 360,
  person: 180,
  select: 160,
  date: 150,
  linked_page: 190,
  progress: 170,
}

const minimumColumnWidths: Partial<Record<FieldType, number>> = {
  text: 260,
  person: 150,
  select: 132,
  date: 136,
  linked_page: 150,
  progress: 140,
}

export function useBoardColumns(fieldDefinitions: FieldDefinition[], viewConfig: ViewConfig) {
  return useMemo<BoardTableColumn[]>(() => {
    const visibleFields = fieldDefinitions
      .filter((field) => !field.isHidden && !viewConfig.hiddenFields.includes(field.id))
      .sort((a, b) => a.position - b.position)

    const orderedFields =
      viewConfig.columnOrder.length === 0
        ? visibleFields
        : [...visibleFields].sort((a, b) => {
            const aIndex = viewConfig.columnOrder.indexOf(a.id)
            const bIndex = viewConfig.columnOrder.indexOf(b.id)
            return (aIndex === -1 ? Number.MAX_SAFE_INTEGER : aIndex) - (bIndex === -1 ? Number.MAX_SAFE_INTEGER : bIndex)
          })

    return orderedFields.map((field) => {
      const minWidth = minimumColumnWidths[field.fieldType] ?? 132
      const defaultWidth = defaultColumnWidths[field.fieldType] ?? 150
      return {
        id: field.id,
        field,
        minWidth,
        width: Math.max(viewConfig.columnWidths[field.id] ?? defaultWidth, minWidth),
        isVisible: true,
      }
    })
  }, [fieldDefinitions, viewConfig.columnOrder, viewConfig.columnWidths, viewConfig.hiddenFields])
}

