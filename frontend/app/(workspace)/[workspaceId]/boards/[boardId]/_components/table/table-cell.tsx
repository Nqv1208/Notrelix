"use client"

import { useState } from "react"
import { CalendarDays, FileText, MessageSquareText, Paperclip, Pencil, UserPlus } from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { Progress } from "@/components/ui/progress"
import { useBoardDocsPanel, useUpdateCard, useUpdateFieldValue } from "@/features/boards/hooks"
import type { Board, BoardMember, Card, FieldDefinition, FieldOption } from "@/features/boards/types"
import { cn } from "@/lib/utils"
import { formatDate, getChecklistProgress, getOptionToneClass, toDateInputValue } from "./table-utils"

export function TableCell({
  board,
  card,
  field,
  onOpenDetail,
}: {
  board: Board
  card: Card
  field: FieldDefinition
  onOpenDetail: () => void
}) {
  const semantic = getFieldSemantic(field)
  if (semantic === "title") return <TitleCell card={card} field={field} onOpenDetail={onOpenDetail} />
  if (field.fieldType === "person") return <OwnerCell board={board} card={card} field={field} />
  if (field.fieldType === "date") return <DateCell card={card} field={field} />
  if (field.fieldType === "timeline") return <TimelineCell card={card} field={field} />
  if (field.fieldType === "checkbox") return <CheckboxCell card={card} field={field} />
  if (field.fieldType === "linked_page") return <LinkedDocCell pageId={card.linkedPageId} />
  if (field.fieldType === "progress") return <ProgressCell card={card} />

  if (field.fieldType === "select") {
    const value = String(card.fieldValues[field.id] ?? (semantic === "priority" ? card.priority : semantic === "status" ? card.status : ""))
    return <SelectCell card={card} field={field} value={value} options={field.options} />
  }

  return <TextFieldCell card={card} field={field} />
}

function TitleCell({
  card,
  field,
  onOpenDetail,
}: {
  card: Card
  field: FieldDefinition
  onOpenDetail: () => void
}) {
  const [editing, setEditing] = useState(false)
  const [value, setValue] = useState(card.title)
  const updateCard = useUpdateCard(card.boardId, card.workspaceId)

  function commit() {
    const next = value.trim()
    setEditing(false)
    if (!next || next === card.title) {
      setValue(card.title)
      return
    }
    updateCard.mutate({ cardId: card.id, patch: { title: next } })
  }

  if (editing) {
    return (
      <Input
        autoFocus
        className="h-8 border-0 bg-muted px-2 shadow-none focus-visible:ring-1"
        value={value}
        onClick={(event) => event.stopPropagation()}
        onChange={(event) => setValue(event.target.value)}
        onBlur={commit}
        onKeyDown={(event) => {
          if (event.key === "Enter") commit()
          if (event.key === "Escape") {
            setValue(card.title)
            setEditing(false)
          }
        }}
        aria-label={`Edit ${field.name}`}
      />
    )
  }

  return (
    <div className="min-w-0">
      <div className="flex min-w-0 items-center gap-2">
        <button type="button" className="min-w-0 text-left" onClick={onOpenDetail}>
          <p className="truncate font-medium text-foreground">{card.title}</p>
        </button>
        <button
          type="button"
          className="rounded p-1 text-muted-foreground opacity-0 transition hover:bg-muted hover:text-foreground group-hover/row:opacity-100"
          aria-label={`Edit ${card.title}`}
          onClick={(event) => {
            event.stopPropagation()
            setEditing(true)
          }}
        >
          <Pencil className="size-3.5" />
        </button>
      </div>
      <div className="mt-0.5 flex items-center gap-2 text-xs text-muted-foreground">
        {card._count.comments > 0 ? (
          <span className="inline-flex items-center gap-1">
            <MessageSquareText className="size-3" />
            {card._count.comments}
          </span>
        ) : null}
        {card._count.attachments > 0 ? (
          <span className="inline-flex items-center gap-1">
            <Paperclip className="size-3" />
            {card._count.attachments}
          </span>
        ) : null}
      </div>
    </div>
  )
}

function OwnerCell({ board, card, field }: { board: Board; card: Card; field: FieldDefinition }) {
  const updateFieldValue = useUpdateFieldValue(card.boardId, card.workspaceId)
  const selectedUserIds = new Set(card.members.map((member) => member.userId))

  function toggleMember(member: BoardMember) {
    const next = new Set(selectedUserIds)
    if (next.has(member.userId)) next.delete(member.userId)
    else next.add(member.userId)
    updateFieldValue.mutate({ cardId: card.id, fieldDefinitionId: field.id, value: Array.from(next) })
  }

  return (
    <Popover>
      <PopoverTrigger asChild>
        <button type="button" className="flex max-w-full items-center gap-2 text-left" onClick={(event) => event.stopPropagation()}>
          {card.members.length > 0 ? (
            <>
              <div className="-space-x-2">
                {card.members.slice(0, 3).map((member) => (
                  <Avatar key={member.id} className="inline-flex size-7 border-2 border-card">
                    <AvatarFallback className="text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: member.color }}>
                      {member.initials}
                    </AvatarFallback>
                  </Avatar>
                ))}
              </div>
              <span className="truncate text-sm text-muted-foreground">{card.members[0]?.name}</span>
            </>
          ) : (
            <span className="inline-flex items-center gap-2 text-sm text-muted-foreground">
              <UserPlus className="size-4" />
              Unassigned
            </span>
          )}
        </button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-64 p-2" onClick={(event) => event.stopPropagation()}>
        <div className="px-2 py-1 text-xs font-semibold uppercase text-muted-foreground">Owners</div>
        <div className="space-y-1">
          {board.members.map((member) => (
            <button
              type="button"
              key={member.id}
              className="flex w-full items-center gap-2 rounded-md px-2 py-2 text-left text-sm transition hover:bg-accent"
              onClick={() => toggleMember(member)}
            >
              <Avatar className="size-7">
                <AvatarFallback className="text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: member.color }}>
                  {member.initials}
                </AvatarFallback>
              </Avatar>
              <span className="min-w-0 flex-1 truncate">{member.name}</span>
              {selectedUserIds.has(member.userId) ? <Badge variant="secondary" className="rounded-full">On</Badge> : null}
            </button>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  )
}

function SelectCell({
  card,
  field,
  value,
  options,
}: {
  card: Card
  field: FieldDefinition
  value?: string
  options: FieldOption[]
}) {
  const updateFieldValue = useUpdateFieldValue(card.boardId, card.workspaceId)
  const option = options.find((item) => item.id === value)

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button type="button" className="max-w-full text-left" onClick={(event) => event.stopPropagation()} aria-label={`${field.name}: ${option?.label ?? "Empty"}`}>
          {option ? (
            <Badge
              variant="secondary"
              className={cn("max-w-full rounded-full border-0 px-2.5 py-0.5 text-xs font-medium", getOptionToneClass(option.id))}
            >
              <span className="truncate">{option.label}</span>
            </Badge>
          ) : (
            <span className="text-sm text-muted-foreground/50">—</span>
          )}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" onClick={(event) => event.stopPropagation()}>
        {options.map((item) => (
          <DropdownMenuItem key={item.id} onClick={() => updateFieldValue.mutate({ cardId: card.id, fieldDefinitionId: field.id, value: item.id })}>
            <span className="size-2 rounded-full" style={{ backgroundColor: item.color }} />
            {item.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

function DateCell({ card, field }: { card: Card; field: FieldDefinition }) {
  const updateFieldValue = useUpdateFieldValue(card.boardId, card.workspaceId)
  const rawValue = typeof card.fieldValues[field.id] === "string" ? String(card.fieldValues[field.id]) : card.dueDate
  const value = toDateInputValue(rawValue)

  return (
    <Popover>
      <PopoverTrigger asChild>
        <button type="button" className="inline-flex items-center gap-2 text-sm text-muted-foreground" onClick={(event) => event.stopPropagation()}>
          <CalendarDays className="size-4" />
          <span>{formatDate(rawValue)}</span>
        </button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-56" onClick={(event) => event.stopPropagation()}>
        <Input
          type="date"
          aria-label={`Edit ${field.name}`}
          value={value}
          onChange={(event) => {
            const next = event.target.value
            updateFieldValue.mutate({ cardId: card.id, fieldDefinitionId: field.id, value: next ? new Date(`${next}T09:00:00.000Z`).toISOString() : null })
          }}
        />
      </PopoverContent>
    </Popover>
  )
}

function TimelineCell({ card, field }: { card: Card; field: FieldDefinition }) {
  const updateFieldValue = useUpdateFieldValue(card.boardId, card.workspaceId)
  const value = (card.fieldValues[field.id] as { start?: string; end?: string } | undefined) ?? {}

  return (
    <div className="flex min-w-0 items-center gap-1" onClick={(event) => event.stopPropagation()}>
      <Input
        type="date"
        value={toDateInputValue(value.start)}
        onChange={(event) => updateFieldValue.mutate({ cardId: card.id, fieldDefinitionId: field.id, value: { ...value, start: event.target.value } })}
        className="h-8 w-[120px] bg-card"
        aria-label={`Edit ${field.name} start`}
      />
      <Input
        type="date"
        value={toDateInputValue(value.end)}
        onChange={(event) => updateFieldValue.mutate({ cardId: card.id, fieldDefinitionId: field.id, value: { ...value, end: event.target.value } })}
        className="h-8 w-[120px] bg-card"
        aria-label={`Edit ${field.name} end`}
      />
    </div>
  )
}

function CheckboxCell({ card, field }: { card: Card; field: FieldDefinition }) {
  const updateFieldValue = useUpdateFieldValue(card.boardId, card.workspaceId)
  return (
    <Checkbox
      checked={Boolean(card.fieldValues[field.id])}
      onClick={(event) => event.stopPropagation()}
      onCheckedChange={(checked) => updateFieldValue.mutate({ cardId: card.id, fieldDefinitionId: field.id, value: Boolean(checked) })}
      aria-label={`Toggle ${field.name}`}
    />
  )
}

function TextFieldCell({ card, field }: { card: Card; field: FieldDefinition }) {
  const [editing, setEditing] = useState(false)
  const [value, setValue] = useState(String(card.fieldValues[field.id] ?? ""))
  const updateFieldValue = useUpdateFieldValue(card.boardId, card.workspaceId)

  function commit() {
    setEditing(false)
    const next = value.trim()
    if (next === String(card.fieldValues[field.id] ?? "")) return
    updateFieldValue.mutate({ cardId: card.id, fieldDefinitionId: field.id, value: next })
  }

  if (editing) {
    return (
      <Input
        autoFocus
        value={value}
        onChange={(event) => setValue(event.target.value)}
        onBlur={commit}
        onClick={(event) => event.stopPropagation()}
        onKeyDown={(event) => {
          if (event.key === "Enter") commit()
          if (event.key === "Escape") {
            setValue(String(card.fieldValues[field.id] ?? ""))
            setEditing(false)
          }
        }}
        className="h-8 border-0 bg-muted px-2 shadow-none focus-visible:ring-1"
        aria-label={`Edit ${field.name}`}
      />
    )
  }

  return (
    <button type="button" className="min-w-0 truncate text-left text-sm text-foreground/80 hover:text-foreground transition-colors" onClick={(event) => {
      event.stopPropagation()
      setEditing(true)
    }}>
      {value || <span className="text-muted-foreground/50">—</span>}
    </button>
  )
}

function LinkedDocCell({ pageId }: { pageId?: string }) {
  const { openDoc } = useBoardDocsPanel()
  if (!pageId) return <span className="text-sm text-muted-foreground">No doc</span>

  return (
    <Button
      variant="ghost"
      size="sm"
      className="h-8 max-w-full justify-start px-2 text-muted-foreground"
      onClick={(event) => {
        event.stopPropagation()
        openDoc(pageId)
      }}
    >
      <FileText className="size-4 text-primary" />
      <span className="truncate">{pageId}</span>
    </Button>
  )
}

function ProgressCell({ card }: { card: Card }) {
  const value = getChecklistProgress(card)
  return (
    <div className="flex min-w-0 items-center gap-2">
      <Progress value={value} className="h-2" />
      <span className="w-9 text-xs text-muted-foreground">{value}%</span>
    </div>
  )
}

function getFieldSemantic(field: FieldDefinition) {
  const name = field.name.trim().toLowerCase()
  if (name === "task" || name === "title" || name === "name" || field.id.endsWith("field-title")) return "title"
  if (name.includes("priority") || field.id.endsWith("field-priority")) return "priority"
  if (name.includes("status") || field.id.endsWith("field-status")) return "status"
  return field.fieldType
}
