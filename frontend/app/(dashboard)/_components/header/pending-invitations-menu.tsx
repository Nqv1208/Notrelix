"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { UserPlus, Loader2, Check, X, Calendar, User, Briefcase } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { usePendingInvitations, useAcceptInvitation } from "@/features/workspace/hooks"
import { cn } from "@/lib/utils"

export function PendingInvitationsMenu() {
  const router = useRouter()
  const [open, setOpen] = useState(false)
  const { data: invitations, isLoading, refetch } = usePendingInvitations()
  const acceptMutation = useAcceptInvitation()
  const [acceptingToken, setAcceptingToken] = useState<string | null>(null)

  const hasInvitations = invitations && invitations.length > 0

  const handleAccept = (token: string) => {
    setAcceptingToken(token)
    acceptMutation.mutate(token, {
      onSuccess: (data) => {
        setOpen(false)
        setAcceptingToken(null)
        refetch() // Refresh the pending invitations list
        if (data && data.workspaceSlug) {
          router.push(`/${data.workspaceSlug}`)
        } else {
          router.push("/")
        }
      },
      onError: () => {
        setAcceptingToken(null)
      }
    })
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          className={cn(
            "relative rounded-lg p-2 text-muted-foreground transition-all hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
            hasInvitations && "text-primary animate-pulse"
          )}
          aria-label="Pending workspace invitations"
        >
          <UserPlus className="size-[18px]" />
          {hasInvitations && (
            <span className="absolute -right-0.5 -top-0.5 flex size-4 items-center justify-center rounded-full border border-card bg-emerald-500 text-[9px] font-bold text-white shadow-sm animate-bounce">
              {invitations.length}
            </span>
          )}
        </button>
      </PopoverTrigger>
      
      <PopoverContent align="end" className="w-80 p-0 border-border/60 bg-card/95 shadow-xl backdrop-blur-md rounded-2xl overflow-hidden z-[100]">
        <div className="flex items-center justify-between border-b border-border/40 px-4 py-3 bg-muted/30">
          <h4 className="text-sm font-semibold text-foreground flex items-center gap-2">
            <UserPlus className="size-4 text-primary" />
            Lời mời tham gia ({invitations?.length || 0})
          </h4>
          {isLoading && <Loader2 className="size-3.5 animate-spin text-muted-foreground" />}
        </div>

        <div className="max-h-80 overflow-y-auto divide-y divide-border/40">
          {isLoading && !invitations ? (
            <div className="flex flex-col items-center justify-center py-8 text-center text-xs text-muted-foreground gap-2">
              <Loader2 className="size-6 animate-spin text-primary" />
              <span>Đang tải lời mời...</span>
            </div>
          ) : !hasInvitations ? (
            <div className="flex flex-col items-center justify-center py-8 px-4 text-center text-xs text-muted-foreground gap-2">
              <div className="rounded-full bg-muted p-2 text-muted-foreground/60">
                <UserPlus className="size-5" />
              </div>
              <p className="font-medium text-foreground/80">Không có lời mời nào</p>
              <p className="text-[11px] leading-normal text-muted-foreground/80 max-w-[200px]">
                Khi người khác mời bạn vào Workspace của họ, lời mời sẽ hiển thị tại đây.
              </p>
            </div>
          ) : (
            invitations.map((invite: any) => {
              const isAccepting = acceptingToken === invite.token
              return (
                <div key={invite.id} className="p-4 hover:bg-muted/10 transition-colors space-y-3">
                  <div className="space-y-1.5">
                    <h5 className="text-sm font-bold text-foreground leading-snug">
                      Workspace: {invite.workspaceName}
                    </h5>
                    <div className="space-y-1 text-xs text-muted-foreground">
                      <div className="flex items-center gap-1.5">
                        <User className="size-3.5 text-primary/75" />
                        <span>Người mời: <strong className="text-foreground/90 font-medium">{invite.inviterName}</strong></span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Briefcase className="size-3.5 text-primary/75" />
                        <span>Vai trò: <strong className="text-foreground/90 font-medium capitalize">{invite.role}</strong></span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Calendar className="size-3.5 text-primary/75" />
                        <span>Hết hạn: {new Date(invite.expiresAt).toLocaleDateString("vi-VN")}</span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 pt-1">
                    <Button
                      size="sm"
                      onClick={() => handleAccept(invite.token)}
                      disabled={isAccepting || acceptMutation.isPending}
                      className="flex-1 h-8 rounded-lg text-xs font-semibold gap-1.5 bg-emerald-600 hover:bg-emerald-500 text-white shadow-sm"
                    >
                      {isAccepting ? (
                        <Loader2 className="size-3 animate-spin" />
                      ) : (
                        <Check className="size-3" />
                      )}
                      Chấp nhận
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => setOpen(false)}
                      disabled={isAccepting || acceptMutation.isPending}
                      className="h-8 w-8 p-0 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted/80"
                    >
                      <X className="size-3.5" />
                    </Button>
                  </div>
                </div>
              )
            })
          )}
        </div>
      </PopoverContent>
    </Popover>
  )
}
