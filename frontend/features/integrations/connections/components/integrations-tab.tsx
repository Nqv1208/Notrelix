"use client"

import { Calendar, Mail } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"

export function IntegrationsTab() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold tracking-tight text-foreground">Connected services</h2>
        <p className="text-sm text-muted-foreground mt-1">Integrate Notrelix with your favorite tools to automate tasks.</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col justify-between p-5 rounded-xl border border-border/60 bg-card hover:border-primary/20 transition-all">
          <div className="space-y-3">
            <div className="flex items-center gap-3">
              <span className="flex size-10 items-center justify-center rounded-xl bg-orange-100 dark:bg-orange-950/40 text-orange-600 dark:text-orange-400">
                <Calendar className="size-5" />
              </span>
              <div>
                <p className="font-semibold text-sm text-foreground">Google Calendar</p>
                <p className="text-[10px] text-muted-foreground uppercase tracking-wider font-semibold">Calendar Sync</p>
              </div>
            </div>
            <p className="text-xs text-muted-foreground leading-relaxed">Sync your cards due dates and schedules with Google Calendar in real-time.</p>
          </div>
          <Button variant="outline" className="rounded-lg mt-4 w-full sm:w-auto self-start">Connect</Button>
        </div>

        <div className="flex flex-col justify-between p-5 rounded-xl border border-border/60 bg-card hover:border-primary/20 transition-all">
          <div className="space-y-3">
            <div className="flex items-center gap-3">
              <span className="flex size-10 items-center justify-center rounded-xl bg-emerald-100 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400">
                <Mail className="size-5" />
              </span>
              <div>
                <p className="font-semibold text-sm text-foreground">Email Notifications</p>
                <p className="text-[10px] text-muted-foreground uppercase tracking-wider font-semibold">Digests & Alerts</p>
              </div>
            </div>
            <p className="text-xs text-muted-foreground leading-relaxed">Send daily digests of active tasks, changes, and assignments to workspace members.</p>
          </div>
          <div className="flex items-center justify-between mt-4">
            <span className="text-xs text-muted-foreground">Enabled</span>
            <Switch defaultChecked className="data-[state=checked]:bg-primary" />
          </div>
        </div>
      </div>
    </div>
  )
}
