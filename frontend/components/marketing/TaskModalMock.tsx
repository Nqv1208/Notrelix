"use client"

import { X, Calendar, User, Tag, LayoutPanelLeft, ListTodo, AlertTriangle, ArrowRight } from "lucide-react"
import { Task } from "@/hooks/use-demo-timeline"
import { cn } from "@/lib/utils"

interface TaskModalMockProps {
  task: Task | null
  isOpen: boolean
  onClose?: () => void
}

export function TaskModalMock({ task, isOpen, onClose }: TaskModalMockProps) {
  if (!isOpen || !task) return null

  return (
    <div className="absolute inset-0 z-30 flex items-center justify-center bg-zinc-950/45 p-6 backdrop-blur-xs">
      <div className="relative w-full max-w-lg rounded-xl border border-zinc-200 bg-white shadow-2xl dark:border-zinc-800 dark:bg-zinc-900 animate-in fade-in-50 zoom-in-95 duration-200">
        
        {/* Modal Header */}
        <div className="flex items-center justify-between border-b border-zinc-200/80 px-4.5 py-3.5 dark:border-zinc-800">
          <div className="flex items-center gap-2 text-zinc-500 dark:text-zinc-400">
            <LayoutPanelLeft className="h-4 w-4" />
            <span className="text-[10px] font-semibold tracking-wide uppercase">In Progress / Task Details</span>
          </div>
          <button
            onClick={onClose}
            className="rounded-lg p-1 text-zinc-400 hover:bg-zinc-100 hover:text-zinc-700 dark:hover:bg-zinc-800 dark:hover:text-zinc-200"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* Modal Body */}
        <div className="grid grid-cols-5 gap-5 p-5 text-xs">
          {/* Main Info Left Column */}
          <div className="col-span-3 space-y-4">
            <div>
              <h3 className="text-sm font-bold text-zinc-950 dark:text-white leading-snug">
                {task.title}
              </h3>
              <p className="mt-2 text-zinc-500 dark:text-zinc-400 leading-relaxed text-[11px]">
                Create a clean, frictionless onboarding screens and layout flow for the new React Native mobile client. Optimize for maximum sign-up rate.
              </p>
            </div>

            {/* Checklist */}
            <div className="space-y-2">
              <div className="flex items-center gap-1.5 font-bold text-zinc-900 dark:text-white">
                <ListTodo className="h-3.5 w-3.5 text-blue-500" />
                <span>Onboarding Subtasks</span>
              </div>
              <div className="space-y-1.5 pl-5">
                <div className="flex items-center gap-2 text-zinc-550 dark:text-zinc-400">
                  <input type="checkbox" checked readOnly className="rounded border-zinc-350 accent-blue-500" />
                  <span className="line-through text-zinc-400">Research onboarding screen templates</span>
                </div>
                <div className="flex items-center gap-2 text-zinc-550 dark:text-zinc-400">
                  <input type="checkbox" checked={!!task.assignee} readOnly className="rounded border-zinc-350 accent-blue-500" />
                  <span className={task.assignee ? "line-through text-zinc-400" : ""}>Design mockup screens in Figma</span>
                </div>
                <div className="flex items-center gap-2 text-zinc-550 dark:text-zinc-400">
                  <input type="checkbox" checked={task.priority === "high"} readOnly className="rounded border-zinc-350 accent-blue-500" />
                  <span className={task.priority === "high" ? "line-through text-zinc-400" : ""}>Review UX with Product Managers</span>
                </div>
              </div>
            </div>
          </div>

          {/* Properties Right Column */}
          <div className="col-span-2 space-y-3.5 border-l border-zinc-150 pl-5 dark:border-zinc-800">
            {/* Assignee Row */}
            <div className="space-y-1">
              <span className="text-[10px] font-bold text-zinc-450 uppercase dark:text-zinc-500">Assignee</span>
              <div className="flex items-center gap-2 rounded-lg border border-zinc-200 bg-zinc-50/50 p-1.5 dark:border-zinc-850 dark:bg-zinc-950/40">
                {task.assignee ? (
                  <>
                    <div className="flex h-5 w-5 items-center justify-center rounded-full bg-blue-100 font-bold text-blue-700 text-[10px] dark:bg-blue-900/60 dark:text-blue-200">
                      {task.assignee.avatar}
                    </div>
                    <span className="font-semibold text-zinc-800 dark:text-zinc-250 truncate">{task.assignee.name}</span>
                  </>
                ) : (
                  <>
                    <User className="h-4 w-4 text-zinc-405" />
                    <span className="text-zinc-400 italic">Unassigned</span>
                  </>
                )}
              </div>
            </div>

            {/* Priority Row */}
            <div className="space-y-1">
              <span className="text-[10px] font-bold text-zinc-450 uppercase dark:text-zinc-500">Priority</span>
              <div className="flex items-center gap-2 rounded-lg border border-zinc-200 bg-zinc-50/50 p-1.5 dark:border-zinc-850 dark:bg-zinc-950/40">
                {task.priority ? (
                  <>
                    <AlertTriangle className={cn(
                      "h-3.5 w-3.5",
                      task.priority === "high" ? "text-amber-500 animate-pulse" : "text-blue-500"
                    )} />
                    <span className="font-semibold text-zinc-800 dark:text-zinc-200 capitalize">{task.priority}</span>
                  </>
                ) : (
                  <span className="text-zinc-400 italic">None</span>
                )}
              </div>
            </div>

            {/* Due Date Row */}
            <div className="space-y-1">
              <span className="text-[10px] font-bold text-zinc-450 uppercase dark:text-zinc-500">Due Date</span>
              <div className="flex items-center gap-2 rounded-lg border border-zinc-200 bg-zinc-50/50 p-1.5 dark:border-zinc-850 dark:bg-zinc-950/40">
                <Calendar className="h-3.5 w-3.5 text-zinc-400" />
                {task.dueDate ? (
                  <span className="font-semibold text-zinc-850 dark:text-zinc-200">{task.dueDate}</span>
                ) : (
                  <span className="text-zinc-400 italic">No deadline</span>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
