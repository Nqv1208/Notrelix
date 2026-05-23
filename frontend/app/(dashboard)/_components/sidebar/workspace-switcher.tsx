"use client"

import type { ComponentType, CSSProperties } from "react"
import { 
  ChevronDown, 
  MoreHorizontal, 
  Search, 
  Settings, 
  Edit2, 
  ArrowUpDown, 
  Star, 
  Trash2, 
  Plus, 
  LayoutGrid, 
  Archive,
  Hexagon,
  FolderOpen,
  LayoutTemplate,
  FileText,
  BarChart3,
  Sparkles,
  Heart,
  FormInput,
  GitBranch,
  Folder,
  Puzzle,
  Download,
  BriefcaseBusiness
} from "lucide-react"

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  DropdownMenuLabel,
} from "@/components/ui/dropdown-menu"
import {
  SidebarGroup,
  SidebarGroupLabel,
  SidebarGroupContent,
} from "@/components/ui/sidebar"

type MenuItemProps = {
  icon?: ComponentType<{ size?: number; className?: string; style?: CSSProperties }>
  label: string
  disabled?: boolean
  rightIcon?: ComponentType<{ size?: number; className?: string; style?: CSSProperties }>
  onClick?: () => void
  className?: string
}

function MenuItem({ icon: Icon, label, disabled, rightIcon: RightIcon, onClick, className }: MenuItemProps) {
  return (
    <DropdownMenuItem
      className={`cursor-pointer gap-3 px-2 py-1.5 focus:bg-[var(--color-fog)] rounded-[6px] ${disabled ? "opacity-50 cursor-default" : ""} ${className || ""}`}
      onClick={disabled ? undefined : onClick}
      disabled={disabled}
    >
      {Icon && <Icon size={16} style={{ color: "var(--color-slate)" }} />}
      <span className="flex-1 text-[13px]" style={{ color: "var(--color-graphite)" }}>
        {label}
      </span>
      {RightIcon && <RightIcon size={14} style={{ color: "var(--color-slate)" }} />}
    </DropdownMenuItem>
  )
}

export function WorkspaceSwitcher() {
  const activeWorkspace = {
    id: "ws-1",
    name: "Notrelix OS",
    iconColor: "var(--color-surface-sun)",
    letter: "N"
  }

  return (
    <SidebarGroup className="mt-2">
      <SidebarGroupLabel className="flex items-center justify-between px-2 py-1 text-[12px] font-semibold uppercase tracking-[0.06em] text-muted-foreground group-data-[collapsible=icon]:hidden">
        Workspaces
        <div className="flex items-center gap-1">
          <button className="p-1 hover:bg-[var(--color-fog)] rounded transition-colors">
            <MoreHorizontal size={16} style={{ color: "var(--color-slate)" }} />
          </button>
          <button className="p-1 hover:bg-[var(--color-fog)] rounded transition-colors">
            <Search size={16} style={{ color: "var(--color-slate)" }} />
          </button>
        </div>
      </SidebarGroupLabel>
      
      <SidebarGroupContent className="px-2">
        <div className="flex items-center gap-2 group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:mx-auto">
          {/* Workspace Dropdown */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button 
                className="flex-1 flex items-center justify-between border rounded-[6px] p-1.5 px-2 hover:bg-[var(--color-fog)] transition-colors focus:outline-none group-data-[collapsible=icon]:p-0 group-data-[collapsible=icon]:border-none group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:bg-transparent"
                style={{ borderColor: "var(--color-silver)", background: "var(--color-paper)" }}
              >
                <div className="flex items-center gap-2 overflow-hidden">
                  <div 
                    className="relative flex items-center justify-center w-6 h-6 rounded-[4px] text-primary-foreground font-bold text-[12px] group-data-[collapsible=icon]:w-8 group-data-[collapsible=icon]:h-8 group-data-[collapsible=icon]:text-[14px]"
                    style={{ background: activeWorkspace.iconColor }}
                  >
                    {activeWorkspace.letter}
                    <div className="absolute -bottom-1 -right-1 bg-card rounded-sm p-px group-data-[collapsible=icon]:hidden">
                      <div className="bg-[var(--color-graphite)] rounded-[2px] w-2.5 h-2.5 flex items-center justify-center">
                        <svg width="6" height="6" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" className="text-primary-foreground"><path d="m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path></svg>
                      </div>
                    </div>
                  </div>
                  <span className="truncate text-[14px] font-semibold group-data-[collapsible=icon]:hidden" style={{ color: "var(--color-graphite)", fontFamily: "var(--font-display)" }}>
                    {activeWorkspace.name}
                  </span>
                </div>
                <ChevronDown size={16} className="group-data-[collapsible=icon]:hidden" style={{ color: "var(--color-slate)" }} />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent 
              className="w-[280px] p-0 rounded-xl shadow-lg border-[var(--color-silver)] py-2"
              align="start"
              style={{ fontFamily: "var(--font-body)" }}
            >
              <div className="px-2 pb-1">
                <MenuItem icon={Settings} label="Manage workspace" className="bg-[var(--color-fog)]" />
                <MenuItem icon={Edit2} label="Edit workspace" rightIcon={ChevronDown} />
                <MenuItem icon={ArrowUpDown} label="Sort workspace" rightIcon={ChevronDown} />
                <MenuItem icon={Star} label="Save as template" disabled />
                <MenuItem icon={Trash2} label="Delete workspace" disabled />
              </div>
              <DropdownMenuSeparator className="bg-[var(--color-silver)] my-1" />
              <div className="px-2">
                <MenuItem icon={Plus} label="Add new workspace" />
                <MenuItem icon={LayoutGrid} label="Browse all workspaces" />
                <MenuItem icon={Archive} label="View archive/trash" rightIcon={ChevronDown} />
              </div>
              <DropdownMenuSeparator className="bg-[var(--color-silver)] my-1" />
              <div className="px-2 pt-1">
                <DropdownMenuItem className="cursor-pointer gap-3 px-2 py-2 focus:bg-[var(--color-fog)] rounded-[6px]">
                  <Hexagon size={18} className="fill-[var(--color-brand-violet)] text-[var(--color-brand-violet)]" />
                  <span className="flex-1 text-[14px]" style={{ color: "var(--color-graphite)" }}>
                    work management overview
                  </span>
                </DropdownMenuItem>
              </div>
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Add New Dropdown */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button 
                className="flex items-center justify-center w-8 h-8 rounded-[6px] transition-colors focus:outline-none group-data-[collapsible=icon]:hidden"
                style={{ background: "var(--color-surface-sky)", color: "var(--color-paper)" }}
              >
                <Plus size={20} />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent 
              className="w-[260px] p-0 rounded-xl shadow-lg border-[var(--color-silver)] py-2"
              align="start"
              side="right"
              sideOffset={8}
              style={{ fontFamily: "var(--font-body)" }}
            >
              <DropdownMenuLabel className="text-[11px] font-normal px-4 py-1" style={{ color: "var(--color-slate)" }}>
                Add new
              </DropdownMenuLabel>
              <div className="px-2">
                <MenuItem icon={FolderOpen} label="Project" />
                <MenuItem icon={BriefcaseBusiness} label="Portfolio" />
              </div>
              <DropdownMenuSeparator className="bg-[var(--color-silver)] my-1" />
              <div className="px-2">
                <MenuItem icon={LayoutTemplate} label="Board" rightIcon={ChevronDown} />
                <MenuItem icon={FileText} label="Doc" rightIcon={ChevronDown} />
                <MenuItem icon={BarChart3} label="Dashboard" />
                <DropdownMenuItem className="cursor-pointer gap-3 px-2 py-1.5 focus:bg-[var(--color-fog)] rounded-[6px]">
                  <Sparkles size={16} className="text-[var(--color-surface-sky)]" />
                  <span className="flex-1 text-[13px]" style={{ color: "var(--color-graphite)" }}>Magic AI solution</span>
                </DropdownMenuItem>
                <DropdownMenuItem className="cursor-pointer gap-3 px-2 py-1.5 focus:bg-[var(--color-fog)] rounded-[6px]">
                  <Heart size={16} className="text-[var(--color-surface-fuchsia)]" />
                  <span className="flex-1 text-[13px]" style={{ color: "var(--color-graphite)" }}>Vibe app</span>
                </DropdownMenuItem>
                <MenuItem icon={FormInput} label="Form" rightIcon={ChevronDown} />
                <MenuItem icon={GitBranch} label="Workflow" />
                <MenuItem icon={Folder} label="Folder" />
              </div>
              <DropdownMenuSeparator className="bg-[var(--color-silver)] my-1" />
              <div className="px-2">
                <MenuItem icon={Puzzle} label="Installed apps" rightIcon={ChevronDown} />
                <MenuItem icon={Download} label="Import data" rightIcon={ChevronDown} />
                <MenuItem icon={Star} label="Template center" />
              </div>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </SidebarGroupContent>
    </SidebarGroup>
  )
}
