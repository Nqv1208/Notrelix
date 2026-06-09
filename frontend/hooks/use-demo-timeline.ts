"use client"

import { useEffect, useState, useRef } from "react"

export type Scene = 1 | 2 | 3 | 4 | 5 | 6 | 7

export interface Task {
  id: string
  title: string
  column: "backlog" | "in-progress" | "review" | "done"
  assignee?: { name: string; avatar: string }
  priority?: "low" | "medium" | "high" | "urgent"
  dueDate?: string
  progress?: number
}

const INITIAL_TASKS: Task[] = [
  { id: "task-1", title: "Setup production environments", column: "done", assignee: { name: "Alex", avatar: "A" }, priority: "urgent", dueDate: "June 12" },
  { id: "task-2", title: "Integrate Google Calendar API", column: "review", assignee: { name: "David", avatar: "D" }, priority: "high", dueDate: "June 15" },
  { id: "task-3", title: "Write API integration tests", column: "review", assignee: { name: "Mai", avatar: "M" }, priority: "medium", dueDate: "June 18" },
]

const GENERATED_TASKS: Task[] = [
  { id: "task-4", title: "Design onboarding flow", column: "backlog" },
  { id: "task-5", title: "Implement Auth Middleware", column: "backlog" },
  { id: "task-6", title: "Write User Guide docs", column: "backlog" },
]

export function useDemoTimeline() {
  const [activeScene, setActiveScene] = useState<Scene>(1)
  const [activeView, setActiveView] = useState<"board" | "docs" | "dashboard">("board")
  const [activeWorkspace, setActiveWorkspace] = useState<"none" | "product-launch">("none")
  const [tasks, setTasks] = useState<Task[]>(INITIAL_TASKS)
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null)
  
  // AI Command Streaming Text
  const [aiCommandText, setAiCommandText] = useState("")
  
  // Docs Editor Streaming Text
  const [docsText, setDocsText] = useState("")
  
  // Cursor properties
  const [cursorPos, setCursorPos] = useState({ x: 50, y: 95 }) // starts off-screen/low
  const [cursorAction, setCursorAction] = useState<"idle" | "pointer" | "clicking" | "dragging">("pointer")
  
  const timelineRef = useRef<NodeJS.Timeout | null>(null)
  const prefersReducedMotionRef = useRef(false)

  useEffect(() => {
    // Detect reduced motion settings
    const mediaQuery = window.matchMedia("(prefers-reduced-motion: reduce)")
    prefersReducedMotionRef.current = mediaQuery.matches

    const handleChange = (e: MediaQueryListEvent) => {
      prefersReducedMotionRef.current = e.matches
    }
    mediaQuery.addEventListener("change", handleChange)
    return () => mediaQuery.removeEventListener("change", handleChange)
  }, [])

  useEffect(() => {
    if (prefersReducedMotionRef.current) {
      // If prefers reduced motion, set final state of tasks and doc and stop looping animations
      setTasks([...INITIAL_TASKS, ...GENERATED_TASKS.map(t => t.id === "task-4" ? { ...t, column: "in-progress" as const, assignee: { name: "Sarah", avatar: "S" }, priority: "high" as const, dueDate: "June 25" } : t)])
      setActiveWorkspace("product-launch")
      setActiveView("board")
      setAiCommandText("Create a launch plan for Mobile App v1")
      setDocsText("### Mobile App Launch Plan\n\n1. Target Audience: SaaS Builders & Teams\n2. Launch Channels: Product Hunt, TechCrunch\n3. Goal: Reach 10k Active Users in Q3.")
      setCursorPos({ x: 50, y: 50 })
      setCursorAction("idle")
      return
    }

    let isCancelled = false
    
    // Timeline runner
    const runTimeline = async () => {
      const sleep = (ms: number) => new Promise((resolve) => {
        if (isCancelled) return
        setTimeout(resolve, ms)
      })

      while (!isCancelled) {
        // SCENE 1: Mở Workspace "Product Launch"
        // Reset states
        setActiveScene(1)
        setActiveView("board")
        setActiveWorkspace("none")
        setTasks(INITIAL_TASKS)
        setIsModalOpen(false)
        setSelectedTaskId(null)
        setAiCommandText("")
        setDocsText("")
        setCursorAction("pointer")
        setCursorPos({ x: 50, y: 95 }) // bottom-center
        
        await sleep(1000)
        if (isCancelled) break
        
        // Move cursor to "Product Launch" workspace item on sidebar
        setCursorPos({ x: 12, y: 24 })
        await sleep(1000)
        if (isCancelled) break
        
        // Click workspace
        setCursorAction("clicking")
        await sleep(200)
        if (isCancelled) break
        setCursorAction("pointer")
        setActiveWorkspace("product-launch")
        await sleep(1200)
        if (isCancelled) break

        // SCENE 2: Streaming Text cho AI Command
        setActiveScene(2)
        // Move cursor to AI command bar
        setCursorPos({ x: 50, y: 14 })
        await sleep(1000)
        if (isCancelled) break
        
        setCursorAction("clicking")
        await sleep(200)
        if (isCancelled) break
        setCursorAction("pointer")
        
        // Stream text "Create a launch plan for Mobile App v1"
        const phrase = "Create a launch plan for Mobile App v1"
        for (let i = 1; i <= phrase.length; i++) {
          setAiCommandText(phrase.slice(0, i))
          await sleep(60)
          if (isCancelled) break
        }
        await sleep(800)
        if (isCancelled) break

        // SCENE 3: Các task tự xuất hiện trong Backlog
        setActiveScene(3)
        // Click the generate button
        setCursorPos({ x: 73, y: 14 })
        await sleep(600)
        if (isCancelled) break
        
        setCursorAction("clicking")
        await sleep(200)
        if (isCancelled) break
        setCursorAction("pointer")
        
        // Add tasks one by one into Backlog
        for (let i = 0; i < GENERATED_TASKS.length; i++) {
          setTasks((prev) => [...prev, GENERATED_TASKS[i]])
          await sleep(600)
          if (isCancelled) break
        }
        await sleep(1200)
        if (isCancelled) break

        // SCENE 4: Cursor kéo task "Design onboarding flow" sang In Progress
        setActiveScene(4)
        // Move cursor to "Design onboarding flow" card (first backlog card, x=26, y=42)
        setCursorPos({ x: 26, y: 42 })
        await sleep(1000)
        if (isCancelled) break
        
        setCursorAction("dragging")
        await sleep(300)
        if (isCancelled) break
        
        // Drag to "In Progress" column (x=45, y=36)
        setCursorPos({ x: 45, y: 36 })
        await sleep(1200)
        if (isCancelled) break
        
        // Drop card into In Progress
        setTasks((prev) =>
          prev.map((t) => (t.id === "task-4" ? { ...t, column: "in-progress" } : t))
        )
        setCursorAction("pointer")
        await sleep(1000)
        if (isCancelled) break

        // SCENE 5: Mở task detail và cập nhật assignee/priority/due date
        setActiveScene(5)
        // Move to the card inside In Progress to double click or click to open modal
        setCursorPos({ x: 45, y: 36 })
        await sleep(600)
        if (isCancelled) break
        
        setCursorAction("clicking")
        await sleep(200)
        if (isCancelled) break
        setCursorAction("pointer")
        setSelectedTaskId("task-4")
        setIsModalOpen(true)
        await sleep(1000)
        if (isCancelled) break

        // Move cursor to "Assignee" select inside modal (x=62, y=42)
        setCursorPos({ x: 62, y: 44 })
        await sleep(800)
        if (isCancelled) break
        
        setCursorAction("clicking")
        await sleep(200)
        if (isCancelled) break
        setCursorAction("pointer")
        // Update assignee
        setTasks((prev) =>
          prev.map((t) =>
            t.id === "task-4"
              ? { ...t, assignee: { name: "Sarah", avatar: "S" } }
              : t
          )
        )
        await sleep(800)
        if (isCancelled) break

        // Move cursor to "Priority" (x=62, y=52)
        setCursorPos({ x: 62, y: 52 })
        await sleep(800)
        if (isCancelled) break
        
        setCursorAction("clicking")
        await sleep(200)
        if (isCancelled) break
        setCursorAction("pointer")
        // Update priority
        setTasks((prev) =>
          prev.map((t) => (t.id === "task-4" ? { ...t, priority: "high" } : t))
        )
        await sleep(800)
        if (isCancelled) break

        // Move cursor to "Due Date" (x=62, y=60)
        setCursorPos({ x: 62, y: 60 })
        await sleep(800)
        if (isCancelled) break
        
        setCursorAction("clicking")
        await sleep(200)
        if (isCancelled) break
        setCursorAction("pointer")
        // Update due date
        setTasks((prev) =>
          prev.map((t) => (t.id === "task-4" ? { ...t, dueDate: "June 25" } : t))
        )
        await sleep(800)
        if (isCancelled) break

        // Move to close button (x=73, y=28)
        setCursorPos({ x: 73, y: 28 })
        await sleep(800)
        if (isCancelled) break
        
        setCursorAction("clicking")
        await sleep(200)
        if (isCancelled) break
        setCursorAction("pointer")
        setIsModalOpen(false)
        setSelectedTaskId(null)
        await sleep(1000)
        if (isCancelled) break

        // SCENE 6: Chuyển sang Docs và streaming nội dung Product Launch Plan
        setActiveScene(6)
        // Move to "Docs" tab in main view toggles (x=24, y=14)
        setCursorPos({ x: 24, y: 14 })
        await sleep(1000)
        if (isCancelled) break
        
        setCursorAction("clicking")
        await sleep(200)
        if (isCancelled) break
        setCursorAction("pointer")
        setActiveView("docs")
        await sleep(1000)
        if (isCancelled) break

        // Move to doc area and stream
        setCursorPos({ x: 50, y: 40 })
        const documentDraft = `### Mobile App Launch Plan\n\n- **Target Audience**: Product Managers & Developers seeking absolute workspace magic.\n- **Launch Target**: Product Hunt (Goal: #1 Product of the Day) & Hacker News.\n- **Key Assets**: Onboarding tutorials, product video demo, interactive landing page.\n- **Metrics**: Track signup rate and 7-day retention of workspace invitees.`
        
        // Typestream
        for (let i = 1; i <= documentDraft.length; i += 2) {
          setDocsText(documentDraft.slice(0, i))
          await sleep(20)
          if (isCancelled) break
        }
        await sleep(2000)
        if (isCancelled) break

        // SCENE 7: Chuyển sang Dashboard hiển thị progress/team status
        setActiveScene(7)
        // Move to "Dashboard" tab (x=33, y=14)
        setCursorPos({ x: 33, y: 14 })
        await sleep(1000)
        if (isCancelled) break
        
        setCursorAction("clicking")
        await sleep(200)
        if (isCancelled) break
        setCursorAction("pointer")
        setActiveView("dashboard")
        
        // Wait at dashboard before looping
        await sleep(4000)
        if (isCancelled) break
      }
    }

    runTimeline()

    return () => {
      isCancelled = true
    }
  }, [])

  return {
    activeScene,
    activeView,
    activeWorkspace,
    tasks,
    isModalOpen,
    selectedTask: tasks.find((t) => t.id === selectedTaskId) || null,
    aiCommandText,
    docsText,
    cursorPos,
    cursorAction,
  }
}
