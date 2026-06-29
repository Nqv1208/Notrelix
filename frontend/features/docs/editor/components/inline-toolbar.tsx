"use client"

import { useState, useEffect, useRef } from "react"
import {
  Bold, Italic, Underline, Strikethrough, Code,
  Link, Type, ChevronDown,
} from "lucide-react"

interface InlineToolbarProps {
  onBold?: () => void
  onItalic?: () => void
  onCode?: () => void
  onLink?: () => void
}

export function InlineToolbar({ onBold, onItalic, onCode, onLink }: InlineToolbarProps) {
  const [visible, setVisible] = useState(false)
  const [position, setPosition] = useState({ top: 0, left: 0 })
  const toolbarRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleSelectionChange() {
      const selection = window.getSelection()
      if (!selection || selection.isCollapsed || !selection.toString().trim()) {
        setVisible(false)
        return
      }

      const range = selection.getRangeAt(0)
      const rect = range.getBoundingClientRect()

      setPosition({
        top: rect.top + window.scrollY - 48,
        left: rect.left + rect.width / 2,
      })
      setVisible(true)
    }

    document.addEventListener("mouseup", handleSelectionChange)
    document.addEventListener("keyup", handleSelectionChange)
    return () => {
      document.removeEventListener("mouseup", handleSelectionChange)
      document.removeEventListener("keyup", handleSelectionChange)
    }
  }, [])

  if (!visible) return null

  const actions = [
    { icon: Bold, label: "Bold", onClick: onBold },
    { icon: Italic, label: "Italic", onClick: onItalic },
    { icon: Underline, label: "Underline", onClick: undefined },
    { icon: Strikethrough, label: "Strikethrough", onClick: undefined },
    { icon: Code, label: "Code", onClick: onCode },
  ]

  return (
    <div
      ref={toolbarRef}
      className="fixed z-[9999] flex items-center gap-0.5 px-1 py-0.5"
      style={{
        top: position.top,
        left: position.left,
        transform: "translateX(-50%)",
        background: "var(--color-deep-space)",
        borderRadius: "var(--radius-button)",
        boxShadow: "var(--shadow-xl)",
        opacity: visible ? 1 : 0,
        transition: "opacity 150ms ease",
      }}
      role="toolbar"
      aria-label="Text formatting"
    >
      {/* Block type selector */}
      <button
        className="flex items-center gap-1 px-2 py-1 rounded text-[12px] font-medium transition-colors"
        style={{
          color: "var(--color-paper)",
          fontFamily: "var(--font-display)",
        }}
        onMouseEnter={e => (e.currentTarget.style.background = "rgba(255,255,255,0.1)")}
        onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
      >
        <Type size={12} />
        <ChevronDown size={10} />
      </button>

      <div className="w-px h-4 mx-0.5" style={{ background: "rgba(255,255,255,0.2)" }} />

      {/* Format actions */}
      {actions.map(({ icon: Icon, label, onClick }) => (
        <button
          key={label}
          onClick={onClick}
          className="p-1.5 rounded transition-colors"
          style={{ color: "var(--color-paper)" }}
          onMouseEnter={e => (e.currentTarget.style.background = "rgba(255,255,255,0.1)")}
          onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
          title={label}
          aria-label={label}
        >
          <Icon size={14} />
        </button>
      ))}

      <div className="w-px h-4 mx-0.5" style={{ background: "rgba(255,255,255,0.2)" }} />

      {/* Link button */}
      <button
        onClick={onLink}
        className="flex items-center gap-1 px-2 py-1 rounded text-[12px] font-medium transition-colors"
        style={{
          color: "var(--color-paper)",
          fontFamily: "var(--font-display)",
        }}
        onMouseEnter={e => (e.currentTarget.style.background = "rgba(255,255,255,0.1)")}
        onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
      >
        <Link size={12} />
        Link
      </button>
    </div>
  )
}
