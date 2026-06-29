"use client"

import { useEffect, useState } from "react"
import type { EditorSelectionState } from "../types/editor.types"
import type { ID } from "../../shared/types/ids.types"

const initialSelection: EditorSelectionState = {
  activeBlockId: null,
  hasSelection: false,
  selectionText: "",
  selectionRect: null,
}

export function useEditorSelection(activeBlockId: ID | null) {
  const [selection, setSelection] = useState<EditorSelectionState>(initialSelection)

  useEffect(() => {
    function handleSelectionChange() {
      const browserSelection = window.getSelection()
      const selectionText = browserSelection?.toString().trim() ?? ""
      if (!browserSelection || selectionText.length === 0 || browserSelection.rangeCount === 0) {
        setSelection({ ...initialSelection, activeBlockId })
        return
      }
      const range = browserSelection.getRangeAt(0)
      setSelection({
        activeBlockId,
        hasSelection: true,
        selectionText,
        selectionRect: range.getBoundingClientRect(),
      })
    }

    document.addEventListener("selectionchange", handleSelectionChange)
    return () => document.removeEventListener("selectionchange", handleSelectionChange)
  }, [activeBlockId])

  return selection
}
