"use client"

import { FileText, Plus, Eye, Share2, Sparkles, HelpCircle } from "lucide-react"
import { StreamingText } from "./StreamingText"

interface DocsEditorMockProps {
  docsText: string
}

export function DocsEditorMock({ docsText }: DocsEditorMockProps) {
  return (
    <div className="flex h-full w-full bg-white dark:bg-zinc-950">
      
      {/* Internal Docs Sidebar */}
      <div className="hidden w-40 flex-col border-r border-zinc-150 bg-zinc-50/50 p-2.5 sm:flex dark:border-zinc-800 dark:bg-zinc-900/30">
        <div className="flex items-center justify-between text-[9px] font-bold tracking-wider text-zinc-400 uppercase mb-2">
          <span>Launch Docs</span>
          <Plus className="h-3 w-3 hover:text-zinc-700 cursor-pointer" />
        </div>
        <div className="space-y-1 text-xs">
          <div className="flex items-center gap-1.5 rounded-md bg-blue-50/70 p-1.5 font-bold text-blue-600 dark:bg-blue-950/20 dark:text-blue-400">
            <FileText className="h-3.5 w-3.5 shrink-0" />
            <span className="truncate">Launch Plan</span>
          </div>
          <div className="flex items-center gap-1.5 rounded-md p-1.5 text-zinc-500 hover:bg-zinc-100 dark:hover:bg-zinc-900">
            <FileText className="h-3.5 w-3.5 shrink-0" />
            <span className="truncate">Engineering Spec</span>
          </div>
          <div className="flex items-center gap-1.5 rounded-md p-1.5 text-zinc-500 hover:bg-zinc-100 dark:hover:bg-zinc-900">
            <FileText className="h-3.5 w-3.5 shrink-0" />
            <span className="truncate">Competitor Research</span>
          </div>
        </div>
      </div>

      {/* Editor Content Area */}
      <div className="flex-1 flex flex-col min-w-0">
        
        {/* Local Editor Toolbar */}
        <div className="flex h-10 items-center justify-between border-b border-zinc-150 px-4 dark:border-zinc-800">
          <div className="flex items-center gap-1 text-[10px] text-zinc-400 font-medium">
            <span>Workspaces</span>
            <span>/</span>
            <span className="text-zinc-650 dark:text-zinc-350">Product Launch</span>
            <span>/</span>
            <span className="font-semibold text-zinc-850 dark:text-zinc-200">Launch Plan</span>
          </div>
          <div className="flex items-center gap-2">
            <button className="flex items-center gap-1 rounded border border-zinc-200 bg-white px-2 py-1 text-[10px] font-medium text-zinc-600 shadow-xs hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300 dark:hover:bg-zinc-850">
              <Share2 className="h-3 w-3" />
              <span>Share</span>
            </button>
            <button className="flex items-center gap-1 rounded bg-blue-600 px-2 py-1 text-[10px] font-bold text-white shadow-xs hover:bg-blue-500">
              <span>Publish</span>
            </button>
          </div>
        </div>

        {/* Scrollable Doc Workspace */}
        <div className="flex-1 overflow-y-auto">
          {/* Doc Cover */}
          <div className="h-24 w-full bg-gradient-to-r from-blue-500 via-indigo-500 to-purple-600" />
          
          <div className="relative max-w-xl mx-auto px-6 pb-8 -mt-6">
            {/* Doc Emoji Icon */}
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-white text-2xl shadow-md border border-zinc-150 dark:bg-zinc-900 dark:border-zinc-800 select-none">
              🚀
            </div>

            {/* Doc Title */}
            <h1 className="mt-4 text-xl font-extrabold text-zinc-950 tracking-tight dark:text-white">
              Product Launch Plan
            </h1>

            {/* AI Assistant Badge / Alert */}
            <div className="mt-3.5 flex items-center gap-2 rounded-lg border border-blue-100 bg-blue-50/50 p-2.5 text-[11px] text-blue-800 dark:border-blue-950 dark:bg-blue-950/20 dark:text-blue-300 animate-pulse">
              <Sparkles className="h-3.5 w-3.5 text-blue-500 shrink-0" />
              <span className="font-semibold">AI Copilot: Autowriting document drafts...</span>
            </div>

            {/* Live Streaming Content */}
            <div className="mt-6 prose prose-sm dark:prose-invert">
              <StreamingText text={docsText} className="text-zinc-700 dark:text-zinc-300 font-sans text-xs leading-relaxed" />
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
