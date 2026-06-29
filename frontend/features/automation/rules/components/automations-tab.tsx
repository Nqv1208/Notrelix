"use client"

import { Plus } from "lucide-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"

export function AutomationsTab() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold tracking-tight text-foreground">Automation Rules</h2>
        <p className="text-sm text-muted-foreground mt-1">Define trigger-action workflows to reduce repetitive tasks.</p>
      </div>

      <div className="space-y-3">
        <div className="flex items-center justify-between p-4 rounded-xl border border-border/60 bg-card">
          <div className="pr-4">
            <p className="font-semibold text-sm text-foreground">When card status goes to &quot;Done&quot;</p>
            <p className="text-xs text-muted-foreground mt-0.5">Archive the card and remove all member assignments automatically.</p>
          </div>
          <Switch defaultChecked className="data-[state=checked]:bg-primary" />
        </div>

        <div className="flex items-center justify-between p-4 rounded-xl border border-border/60 bg-card">
          <div className="pr-4">
            <p className="font-semibold text-sm text-foreground">When card has urgent priority</p>
            <p className="text-xs text-muted-foreground mt-0.5">Notify the workspace owner instantly via email.</p>
          </div>
          <Switch className="data-[state=checked]:bg-primary" />
        </div>

        <Button variant="outline" className="w-full border-dashed rounded-xl py-5 hover:bg-muted/40" onClick={() => toast.info("Rule creator coming soon.")}>
          <Plus className="size-4 mr-2" />
          Create custom automation rule
        </Button>
      </div>
    </div>
  )
}
