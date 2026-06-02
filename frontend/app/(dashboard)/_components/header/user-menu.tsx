"use client"

import type { ComponentType, CSSProperties, ReactNode } from "react"
import { useState, useEffect } from "react"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { useAuthUser } from "@/features/auth/hooks/useAuthUser"
import { useLogout } from "@/features/auth/hooks/useLogout"
import { 
  User, 
  Download, 
  Code2, 
  Rocket, 
  Trash2, 
  Archive, 
  Sparkles, 
  Settings, 
  Users, 
  LogOut,
  Puzzle,
  Smartphone,
  FlaskConical,
  Command,
  UserPlus,
  HelpCircle,
  SunMoon,
  Gem,
  BellRing,
  ChevronRight,
  Hexagon
} from "lucide-react"

type UserMenuItemProps = {
  icon: ComponentType<{ size?: number; className?: string; style?: CSSProperties }>
  label: string
  badge?: string
  rightElement?: ReactNode
  onClick?: () => void
  danger?: boolean
}

function UserMenuItem({ icon: Icon, label, badge, rightElement, onClick, danger }: UserMenuItemProps) {
  return (
    <DropdownMenuItem 
      className="cursor-pointer gap-3 rounded-md px-2 py-1.5 text-popover-foreground focus:bg-muted"
      onClick={onClick}
    >
      <Icon size={16} className={danger ? "text-destructive" : "text-muted-foreground"} />
      <span className={danger ? "flex-1 text-[13px] text-destructive" : "flex-1 text-[13px] text-foreground"}>
        {label}
      </span>
      {badge && (
        <span className="rounded border border-primary px-1.5 py-0.5 text-[10px] font-medium text-primary">
          {badge}
        </span>
      )}
      {rightElement}
    </DropdownMenuItem>
  )
}

export function UserMenu() {
  const { user } = useAuthUser()
  const { mutate: logout } = useLogout()
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  const displayUser = user ?? {
    name: "Notrelix User",
    email: "workspace@notrelix.app",
  }

  const initials = displayUser.name
    ? displayUser.name.split(" ").map(n => n[0]).join("").toUpperCase().slice(0, 2)
    : "U"

  if (!mounted) {
    return (
      <button
        className="relative ml-2 h-8 w-8 rounded-full focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        aria-label="User settings"
        type="button"
      >
        <Avatar className="h-8 w-8">
          <AvatarFallback className="bg-primary text-xs font-semibold text-primary-foreground">
            {initials}
          </AvatarFallback>
        </Avatar>
      </button>
    )
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          className="relative ml-2 h-8 w-8 rounded-full focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
          aria-label="User settings"
        >
          <Avatar className="h-8 w-8">
            <AvatarImage src="" alt={displayUser.name} />
            <AvatarFallback className="bg-primary text-xs font-semibold text-primary-foreground">
              {initials}
            </AvatarFallback>
          </Avatar>
        </button>
      </DropdownMenuTrigger>
      
      <DropdownMenuContent
        className="w-[500px] rounded-xl border-border p-0 shadow-lg"
        align="end"
        sideOffset={8}
        style={{ fontFamily: "var(--font-body)" }}
      >
        {/* Team Header */}
        <div className="p-4 flex items-center gap-3">
          <Hexagon className="fill-primary text-primary" size={24} />
          <span className="text-[15px] font-medium text-foreground">
            {displayUser.name}&apos;s Team
          </span>
        </div>
        
        <DropdownMenuSeparator className="m-0 bg-border" />

        {/* Two Columns Body */}
        <div className="flex">
          {/* Account Column */}
          <div className="flex-1 border-r border-border p-2">
            <DropdownMenuLabel className="mb-1 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Account
            </DropdownMenuLabel>
            <div className="flex flex-col gap-0.5">
              <UserMenuItem icon={User} label="My profile" />
              <UserMenuItem icon={Download} label="Import data" />
              <UserMenuItem icon={Code2} label="Developers" />
              <UserMenuItem icon={Rocket} label="Spaces" badge="Alpha" />
              <UserMenuItem icon={Trash2} label="Trash" />
              <UserMenuItem icon={Archive} label="Archive" />
              <UserMenuItem icon={Sparkles} label="AI Usage" />
              <UserMenuItem icon={Settings} label="Administration" />
              <UserMenuItem icon={Users} label="Teams" />
              <UserMenuItem icon={LogOut} label="Log out" onClick={() => logout()} danger />
            </div>
          </div>

          {/* Explore Column */}
          <div className="flex-1 p-2">
            <DropdownMenuLabel className="mb-1 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Explore
            </DropdownMenuLabel>
            <div className="flex flex-col gap-0.5 mb-3">
              <UserMenuItem icon={Puzzle} label="App marketplace" />
              <UserMenuItem icon={Smartphone} label="Mobile app" />
              <UserMenuItem icon={FlaskConical} label="monday.labs" />
              <UserMenuItem icon={Command} label="Shortcuts" />
            </div>
            
            <DropdownMenuSeparator className="my-2 bg-border" />
            
            <div className="flex flex-col gap-0.5">
              <UserMenuItem icon={UserPlus} label="Invite members" />
              <UserMenuItem icon={HelpCircle} label="Get help" />
              <UserMenuItem icon={SunMoon} label="Change theme" rightElement={<ChevronRight size={14} className="text-muted-foreground" />} />
            </div>

            <div className="px-2 mt-4">
              <button className="flex w-full items-center justify-center gap-2 rounded-md bg-primary py-1.5 text-[13px] font-medium text-primary-foreground transition-colors hover:bg-primary/90">
                <Gem size={14} />
                Upgrade
              </button>
            </div>
          </div>
        </div>

        <DropdownMenuSeparator className="m-0 bg-border" />

        {/* Footer: Working Status */}
        <div className="flex cursor-pointer items-center justify-between rounded-b-xl p-3 transition-colors hover:bg-muted">
          <div className="flex items-center gap-2">
            <DropdownMenuLabel className="p-0 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Working status
            </DropdownMenuLabel>
          </div>
          <div className="w-full mt-2 flex items-center justify-between">
            <div className="flex items-center gap-2 text-[13px] text-foreground">
              <BellRing size={14} className="text-muted-foreground" />
              <span>Do not disturb</span>
            </div>
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-1.5 text-[13px]">
                <div className="h-3.5 w-3.5 rounded-full border border-muted-foreground"></div>
                <span className="text-muted-foreground">On</span>
              </div>
              <div className="flex items-center gap-1.5 text-[13px] font-medium">
                <div className="h-3.5 w-3.5 rounded-full border-[4px] border-primary bg-card"></div>
                <span className="text-foreground">Off</span>
              </div>
              <div className="ml-2 flex items-center text-[12px] text-muted-foreground">
                More <ChevronRight size={12} />
              </div>
            </div>
          </div>
        </div>

      </DropdownMenuContent>
    </DropdownMenu>
  )
}
